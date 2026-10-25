using System.Security.Cryptography;

namespace GlicemiaApp.Core;

public static class PinHasher
{
    const int Iterations = 210_000, SaltSize = 16, HashSize = 32; // PBKDF2-HMAC-SHA512 (recomendação OWASP)

    public static string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(Derive(pin, salt))}";
    }

    public static bool Verify(string pin, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            return CryptographicOperations.FixedTimeEquals(Derive(pin, salt), expected); // tempo constante
        }
        catch (FormatException) { return false; }
    }

    /// <summary>Retorna a mensagem de erro, ou null se o PIN novo é aceitável.</summary>
    public static string? ValidateNew(string pin, string confirm)
    {
        if (pin.Length != 6 || !pin.All(char.IsAsciiDigit)) return "O PIN deve ter exatamente 6 dígitos.";
        if (pin.Distinct().Count() == 1) return "Evite PIN com todos os dígitos iguais.";
        if (Enumerable.Range(1, 5).All(i => pin[i] - pin[i - 1] == 1)
            || Enumerable.Range(1, 5).All(i => pin[i - 1] - pin[i] == 1))
            return "Evite sequências como 123456.";
        return pin == confirm ? null : "A confirmação não confere com o PIN.";
    }

    static byte[] Derive(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA512, HashSize);
}
