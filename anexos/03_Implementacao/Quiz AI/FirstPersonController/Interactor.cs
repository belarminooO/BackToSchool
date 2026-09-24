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
        _playerState = GetComponentInParent<PlayerState>() ?? GetComponent<PlayerState>();

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.performed += OnInteract;
    }

    private void OnEnable() => interactAction.Enable();

    private void OnDisable() => interactAction.Disable();

    private void OnDestroy() => interactAction.performed -= OnInteract;

    private void LateUpdate()
    {
        if (e_indicator == null)
        {
            return;
        }

        Transform source = GetSource();
        if (source == null)
        {
            e_indicator.SetActive(false);
            return;
        }

        e_indicator.SetActive(HasInteractableInFront(source));
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Transform source = GetSource();
        if (source == null)
        {
            return;
        }

        if (TryInteract(source))
        {
            return;
        }

        if (IsSitting() && _playerController != null)
        {
            _playerController.TryStandUp();
        }
    }

    private bool HasInteractableInFront(Transform source)
    {
        foreach (var hit in GetHits(source))
        {
            if (IsPartOfPlayer(hit.collider.gameObject))
            {
                continue;
            }

            if (TryGetInteractable(hit.collider.gameObject, out _))
            {
                return true;
            }

            if (!hit.collider.isTrigger)
            {
                break;
            }
        }

        return false;
    }

    private bool TryInteract(Transform source)
    {
        foreach (var hit in GetHits(source))
        {
            if (IsPartOfPlayer(hit.collider.gameObject))
            {
                continue;
            }

            if (TryGetInteractable(hit.collider.gameObject, out IInteractable interactable))
            {
                interactable.Interact(gameObject);
                return true;
            }

            if (!hit.collider.isTrigger)
            {
                break;
            }
        }

        return false;
    }

    private RaycastHit[] GetHits(Transform source)
    {
        Ray ray = new Ray(source.position, source.forward);
        int layerMask = interactableLayers | (1 << 0) | (1 << 6);
        RaycastHit[] hits = Physics.RaycastAll(ray, InteractRange, layerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }

    private static bool TryGetInteractable(GameObject hitObject, out IInteractable interactable)
    {
        interactable = hitObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            return true;
        }

        interactable = hitObject.GetComponentInChildren<IInteractable>();
        return interactable != null;
    }

    private bool IsSitting()
    {
        return _playerState != null &&
               _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting;
    }

    private bool IsPartOfPlayer(GameObject obj)
    {
        Transform root = _playerController != null ? _playerController.transform : transform.root;
        return obj.transform == root || obj.transform.IsChildOf(root);
    }

    private Transform GetSource()
    {
        if (InteractorSource != null)
        {
            return InteractorSource;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }
}
