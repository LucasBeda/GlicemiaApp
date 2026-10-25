using System.Globalization;

namespace GlicemiaApp.Core;

public sealed record ReadingInput(int ValueMgDl, string Context, string Note);

/// <summary>Única porta de entrada para dados digitados: lista de permissão, nunca lista de bloqueio.</summary>
public static class InputValidator
{
    public const int MinMgDl = 20, MaxMgDl = 600, MaxNoteLength = 140;

    /// <summary>Quantos dias para trás é possível registrar. É também a janela exibida no histórico.</summary>
    public const int MaxBackdateDays = 30;

    public static readonly string[] Contexts =
        ["Café da manhã", "Almoço", "Jantar", "Outro"];

    public static (ReadingInput? Input, string? Error) Validate(string? valueText, string? context, string? note)
    {
        // NumberStyles.None: só dígitos (rejeita sinal, espaço, hexa, notação científica).
        if (!int.TryParse(valueText?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value is < MinMgDl or > MaxMgDl)
            return (null, $"Informe um valor inteiro entre {MinMgDl} e {MaxMgDl} mg/dL.");

        if (context is null || Array.IndexOf(Contexts, context) < 0)
            return (null, "Selecione um contexto válido.");

        var clean = new string((note ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (clean.Length > MaxNoteLength)
            return (null, $"A observação deve ter no máximo {MaxNoteLength} caracteres.");

        return (new ReadingInput(value, context, clean), null);
    }

    /// <summary>Data/hora da medição (em UTC): sem futuro e dentro da janela do histórico.</summary>
    public static string? ValidateMeasuredAt(DateTime measuredUtc, DateTime nowUtc)
    {
        if (measuredUtc > nowUtc.AddMinutes(5)) return "A data e a hora não podem estar no futuro.";

        // Compara por dia local: é o mesmo limite do seletor de datas e do filtro do histórico.
        if (measuredUtc.ToLocalTime().Date < nowUtc.ToLocalTime().Date.AddDays(-MaxBackdateDays))
            return $"Só é possível registrar medições dos últimos {MaxBackdateDays} dias.";

        return null;
    }
}
