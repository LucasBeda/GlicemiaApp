# Segurança do app Glicemia

Referência: OWASP MASVS (categorias entre parênteses) e MASTG para os testes.

## 1. Modelo de ameaça

| Ativo | Ameaça | Quem |
|---|---|---|
| Valores de glicemia e observações | Leitura do arquivo do banco | Aparelho perdido/roubado, malware, `adb` |
| Acesso ao app | Adivinhar o PIN | Pessoa com o aparelho desbloqueado |
| Conteúdo da tela | Captura, gravação, miniatura | Outro app, shoulder surfing |
| Últimos registros no widget | Ver valores na tela inicial sem PIN | Quem usa o aparelho desbloqueado ou olha a tela |
| Integridade dos dados | Entrada maliciosa, SQL injection | Qualquer entrada digitada |

Fora do escopo desta versão: aparelho com root/jailbreak e atacante com Frida (veja seção 5).

## 2. Controles implementados

| Controle | Onde | Como testar |
|---|---|---|
| **Banco criptografado** AES-256 (SQLCipher), chave aleatória de 256 bits no Keystore/Keychain (STORAGE, CRYPTO) | `DatabaseService`, `SecurityService` | Extrair `glicemia.db3` de um aparelho com root: `sqlite3` deve falhar com "file is not a database"; `strings` não mostra nada legível |
| **Sem backup** (`allowBackup=false`, `dataExtractionRules`) (STORAGE) | `AndroidManifest.xml`, `data_extraction_rules.xml` | `adb backup -f x.ab <pacote>` gera arquivo vazio |
| **PIN com PBKDF2-HMAC-SHA512**, 210.000 iterações, salt de 16 bytes, comparação em tempo constante (AUTH, CRYPTO) | `Core/PinHasher.cs` | Testes unitários; conferir que o SecureStorage não guarda o PIN em claro |
| **Política de PIN** (6 dígitos, sem repetidos/sequências) | `PinHasher.ValidateNew` | Testes unitários |
| **Bloqueio progressivo**: 5 erros livres, depois 30 s, 60 s, 120 s... até 15 min; mensagem genérica (AUTH) | `SecurityService.VerifyPinAsync` | Errar o PIN 6 vezes e observar a espera; tentar durante a espera |
| **Auto-lock**: ao ir para segundo plano, após 2 min sem ação e pelo botão "Bloquear"; o banco é fechado e a lista some da memória da tela (AUTH) | `SessionService`, `App.xaml.cs`, `DatabaseService` | Deixar o app parado 2 min; minimizar e voltar |
| **Autorização em toda ação**: `EnsureUnlocked()` antes de gravar, listar ou excluir (AUTH) | `GlucoseRepository` | Chamar o repositório com a sessão bloqueada: lança `UnauthorizedAccessException` e o app volta ao PIN |
| **Validação por lista de permissão** (faixa 20–600, contexto fixo, observação sem caracteres de controle e com limite) (CODE) | `Core/InputValidator.cs` | Testes unitários, incluindo `0x40`, `1e2`, sinais e texto de SQL injection |
| **SQL parametrizado**: só API tipada do sqlite-net, sem concatenar texto (CODE) | `GlucoseRepository` | Revisão de código: não existe SQL montado com entrada do usuário |
| **Trilha de auditoria** sem dados clínicos: `unlock`, `unlock_failed`, `add_reading`, `view_history`, `delete_reading`, `pin_created` (PRIVACY) | `AuditService` | Conferir que a tabela `AuditEntry` só tem nome da ação e horário |
| **Erros genéricos** sem stack trace na tela (CODE) | `SecureViewModel.RunAsync` | Forçar falha (apagar o banco) e ver a mensagem |
| **Tela protegida**: `FLAG_SECURE` no Android; cobertura da tela no iOS ao perder o foco (PLATFORM) | `MainActivity.cs`, `AppDelegate.cs` | Tirar print (deve falhar/sair preto); abrir a lista de apps recentes |
| **Anti-tapjacking** (`FilterTouchesWhenObscured`) (PLATFORM) | `MainActivity.cs` | Sobrepor janela com um app de overlay e tocar |
| **Superfície mínima**: sem permissão INTERNET, sem tráfego em texto puro, sem deep links, sem providers; exportados apenas a Activity launcher e o receiver do widget (NETWORK, PLATFORM) | `AndroidManifest.xml`, `Info.plist` | `apktool d` e ler o manifesto; `adb shell dumpsys package <pacote>` |
| **Widget com exposição consciente**: mostra até 3 datas recentes, com os valores mais recentes de café da manhã, almoço e jantar e as observações, lidos por `WidgetFeed` sem PIN (veja a seção 5). O botão "+ Registrar" usa `PendingIntent` imutável; o intent pode ser enviado por qualquer app, mas só abre a tela **depois do PIN**, e o contexto recebido é conferido contra a lista de permissão (PLATFORM, AUTH, PRIVACY) | `GlucoseWidget.cs`, `WidgetFeed.cs`, `QuickAddRequest.cs`, `MainActivity.cs` | `adb shell am start -a com.exemplo.glicemia.QUICK_ADD -n <pacote>/crc64....MainActivity --es context "x"` com o app bloqueado: deve pedir PIN; com contexto inválido: usa o padrão |
| **Sem logs de dados**; teclado sem previsão de texto nos campos sensíveis (STORAGE, PRIVACY) | `MauiProgram.cs`, Views | `adb logcat` durante o uso; conferir que o teclado não sugere valores já digitados |

