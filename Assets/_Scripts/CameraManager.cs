using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera insideCam;   
    [SerializeField] private CinemachineCamera outsideCam; 
    
    private void Awake()
    {
        SetActiveCamera(outsideCam);
    }

    public void SetActiveCamera(CinemachineCamera cam)
    {
        insideCam.Priority = 5;
        outsideCam.Priority = 5;

        cam.Priority = 20;
    }
}
