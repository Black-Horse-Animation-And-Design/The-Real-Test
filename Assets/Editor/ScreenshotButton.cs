using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(ScreenshotCamera))]
public class ScreenshotButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScreenshotCamera screenshotCam = (ScreenshotCamera)target;

        if (GUILayout.Button("Take Screenshot"))
        {
            screenshotCam.TakeScreenshot();
        }

    }
}
