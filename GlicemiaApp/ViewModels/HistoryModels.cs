using GlicemiaApp.Models;

namespace GlicemiaApp.ViewModels;

public sealed class ReadingRow(GlucoseReading reading)
{
    // Faixa de referência genérica, apenas visual. Não é orientação médica.
    const int Low = 70, High = 180;

    public int Id => reading.Id;
    public string Time => reading.MeasuredAtUtc.ToLocalTime().ToString("HH:mm");
    public int ValueMgDl => reading.ValueMgDl;
    public string Context => reading.Context;
    public string Note => reading.Note;
    public bool HasNote => reading.Note.Length > 0;
    public string Status => reading.ValueMgDl < Low ? "Baixa" : reading.ValueMgDl > High ? "Alta" : "Na meta";
}

/// <summary>Um dia do histórico: a data e os registros daquele dia.</summary>
public sealed class DayGroup : List<ReadingRow>
{
    public DayGroup(DateTime day, IEnumerable<ReadingRow> rows) : base(rows) => Day = day;

    public DateTime Day { get; }
    public string Title => Day.ToString("D");
    public string Summary => $"{Count} registro(s) · média {(int)Math.Round(this.Average(r => r.ValueMgDl))} mg/dL";
}
