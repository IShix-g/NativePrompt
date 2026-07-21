using NativePrompt.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace NativePrompt.Tests
{
    public sealed class NativePromptEditorPreviewTests
    {
        private EditorNativePromptPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _presenter = new EditorNativePromptPresenter();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Reset();
        }

        [Test]
        public void PreviewAssets_AreLoadableOutsidePlayerSpecialFolders()
        {
            Assert.That(EditorNativePromptPresenter.PreviewAssetsAreLoadable(), Is.True);
            Assert.That(EditorNativePromptPresenter.PreviewAssetsRoot, Does.Not.Contain("/Resources"));
            Assert.That(EditorNativePromptPresenter.PreviewAssetsRoot, Does.Not.Contain("/StreamingAssets"));
        }

        [Test]
        public void Alert_UsesIosCardStructure()
        {
            _presenter.ShowAlert(
                "alert",
                new AlertOptions
                {
                    Title = "Title",
                    Content = "Message",
                    NoButtonText = "No",
                    YesButtonText = "Yes"
                },
                (_, _) => { });

            VisualElement root = _presenter.RootForTesting;
            Assert.That(root.Q<VisualElement>(className: "np-alert-card"), Is.Not.Null);
            Assert.That(root.Query<Button>(className: "np-alert-action").ToList(), Has.Count.EqualTo(2));

            _presenter.DismissAlert("alert");
            Assert.That(root.Q<VisualElement>(className: "np-alert-card"), Is.Null);
        }

        [Test]
        public void ContentOnlyAlert_CentersContentInBodyArea()
        {
            _presenter.ShowAlert(
                "alert",
                new AlertOptions
                {
                    Content = "Content only alert",
                    CloseButtonText = "Close"
                },
                (_, _) => { });

            VisualElement root = _presenter.RootForTesting;
            Assert.That(root.Q<Label>(className: "np-alert-content--only"), Is.Not.Null);
            Assert.That(root.Query<Button>(className: "np-alert-action").ToList(), Has.Count.EqualTo(1));
        }

        [Test]
        public void BottomSheet_UsesIosActionGroupAndSeparateCancel()
        {
            _presenter.ShowBottomSheet(
                "sheet",
                new BottomSheetOptions
                {
                    Title = "Title",
                    Content = "Message",
                    CancelButtonText = "Cancel",
                    Actions = new[]
                    {
                        new BottomSheetAction { Id = "first", Text = "First" },
                        new BottomSheetAction
                        {
                            Id = "delete",
                            Text = "Delete",
                            Style = BottomSheetActionStyle.Destructive
                        }
                    }
                },
                (_, _) => { },
                _ => { });

            VisualElement root = _presenter.RootForTesting;
            Assert.That(root.Q<VisualElement>(className: "np-sheet-group"), Is.Not.Null);
            Assert.That(root.Query<Button>(className: "np-sheet-action").ToList(), Has.Count.EqualTo(2));
            Assert.That(root.Q<Button>(className: "np-sheet-cancel"), Is.Not.Null);
        }

        [Test]
        public void ToastAndLoading_ReflectPositionAndBlockingOptions()
        {
            _presenter.ShowToast(
                "toast",
                new ToastOptions
                {
                    Message = "Saved",
                    Position = ToastPosition.Top,
                    DismissOnTap = true
                },
                _ => { });
            _presenter.ShowLoading(
                "loading",
                new LoadingOptions
                {
                    BlocksInteraction = true,
                    ShowsBackground = true,
                    BackgroundColor = Color.white,
                    BackgroundOpacity = 0.5f,
                    Position = LoadingPosition.BottomRight,
                    Size = LoadingSize.Large,
                    Message = "Working"
                },
                true);

            VisualElement root = _presenter.RootForTesting;
            Assert.That(
                root.Q<VisualElement>(className: "np-toast-positioner--top"),
                Is.Not.Null);
            Assert.That(
                root.Q<VisualElement>(className: "np-loading-positioner--bottom-right"),
                Is.Not.Null);
            Assert.That(
                root.Q<VisualElement>("native-prompt-loading-layer").pickingMode,
                Is.EqualTo(PickingMode.Position));
            Assert.That(root.Q<VisualElement>(className: "np-loading-spinner"), Is.Not.Null);
            Assert.That(root.Q<Label>(className: "np-loading-message").text, Is.EqualTo("Working"));
        }

        [Test]
        public void StoreReview_UsesImageFreeIosStyleAndCanBeDismissed()
        {
            _presenter.ShowReview();

            VisualElement root = _presenter.RootForTesting;
            Assert.That(root.Q<VisualElement>(className: "np-review-card"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "np-review-app-icon"), Is.Not.Null);
            Assert.That(root.Query<Image>().ToList(), Is.Empty);
            Assert.That(root.Query<Button>(className: "np-review-star").ToList(), Has.Count.EqualTo(5));
            Assert.That(root.Q<Button>("native-prompt-review-not-now").text, Is.EqualTo("Not Now"));
            Assert.That(root.Q<Button>("native-prompt-review-submit").text, Is.EqualTo("Submit"));

            _presenter.SelectReviewRating(3);
            var stars = root.Query<Button>(className: "np-review-star").ToList();
            Assert.That(stars[0].text, Is.EqualTo("★"));
            Assert.That(stars[2].text, Is.EqualTo("★"));
            Assert.That(stars[3].text, Is.EqualTo("☆"));
            Assert.That(
                root.Q<Button>("native-prompt-review-not-now").style.display.value,
                Is.EqualTo(DisplayStyle.None));
            Assert.That(
                root.Q<Button>("native-prompt-review-submit").style.display.value,
                Is.EqualTo(DisplayStyle.Flex));

            _presenter.DismissReview();
            Assert.That(root.Q<VisualElement>(className: "np-review-card"), Is.Null);
        }
    }
}