## 3. Segurança de cada ação

| Ação | Controles aplicados |
|---|---|
| Criar PIN | Política de PIN, hash PBKDF2, auditoria |
| Desbloquear | Bloqueio progressivo, comparação em tempo constante, mensagem genérica, auditoria (sucesso e falha) |
| Registrar glicemia | Sessão válida, validação por lista de permissão, SQL parametrizado, horário gerado pelo app (não digitado), auditoria |
| Adicionar ao histórico (modal com data) | Sessão válida, validação por lista de permissão, data sem futuro e limitada aos últimos 30 dias (`ValidateMeasuredAt`, mesmo limite do seletor de datas), SQL parametrizado, auditoria própria (`add_reading_backdated`), modal fechado ao bloquear o app |
| Registro rápido (widget) | Pedido fica pendente até o PIN, sessão válida, validação por lista de permissão (valor e contexto), data e hora geradas pelo app, SQL parametrizado, auditoria (`add_reading`), app volta para a tela inicial e bloqueia ao salvar |
| Ver registros no widget | **Sem PIN, por escolha de produto.** Exibe valores e observações das até 3 datas mais recentes; somente leitura, sem auditoria (o widget atualiza ao salvar/excluir e ao ser adicionado, e gravar auditoria a cada atualização encheria a tabela). Para remover a exposição, basta tirar o widget da tela inicial |
| Ver histórico | Sessão válida, janela de 30 dias e limite de 1000 linhas, auditoria, dados limpos da memória ao bloquear |
| Excluir registro | Sessão válida, confirmação explícita, auditoria |
| Bloquear | Fecha o banco, limpa a lista e volta ao PIN |

## 4. Roteiro de pentest sugerido

1. **Análise estática**: gerar o APK Release e rodar o MobSF; `apktool d app.apk` e revisar o manifesto (`allowBackup`, `debuggable`, componentes exportados, permissões).
2. **Armazenamento**: em aparelho com root, listar `/data/data/<pacote>`, abrir o banco e as preferências; conferir que nada sensível está em claro.
3. **Backup**: `adb backup` e extração com `abe.jar`.
4. **Componentes**: `drozer` / `adb shell am start` tentando abrir telas internas sem passar pelo PIN.
5. **Autenticação**: automatizar tentativas de PIN; conferir o bloqueio progressivo; mudar o relógio do aparelho.
6. **Tela**: screenshot, gravação de tela, lista de recentes, overlay.
7. **Dinâmica**: Frida/objection para tentar contornar o PIN (esperado: possível em aparelho com root, veja a seção 5).
8. **Memória**: dump do processo após bloquear e procurar valores/PIN.

## 5. Limitações conhecidas (e como evoluir)

- **A chave do banco não deriva do PIN.** O PIN protege a interface; o banco depende do Keystore/Keychain. Um atacante com root e Frida pode extrair a chave do processo. Evolução: derivar a chave do PIN (KEK) ou usar uma chave do Keystore ligada à biometria.
- **Sem biometria** nem detecção de root/jailbreak, ofuscação ou verificação de integridade do app (MASVS-RESILIENCE). Evolução: Play Integrity API / App Attest e biometria.
- **Contador de tentativas no SecureStorage** com relógio UTC do aparelho: quem alterar a hora pode encurtar a espera. Limpar os dados do app apaga também o banco, então não vira bypass de acesso.
- **O widget exibe valores e observações sem PIN.** Quem usar o aparelho desbloqueado, ou olhar a tela inicial, vê os dados das até 3 datas mais recentes; `FLAG_SECURE` não vale para a tela inicial. Além disso, a leitura do widget reabre o banco criptografado enquanto o app está bloqueado e ele fica aberto na memória até o próximo bloqueio. Para remover essa exposição, tire o widget da tela inicial.
- **Sem exportação nem compartilhamento** de dados: é uma escolha de segurança. Se for adicionar, cifrar o arquivo exportado e pedir PIN de novo.
- **Faixa Baixa/Na meta/Alta** (70 e 180 mg/dL) é apenas visual e genérica. Não é diagnóstico.
