using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NativePrompt.Samples
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class NativePromptSampleController : MonoBehaviour
    {
        public const float LogicalWidth = 540f;
        public const float LogicalHeight = 960f;
        public const float LogicalAspectRatio = LogicalWidth / LogicalHeight;
        private const float BlockingLoadingAutoDismissSeconds = 3f;

        public static readonly string[] RequiredButtonNames =
        {
            "alert-content-button",
            "alert-yes-button",
            "alert-no-button",
            "alert-full-button",
            "sheet-standard-button",
            "sheet-destructive-button",
            "sheet-disabled-button",
            "toast-auto-button",
            "toast-tap-button",
            "toast-manual-button",
            "toast-dismiss-button",
            "loading-spinner-button",
            "loading-background-button",
            "loading-block-button",
            "loading-block-background-button",
            "loading-dismiss-button"
        };

        private readonly Dictionary<string, string> _boundApis = new Dictionary<string, string>();
        private readonly Dictionary<string, Action> _actions = new Dictionary<string, Action>();
        private VisualElement _root;
        private VisualElement _logicalViewport;
        private Label _resultLabel;
        private ToastHandle _manualToast;
        private LoadingHandle _loading;
        private Coroutine _loadingAutoDismissCoroutine;

        public IReadOnlyDictionary<string, string> BoundApis => _boundApis;

        private void OnEnable()
        {
            Application.targetFrameRate = 60;
            _root = GetComponent<UIDocument>().rootVisualElement;
            _logicalViewport = _root.Q<VisualElement>("logical-viewport");
            _resultLabel = _root.Q<Label>("result-value");

            if (_logicalViewport == null || _resultLabel == null)
            {
                Debug.LogError("NativePrompt sample UXML is missing required elements.", this);
                return;
            }

            BindActions();
            _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            FitLogicalViewport(_root.resolvedStyle.width, _root.resolvedStyle.height);
            SetResult("Ready");
        }

        private void OnDisable()
        {
            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

                foreach (KeyValuePair<string, Action> binding in _actions)
                {
                    Button button = _root.Q<Button>(binding.Key);
                    if (button != null)
                    {
                        button.clicked -= binding.Value;
                    }
                }
            }

            _boundApis.Clear();
            _actions.Clear();
            _manualToast = null;
            StopLoadingAutoDismiss();
            _loading?.Dispose();
            _loading = null;
        }

        public string GetBoundApi(string buttonName)
        {
            return _boundApis.TryGetValue(buttonName, out string api) ? api : null;
        }

        public void SetResultForTesting(string value)
        {
            SetResult(value);
        }

        private void BindActions()
        {
            _boundApis.Clear();
            _actions.Clear();

            Bind("alert-content-button", "NP.ShowAlert", ShowContentAlert);
            Bind("alert-yes-button", "NP.ShowAlert", ShowYesAlert);
            Bind("alert-no-button", "NP.ShowAlert", ShowNoAlert);
            Bind("alert-full-button", "NP.ShowAlert", ShowFullAlert);
            Bind("sheet-standard-button", "NP.ShowBottomSheet", ShowStandardBottomSheet);
            Bind("sheet-destructive-button", "NP.ShowBottomSheet", ShowDestructiveBottomSheet);
            Bind("sheet-disabled-button", "NP.ShowBottomSheet", ShowDisabledBottomSheet);
            Bind("toast-auto-button", "NP.ShowToast", ShowAutoToast);
            Bind("toast-tap-button", "NP.ShowToast", ShowTapToast);
            Bind("toast-manual-button", "NP.ShowToast", ShowManualToast);
            Bind("toast-dismiss-button", "ToastHandle.Dismiss", DismissManualToast);
            Bind("loading-spinner-button", "NP.ShowLoading", ShowSpinnerLoading);
            Bind("loading-background-button", "NP.ShowLoading", ShowBackgroundLoading);
            Bind("loading-block-button", "NP.ShowLoading", ShowBlockingLoading);
            Bind(
                "loading-block-background-button",
                "NP.ShowLoading",
                ShowBlockingBackgroundLoading);
            Bind("loading-dismiss-button", "LoadingHandle.Dismiss", DismissLoading);
        }

        private void Bind(string buttonName, string api, Action action)
        {
            Button button = _root.Q<Button>(buttonName);
            if (button == null)
            {
                Debug.LogError($"NativePrompt sample button is missing: {buttonName}", this);
                return;
            }

            _boundApis.Add(buttonName, api);
            _actions.Add(buttonName, action);
            button.clicked += action;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitLogicalViewport(evt.newRect.width, evt.newRect.height);
        }

        private void FitLogicalViewport(float availableWidth, float availableHeight)
        {
            if (_logicalViewport == null || availableWidth <= 0f || availableHeight <= 0f ||
                float.IsNaN(availableWidth) || float.IsNaN(availableHeight))
            {
                return;
            }

            float width = Mathf.Min(LogicalWidth, availableWidth, availableHeight * LogicalAspectRatio);
            float height = width / LogicalAspectRatio;
            _logicalViewport.style.width = width;
            _logicalViewport.style.height = height;
        }

        private void ShowContentAlert()
        {
            SetResult("Alert: waiting");
            NP.ShowAlert(
                new AlertOptions { Content = "Content only alert" },
                result => SetResult($"Alert: {result}"));
        }

        private void ShowYesAlert()
        {
            SetResult("Alert: waiting");
            NP.ShowAlert(
                new AlertOptions { Content = "Alert with Yes", YesButtonText = "Yes" },
                result => SetResult($"Alert: {result}"));
        }

        private void ShowNoAlert()
        {
            SetResult("Alert: waiting");
            NP.ShowAlert(
                new AlertOptions { Content = "Alert with No", NoButtonText = "No" },
                result => SetResult($"Alert: {result}"));
        }

        private void ShowFullAlert()
        {
            SetResult("Alert: waiting");
            NP.ShowAlert(
                new AlertOptions
                {
                    Title = "Native Alert",
                    Content = "Choose an answer",
                    YesButtonText = "Yes",
                    NoButtonText = "No"
                },
                result => SetResult($"Alert: {result}"));
        }

        private void ShowStandardBottomSheet()
        {
            ShowBottomSheet(
                "Standard Bottom Sheet",
                new BottomSheetAction { Id = "first", Text = "First action" },
                new BottomSheetAction { Id = "second", Text = "Second action" });
        }

        private void ShowDestructiveBottomSheet()
        {
            ShowBottomSheet(
                "Destructive Action",
                new BottomSheetAction { Id = "keep", Text = "Keep" },
                new BottomSheetAction
                {
                    Id = "delete",
                    Text = "Delete",
                    Style = BottomSheetActionStyle.Destructive
                });
        }

        private void ShowDisabledBottomSheet()
        {
            ShowBottomSheet(
                "Disabled Action",
                new BottomSheetAction { Id = "available", Text = "Available" },
                new BottomSheetAction { Id = "unavailable", Text = "Unavailable", Enabled = false });
        }

        private void ShowBottomSheet(string title, params BottomSheetAction[] actions)
        {
            SetResult("Bottom Sheet: waiting");
            NP.ShowBottomSheet(
                new BottomSheetOptions
                {
                    Title = title,
                    Content = "Select an action",
                    Actions = actions
                },
                result => SetResult(result.IsCancelled
                    ? "Bottom Sheet: Cancelled"
                    : $"Bottom Sheet: {result.ActionId}"));
        }

        private void ShowAutoToast()
        {
            SetResult("Toast: shown (auto)");
            NP.ShowToast(
                new ToastOptions { Message = "This toast closes automatically" },
                reason => SetResult($"Toast: {reason}"));
        }

        private void ShowTapToast()
        {
            SetResult("Toast: shown (tap to close)");
            NP.ShowToast(
                new ToastOptions
                {
                    Message = "Tap this toast",
                    AutoDismiss = false,
                    DismissOnTap = true
                },
                reason => SetResult($"Toast: {reason}"));
        }

        private void ShowManualToast()
        {
            SetResult("Toast: shown (manual)");
            _manualToast = NP.ShowToast(
                new ToastOptions
                {
                    Message = "Use Dismiss Manual Toast",
                    AutoDismiss = false,
                    DismissOnTap = false
                },
                reason =>
                {
                    _manualToast = null;
                    SetResult($"Toast: {reason}");
                });
        }

        private void DismissManualToast()
        {
            if (_manualToast == null)
            {
                SetResult("Toast: no manual toast is active");
                return;
            }

            _manualToast.Dismiss();
        }

        private void ShowSpinnerLoading()
        {
            ShowLoading(new LoadingOptions
            {
                ShowDelaySeconds = 0f
            }, "Loading: spinner only, bottom-right / medium, pass-through");
        }

        private void ShowBackgroundLoading()
        {
            ShowLoading(new LoadingOptions
            {
                ShowsBackground = true,
                BackgroundColor = new Color(0.85f, 0.93f, 1f),
                BackgroundOpacity = 0.65f,
                Position = LoadingPosition.Center,
                Size = LoadingSize.Medium,
                Message = "Loading with pass-through background",
                ShowDelaySeconds = 0.25f
            }, "Loading: centered background, pass-through");
        }

        private void ShowBlockingLoading()
        {
            ShowLoading(new LoadingOptions
            {
                BlocksInteraction = true,
                Position = LoadingPosition.BottomLeft,
                Size = LoadingSize.Large,
                Message = "Input blocked immediately",
                ShowDelaySeconds = 1f
            }, "Loading: invisible blocker, 1s visual delay");
        }

        private void ShowBlockingBackgroundLoading()
        {
            ShowLoading(new LoadingOptions
            {
                BlocksInteraction = true,
                ShowsBackground = true,
                BackgroundColor = Color.white,
                BackgroundOpacity = 0.5f,
                Position = LoadingPosition.Center,
                Size = LoadingSize.Medium,
                Message = "Processing...",
                ShowDelaySeconds = 0.25f
            }, "Loading: centered background and blocker");
        }

        private void ShowLoading(LoadingOptions options, string result)
        {
            StopLoadingAutoDismiss();
            _loading?.Dismiss();
            _loading = NP.ShowLoading(options).AddTo(this);
            if (options.BlocksInteraction)
            {
                _loadingAutoDismissCoroutine = StartCoroutine(
                    DismissLoadingAfterDelay(_loading));
                SetResult($"{result}; auto-dismiss in {BlockingLoadingAutoDismissSeconds:0}s");
                return;
            }

            SetResult(result);
        }

        private void DismissLoading()
        {
            StopLoadingAutoDismiss();
            if (_loading == null)
            {
                SetResult("Loading: no request is active");
                return;
            }

            _loading.Dismiss();
            _loading = null;
            SetResult("Loading: dismissed");
        }

        private IEnumerator DismissLoadingAfterDelay(LoadingHandle loading)
        {
            yield return new WaitForSecondsRealtime(BlockingLoadingAutoDismissSeconds);
            _loadingAutoDismissCoroutine = null;

            if (!ReferenceEquals(_loading, loading))
            {
                yield break;
            }

            loading.Dismiss();
            _loading = null;
            SetResult($"Loading: auto-dismissed after {BlockingLoadingAutoDismissSeconds:0}s");
        }

        private void StopLoadingAutoDismiss()
        {
            if (_loadingAutoDismissCoroutine == null)
            {
                return;
            }

            StopCoroutine(_loadingAutoDismissCoroutine);
            _loadingAutoDismissCoroutine = null;
        }

        private void SetResult(string value)
        {
            if (_resultLabel != null)
            {
                _resultLabel.text = value;
            }
        }
    }
}
