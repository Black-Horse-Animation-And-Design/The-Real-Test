using System.IO;
using UnityEngine;

public class ScreenshotCamera : MonoBehaviour
{
    Camera cam;

    [SerializeField] Vector2Int screenshotSize;

    [SerializeField] string screenshotName;

    [SerializeField] Transform pivot;
    [SerializeField] bool rotates;
    Vector3 input;


    private void FixedUpdate()
    {
        if (rotates) pivot.Rotate(Vector3.up / 5);
        transform.position += (transform.right * input.x + transform.forward * input.y) * Time.deltaTime;

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

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 forward = transform.up;
        Vector3 right = transform.right;

        forward.Normalize();
        right.Normalize();

        input = forward * y + right * -x;

    }

}