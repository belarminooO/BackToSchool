using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerCameraStack : MonoBehaviour
{
    [SerializeField] public Camera worldCamera;
    [SerializeField] public Camera pickUpCamera;
    [SerializeField] public Camera thirdPersonCamera;

    private bool _isLocalPlayer = false;
    private bool _isThirdPerson = false;

    public void initLocalPlayer()
    {
        _isLocalPlayer = true;
        StartCoroutine(StateChanges());
    }

    private void OnDisable()
    {
        if (QuizGameManager.instance != null && QuizGameManager.instance.currentStateName != null)
            QuizGameManager.instance.currentStateName.onChanged -= OnStateChanged;
    }
    private void OnDestroy()
    {
        if (QuizGameManager.instance != null && QuizGameManager.instance.currentStateName != null)
            QuizGameManager.instance.currentStateName.onChanged -= OnStateChanged;
    }

    private IEnumerator StateChanges()
    {
        while (QuizGameManager.instance == null || QuizGameManager.instance.currentStateName == null)
            yield return null;

        QuizGameManager.instance.currentStateName.onChanged += OnStateChanged;
        OnStateChanged(QuizGameManager.instance.currentStateName.value);
    }

    private void OnStateChanged(string newState)
    {
        if (!_isLocalPlayer)
        {
            return;
        }

        bool isThirdPerson = newState == "ObstacleRaceStateNode";

        if (Camera.main != null) 
            Camera.main.enabled = !isThirdPerson;
        if (worldCamera != null) 
            worldCamera.enabled = false;
        if (thirdPersonCamera != null) {
            thirdPersonCamera.enabled = isThirdPerson;
            thirdPersonCamera.clearFlags = CameraClearFlags.Skybox;
            thirdPersonCamera.cullingMask = -1; 
        }

        FirstPersonNetworkState myState = GetComponentInParent<FirstPersonNetworkState>();
        if (myState == null) myState = Object.FindAnyObjectByType<FirstPersonNetworkState>();
        
        if (myState != null && myState.isOwner)
        {
            foreach (var renderer in myState.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.shadowCastingMode = isThirdPerson 
                    ? UnityEngine.Rendering.ShadowCastingMode.On 
                    : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }
}
