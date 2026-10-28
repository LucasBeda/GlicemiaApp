namespace GlicemiaApp.Services;

public enum BiometricAuthenticationResult
{
    Success,
    Canceled,
    Unavailable,
    Failed
}

public interface IBiometricAuthenticationService
{
    bool IsAvailable { get; }
    Task<BiometricAuthenticationResult> AuthenticateAsync(string reason);
}
