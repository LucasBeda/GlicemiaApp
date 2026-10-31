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

    public async Task<string?> AddManyBackdatedAsync(
        IReadOnlyList<(string? ValueText, string? Context, string? Note, DateTime MeasuredAtLocal)> readings)
    {
        session.EnsureUnlocked();
        if (readings.Count == 0) return "Preencha ao menos uma marcação.";

        var nowUtc = DateTime.UtcNow;
        var rows = new List<GlucoseReading>(readings.Count);
        for (var index = 0; index < readings.Count; index++)
        {
            var reading = readings[index];
            var (input, error) = InputValidator.Validate(reading.ValueText, reading.Context, reading.Note);
            if (input is null) return $"Medição {index + 1}: {error}";

            var measuredUtc = reading.MeasuredAtLocal.ToUniversalTime();
            var dateError = InputValidator.ValidateMeasuredAt(measuredUtc, nowUtc);
            if (dateError is not null) return $"Medição {index + 1}: {dateError}";

            rows.Add(new GlucoseReading
            {
                MeasuredAtUtc = measuredUtc,
                ValueMgDl = input.ValueMgDl,
                Context = input.Context,
                Note = input.Note,
            });
        }

        var db = await database.GetAsync();
        await db.RunInTransactionAsync(connection =>
        {
            foreach (var row in rows) connection.Insert(row);
        });
        await audit.LogAsync("add_reading_backdated");
        Changed?.Invoke();
        return null;
    }

    public async Task<string?> UpdateAsync(int id, string? valueText, string? context, string? note, DateTime measuredAtLocal)
    {
        session.EnsureUnlocked();
        var (input, error) = InputValidator.Validate(valueText, context, note);
        if (input is null) return error;

        var measuredUtc = measuredAtLocal.ToUniversalTime();
        var dateError = InputValidator.ValidateMeasuredAt(measuredUtc, DateTime.UtcNow);
        if (dateError is not null) return dateError;

        var db = await database.GetAsync();
        var reading = await db.FindAsync<GlucoseReading>(id);
        if (reading is null) return "O registro não foi encontrado. Atualize o histórico e tente novamente.";

        reading.MeasuredAtUtc = measuredUtc;
        reading.ValueMgDl = input.ValueMgDl;
        reading.Context = input.Context;
        reading.Note = input.Note;
        await db.UpdateAsync(reading);
        await audit.LogAsync("edit_reading");
        Changed?.Invoke();
        return null;
    }

    public async Task<string?> UpdateManyAsync(
        IReadOnlyList<(int Id, string? ValueText, string? Context, DateTime MeasuredAtLocal)> readings,
        string? note)
    {
        session.EnsureUnlocked();
        if (readings.Count == 0) return "Não há medições para atualizar.";

        var nowUtc = DateTime.UtcNow;
        var updates = new List<(int Id, GlucoseReading Reading)>(readings.Count);
        var ids = new HashSet<int>();
        for (var index = 0; index < readings.Count; index++)
        {
            var item = readings[index];
            if (!ids.Add(item.Id)) return "Há medições duplicadas na edição. Feche e abra o histórico novamente.";

            var (input, error) = InputValidator.Validate(item.ValueText, item.Context, note);
            if (input is null) return $"Medição {index + 1}: {error}";

            var measuredUtc = item.MeasuredAtLocal.ToUniversalTime();
            var dateError = InputValidator.ValidateMeasuredAt(measuredUtc, nowUtc);
            if (dateError is not null) return $"Medição {index + 1}: {dateError}";

            updates.Add((item.Id, new GlucoseReading
            {
                MeasuredAtUtc = measuredUtc,
                ValueMgDl = input.ValueMgDl,
                Context = input.Context,
                Note = input.Note,
            }));
        }

        var db = await database.GetAsync();
        var existing = new List<GlucoseReading>(updates.Count);
        foreach (var update in updates)
        {
            var reading = await db.FindAsync<GlucoseReading>(update.Id);
            if (reading is null) return "Um ou mais registros não foram encontrados. Atualize o histórico e tente novamente.";
            existing.Add(reading);
        }

        await db.RunInTransactionAsync(connection =>
        {
            for (var index = 0; index < updates.Count; index++)
            {
                existing[index].MeasuredAtUtc = updates[index].Reading.MeasuredAtUtc;
                existing[index].ValueMgDl = updates[index].Reading.ValueMgDl;
                existing[index].Context = updates[index].Reading.Context;
                existing[index].Note = updates[index].Reading.Note;
                connection.Update(existing[index]);
            }
        });
        await audit.LogAsync("edit_reading");
        Changed?.Invoke();
        return null;
    }

    public async Task<string?> DeleteManyAsync(IReadOnlyCollection<int> ids)
    {
        session.EnsureUnlocked();
        if (ids.Count == 0) return "Não há medições para excluir.";

        var uniqueIds = ids.ToHashSet();
        if (uniqueIds.Count != ids.Count || uniqueIds.Any(id => id <= 0))
            return "A lista de medições é inválida. Atualize o histórico e tente novamente.";

        var db = await database.GetAsync();
        var readings = new List<GlucoseReading>(uniqueIds.Count);
        foreach (var id in uniqueIds)
        {
            var reading = await db.FindAsync<GlucoseReading>(id);
            if (reading is null)
                return "Um ou mais registros não foram encontrados. Atualize o histórico e tente novamente.";
            readings.Add(reading);
        }

        await db.RunInTransactionAsync(connection =>
        {
            foreach (var reading in readings)
                connection.Delete<GlucoseReading>(reading.Id);
        });
        await audit.LogAsync("delete_reading");
        Changed?.Invoke();
        return null;
    }

    public async Task<List<GlucoseReading>> ListAsync(int? days)
    {
        session.EnsureUnlocked();
        var db = await database.GetAsync();
        var query = db.Table<GlucoseReading>();
        if (days.HasValue)
        {
            var since = DateTime.Today.AddDays(1 - days.Value).ToUniversalTime();
            query = query.Where(r => r.MeasuredAtUtc >= since);
        }

        var orderedQuery = query.OrderByDescending(r => r.MeasuredAtUtc);
        var rows = days.HasValue
            ? await orderedQuery.Take(1000).ToListAsync()
            : await orderedQuery.ToListAsync();
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
