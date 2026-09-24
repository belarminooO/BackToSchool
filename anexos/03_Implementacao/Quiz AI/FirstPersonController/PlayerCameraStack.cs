using System.Collections;
using UnityEngine;

public class PlayerCameraStack : MonoBehaviour
{
    [SerializeField] public Camera worldCamera;
    [SerializeField] public Camera pickUpCamera;
    [SerializeField] public Camera thirdPersonCamera;

    private bool _isLocalPlayer;

    public void initLocalPlayer()
    {
        if (_isLocalPlayer)
        {
            return;
        }

        _isLocalPlayer = true;
        StartCoroutine(WaitForGameState());
    }

    private void OnDisable() => UnsubscribeFromStateChanges();

    private void OnDestroy() => UnsubscribeFromStateChanges();

    private IEnumerator WaitForGameState()
    {
        while (QuizGameManager.instance == null || QuizGameManager.instance.currentStateName == null)
        {
            yield return null;
        }

        UnsubscribeFromStateChanges();
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
        {
            Camera.main.enabled = !isThirdPerson;
        }

        if (worldCamera != null)
        {
            worldCamera.enabled = false;
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = isThirdPerson;
            thirdPersonCamera.clearFlags = CameraClearFlags.Skybox;
            thirdPersonCamera.cullingMask = -1;
        }

        UpdateLocalBodyVisibility(isThirdPerson);
    }

    private void UnsubscribeFromStateChanges()
    {
        var manager = QuizGameManager.instance;
        if (manager != null && manager.currentStateName != null)
        {
            manager.currentStateName.onChanged -= OnStateChanged;
        }
    }

    private void UpdateLocalBodyVisibility(bool isThirdPerson)
    {
        FirstPersonNetworkState myState = GetComponentInParent<FirstPersonNetworkState>() ??
                                          Object.FindAnyObjectByType<FirstPersonNetworkState>();

        if (myState == null || !myState.isOwner)
        {
            return;
        }

        foreach (var renderer in myState.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            renderer.shadowCastingMode = isThirdPerson
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}
