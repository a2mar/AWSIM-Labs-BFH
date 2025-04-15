using UnityEngine;
using System.IO;

public class CameraCapture : MonoBehaviour
{
    public Camera rlCam;
    public RenderTexture renderTexture;

    public string savePath = "BFH/CameraCaptures";

    void Start()
    {
        if (rlCam != null && renderTexture != null)
        {
            rlCam.targetTexture = renderTexture;
        }
        SaveCameraImage();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveCameraImage();
        }
    }

    public void SaveCameraImage()
    {
        // Set the active RenderTexture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        // Render the camera's view
        rlCam.Render();

        // Create a Texture2D with the size of the RenderTexture
        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        // Optional: Reset RenderTexture
        RenderTexture.active = currentRT;

        // Encode texture to PNG
        byte[] bytes = image.EncodeToJPG();

        // Save to file
        string dir = Path.Combine(Application.dataPath, savePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, $"RLCamCapture_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg");
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Image saved to: {filePath}");
    }
}
