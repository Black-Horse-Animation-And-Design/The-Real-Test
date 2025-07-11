using System.Collections;
using System.IO;
using UnityEngine;

public class ScreenshotCamera : MonoBehaviour
{
    Camera cam;

    [SerializeField] Vector2Int screenshotSize;

    [SerializeField] string screenshotName;

    [SerializeField] Transform pivot;
    [SerializeField] bool rotates;


    private void Awake()
    {
        // Destroy(gameObject);
    }
    private void FixedUpdate()
    {
        pivot.Rotate(Vector3.up / 5);
    }
    public void TakeScreenshot()
    {

        cam = GetComponent<Camera>();

        RenderTexture rt = new RenderTexture(screenshotSize.x, screenshotSize.y, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(screenshotSize.x, screenshotSize.y);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, screenshotSize.x, screenshotSize.y), 0, 0);
        cam.targetTexture = null;

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = "Assets/" + screenshotName;


        if (File.Exists(filename + ".png"))
        {
            for (int i = 1; i < 1000; i++)
            {
                if (File.Exists(filename + i + ".png")) continue;
                else
                {
                    filename += i;
                    break;
                }
            }
        }
        filename += ".png";
        File.WriteAllBytes(filename, bytes);

    }

    IEnumerator TakingVideoRoutine()
    {
        yield return new WaitForSeconds(2);

        float timer = 1;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return new WaitForSeconds(1 / 60);
            TakeScreenshot();
        }

    }

}