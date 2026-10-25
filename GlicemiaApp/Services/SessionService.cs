namespace GlicemiaApp.Services;

/// <summary>Estado de sessão: toda ação sensível passa por EnsureUnlocked().</summary>
public sealed class SessionService
{
    static readonly long IdleLimitMs = 2 * 60 * 1000;

    readonly Timer _idleTimer;
    long _lastActivity;

    public bool IsUnlocked { get; private set; }
    public event Action? Locked;

    public SessionService() =>
        _idleTimer = new Timer(_ =>
        {
            if (IsUnlocked && Environment.TickCount64 - _lastActivity > IdleLimitMs) Lock();
        }, null, 30_000, 30_000);

    public void Unlock() { IsUnlocked = true; Touch(); }

    // TickCount64 é monotônico: mudar o relógio do aparelho não estende a sessão.
    public void Touch() => _lastActivity = Environment.TickCount64;

    public void Lock()
    {
        if (!IsUnlocked) return;
        IsUnlocked = false;
        Locked?.Invoke();
    }

    public void EnsureUnlocked()
    {
        if (!IsUnlocked) throw new UnauthorizedAccessException();
        Touch();
    }
}
