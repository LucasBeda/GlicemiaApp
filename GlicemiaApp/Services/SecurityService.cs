using GlicemiaApp.Core;
using System.Security.Cryptography;

namespace GlicemiaApp.Services;

/// <summary>Segredos no Keystore (Android) / Keychain (iOS) via SecureStorage.</summary>
public sealed class SecurityService
{
    const string DbKeyName = "db_key", PinName = "pin_hash", FailsName = "pin_fails", LockUntilName = "pin_lock_until";
    const int FreeAttempts = 5, MaxLockSeconds = 900;

    static Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    static Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    public async Task<string> GetOrCreateDbKeyAsync()
    {
        var key = await GetAsync(DbKeyName);
        if (key is null)
        {
            key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // 256 bits aleatórios
            await SetAsync(DbKeyName, key);
        }
        return key;
    }

    public async Task<bool> HasPinAsync() => await GetAsync(PinName) is not null;

    public async Task SetPinAsync(string pin) =>
        await SetAsync(PinName, await Task.Run(() => PinHasher.Hash(pin)));

    /// <summary>Wait &gt; 0 significa bloqueio temporário por excesso de tentativas.</summary>
    public async Task<(bool Ok, TimeSpan Wait)> VerifyPinAsync(string pin)
    {
        var now = DateTime.UtcNow.Ticks;
        long.TryParse(await GetAsync(LockUntilName), out var until);
        if (until > now) return (false, TimeSpan.FromTicks(until - now)); // nem verifica durante o bloqueio

        var stored = await GetAsync(PinName);
        if (stored is not null && await Task.Run(() => PinHasher.Verify(pin, stored)))
        {
            SecureStorage.Default.Remove(FailsName);
            SecureStorage.Default.Remove(LockUntilName);
            return (true, TimeSpan.Zero);
        }

        int.TryParse(await GetAsync(FailsName), out var fails);
        fails++;
        await SetAsync(FailsName, fails.ToString());

        // 5 erros livres; depois 30 s, 60 s, 120 s... até 15 min.
        var wait = fails < FreeAttempts
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Math.Min(MaxLockSeconds, 30 * Math.Pow(2, fails - FreeAttempts)));
        if (wait > TimeSpan.Zero) await SetAsync(LockUntilName, (DateTime.UtcNow + wait).Ticks.ToString());
        return (false, wait);
    }
}
