using GlicemiaApp.Models;

namespace GlicemiaApp.Services;

/// <summary>
/// Leitura para o widget da tela inicial. É a ÚNICA leitura que não exige sessão desbloqueada
/// (o widget aparece sem PIN): por isso devolve só os últimos registros e nunca a observação.
/// </summary>
public sealed class WidgetFeed(DatabaseService database)
{
    public const int Count = 5;

    public async Task<List<GlucoseReading>> LatestAsync()
    {
        var db = await database.GetAsync();
        return await db.Table<GlucoseReading>()
            .OrderByDescending(r => r.MeasuredAtUtc)
            .Take(Count)
            .ToListAsync();
    }
}
