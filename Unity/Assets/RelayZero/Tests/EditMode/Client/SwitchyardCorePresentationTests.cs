using NUnit.Framework;
using RelayZero.Client.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RelayZero.Tests.EditMode.Client
{
    public sealed class SwitchyardCorePresentationTests
    {
        [Test]
        public void SwitchyardDemoReferencesCoreProxyAtAuthoritativeDiameter()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Switchyard.unity", OpenSceneMode.Single);
            SwitchyardMovementDemo demo = Object.FindAnyObjectByType<SwitchyardMovementDemo>(
                FindObjectsInactive.Include);

            Assert.That(demo, Is.Not.Null);
            SerializedProperty coreViewProperty = new SerializedObject(demo).FindProperty("coreView");
            Assert.That(coreViewProperty, Is.Not.Null);
            Assert.That(coreViewProperty.objectReferenceValue, Is.TypeOf<Transform>());
            Transform coreView = (Transform)coreViewProperty.objectReferenceValue;
            Assert.That(coreView.name, Is.EqualTo("Core Readability Proxy"));
            Assert.That(coreView.localScale, Is.EqualTo(Vector3.one * 0.6f));
        }
    }
}
