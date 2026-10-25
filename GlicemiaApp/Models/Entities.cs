using SQLite;

namespace GlicemiaApp.Models;

public class GlucoseReading
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime MeasuredAtUtc { get; set; }
    public int ValueMgDl { get; set; }
    public string Context { get; set; } = "";
    public string Note { get; set; } = "";
}

/// <summary>Trilha de auditoria. Nunca guarda valores clínicos, só o nome da ação.</summary>
public class AuditEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public DateTime AtUtc { get; set; }
    public string Action { get; set; } = "";
}
