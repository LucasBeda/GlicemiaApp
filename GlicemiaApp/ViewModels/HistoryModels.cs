using GlicemiaApp.Models;
using GlicemiaApp.Core;

namespace GlicemiaApp.ViewModels;

public sealed class ReadingRow(GlucoseReading reading)
{
    // Faixa de referência genérica, apenas visual. Não é orientação médica.
    const int Low = 80, High = 180;

    public int Id => reading.Id;
    public DateTime MeasuredAtLocal => reading.MeasuredAtUtc.ToLocalTime();
    public string Time => reading.MeasuredAtUtc.ToLocalTime().ToString("HH:mm");
    public int ValueMgDl => reading.ValueMgDl;
    public string Context => reading.Context;
    public string Note => reading.Note;
    public bool HasNote => reading.Note.Length > 0;
    public string Status => reading.ValueMgDl < Low ? "Baixa" : reading.ValueMgDl > High ? "Alta" : "Na meta";
}

/// <summary>Uma linha do histórico, com a medição mais recente de cada refeição naquele dia.</summary>
public sealed class HistoryDay
{
    public HistoryDay(DateTime day, IEnumerable<GlucoseReading> readings)
    {
        Day = day;
        var rows = readings.ToArray();

        var breakfastReadings = ForContext(rows, InputValidator.Contexts[0]);
        var lunchReadings = ForContext(rows, InputValidator.Contexts[1]);
        var dinnerReadings = ForContext(rows, InputValidator.Contexts[2]);
        var otherReadings = ForContext(rows, InputValidator.Contexts[3]);

        Readings = rows
            .OrderBy(reading => reading.MeasuredAtUtc)
            .Select(reading => new ReadingRow(reading))
            .ToArray();
        LatestReading = Readings[^1];
        Breakfast = breakfastReadings.LastOrDefault();
        Lunch = lunchReadings.LastOrDefault();
        Dinner = dinnerReadings.LastOrDefault();
        OtherReading = otherReadings.LastOrDefault();
        BreakfastValue = FormatValues(breakfastReadings);
        LunchValue = FormatValues(lunchReadings);
        DinnerValue = FormatValues(dinnerReadings);
        OtherReadings = otherReadings.Count > 0 ? string.Join(" · ", otherReadings.Select(reading => $"Outra Marcação: {reading.ValueMgDl} ás {reading.Time}")) : "";

        Note = string.Join(" · ", rows
            .Where(reading => !string.IsNullOrWhiteSpace(reading.Note))
            .OrderByDescending(reading => reading.MeasuredAtUtc)
            .Select(reading => $"Obs: {reading.Note.Trim()}")
            .Distinct());
        CombinedNote = string.Join(" · ", rows
            .Where(reading => !string.IsNullOrWhiteSpace(reading.Note))
            .OrderByDescending(reading => reading.MeasuredAtUtc)
            .Select(reading => reading.Note.Trim())
            .Distinct());
    }

    public DateTime Day { get; }
    public string Date => Day.ToString("dd/MM/yy");
    public IReadOnlyList<ReadingRow> Readings { get; }
    public ReadingRow LatestReading { get; }
    public ReadingRow? Breakfast { get; }
    public ReadingRow? Lunch { get; }
    public ReadingRow? Dinner { get; }
    public ReadingRow? OtherReading { get; }
    public string BreakfastValue { get; }
    public string LunchValue { get; }
    public string DinnerValue { get; }
    public string OtherReadings { get; }
    public string BreakfastStatus => Breakfast?.Status ?? "";
    public string LunchStatus => Lunch?.Status ?? "";
    public string DinnerStatus => Dinner?.Status ?? "";
    public bool HasBreakfast => Breakfast is not null;
    public bool HasLunch => Lunch is not null;
    public bool HasDinner => Dinner is not null;
    public bool HasOtherReadings => OtherReadings.Length > 0;
    public string Note { get; }
    public string CombinedNote { get; }
    public bool HasNote => Note.Length > 0;

    static List<ReadingRow> ForContext(IEnumerable<GlucoseReading> readings, string context) =>
        readings
            .Where(item => item.Context == context)
            .OrderBy(item => item.MeasuredAtUtc)
            .Select(item => new ReadingRow(item))
            .ToList();

    static string FormatValues(IEnumerable<ReadingRow> readings)
    {
        var values = readings.Select(reading => reading.ValueMgDl.ToString()).ToArray();
        return values.Length == 0 ? "—" : string.Join(" | ", values);
    }
}
