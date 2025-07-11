using ProceduralPixels.BakeAO;
using ProceduralPixels.BakeAO.Editor;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralPixels.BakeAO.Editor
{
    public class BakeStaticRenderers : IProcessSceneWithReport
    {
        public int callbackOrder => int.MinValue + 101;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!BakeAOSettings.Instance.bakeMaterialsForStaticGameObjects)
                return;

            var allStaticBakeAOObjects = scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<GenericBakeAO>(false))
                .Where(o => o.gameObject.isStatic);

            foreach (var bakeAOComponent in allStaticBakeAOObjects)
            {
                var renderer = bakeAOComponent.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                // Disable Bake AO, so it does not change the renderer state when destroying the component next.
                bakeAOComponent.enabled = false;

                // Copy materials
                renderer.sharedMaterials = renderer.materials;
                foreach (var material in renderer.sharedMaterials)
                    bakeAOComponent.SetupMaterialProperties(material);

                // Destroy the component
                if (Application.isPlaying)
                    GameObject.Destroy(bakeAOComponent);
                else
                    GameObject.DestroyImmediate(bakeAOComponent);
            }
        }
    } 
}
