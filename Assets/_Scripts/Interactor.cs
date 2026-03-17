using UnityEngine;
using UnityEngine.InputSystem;
using GinjaGaming.FinalCharacterController;

public interface IInteractable
{
    void Interact(GameObject interactor);
}

public class Interactor : MonoBehaviour
{
    [Header("References")]
    public Transform InteractorSource;
    public float InteractRange = 5f;
    public GameObject e_indicator;

    [Header("Layer Settings")]
    public LayerMask interactableLayers;
    private InputAction interactAction;

    private PlayerController _playerController;
    private PlayerState _playerState;

    private void Awake()
    {
        _playerController = GetComponentInParent<PlayerController>() ?? GetComponent<PlayerController>();
        _playerState      = GetComponentInParent<PlayerState>()      ?? GetComponent<PlayerState>();

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.performed += OnInteract;
    }

    private void OnEnable()  { interactAction.Enable(); }
    private void OnDisable() { interactAction.Disable(); }
    private void OnDestroy() { interactAction.performed -= OnInteract; }

    private void LateUpdate()
    {
        if (e_indicator == null) return;

        if (_playerState != null && _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting)
        {
            e_indicator.SetActive(true);
            return;
        }

        Transform source = GetSource();
        if (source == null) { e_indicator.SetActive(false); return; }

        bool canInteract = false;
        Ray r = new Ray(source.position, source.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange, interactableLayers, QueryTriggerInteraction.Collide))
            canInteract = hitInfo.collider.gameObject.TryGetComponent(out IInteractable _);

        e_indicator.SetActive(canInteract);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        bool isSitting = _playerState != null &&
                         _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting;

        Transform source = GetSource();
        if (source == null) return;

        Ray r = new Ray(source.position, source.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange, interactableLayers, QueryTriggerInteraction.Collide))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
            {
                interactObj.Interact(gameObject);
                return; 
            }
        }

        if (isSitting && _playerController != null)
            _playerController.TryStandUp();
    }

    private Transform GetSource()
    {
        if (InteractorSource != null) 
            return InteractorSource;
        if (Camera.main != null) 
            return Camera.main.transform;
        return null;
    }
}
