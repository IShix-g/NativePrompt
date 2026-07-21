using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NativePrompt.Editor
{
    [InitializeOnLoad]
    internal sealed class EditorNativePromptStrategy : INativePromptStrategy
    {
        private readonly EditorNativePromptPresenter _presenter =
            new EditorNativePromptPresenter();
        private readonly HashSet<string> _toasts =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _toastDeadlines =
            new Dictionary<string, double>(StringComparer.Ordinal);
        private string _loadingRequestId;
        private LoadingOptions _loadingOptions;
        private double _loadingDeadline;

        static EditorNativePromptStrategy()
        {
            NativePromptStrategyRegistry.RegisterEditorFactory(
                () => new EditorNativePromptStrategy());
        }

        public void ShowAlert(string requestId, AlertOptions options)
        {
            _presenter.ShowAlert(
                requestId,
                options,
                (completedRequestId, result) =>
                    NativePromptCallbackReceiver.AlertCompleted(completedRequestId, result));
            NativePromptCallbackReceiver.AlertOpened(requestId);
        }

        public void DismissAlert(string requestId)
        {
            _presenter.DismissAlert(requestId);
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            _presenter.ShowBottomSheet(
                requestId,
                options,
                (completedRequestId, actionId) =>
                    NativePromptCallbackReceiver.BottomSheetActionSelected(
                        completedRequestId,
                        actionId),
                completedRequestId =>
                    NativePromptCallbackReceiver.BottomSheetCancelled(completedRequestId));
            NativePromptCallbackReceiver.BottomSheetOpened(requestId);
        }

        public void DismissBottomSheet(string requestId)
        {
            _presenter.DismissBottomSheet(requestId);
        }

        public void ShowToast(string requestId, ToastOptions options)
        {
            Debug.Log($"NativePrompt Toast: {options.Message}");
            _toasts.Add(requestId);
            _presenter.ShowToast(requestId, options, CompleteToastTap);
            NativePromptCallbackReceiver.ToastShown(requestId);
            if (!options.AutoDismiss)
            {
                return;
            }

            _toastDeadlines[requestId] = EditorApplication.timeSinceStartup + options.Duration;
            EditorApplication.update -= UpdateToasts;
            EditorApplication.update += UpdateToasts;
        }

        public void DismissToast(string requestId)
        {
            _toasts.Remove(requestId);
            _toastDeadlines.Remove(requestId);
            _presenter.DismissToast(requestId);
            UnsubscribeToastsWhenIdle();
        }

        public void ShowLoading(string requestId, LoadingOptions options)
        {
            bool showVisualsImmediately = options.ShowDelaySeconds <= 0f;
            _presenter.ShowLoading(requestId, options, showVisualsImmediately);
            _loadingRequestId = requestId;
            _loadingOptions = options;
            _loadingDeadline = EditorApplication.timeSinceStartup + options.ShowDelaySeconds;
            EditorApplication.update -= UpdateLoading;
            if (showVisualsImmediately)
            {
                LogLoading();
            }
            else
            {
                EditorApplication.update += UpdateLoading;
            }
        }

        public void DismissLoading(string requestId)
        {
            if (_loadingRequestId != requestId)
            {
                return;
            }

            ClearLoading();
        }

        public void RequestReview()
        {
            _presenter.ShowReview();
            Debug.Log(
                "NativePrompt Store Review: request accepted in the Unity Editor; " +
                "showing a simulated preview. Platform display and submission are not " +
                "guaranteed.");
        }

        public void Reset()
        {
            _toasts.Clear();
            _toastDeadlines.Clear();
            EditorApplication.update -= UpdateToasts;
            ClearLoading();
            _presenter.Reset();
        }

        private void CompleteToastTap(string requestId)
        {
            if (!_toasts.Remove(requestId))
            {
                return;
            }

            _toastDeadlines.Remove(requestId);
            _presenter.DismissToast(requestId);
            UnsubscribeToastsWhenIdle();
            NativePromptCallbackReceiver.ToastDismissed(
                requestId,
                ToastDismissReason.Tapped);
        }

        private void UpdateToasts()
        {
            double now = EditorApplication.timeSinceStartup;
            string[] requestIds = null;
            int count = 0;
            foreach (KeyValuePair<string, double> toast in _toastDeadlines)
            {
                if (toast.Value > now)
                {
                    continue;
                }

                if (requestIds == null)
                {
                    requestIds = new string[_toastDeadlines.Count];
                }
                requestIds[count++] = toast.Key;
            }

            for (int index = 0; index < count; index++)
            {
                string requestId = requestIds[index];
                if (_toastDeadlines.Remove(requestId) && _toasts.Remove(requestId))
                {
                    _presenter.DismissToast(requestId);
                    NativePromptCallbackReceiver.ToastDismissed(
                        requestId,
                        ToastDismissReason.TimedOut);
                }
            }

            UnsubscribeToastsWhenIdle();
        }

        private void UnsubscribeToastsWhenIdle()
        {
            if (_toastDeadlines.Count == 0)
            {
                EditorApplication.update -= UpdateToasts;
            }
        }

        private void UpdateLoading()
        {
            if (_loadingOptions == null ||
                EditorApplication.timeSinceStartup < _loadingDeadline)
            {
                return;
            }

            EditorApplication.update -= UpdateLoading;
            _presenter.ShowLoadingVisuals(_loadingRequestId);
            LogLoading();
        }

        private void LogLoading()
        {
            if (_loadingOptions == null)
            {
                return;
            }

            Debug.Log(
                $"NativePrompt Loading: position={_loadingOptions.Position}, " +
                $"size={_loadingOptions.Size}, spinnerColor={_loadingOptions.SpinnerColor}, " +
                $"message={_loadingOptions.Message ?? "<none>"}, " +
                $"messageColor={_loadingOptions.MessageColor}, " +
                $"messageFontSize={_loadingOptions.MessageFontSize}, " +
                $"background={_loadingOptions.ShowsBackground}, " +
                $"blocksInteraction={_loadingOptions.BlocksInteraction}");
        }

        private void ClearLoading()
        {
            EditorApplication.update -= UpdateLoading;
            if (_loadingRequestId != null)
            {
                _presenter.DismissLoading(_loadingRequestId);
            }
            _loadingRequestId = null;
            _loadingOptions = null;
            _loadingDeadline = 0d;
        }
    }
}
