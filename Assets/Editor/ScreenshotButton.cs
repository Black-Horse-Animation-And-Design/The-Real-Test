using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(ScreenshotCamera))]
public class ScreenshotButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScreenshotCamera terrainModifier = (ScreenshotCamera)target;

        if (GUILayout.Button("Take Screenshot"))
        {
            terrainModifier.TakeScreenshot();
        }


    }
}
