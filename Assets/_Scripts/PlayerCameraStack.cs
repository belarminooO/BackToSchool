using UnityEngine;
using UnityEngine.Rendering.Universal;
public class PlayerCameraStack : MonoBehaviour
{
    [SerializeField] public Camera worldCamera;
    [SerializeField] public Camera pickUpCamera;

    private void OnEnable()
    {
        var cameraData = Camera.main.GetUniversalAdditionalCameraData();
        if (cameraData != null )
        {
            if (worldCamera != null && !cameraData.cameraStack.Contains(worldCamera))
            {
                cameraData.cameraStack.Add(worldCamera);
            }

            if (pickUpCamera != null && !cameraData.cameraStack.Contains(pickUpCamera))
            {
                cameraData.cameraStack.Add(pickUpCamera);
            }
        }
    }

    private void OnDisable()
    {
        if (Camera.main != null)
        {
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                if (worldCamera != null && cameraData.cameraStack.Contains(worldCamera))
                {
                    cameraData.cameraStack.Remove(worldCamera);
                }
                if (pickUpCamera != null && cameraData.cameraStack.Contains(pickUpCamera))
                {
                    cameraData.cameraStack.Remove(pickUpCamera);
                }
            }
        }
    }
}