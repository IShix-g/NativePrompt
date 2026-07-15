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
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogicalViewportMaintainsNineBySixteenRatio()
        {
            NativePromptSampleController controller = Object.FindFirstObjectByType<NativePromptSampleController>();
            VisualElement viewport = controller.GetComponent<UIDocument>()
                .rootVisualElement.Q<VisualElement>("logical-viewport");
            yield return null;

            Assert.That(viewport.resolvedStyle.width, Is.GreaterThan(0f));
            Assert.That(viewport.resolvedStyle.height, Is.GreaterThan(0f));
            Assert.That(
                viewport.resolvedStyle.width / viewport.resolvedStyle.height,
                Is.EqualTo(NativePromptSampleController.LogicalAspectRatio).Within(0.001f));
            Assert.That(viewport.resolvedStyle.width, Is.LessThanOrEqualTo(NativePromptSampleController.LogicalWidth));
            Assert.That(viewport.resolvedStyle.height, Is.LessThanOrEqualTo(NativePromptSampleController.LogicalHeight));
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
    }
}
