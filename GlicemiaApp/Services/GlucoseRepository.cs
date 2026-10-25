using GlicemiaApp.Core;
using GlicemiaApp.Models;

namespace GlicemiaApp.Services;

/// <summary>
/// Toda ação de dados segue a mesma ordem: sessão válida -> validação -> consulta parametrizada -> auditoria.
/// As consultas usam a API tipada do sqlite-net (parâmetros "?"), nunca concatenação de texto.
/// </summary>
public sealed class GlucoseRepository(DatabaseService database, SessionService session, AuditService audit)
{
    /// <summary>Disparado depois de salvar ou excluir (usado para atualizar o widget).</summary>
    public static event Action? Changed;

    /// <param name="measuredAtLocal">Data/hora da medição no horário do aparelho. Null = agora.</param>
    /// <returns>Mensagem de erro de validação, ou null se salvou.</returns>
    public async Task<string?> AddAsync(string? valueText, string? context, string? note, DateTime? measuredAtLocal = null)
    {
        session.EnsureUnlocked();
        var (input, error) = InputValidator.Validate(valueText, context, note);
        if (input is null) return error;

        var measuredUtc = measuredAtLocal?.ToUniversalTime() ?? DateTime.UtcNow;
        var dateError = InputValidator.ValidateMeasuredAt(measuredUtc, DateTime.UtcNow);
        if (dateError is not null) return dateError;

        var db = await database.GetAsync();
        await db.InsertAsync(new GlucoseReading
        {
            MeasuredAtUtc = measuredUtc,
            ValueMgDl = input.ValueMgDl,
            Context = input.Context,
            Note = input.Note,
        });
        await audit.LogAsync(measuredAtLocal is null ? "add_reading" : "add_reading_backdated");
        Changed?.Invoke();
        return null;
    }

    public async Task<List<GlucoseReading>> ListAsync(int days)
    {
        session.EnsureUnlocked();
        var since = DateTime.Today.AddDays(-days).ToUniversalTime(); // meia-noite local, igual ao seletor de datas
        var db = await database.GetAsync();
        var rows = await db.Table<GlucoseReading>()
            .Where(r => r.MeasuredAtUtc >= since)
            .OrderByDescending(r => r.MeasuredAtUtc)
            .Take(1000)
            .ToListAsync();
        await audit.LogAsync("view_history");
        return rows;
    }

    public async Task DeleteAsync(int id)
    {
        session.EnsureUnlocked();
        var db = await database.GetAsync();
        await db.DeleteAsync<GlucoseReading>(id);
        await audit.LogAsync("delete_reading");
        Changed?.Invoke();
    }
}
