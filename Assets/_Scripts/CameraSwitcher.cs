using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineCamera outsideCamera;
    public CinemachineCamera insideCamera;

    [Header("Priority")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    void Start()
    {
        GoOutside();
    }

    public void GoInside()
    {
        SetPriority(insideCamera, activePriority);
        SetPriority(outsideCamera, inactivePriority);
    }
    public void GoOutside()
    {
        SetPriority(outsideCamera, activePriority);
        SetPriority(insideCamera, inactivePriority);
    }

    private void SetPriority(CinemachineCamera cam, int value)
    {
        cam.Priority = new PrioritySettings { 
            Enabled = true, 
            Value = value 
        };
    }
}