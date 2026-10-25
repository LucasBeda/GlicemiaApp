using GlicemiaApp.Models;

namespace GlicemiaApp.Services;

public sealed class AuditService(DatabaseService database)
{
    public async Task LogAsync(string action)
    {
        var db = await database.GetAsync();
        await db.InsertAsync(new AuditEntry { AtUtc = DateTime.UtcNow, Action = action });
        // shortcut: mantém só as últimas 500 linhas, sem tela para consultar; criar uma tela se a auditoria virar requisito de uso.
        await db.ExecuteAsync("DELETE FROM AuditEntry WHERE Id <= (SELECT MAX(Id) FROM AuditEntry) - 500");
    }
}
