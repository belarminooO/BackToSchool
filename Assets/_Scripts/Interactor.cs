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
        if (e_indicator == null) 
            return;

        Transform source = GetSource();
        if (source == null) { e_indicator.SetActive(false); return; }

        bool canInteract = false;
        Ray r = new Ray(source.position, source.forward);
        
        // Use RaycastAll to find something interactable while skipping the player's own capsule/body
        // We include Layer 0 (Default) and Layer 6 (Interactable)
        int layerMask = interactableLayers | (1 << 0) | (1 << 6);
        RaycastHit[] hits = Physics.RaycastAll(r, InteractRange, layerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Skip if we hit ourselves
            if (IsPartOfPlayer(hit.collider.gameObject)) continue;

            // ULTIMATE SEARCH: check self, parents, AND children.
            // This covers every possible prefab structure (script at top, or script on a specific child part)
            if (hit.collider.gameObject.GetComponentInParent<IInteractable>() != null || 
                hit.collider.gameObject.GetComponentInChildren<IInteractable>() != null)
            {
                canInteract = true;
                break;
            }
            
            // If we hit a solid wall (no interaction anywhere in hierarchy), stop the ray
            if (!hit.collider.isTrigger) break; 
        }

        e_indicator.SetActive(canInteract);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        bool isSitting = _playerState != null &&
                         _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting;

        Transform source = GetSource();
        if (source == null) return;

        Ray r = new Ray(source.position, source.forward);
        int layerMask = interactableLayers | (1 << 0) | (1 << 6);
        RaycastHit[] hits = Physics.RaycastAll(r, InteractRange, layerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (IsPartOfPlayer(hit.collider.gameObject)) continue;

            // Try to find the interaction script anywhere in the hierarchy of the hit object
            IInteractable interactObj = hit.collider.gameObject.GetComponentInParent<IInteractable>();
            if (interactObj == null) interactObj = hit.collider.gameObject.GetComponentInChildren<IInteractable>();

            if (interactObj != null)
            {
                interactObj.Interact(gameObject);
                return; 
            }
            if (!hit.collider.isTrigger) break;
        }

        if (isSitting && _playerController != null)
            _playerController.TryStandUp();
    }

    private bool IsPartOfPlayer(GameObject obj)
    {
        // Check if the object is the player root or a child of it
        Transform root = _playerController != null ? _playerController.transform : transform.root;
        return obj.transform == root || obj.transform.IsChildOf(root);
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
