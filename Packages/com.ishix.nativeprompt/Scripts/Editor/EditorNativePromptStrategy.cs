using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NativePrompt.Editor
{
    [InitializeOnLoad]
    internal sealed class EditorNativePromptStrategy : INativePromptStrategy
    {
        private readonly Dictionary<string, EditorPromptWindow> _alerts =
            new Dictionary<string, EditorPromptWindow>(StringComparer.Ordinal);
        private readonly Dictionary<string, EditorPromptWindow> _bottomSheets =
            new Dictionary<string, EditorPromptWindow>(StringComparer.Ordinal);
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
            EditorPromptWindow window = EditorPromptWindow.CreateAlert(
                requestId,
                options,
                CompleteAlert,
                AlertWindowClosed);
            _alerts.Add(requestId, window);
            window.ShowUtility();
            NativePromptCallbackReceiver.AlertOpened(requestId);
        }

        public void DismissAlert(string requestId)
        {
            if (_alerts.TryGetValue(requestId, out EditorPromptWindow window))
            {
                _alerts.Remove(requestId);
                window.CloseWithoutCallback();
            }
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            EditorPromptWindow window = EditorPromptWindow.CreateBottomSheet(
                requestId,
                options,
                CompleteBottomSheetAction,
                CompleteBottomSheetCancellation,
                BottomSheetWindowClosed);
            _bottomSheets.Add(requestId, window);
            window.ShowUtility();
            NativePromptCallbackReceiver.BottomSheetOpened(requestId);
        }

        public void DismissBottomSheet(string requestId)
        {
            if (_bottomSheets.TryGetValue(requestId, out EditorPromptWindow window))
            {
                _bottomSheets.Remove(requestId);
                window.CloseWithoutCallback();
            }
        }

        public void ShowToast(string requestId, ToastOptions options)
        {
            Debug.Log($"NativePrompt Toast: {options.Message}");
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
            _toastDeadlines.Remove(requestId);
            UnsubscribeWhenIdle();
        }

        public void ShowLoading(string requestId, LoadingOptions options)
        {
            _loadingRequestId = requestId;
            _loadingOptions = options;
            _loadingDeadline = EditorApplication.timeSinceStartup + options.ShowDelaySeconds;
            EditorApplication.update -= UpdateLoading;
            if (options.ShowDelaySeconds <= 0f)
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

        public void Reset()
        {
            CloseAll(_alerts);
            CloseAll(_bottomSheets);
            _toastDeadlines.Clear();
            EditorApplication.update -= UpdateToasts;
            ClearLoading();
        }

        private void CompleteAlert(string requestId, AlertResult result)
        {
            if (_alerts.TryGetValue(requestId, out EditorPromptWindow window))
            {
                _alerts.Remove(requestId);
                window.CloseWithoutCallback();
                NativePromptCallbackReceiver.AlertCompleted(requestId, result);
            }
        }

        private void AlertWindowClosed(string requestId)
        {
            if (_alerts.Remove(requestId))
            {
                NativePromptCallbackReceiver.AlertCompleted(requestId, AlertResult.Closed);
            }
        }

        private void CompleteBottomSheetAction(string requestId, string actionId)
        {
            if (_bottomSheets.TryGetValue(requestId, out EditorPromptWindow window))
            {
                _bottomSheets.Remove(requestId);
                window.CloseWithoutCallback();
                NativePromptCallbackReceiver.BottomSheetActionSelected(requestId, actionId);
            }
        }

        private void CompleteBottomSheetCancellation(string requestId)
        {
            if (_bottomSheets.TryGetValue(requestId, out EditorPromptWindow window))
            {
                _bottomSheets.Remove(requestId);
                window.CloseWithoutCallback();
                NativePromptCallbackReceiver.BottomSheetCancelled(requestId);
            }
        }

        private void BottomSheetWindowClosed(string requestId)
        {
            if (_bottomSheets.Remove(requestId))
            {
                NativePromptCallbackReceiver.BottomSheetCancelled(requestId);
            }
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
                if (_toastDeadlines.Remove(requestId))
                {
                    NativePromptCallbackReceiver.ToastDismissed(
                        requestId,
                        ToastDismissReason.TimedOut);
                }
            }

            UnsubscribeWhenIdle();
        }

        private void UnsubscribeWhenIdle()
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
                $"size={_loadingOptions.Size}, message={_loadingOptions.Message ?? "<none>"}, " +
                $"messageColor={_loadingOptions.MessageColor}, " +
                $"messageFontSize={_loadingOptions.MessageFontSize}, " +
                $"background={_loadingOptions.ShowsBackground}, " +
                $"blocksInteraction={_loadingOptions.BlocksInteraction}");
        }

        private void ClearLoading()
        {
            EditorApplication.update -= UpdateLoading;
            _loadingRequestId = null;
            _loadingOptions = null;
            _loadingDeadline = 0d;
        }

        private static void CloseAll(Dictionary<string, EditorPromptWindow> windows)
        {
            EditorPromptWindow[] values = new EditorPromptWindow[windows.Count];
            windows.Values.CopyTo(values, 0);
            windows.Clear();
            foreach (EditorPromptWindow window in values)
            {
                window.CloseWithoutCallback();
            }
        }
    }

    internal sealed class EditorPromptWindow : EditorWindow
    {
        private string _requestId;
        private AlertOptions _alert;
        private BottomSheetOptions _bottomSheet;
        private Action<string, AlertResult> _alertCompleted;
        private Action<string, string> _actionSelected;
        private Action<string> _cancelled;
        private Action<string> _closed;
        private bool _suppressClose;

        internal static EditorPromptWindow CreateAlert(
            string requestId,
            AlertOptions options,
            Action<string, AlertResult> completed,
            Action<string> closed)
        {
            var window = CreateInstance<EditorPromptWindow>();
            window._requestId = requestId;
            window._alert = options;
            window._alertCompleted = completed;
            window._closed = closed;
            window.titleContent = new GUIContent(options.Title ?? "NativePrompt Alert");
            window.minSize = new Vector2(360f, 140f);
            return window;
        }

        internal static EditorPromptWindow CreateBottomSheet(
            string requestId,
            BottomSheetOptions options,
            Action<string, string> actionSelected,
            Action<string> cancelled,
            Action<string> closed)
        {
            var window = CreateInstance<EditorPromptWindow>();
            window._requestId = requestId;
            window._bottomSheet = options;
            window._actionSelected = actionSelected;
            window._cancelled = cancelled;
            window._closed = closed;
            window.titleContent = new GUIContent(options.Title ?? "NativePrompt Bottom Sheet");
            window.minSize = new Vector2(360f, 180f);
            return window;
        }

        internal void CloseWithoutCallback()
        {
            _suppressClose = true;
            Close();
        }

        private void OnGUI()
        {
            GUILayout.Space(12f);
            if (_alert != null)
            {
                DrawAlert();
            }
            else if (_bottomSheet != null)
            {
                DrawBottomSheet();
            }
        }

        private void DrawAlert()
        {
            EditorGUILayout.LabelField(_alert.Content, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (_alert.NoButtonText != null && GUILayout.Button(_alert.NoButtonText))
                {
                    _alertCompleted(_requestId, AlertResult.No);
                }
                if (_alert.YesButtonText != null && GUILayout.Button(_alert.YesButtonText))
                {
                    _alertCompleted(_requestId, AlertResult.Yes);
                }
                if (_alert.YesButtonText == null && _alert.NoButtonText == null &&
                    GUILayout.Button(_alert.CloseButtonText))
                {
                    _alertCompleted(_requestId, AlertResult.Closed);
                }
            }
            GUILayout.Space(12f);
        }

        private void DrawBottomSheet()
        {
            if (_bottomSheet.Content != null)
            {
                EditorGUILayout.LabelField(_bottomSheet.Content, EditorStyles.wordWrappedLabel);
                GUILayout.Space(8f);
            }

            foreach (BottomSheetAction action in _bottomSheet.Actions)
            {
                using (new EditorGUI.DisabledScope(!action.Enabled))
                {
                    GUIStyle style = action.Style == BottomSheetActionStyle.Destructive
                        ? new GUIStyle(GUI.skin.button) { normal = { textColor = new Color(0.75f, 0.1f, 0.1f) } }
                        : GUI.skin.button;
                    if (GUILayout.Button(action.Text, style))
                    {
                        _actionSelected(_requestId, action.Id);
                    }
                }
            }

            GUILayout.Space(4f);
            if (GUILayout.Button(_bottomSheet.CancelButtonText))
            {
                _cancelled(_requestId);
            }
            GUILayout.Space(12f);
        }

        private void OnDestroy()
        {
            if (!_suppressClose)
            {
                _closed?.Invoke(_requestId);
            }
        }
    }
}
