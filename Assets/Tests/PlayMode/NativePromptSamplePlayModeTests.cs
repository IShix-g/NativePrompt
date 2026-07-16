using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NativePrompt.Samples.Tests
{
    public sealed class NativePromptSamplePlayModeTests
    {
        private const string ScenePath = "Assets/Samples/NativePrompt/NativePromptSample.unity";

        [UnitySetUp]
        public IEnumerator LoadSampleScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, $"Could not start loading {ScenePath}");
            yield return load;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithRequiredUiAndActions()
        {
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.path, Is.EqualTo(ScenePath));

            NativePromptSampleController controller = Object.FindFirstObjectByType<NativePromptSampleController>();
            Assert.That(controller, Is.Not.Null);

            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            VisualElement root = document.rootVisualElement;
            Assert.That(root.Q<VisualElement>("logical-viewport"), Is.Not.Null);
            Assert.That(root.Q<Label>("result-value"), Is.Not.Null);

            foreach (string buttonName in NativePromptSampleController.RequiredButtonNames)
            {
                Assert.That(root.Q<Button>(buttonName), Is.Not.Null, $"Missing button: {buttonName}");
                Assert.That(controller.GetBoundApi(buttonName), Is.Not.Null, $"Unbound button: {buttonName}");
            }

            Assert.That(controller.GetBoundApi("alert-full-button"), Is.EqualTo("NP.ShowAlert"));
            Assert.That(controller.GetBoundApi("sheet-destructive-button"), Is.EqualTo("NP.ShowBottomSheet"));
            Assert.That(controller.GetBoundApi("toast-manual-button"), Is.EqualTo("NP.ShowToast"));
            Assert.That(controller.GetBoundApi("toast-dismiss-button"), Is.EqualTo("ToastHandle.Dismiss"));
            Assert.That(
                controller.GetBoundApi("loading-size-medium-button"),
                Is.EqualTo("NP.ShowLoading"));
            Assert.That(
                controller.GetBoundApi("loading-dismiss-button"),
                Is.EqualTo("LoadingHandle.Dismiss"));
            Assert.That(root.Q<Button>("loading-background-button"), Is.Null);
            Assert.That(root.Q<Button>("loading-block-button"), Is.Null);
            Assert.That(root.Q<Button>("loading-position-top-right-button"), Is.Null);
            Assert.That(root.Q<Button>("loading-position-bottom-left-button"), Is.Null);
            Assert.That(
                root.Q<Button>("loading-size-medium-button").ClassListContains("selected-option"),
                Is.False);
            Assert.That(
                root.Q<Button>("loading-position-bottom-right-button")
                    .ClassListContains("selected-option"),
                Is.False);
            Assert.That(
                root.Q<Button>("loading-size-small-button").ClassListContains("selected-option"),
                Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogicalViewportMaintainsNineBySixteenRatio()
        {
            NativePromptSampleController controller = Object.FindFirstObjectByType<NativePromptSampleController>();
            UIDocument document = controller.GetComponent<UIDocument>();
            VisualElement viewport = document.rootVisualElement.Q<VisualElement>("logical-viewport");
            yield return null;

            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            Assert.That(document.panelSettings.scaleMode, Is.EqualTo(PanelScaleMode.ScaleWithScreenSize));
            Assert.That(document.panelSettings.screenMatchMode, Is.EqualTo(PanelScreenMatchMode.MatchWidthOrHeight));
            Assert.That(document.panelSettings.match, Is.Zero);
            Assert.That(viewport.resolvedStyle.width, Is.GreaterThan(0f));
            Assert.That(viewport.resolvedStyle.height, Is.GreaterThan(0f));
            Assert.That(
                viewport.resolvedStyle.width / viewport.resolvedStyle.height,
                Is.EqualTo(NativePromptSampleController.LogicalAspectRatio).Within(0.001f));
            Assert.That(viewport.resolvedStyle.width, Is.LessThanOrEqualTo(NativePromptSampleController.LogicalWidth));
            Assert.That(viewport.resolvedStyle.height, Is.LessThanOrEqualTo(NativePromptSampleController.LogicalHeight));
        }

        [UnityTest]
        public IEnumerator ButtonsUseTwoColumnsWithoutOverlappingSections()
        {
            NativePromptSampleController controller = Object.FindFirstObjectByType<NativePromptSampleController>();
            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            yield return null;

            Button alertContent = root.Q<Button>("alert-content-button");
            Button alertYes = root.Q<Button>("alert-yes-button");
            Button alertNo = root.Q<Button>("alert-no-button");
            Button alertFull = root.Q<Button>("alert-full-button");
            Button sheetStandard = root.Q<Button>("sheet-standard-button");
            Button sheetDisabled = root.Q<Button>("sheet-disabled-button");
            Button toastAuto = root.Q<Button>("toast-auto-button");
            Button toastManual = root.Q<Button>("toast-manual-button");
            Button toastDismiss = root.Q<Button>("toast-dismiss-button");
            Button loadingSizeSmall = root.Q<Button>("loading-size-small-button");
            Button loadingSizeMedium = root.Q<Button>("loading-size-medium-button");
            Button loadingSizeLarge = root.Q<Button>("loading-size-large-button");
            Button loadingPositionTopLeft = root.Q<Button>("loading-position-top-left-button");
            Button loadingPositionCenter = root.Q<Button>("loading-position-center-button");
            Button loadingPositionBottomRight = root.Q<Button>("loading-position-bottom-right-button");
            Button loadingBlockBackground = root.Q<Button>("loading-block-background-button");
            Button loadingDismiss = root.Q<Button>("loading-dismiss-button");
            VisualElement resultPanel = root.Q<VisualElement>("result-panel");

            Assert.That(alertContent.worldBound.y, Is.EqualTo(alertYes.worldBound.y).Within(0.5f));
            Assert.That(alertNo.worldBound.y, Is.EqualTo(alertFull.worldBound.y).Within(0.5f));
            Assert.That(alertFull.worldBound.yMax, Is.LessThan(sheetStandard.worldBound.yMin));
            Assert.That(sheetDisabled.worldBound.yMax, Is.LessThan(toastAuto.worldBound.yMin));
            Assert.That(toastManual.worldBound.y, Is.EqualTo(toastDismiss.worldBound.y).Within(0.5f));
            Assert.That(toastDismiss.worldBound.yMax, Is.LessThan(loadingSizeSmall.worldBound.yMin));
            Assert.That(
                loadingSizeSmall.worldBound.y,
                Is.EqualTo(loadingSizeMedium.worldBound.y).Within(0.5f));
            Assert.That(
                loadingSizeSmall.worldBound.y,
                Is.EqualTo(loadingSizeLarge.worldBound.y).Within(0.5f));
            Assert.That(
                loadingSizeSmall.worldBound.y,
                Is.EqualTo(loadingPositionTopLeft.worldBound.y).Within(0.5f));
            Assert.That(
                loadingSizeSmall.worldBound.y,
                Is.EqualTo(loadingPositionCenter.worldBound.y).Within(0.5f));
            Assert.That(
                loadingSizeSmall.worldBound.y,
                Is.EqualTo(loadingPositionBottomRight.worldBound.y).Within(0.5f));
            Assert.That(
                loadingBlockBackground.worldBound.y,
                Is.EqualTo(loadingDismiss.worldBound.y).Within(0.5f));
            Assert.That(loadingDismiss.worldBound.yMax, Is.LessThan(resultPanel.worldBound.yMin));
        }

        [UnityTest]
        public IEnumerator ResultAreaCanBeUpdated()
        {
            NativePromptSampleController controller = Object.FindFirstObjectByType<NativePromptSampleController>();
            const string expected = "Test result: updated";

            controller.SetResultForTesting(expected);
            yield return null;

            Label result = controller.GetComponent<UIDocument>()
                .rootVisualElement.Q<Label>("result-value");
            Assert.That(result.text, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator DestroyingOwnerSilentlyDisposesPrompt()
        {
            var ownerObject = new GameObject("Prompt owner");
            PromptHandlePlayModeOwner owner =
                ownerObject.AddComponent<PromptHandlePlayModeOwner>();
            int callbackCount = 0;
            bool destroyTokenCancelled = false;
            owner.destroyCancellationToken.Register(() => destroyTokenCancelled = true);
            LogAssert.Expect(LogType.Log, "NativePrompt Toast: Lifecycle");
            ToastHandle handle = NP.ShowToast(
                new ToastOptions
                {
                    Message = "Lifecycle",
                    AutoDismiss = false
                },
                _ => callbackCount++).AddTo(owner);

            Object.Destroy(ownerObject);
            yield return null;
            handle.Dismiss();
            yield return null;

            Assert.That(destroyTokenCancelled, Is.True);
            Assert.That(callbackCount, Is.Zero);
        }
    }

    internal sealed class PromptHandlePlayModeOwner : MonoBehaviour
    {
    }
}
