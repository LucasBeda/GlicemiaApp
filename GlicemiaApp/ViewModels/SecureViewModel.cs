using CommunityToolkit.Mvvm.ComponentModel;

namespace GlicemiaApp.ViewModels;

/// <summary>Base comum: mensagem na tela e tratamento de erro padronizado, sem vazar detalhes técnicos.</summary>
public abstract partial class SecureViewModel : ObservableObject
{
    [ObservableProperty] private string message = "";
    [ObservableProperty] private bool isError;

    protected void Show(string text, bool error = false)
    {
        Message = text;
        IsError = error;
    }

    protected async Task RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (UnauthorizedAccessException) { await Shell.Current.GoToAsync("//lock"); } // sessão expirada
        catch (Exception) { Show("Não foi possível concluir a operação. Tente novamente.", true); }
    }
}
