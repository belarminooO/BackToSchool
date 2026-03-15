using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public interface IInteractable
{
    void Interact(GameObject interactor);
}


public class Interactor : MonoBehaviour
{
    public Transform InteractorSource;
    public float InteractRange = 5f;
    public GameObject e_indicator;

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

        interactAction.performed += OnInteract;
    }

    private void OnEnable()
    {
        interactAction.Enable();

    }

    private void OnDisable()
    {
        interactAction.Disable();
    }

    private void OnDestroy()
    {
        interactAction.performed -= OnInteract;
    }

    private void LateUpdate()
    {
        if (e_indicator == null)
        {
            return;
        }

        Transform source = InteractorSource;
        if (source == null && Camera.main != null)
        {
            source = Camera.main.transform;
        }

        if (source == null)
        {
            return;
        }

        bool canInteract = false;
        Ray r = new Ray(source.position, source.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interacatObj))
            {
                canInteract = true;
            }
        }

        e_indicator.SetActive(canInteract);

    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Transform source = InteractorSource;
        if (source == null && Camera.main != null)
        {
            source = Camera.main.transform;
        }

        if (source == null)
        {
            return;
        }

        Ray r = new Ray(source.position, source.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interacatObj))
            {
                interacatObj.Interact(gameObject);
            }
        }
    }


    //void Update()
    //{
    //    if(Input.GetKeyDown(KeyCode.E))
    //    {
    //        Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
    //        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange))
    //        {
    //            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interacatObj))
    //            {
    //                interacatObj.Interact();
    //            }
    //        }
    //    }
    //}
}