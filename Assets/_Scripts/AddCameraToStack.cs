using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AddCameraToStack : MonoBehaviour
{
    private Camera overlayCamera;
    void OnEnable()
    {
        overlayCamera = GetComponent<Camera>();
        if (overlayCamera != null && Camera.main != null)
        {
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            if (cameraData != null && !cameraData.cameraStack.Contains(overlayCamera))
            {
                cameraData.cameraStack.Add(overlayCamera);
            }
        }
    }

    void OnDisable()
    {
        if (overlayCamera != null && Camera.main != null)
        {
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.cameraStack.Remove(overlayCamera);
            }
        }
    }
}
