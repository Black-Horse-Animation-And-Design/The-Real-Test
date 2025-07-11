using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralPixels.BakeAO.Editor
{
    public class InjectBakeAORuntimeSettings : IProcessSceneWithReport
    {
        public int callbackOrder => int.MinValue + 100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var go = new GameObject("BakeAORuntimeSettingsReference", typeof(BakeAORuntimeSettingsInitializer));
            go.hideFlags = HideFlags.HideInInspector & HideFlags.HideInHierarchy & HideFlags.NotEditable;

            var settingsReferenceComponent = go.GetComponent<BakeAORuntimeSettingsInitializer>();
            settingsReferenceComponent.settingsReference = BakeAORuntimeSettings.GetOrCreateAsset();
        }
    } 
}
