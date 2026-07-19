using System;
using System.Threading;
using UnityEngine;

namespace NativePrompt
{
    internal static class NativePromptRuntime
    {
        private static readonly object Gate = new object();
        private static NativePromptCoordinator _coordinator;

        internal static AlertHandle ShowAlert(AlertOptions options, Action<AlertResult> onCompleted)
        {
            return GetCoordinator().ShowAlert(options, onCompleted);
        }

        internal static Awaitable<AlertResult> ShowAlertAsync(
            AlertOptions options,
            CancellationToken cancellationToken)
        {
            return GetCoordinator().ShowAlertAsync(options, cancellationToken);
        }

        internal static BottomSheetHandle ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted)
        {
            return GetCoordinator().ShowBottomSheet(options, onCompleted);
        }

        internal static Awaitable<BottomSheetResult> ShowBottomSheetAsync(
            BottomSheetOptions options,
            CancellationToken cancellationToken)
        {
            return GetCoordinator().ShowBottomSheetAsync(options, cancellationToken);
        }

        internal static ToastHandle ShowToast(
            ToastOptions options,
            Action<ToastDismissReason> onDismissed)
        {
            return GetCoordinator().ShowToast(options, onDismissed);
        }

        internal static Awaitable<ToastDismissReason> ShowToastAsync(
            ToastOptions options,
            CancellationToken cancellationToken)
        {
            return GetCoordinator().ShowToastAsync(options, cancellationToken);
        }

        internal static LoadingHandle ShowLoading(LoadingOptions options)
        {
            return GetCoordinator().ShowLoading(options);
        }

        internal static void ReceiveAlert(string requestId, AlertResult result)
        {
            GetCurrentCoordinator()?.ReceiveAlert(requestId, result);
        }

        internal static void ReceiveAlertOpened(string requestId)
        {
            GetCurrentCoordinator()?.ReceiveAlertOpened(requestId);
        }

        internal static void ReceiveBottomSheet(string requestId, BottomSheetResult result)
        {
            GetCurrentCoordinator()?.ReceiveBottomSheet(requestId, result);
        }

        internal static void ReceiveBottomSheetOpened(string requestId)
        {
            GetCurrentCoordinator()?.ReceiveBottomSheetOpened(requestId);
        }

        internal static void ReceiveToast(string requestId, ToastDismissReason reason)
        {
            GetCurrentCoordinator()?.ReceiveToast(requestId, reason);
        }

        internal static void ReceiveToastShown(string requestId)
        {
            GetCurrentCoordinator()?.ReceiveToastShown(requestId);
        }

        internal static void Reset()
        {
            GetCurrentCoordinator()?.Reset();
        }

        internal static void SetForTesting(
            INativePromptStrategy strategy,
            IMainThreadDispatcher dispatcher)
        {
            var replacement = new NativePromptCoordinator(strategy, dispatcher);
            NativePromptCoordinator previous;
            lock (Gate)
            {
                previous = _coordinator;
                _coordinator = replacement;
            }

            previous?.Reset();
        }

        internal static void RestoreDefaultForTesting()
        {
            NativePromptCoordinator previous;
            lock (Gate)
            {
                previous = _coordinator;
                _coordinator = null;
            }

            previous?.Reset();
        }

        internal static int PendingCallbackCountForTesting =>
            GetCurrentCoordinator()?.PendingCallbackCount ?? 0;

        internal static int ActiveLoadingCountForTesting =>
            GetCurrentCoordinator()?.ActiveLoadingCount ?? 0;

        internal static bool IsLoading =>
            (GetCurrentCoordinator()?.ActiveLoadingCount ?? 0) > 0;

        private static NativePromptCoordinator GetCoordinator()
        {
            lock (Gate)
            {
                if (_coordinator == null)
                {
                    _coordinator = new NativePromptCoordinator(
                        NativePromptStrategyRegistry.CreateForCurrentPlatform(),
                        new UnityMainThreadDispatcher());
                }

                return _coordinator;
            }
        }

        private static NativePromptCoordinator GetCurrentCoordinator()
        {
            lock (Gate)
            {
                return _coordinator;
            }
        }
    }

    internal static class NativePromptRuntimeLifecycle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            NativePromptRuntime.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateLifecycleHost()
        {
            var host = new GameObject("NativePrompt Runtime");
            host.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<NativePromptRuntimeHost>();
        }
    }

    internal sealed class NativePromptRuntimeHost : MonoBehaviour
    {
        private void OnDestroy()
        {
            NativePromptRuntime.Reset();
        }
    }
}
