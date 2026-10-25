using GlicemiaApp.Models;

namespace GlicemiaApp.Services;

/// <summary>
/// Leitura para o widget da tela inicial. É a ÚNICA leitura que não exige sessão desbloqueada
/// (o widget aparece sem PIN): os valores e observações retornados ficam visíveis na tela inicial.
/// </summary>
public sealed class WidgetFeed(DatabaseService database)
{
    public const int DayCount = 3;

    public async Task<List<GlucoseReading>> LatestAsync()
    {
        var db = await database.GetAsync();
        return await db.Table<GlucoseReading>()
            .OrderByDescending(r => r.MeasuredAtUtc)
            .Take(100)
            .ToListAsync();
    }
}
