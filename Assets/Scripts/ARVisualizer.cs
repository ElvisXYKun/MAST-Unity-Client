using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

/// <summary>
/// Configures AR Session and AR Camera background for iPad.
/// Integrates seamlessly with the scene's camera.
/// </summary>
public class ARVisualizer : MonoBehaviour
{
    [Header("AR Setup")]
    public bool enableAR = true;

    private ARSession _arSession;
    private XROrigin _xrOrigin;

    void Start()
    {
        if (enableAR)
        {
            SetupARComponents();
        }
    }

    void SetupARComponents()
    {
        // Ensure there is an ARSession object in the scene
        var sessionObj = GameObject.Find("AR Session");
        if (sessionObj == null)
        {
            sessionObj = new GameObject("AR Session");
            _arSession = sessionObj.AddComponent<ARSession>();
        }

        // Configure the Camera's XR Origin
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                xrOriginObj = new GameObject("XR Origin");
                _xrOrigin = xrOriginObj.AddComponent<XROrigin>();
                _xrOrigin.Camera = mainCam;

                // Add AR components for the camera background feed
                if (mainCam.gameObject.GetComponent<ARCameraManager>() == null)
                    mainCam.gameObject.AddComponent<ARCameraManager>();

                if (mainCam.gameObject.GetComponent<ARCameraBackground>() == null)
                    mainCam.gameObject.AddComponent<ARCameraBackground>();
            }
        }
        Debug.Log("[MAST AR] AR Foundation components initialized.");
    }
}
