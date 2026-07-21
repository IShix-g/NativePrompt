using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NativePrompt.Editor
{
    internal sealed class EditorNativePromptPresenter
    {
        internal const string PreviewAssetsRoot =
            "Packages/com.ishix.nativeprompt/Scripts/Editor/PreviewAssets";
        internal const string PreviewUxmlPath =
            PreviewAssetsRoot + "/NativePromptPreview.uxml";
        internal const string PreviewUssPath =
            PreviewAssetsRoot + "/NativePromptPreview.uss";
        internal const string PreviewThemePath =
            PreviewAssetsRoot + "/NativePromptPreviewTheme.tss";

        private const string AlertPositionerName = "native-prompt-alert-positioner";
        private const string LoadingBackgroundName = "native-prompt-loading-background";
        private const string LoadingContentName = "native-prompt-loading-content";
        private const float PreviewSortingOrder = 32760f;

        private readonly Dictionary<string, VisualElement> _bottomSheetViews =
            new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly List<Button> _reviewStars = new List<Button>();
        private GameObject _host;
        private PanelSettings _panelSettings;
        private VisualElement _modalLayer;
        private VisualElement _toastLayer;
        private VisualElement _loadingLayer;
        private string _alertRequestId;
        private VisualElement _alertView;
        private VisualElement _reviewView;
        private Button _reviewDismissButton;
        private Button _reviewSubmitButton;
        private string _toastRequestId;
        private string _loadingRequestId;
        private LoadingOptions _loadingOptions;

        internal VisualElement RootForTesting =>
            _host == null ? null : _host.GetComponent<UIDocument>().rootVisualElement;

        internal void ShowAlert(
            string requestId,
            AlertOptions options,
            Action<string, AlertResult> completed)
        {
            EnsureCreated();
            DismissAlert(_alertRequestId);

            VisualElement overlay = CreateModalOverlay();
            VisualElement positioner = new VisualElement
            {
                name = AlertPositionerName,
                pickingMode = PickingMode.Ignore
            };
            positioner.AddToClassList("np-alert-positioner");

            VisualElement card = new VisualElement();
            card.AddToClassList("np-alert-card");
            if (options.Title != null)
            {
                card.Add(CreateLabel(options.Title, "np-alert-title"));
            }
            Label content = CreateLabel(options.Content, "np-alert-content");
            if (options.Title != null)
            {
                content.AddToClassList("np-alert-content--with-title");
            }
            else
            {
                content.AddToClassList("np-alert-content--only");
            }
            card.Add(content);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("np-alert-actions");
            if (options.YesButtonText == null && options.NoButtonText == null)
            {
                actions.Add(CreateAlertButton(
                    requestId,
                    options.CloseButtonText,
                    AlertResult.Closed,
                    false,
                    completed));
            }
            else
            {
                bool hasPreviousAction = false;
                if (options.NoButtonText != null)
                {
                    actions.Add(CreateAlertButton(
                        requestId,
                        options.NoButtonText,
                        AlertResult.No,
                        false,
                        completed));
                    hasPreviousAction = true;
                }
                if (options.YesButtonText != null)
                {
                    actions.Add(CreateAlertButton(
                        requestId,
                        options.YesButtonText,
                        AlertResult.Yes,
                        hasPreviousAction,
                        completed));
                }
            }

            card.Add(actions);
            positioner.Add(card);
            overlay.Add(positioner);
            _modalLayer.Add(overlay);
            _modalLayer.style.display = DisplayStyle.Flex;
            _alertRequestId = requestId;
            _alertView = overlay;
        }

        internal void DismissAlert(string requestId)
        {
            if (requestId == null || requestId != _alertRequestId)
            {
                return;
            }

            _alertView?.RemoveFromHierarchy();
            _alertRequestId = null;
            _alertView = null;
            UpdateModalLayerDisplay();
        }

        internal void ShowBottomSheet(
            string requestId,
            BottomSheetOptions options,
            Action<string, string> actionSelected,
            Action<string> cancelled)
        {
            EnsureCreated();
            DismissBottomSheet(requestId);

            VisualElement overlay = CreateModalOverlay();
            VisualElement backdrop = overlay.Q<VisualElement>(className: "np-modal-backdrop");
            backdrop.AddManipulator(new Clickable(() =>
                CompleteBottomSheetCancellation(requestId, cancelled)));

            VisualElement positioner = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            positioner.AddToClassList("np-sheet-positioner");

            VisualElement group = new VisualElement();
            group.AddToClassList("np-sheet-group");
            if (options.Title != null || options.Content != null)
            {
                VisualElement header = new VisualElement();
                header.AddToClassList("np-sheet-header");
                if (options.Title != null)
                {
                    header.Add(CreateLabel(options.Title, "np-sheet-title"));
                }
                if (options.Content != null)
                {
                    header.Add(CreateLabel(options.Content, "np-sheet-content"));
                }
                group.Add(header);
            }

            foreach (BottomSheetAction action in options.Actions)
            {
                var button = new Button();
                button.text = action.Text;
                button.AddToClassList("np-sheet-action");
                if (action.Style == BottomSheetActionStyle.Destructive)
                {
                    button.AddToClassList("np-sheet-action--destructive");
                }
                button.SetEnabled(action.Enabled);
                string actionId = action.Id;
                button.clicked += () =>
                    CompleteBottomSheetAction(requestId, actionId, actionSelected);
                group.Add(button);
            }

            var cancelButton = new Button();
            cancelButton.text = options.CancelButtonText;
            cancelButton.AddToClassList("np-sheet-cancel");
            cancelButton.clicked += () =>
                CompleteBottomSheetCancellation(requestId, cancelled);

            positioner.Add(group);
            positioner.Add(cancelButton);
            overlay.Add(positioner);
            _modalLayer.Add(overlay);
            _modalLayer.style.display = DisplayStyle.Flex;
            _bottomSheetViews.Add(requestId, overlay);
        }

        internal void DismissBottomSheet(string requestId)
        {
            if (requestId == null ||
                !_bottomSheetViews.TryGetValue(requestId, out VisualElement view))
            {
                return;
            }

            _bottomSheetViews.Remove(requestId);
            view.RemoveFromHierarchy();
            UpdateModalLayerDisplay();
        }

        internal void ShowToast(
            string requestId,
            ToastOptions options,
            Action<string> tapped)
        {
            EnsureCreated();
            if (_toastRequestId != null)
            {
                DismissToast(_toastRequestId);
            }

            var positioner = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            positioner.AddToClassList("np-toast-positioner");
            positioner.AddToClassList(GetToastPositionClass(options.Position));

            var toast = new VisualElement
            {
                pickingMode = options.DismissOnTap
                    ? PickingMode.Position
                    : PickingMode.Ignore
            };
            toast.AddToClassList("np-toast");
            toast.Add(CreateLabel(options.Message, "np-toast-label"));
            if (options.DismissOnTap)
            {
                toast.AddManipulator(new Clickable(() => tapped(requestId)));
            }

            positioner.Add(toast);
            _toastLayer.Clear();
            _toastLayer.Add(positioner);
            _toastLayer.style.display = DisplayStyle.Flex;
            _toastRequestId = requestId;
        }

        internal void DismissToast(string requestId)
        {
            if (requestId == null || requestId != _toastRequestId)
            {
                return;
            }

            _toastLayer?.Clear();
            if (_toastLayer != null)
            {
                _toastLayer.style.display = DisplayStyle.None;
            }
            _toastRequestId = null;
        }

        internal void ShowLoading(
            string requestId,
            LoadingOptions options,
            bool showVisualsImmediately)
        {
            EnsureCreated();
            _loadingLayer.Clear();
            _loadingLayer.style.display = DisplayStyle.Flex;
            _loadingLayer.pickingMode = options.BlocksInteraction
                ? PickingMode.Position
                : PickingMode.Ignore;

            var background = new VisualElement
            {
                name = LoadingBackgroundName,
                pickingMode = PickingMode.Ignore
            };
            background.AddToClassList("np-loading-background");
            background.style.backgroundColor = new Color(
                options.BackgroundColor.r,
                options.BackgroundColor.g,
                options.BackgroundColor.b,
                options.BackgroundOpacity);
            background.style.display = showVisualsImmediately && options.ShowsBackground
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            var positioner = new VisualElement
            {
                name = LoadingContentName,
                pickingMode = PickingMode.Ignore
            };
            positioner.AddToClassList("np-loading-positioner");
            positioner.AddToClassList(GetLoadingPositionClass(options.Position));
            positioner.style.display = showVisualsImmediately
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            VisualElement content = CreateLoadingContent(options);
            positioner.Add(content);
            _loadingLayer.Add(background);
            _loadingLayer.Add(positioner);
            _loadingRequestId = requestId;
            _loadingOptions = options;
        }

        internal void ShowLoadingVisuals(string requestId)
        {
            if (requestId == null || requestId != _loadingRequestId || _loadingLayer == null)
            {
                return;
            }

            VisualElement background = _loadingLayer.Q<VisualElement>(LoadingBackgroundName);
            VisualElement content = _loadingLayer.Q<VisualElement>(LoadingContentName);
            if (background != null)
            {
                background.style.display = _loadingOptions.ShowsBackground
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
            if (content != null)
            {
                content.style.display = DisplayStyle.Flex;
            }
        }

        internal void DismissLoading(string requestId)
        {
            if (requestId == null || requestId != _loadingRequestId)
            {
                return;
            }

            _loadingLayer?.Clear();
            if (_loadingLayer != null)
            {
                _loadingLayer.style.display = DisplayStyle.None;
                _loadingLayer.pickingMode = PickingMode.Ignore;
            }
            _loadingRequestId = null;
            _loadingOptions = null;
        }

        internal void ShowReview()
        {
            EnsureCreated();
            DismissReview();

            VisualElement overlay = CreateModalOverlay();
            VisualElement positioner = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            positioner.AddToClassList("np-review-positioner");

            VisualElement card = new VisualElement();
            card.AddToClassList("np-review-card");

            Label monogram = CreateLabel(GetApplicationMonogram(), "np-review-app-monogram");
            var appIcon = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            appIcon.AddToClassList("np-review-app-icon");
            appIcon.Add(monogram);
            card.Add(appIcon);

            string productName = string.IsNullOrWhiteSpace(Application.productName)
                ? "this app"
                : Application.productName;
            card.Add(CreateLabel($"Enjoying {productName}?", "np-review-title"));
            card.Add(CreateLabel(
                "Tap a star to rate it on the App Store.",
                "np-review-content"));

            var divider = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            divider.AddToClassList("np-review-divider");
            card.Add(divider);

            var stars = new VisualElement();
            stars.AddToClassList("np-review-stars");
            for (int rating = 1; rating <= 5; rating++)
            {
                int selectedRating = rating;
                var star = new Button
                {
                    name = $"native-prompt-review-star-{rating}",
                    text = "☆",
                    tooltip = $"{rating} star rating"
                };
                star.AddToClassList("np-review-star");
                star.clicked += () => SelectReviewRating(selectedRating);
                _reviewStars.Add(star);
                stars.Add(star);
            }
            card.Add(stars);

            _reviewDismissButton = new Button(DismissReview)
            {
                name = "native-prompt-review-not-now",
                text = "Not Now"
            };
            _reviewDismissButton.AddToClassList("np-review-action");
            card.Add(_reviewDismissButton);

            _reviewSubmitButton = new Button(DismissReview)
            {
                name = "native-prompt-review-submit",
                text = "Submit"
            };
            _reviewSubmitButton.AddToClassList("np-review-action");
            _reviewSubmitButton.style.display = DisplayStyle.None;
            card.Add(_reviewSubmitButton);

            positioner.Add(card);
            overlay.Add(positioner);
            _modalLayer.Add(overlay);
            _modalLayer.style.display = DisplayStyle.Flex;
            _reviewView = overlay;
        }

        internal void DismissReview()
        {
            _reviewView?.RemoveFromHierarchy();
            _reviewView = null;
            _reviewStars.Clear();
            _reviewDismissButton = null;
            _reviewSubmitButton = null;
            UpdateModalLayerDisplay();
        }

        internal void Reset()
        {
            _bottomSheetViews.Clear();
            _alertRequestId = null;
            _alertView = null;
            _reviewView = null;
            _reviewStars.Clear();
            _reviewDismissButton = null;
            _reviewSubmitButton = null;
            _toastRequestId = null;
            _loadingRequestId = null;
            _loadingOptions = null;
            DestroyPanel();
        }

        internal static bool PreviewAssetsAreLoadable()
        {
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreviewUxmlPath) != null &&
                AssetDatabase.LoadAssetAtPath<StyleSheet>(PreviewUssPath) != null &&
                AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(PreviewThemePath) != null;
        }

        private Button CreateAlertButton(
            string requestId,
            string text,
            AlertResult result,
            bool separated,
            Action<string, AlertResult> completed)
        {
            var button = new Button();
            button.text = text;
            button.AddToClassList("np-alert-action");
            if (separated)
            {
                button.AddToClassList("np-alert-action--separated");
            }
            button.clicked += () => CompleteAlert(requestId, result, completed);
            return button;
        }

        private void CompleteAlert(
            string requestId,
            AlertResult result,
            Action<string, AlertResult> completed)
        {
            if (requestId != _alertRequestId)
            {
                return;
            }

            DismissAlert(requestId);
            completed(requestId, result);
        }

        private void CompleteBottomSheetAction(
            string requestId,
            string actionId,
            Action<string, string> actionSelected)
        {
            if (!_bottomSheetViews.ContainsKey(requestId))
            {
                return;
            }

            DismissBottomSheet(requestId);
            actionSelected(requestId, actionId);
        }

        private void CompleteBottomSheetCancellation(
            string requestId,
            Action<string> cancelled)
        {
            if (!_bottomSheetViews.ContainsKey(requestId))
            {
                return;
            }

            DismissBottomSheet(requestId);
            cancelled(requestId);
        }

        internal void SelectReviewRating(int rating)
        {
            if (_reviewView == null)
            {
                return;
            }

            for (int index = 0; index < _reviewStars.Count; index++)
            {
                bool selected = index < rating;
                Button star = _reviewStars[index];
                star.text = selected ? "★" : "☆";
                star.EnableInClassList("np-review-star--selected", selected);
            }

            _reviewDismissButton.style.display = DisplayStyle.None;
            _reviewSubmitButton.style.display = DisplayStyle.Flex;
        }

        private VisualElement CreateLoadingContent(LoadingOptions options)
        {
            var content = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            content.AddToClassList("np-loading-content");

            bool hasMessage = options.Message != null;
            bool isCorner = options.Position != LoadingPosition.Center;
            bool spinnerOnRight = options.Position == LoadingPosition.TopRight ||
                options.Position == LoadingPosition.BottomRight;
            if (hasMessage && isCorner)
            {
                content.AddToClassList("np-loading-content--horizontal");
            }
            else
            {
                content.AddToClassList("np-loading-content--vertical");
            }

            VisualElement spinner = CreateSpinner(options);
            Label message = hasMessage ? CreateLoadingMessage(options, isCorner) : null;
            if (message != null && spinnerOnRight)
            {
                content.Add(message);
            }
            content.Add(spinner);
            if (message != null && !spinnerOnRight)
            {
                content.Add(message);
            }
            return content;
        }

        private static VisualElement CreateSpinner(LoadingOptions options)
        {
            float size;
            float borderWidth;
            switch (options.Size)
            {
                case LoadingSize.Small:
                    size = 20f;
                    borderWidth = 2.5f;
                    break;
                case LoadingSize.Large:
                    size = 38f;
                    borderWidth = 4f;
                    break;
                case LoadingSize.Medium:
                default:
                    size = 28f;
                    borderWidth = 3f;
                    break;
            }

            var spinner = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            spinner.AddToClassList("np-loading-spinner");
            spinner.style.width = size;
            spinner.style.height = size;
            spinner.style.borderTopWidth = borderWidth;
            spinner.style.borderRightWidth = borderWidth;
            spinner.style.borderBottomWidth = borderWidth;
            spinner.style.borderLeftWidth = borderWidth;
            spinner.style.borderTopLeftRadius = size;
            spinner.style.borderTopRightRadius = size;
            spinner.style.borderBottomRightRadius = size;
            spinner.style.borderBottomLeftRadius = size;

            Color strong = options.SpinnerColor;
            Color faded = options.SpinnerColor;
            faded.a *= 0.18f;
            spinner.style.borderTopColor = faded;
            spinner.style.borderRightColor = strong;
            spinner.style.borderBottomColor = strong;
            spinner.style.borderLeftColor = strong;

            float angle = 0f;
            spinner.schedule.Execute(() =>
            {
                angle = (angle + 24f) % 360f;
                spinner.style.rotate = new Rotate(Angle.Degrees(angle));
            }).Every(40);
            return spinner;
        }

        private static Label CreateLoadingMessage(LoadingOptions options, bool isCorner)
        {
            Label label = CreateLabel(options.Message, "np-loading-message");
            label.style.color = options.MessageColor;
            label.style.fontSize = options.MessageFontSize;
            label.style.maxWidth = isCorner ? 220f : 300f;
            return label;
        }

        private static VisualElement CreateModalOverlay()
        {
            var overlay = new VisualElement();
            overlay.AddToClassList("np-modal-overlay");

            var backdrop = new VisualElement();
            backdrop.AddToClassList("np-modal-backdrop");
            overlay.Add(backdrop);
            return overlay;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text)
            {
                pickingMode = PickingMode.Ignore
            };
            label.AddToClassList(className);
            return label;
        }

        private static string GetApplicationMonogram()
        {
            string productName = Application.productName;
            if (!string.IsNullOrEmpty(productName))
            {
                foreach (char character in productName)
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        return char.ToUpperInvariant(character).ToString();
                    }
                }
            }

            return "A";
        }

        private void EnsureCreated()
        {
            if (_host != null)
            {
                return;
            }

            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreviewUxmlPath);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PreviewUssPath);
            ThemeStyleSheet theme =
                AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(PreviewThemePath);
            if (visualTree == null || styleSheet == null || theme == null)
            {
                throw new InvalidOperationException(
                    $"NativePrompt Editor preview assets could not be loaded from " +
                    $"{PreviewAssetsRoot}.");
            }

            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.name = "NativePrompt Editor Preview Panel";
            _panelSettings.hideFlags = HideFlags.HideAndDontSave;
            _panelSettings.themeStyleSheet = theme;
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panelSettings.referenceResolution = new Vector2Int(540, 960);
            _panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _panelSettings.match = 0.5f;
            _panelSettings.sortingOrder = PreviewSortingOrder;
            _panelSettings.clearColor = false;
            _panelSettings.clearDepthStencil = false;

            _host = new GameObject("NativePrompt Editor Preview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _host.SetActive(false);
            UIDocument document = _host.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = PreviewSortingOrder;
            _host.SetActive(true);

            VisualElement root = document.rootVisualElement;
            root.styleSheets.Add(styleSheet);
            root.pickingMode = PickingMode.Ignore;
            _modalLayer = root.Q<VisualElement>("native-prompt-modal-layer");
            _toastLayer = root.Q<VisualElement>("native-prompt-toast-layer");
            _loadingLayer = root.Q<VisualElement>("native-prompt-loading-layer");
            if (_modalLayer == null || _toastLayer == null || _loadingLayer == null)
            {
                DestroyPanel();
                throw new InvalidOperationException(
                    "NativePrompt Editor preview UXML is missing a required layer.");
            }

            _modalLayer.pickingMode = PickingMode.Ignore;
            _toastLayer.pickingMode = PickingMode.Ignore;
            _loadingLayer.pickingMode = PickingMode.Ignore;
            _modalLayer.style.display = DisplayStyle.None;
            _toastLayer.style.display = DisplayStyle.None;
            _loadingLayer.style.display = DisplayStyle.None;
        }

        private void UpdateModalLayerDisplay()
        {
            if (_modalLayer == null)
            {
                return;
            }

            _modalLayer.style.display = _alertView == null &&
                _reviewView == null &&
                _bottomSheetViews.Count == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void DestroyPanel()
        {
            if (_host != null)
            {
                UnityEngine.Object.DestroyImmediate(_host);
            }
            if (_panelSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(_panelSettings);
            }

            _host = null;
            _panelSettings = null;
            _modalLayer = null;
            _toastLayer = null;
            _loadingLayer = null;
        }

        private static string GetToastPositionClass(ToastPosition position)
        {
            switch (position)
            {
                case ToastPosition.Top:
                    return "np-toast-positioner--top";
                case ToastPosition.Center:
                    return "np-toast-positioner--center";
                case ToastPosition.Bottom:
                default:
                    return "np-toast-positioner--bottom";
            }
        }

        private static string GetLoadingPositionClass(LoadingPosition position)
        {
            switch (position)
            {
                case LoadingPosition.TopLeft:
                    return "np-loading-positioner--top-left";
                case LoadingPosition.TopRight:
                    return "np-loading-positioner--top-right";
                case LoadingPosition.BottomLeft:
                    return "np-loading-positioner--bottom-left";
                case LoadingPosition.BottomRight:
                    return "np-loading-positioner--bottom-right";
                case LoadingPosition.Center:
                default:
                    return "np-loading-positioner--center";
            }
        }
    }
}
