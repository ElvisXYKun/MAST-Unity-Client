using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Periodically captures camera frames from the iPad or webcam,
/// compresses to JPEG, converts to Base64, and streams to the server.
/// </summary>
[RequireComponent(typeof(MastClient))]
public class CameraStreaming : MonoBehaviour
{
    [Header("Camera Settings")]
    public int captureWidth = 640;
    public int captureHeight = 480;
    public float sendIntervalSeconds = 0.1f; // ~10 FPS

    private WebCamTexture _webCamTexture;
    private Texture2D _outputTexture;
    private MastClient _mastClient;
    private bool _isCapturing = false;

    void Start()
    {
        _mastClient = GetComponent<MastClient>();
        StartCoroutine(InitWebCam());
    }

    IEnumerator InitWebCam()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            _webCamTexture = new WebCamTexture(captureWidth, captureHeight, 15);
            _webCamTexture.Play();
            _outputTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            _isCapturing = true;
            StartCoroutine(CaptureAndStreamLoop());
        }
        else
        {
            Debug.LogWarning("[MAST Cam] Camera permission not granted or webcam not available.");
        }
    }

    IEnumerator CaptureAndStreamLoop()
    {
        while (_isCapturing)
        {
            yield return new WaitForSeconds(sendIntervalSeconds);

            if (_webCamTexture != null && _webCamTexture.isPlaying && _webCamTexture.didUpdateThisFrame)
            {
                try
                {
                    // Copy camera texture to output texture
                    _outputTexture.SetPixels(_webCamTexture.GetPixels());
                    _outputTexture.Apply();

                    // Compress to JPG (quality 75 is a good balance for tracking + speed)
                    byte[] jpegBytes = ImageConversion.EncodeToJPG(_outputTexture, 75);

                    // Convert to Base64
                    string base64Image = Convert.ToBase64String(jpegBytes);

                    // Build JSON payload
                    var payload = new CameraPayload
                    {
                        type = "camera",
                        data = base64Image
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    _mastClient.SendData(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[MAST Cam] Capture failed: " + ex.Message);
                }
            }
        }
    }

    void OnDestroy()
    {
        _isCapturing = false;
        if (_webCamTexture != null)
        {
            _webCamTexture.Stop();
        }
    }

    [Serializable]
    private class CameraPayload
    {
        public string type;
        public string data;
    }
}
