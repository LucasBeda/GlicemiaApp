using GlicemiaApp.Models;
using SQLite;

namespace GlicemiaApp.Services;

/// <summary>Banco SQLCipher (AES-256). A conexão é fechada ao bloquear o app.</summary>
public sealed class DatabaseService
{
    readonly SecurityService _security;
    Task<SQLiteAsyncConnection>? _open;

    public DatabaseService(SecurityService security, SessionService session)
    {
        _security = security;
        session.Locked += () => _ = CloseAsync();
    }

    public Task<SQLiteAsyncConnection> GetAsync()
    {
        if (_open is { IsFaulted: true }) _open = null; // não deixa uma falha antiga "grudar"
        return _open ??= OpenAsync();
    }

    async Task<SQLiteAsyncConnection> OpenAsync()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "glicemia.db3"); // diretório privado do app
        var key = await _security.GetOrCreateDbKeyAsync();
        var db = new SQLiteAsyncConnection(new SQLiteConnectionString(path, storeDateTimeAsTicks: true, key: key));
        await db.CreateTableAsync<GlucoseReading>();
        await db.CreateTableAsync<AuditEntry>();
        return db;
    }

    async Task CloseAsync()
    {
        var open = _open;
        _open = null;
        if (open is not null) await (await open).CloseAsync();
    }
}
