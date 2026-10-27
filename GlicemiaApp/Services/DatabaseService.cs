using GlicemiaApp.Models;
using SQLite;

namespace GlicemiaApp.Services;

/// <summary>Banco SQLCipher (AES-256). A conexão é fechada ao bloquear o app.</summary>
public sealed class DatabaseService
{
    readonly SecurityService _security;
    readonly object _openLock = new();
    readonly SemaphoreSlim _schemaLock = new(1, 1);
    Task<SQLiteAsyncConnection>? _open;
    bool _schemaInitialized;

    public DatabaseService(SecurityService security, SessionService session)
    {
        _security = security;
        session.Locked += () => _ = CloseAsync();
    }

    public Task<SQLiteAsyncConnection> GetAsync()
    {
        lock (_openLock)
        {
            if (_open is { IsFaulted: true } or { IsCanceled: true }) _open = null; // não deixa uma falha antiga "grudar"
            return _open ??= OpenAsync();
        }
    }

    async Task<SQLiteAsyncConnection> OpenAsync()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "glicemia.db3"); // diretório privado do app
        var key = await _security.GetOrCreateDbKeyAsync();
        var db = new SQLiteAsyncConnection(new SQLiteConnectionString(path, storeDateTimeAsTicks: true, key: key));
        await _schemaLock.WaitAsync();
        try
        {
            if (!_schemaInitialized)
            {
                await db.CreateTableAsync<GlucoseReading>();
                await db.CreateTableAsync<AuditEntry>();
                _schemaInitialized = true;
            }
        }
        finally
        {
            _schemaLock.Release();
        }
        return db;
    }

    async Task CloseAsync()
    {
        Task<SQLiteAsyncConnection>? open;
        lock (_openLock)
        {
            open = _open;
            _open = null;
        }
        if (open is not null) await (await open).CloseAsync();
    }
}
