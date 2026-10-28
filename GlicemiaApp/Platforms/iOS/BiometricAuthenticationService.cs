using Foundation;
using GlicemiaApp.Services;
using LocalAuthentication;

namespace GlicemiaApp.Platforms;

public sealed class BiometricAuthenticationService : IBiometricAuthenticationService
{
    const LAPolicy BiometricPolicy = LAPolicy.DeviceOwnerAuthenticationWithBiometrics;

    public bool IsAvailable
    {
        get
        {
            using var context = new LAContext();
            return context.CanEvaluatePolicy(BiometricPolicy, out _);
        }
    }

    public async Task<BiometricAuthenticationResult> AuthenticateAsync(string reason)
    {
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(BiometricPolicy, out _))
            return BiometricAuthenticationResult.Unavailable;

        var completion = new TaskCompletionSource<BiometricAuthenticationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        context.EvaluatePolicy(BiometricPolicy, reason, (success, _) =>
            completion.TrySetResult(success
                ? BiometricAuthenticationResult.Success
                : BiometricAuthenticationResult.Canceled));

        return await completion.Task;
    }
}
