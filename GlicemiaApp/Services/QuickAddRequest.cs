using GlicemiaApp.Views;

namespace GlicemiaApp.Services;

/// <summary>
/// Pedido de "registro rápido" (vindo do widget). Fica pendente até o app estar desbloqueado:
/// quem abre a tela é sempre o PIN primeiro, nunca o intent.
/// </summary>
public static class QuickAddRequest
{
    static bool _pending;
    static string? _context;

    public static event Action? Requested;

    public static void Request(string? context)
    {
        _pending = true;
        _context = context;
        Requested?.Invoke();
    }

    /// <summary>Abre o registro rápido se houver pedido pendente. Chamar só com a sessão desbloqueada.</summary>
    public static async Task OpenIfPendingAsync()
    {
        if (!_pending) return;
        _pending = false;

        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services?.GetService(typeof(QuickAddPage)) is not QuickAddPage page) return;

        page.Preset(_context); // o contexto vindo do intent é conferido contra a lista de permissão
        await Shell.Current.Navigation.PushModalAsync(page);
    }
}
