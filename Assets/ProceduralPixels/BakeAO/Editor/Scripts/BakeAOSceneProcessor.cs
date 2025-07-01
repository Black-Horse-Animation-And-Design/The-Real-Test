//using ProceduralPixels.BakeAO;
//using System;
//using UnityEditor.Build;
//using UnityEditor.Build.Reporting;
//using UnityEngine;
//using UnityEngine.Profiling;
//using UnityEngine.SceneManagement;

//public class BakeAOSceneProcessor : IProcessSceneWithReport
//{
//    public int callbackOrder => 0;

//    public void OnProcessScene(Scene scene, BuildReport report)
//    {
//        Profiler.BeginSample(nameof(BakeAOSceneProcessor));

//        try
//        {
//            var rootGameObjects = scene.GetRootGameObjects();
//            foreach (var gameObject in rootGameObjects)
//                ProcessRootGameObject(gameObject, report);
//        }
//        catch (Exception e)
//        {
//            throw e;
//        }
//        finally
//        {
//            Profiler.EndSample();
//        }
//    }

//    private void ProcessRootGameObject(GameObject gameObject, BuildReport report)
//    {
//        GenericBakeAO[] bakeAOs = gameObject.GetComponentsInChildren<GenericBakeAO>();
//        foreach (var bakeAO in bakeAOs)
//            ProcessBakeAO(bakeAO, report);
//    }

//    private void ProcessBakeAO(GenericBakeAO bakeAO, BuildReport report)
//    {
//        if (bakeAO.Mode != BakeAOApplyMode.PrebakedMaterialInstance || !bakeAO.enabled)
//            return;

//        var renderer = bakeAO.rendererRef;
//        if (renderer == null)
//            return;

//        bakeAO.enabled = false;
//        renderer.sharedMaterials = renderer.materials; // instancing

//        foreach (var material in renderer.sharedMaterials)
//            bakeAO.SetupMaterialProperties(material);
//    }
//}
