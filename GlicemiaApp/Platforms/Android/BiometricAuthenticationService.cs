using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Biometrics;
using Android.OS;
using GlicemiaApp.Services;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.Versioning;

namespace GlicemiaApp.Platforms;

public sealed class BiometricAuthenticationService : IBiometricAuthenticationService
{
    public bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(28) ||
                Platform.CurrentActivity?.PackageManager is not { } packageManager)
                return false;

            return packageManager.HasSystemFeature(PackageManager.FeatureFingerprint) ||
                   OperatingSystem.IsAndroidVersionAtLeast(29) &&
                   packageManager.HasSystemFeature(PackageManager.FeatureFace);
        }
    }

    public Task<BiometricAuthenticationResult> AuthenticateAsync(string reason)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(28) || !IsAvailable)
            return Task.FromResult(BiometricAuthenticationResult.Unavailable);

        return MainThread.InvokeOnMainThreadAsync(() => AuthenticateOnMainThread(reason));
    }

    static Task<BiometricAuthenticationResult> AuthenticateOnMainThread(string reason)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(28))
            return Task.FromResult(BiometricAuthenticationResult.Unavailable);

        var activity = Platform.CurrentActivity;
        if (activity?.MainExecutor is not { } executor)
            return Task.FromResult(BiometricAuthenticationResult.Unavailable);

        var completion = new TaskCompletionSource<BiometricAuthenticationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new PromptCallback(completion);
        var prompt = new BiometricPrompt.Builder(activity)
            .SetTitle("Desbloquear Glicemia")
            .SetDescription(reason)
            .SetNegativeButton("Usar PIN", executor, new NegativeButtonListener())
            .Build();

        prompt.Authenticate(new CancellationSignal(), executor, callback);
        return completion.Task;
    }

    [SupportedOSPlatform("android28.0")]
    sealed class PromptCallback(TaskCompletionSource<BiometricAuthenticationResult> completion)
        : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result) =>
            completion.TrySetResult(BiometricAuthenticationResult.Success);

        public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString)
        {
            var result = errorCode switch
            {
                BiometricErrorCode.UserCanceled or BiometricErrorCode.Canceled =>
                    BiometricAuthenticationResult.Canceled,
                BiometricErrorCode.HwNotPresent or BiometricErrorCode.HwUnavailable or BiometricErrorCode.NoBiometrics =>
                    BiometricAuthenticationResult.Unavailable,
                _ => BiometricAuthenticationResult.Failed
            };
            completion.TrySetResult(result);
        }
    }

    sealed class NegativeButtonListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        public void OnClick(IDialogInterface? dialog, int which) { }
    }
}
