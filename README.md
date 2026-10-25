# Glicemia (.NET MAUI 10)

App para registrar a glicemia na hora da medição e ver o histórico agrupado por dia.

- **Registrar**: valor em mg/dL, contexto (jejum, antes/depois da refeição...) e observação. A data e a hora são as do momento do registro.
- **Histórico**: grid com um bloco por dia (data, quantidade e média) e uma linha por registro (hora, mg/dL, contexto, faixa). Toque em uma linha para excluir.
- **Adicionar ao histórico**: botão "+ Adicionar" no histórico abre um modal com os mesmos campos de Registrar, mais data e hora (até 30 dias para trás, sem futuro).
- **Widget Android**: na tela inicial, segure em um espaço vazio > Widgets > Glicemia. O widget mostra os 5 últimos registros (data e hora, mg/dL e contexto) e atualiza ao salvar ou excluir. O botão "+ Registrar" abre o registro rápido (só glicemia e contexto) depois do PIN. Atenção: os valores do widget aparecem na tela inicial sem PIN; para esconder, remova o widget.
- **Segurança**: PIN, banco criptografado, bloqueio automático, tela protegida. Detalhes e roteiro de pentest em [SECURITY.md](SECURITY.md).
- **Design**: tokens de cor em `Resources/Styles/Colors.xaml`, estilos reutilizáveis em `Styles.xaml` (classes `Card`, `Field`, `Chip`, `Title`, `Muted`...), tema claro e escuro automáticos.

## Estrutura

```
GlicemiaApp/
  Core/          lógica pura e testável: InputValidator, PinHasher
  Models/        GlucoseReading, AuditEntry
  Services/      SessionService, SecurityService, DatabaseService, AuditService, GlucoseRepository
  ViewModels/    MVVM (CommunityToolkit.Mvvm)
  Views/         LockPage, AddReadingPage, AddPastReadingPage (modal), QuickAddPage (widget), HistoryPage
  Platforms/     Android (manifesto, FLAG_SECURE, GlucoseWidget + Resources/layout|drawable|xml) e iOS (Info.plist, Entitlements)
GlicemiaApp.Tests/   testes xUnit da camada Core
```

## Como rodar

Requisitos: .NET 10 SDK e o workload MAUI (`dotnet workload install maui`). Para iOS é preciso um Mac. No Visual Studio, abra `GlicemiaApp/GlicemiaApp.csproj`.

```bash
cd GlicemiaApp
dotnet build -f net10.0-android
dotnet build -t:Run -f net10.0-android     # emulador ou aparelho conectado

cd ../GlicemiaApp.Tests
dotnet test
```

Antes de publicar: troque `ApplicationId` em `GlicemiaApp.csproj` e atualize as versões dos pacotes NuGet para as mais recentes.

Para testar a segurança em Release (o `FLAG_SECURE` e o manifesto valem também em Debug, mas o pentest deve ser feito no build Release):

```bash
dotnet publish -f net10.0-android -c Release
```
