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
        var source = readings.ToArray();
        var orderedReadings = source.OrderBy(reading => reading.MeasuredAtUtc).ToArray();
        var readingRows = new ReadingRow[orderedReadings.Length];
        var breakfastReadings = new List<ReadingRow>();
        var lunchReadings = new List<ReadingRow>();
        var dinnerReadings = new List<ReadingRow>();
        var otherReadings = new List<ReadingRow>();

        for (var index = 0; index < orderedReadings.Length; index++)
        {
            var reading = orderedReadings[index];
            var row = readingRows[index] = new ReadingRow(reading);
            if (reading.Context == InputValidator.Contexts[0]) breakfastReadings.Add(row);
            else if (reading.Context == InputValidator.Contexts[1]) lunchReadings.Add(row);
            else if (reading.Context == InputValidator.Contexts[2]) dinnerReadings.Add(row);
            else if (reading.Context == InputValidator.Contexts[3]) otherReadings.Add(row);
        }

        Readings = readingRows;
        LatestReading = Readings[^1];
        Breakfast = breakfastReadings.Count > 0 ? breakfastReadings[^1] : null;
        Lunch = lunchReadings.Count > 0 ? lunchReadings[^1] : null;
        Dinner = dinnerReadings.Count > 0 ? dinnerReadings[^1] : null;
        OtherReading = otherReadings.Count > 0 ? otherReadings[^1] : null;
        BreakfastValue = FormatValues(breakfastReadings);
        LunchValue = FormatValues(lunchReadings);
        DinnerValue = FormatValues(dinnerReadings);
        OtherReadings = otherReadings.Count > 0 ? string.Join(" · ", otherReadings.Select(reading => $"Outra Marcação: {reading.ValueMgDl} ás {reading.Time}")) : "";

        var readingsWithNotes = source
            .Where(reading => !string.IsNullOrWhiteSpace(reading.Note))
            .OrderByDescending(reading => reading.MeasuredAtUtc)
            .Select(reading => reading.Note.Trim())
            .Distinct()
            .ToArray();
        Note = string.Join(" · ", readingsWithNotes.Select(note => $"Obs: {note}"));
        CombinedNote = string.Join(" · ", readingsWithNotes);
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

    static string FormatValues(IEnumerable<ReadingRow> readings)
    {
        var values = readings.Select(reading => reading.ValueMgDl.ToString()).ToArray();
        return values.Length == 0 ? "—" : string.Join(" | ", values);
    }
}
