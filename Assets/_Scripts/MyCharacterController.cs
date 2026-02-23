using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class MyCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public Transform CameraTransform;
    
    [Header("Input Actions")]
    public InputActionReference MoveAction;
    public InputActionReference JumpAction;

    [Header("Movement Settings")]
    public float MaxMoveSpeed = 10f;
    public float MovementSharpness = 15f;
    public float JumpSpeed = 10f;
    public float Gravity = 30f;

    private Vector3 _moveInputVector;
    private bool _jumpRequested;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (MoveAction == null || JumpAction == null) return;

        Vector2 moveInput = MoveAction.action.ReadValue<Vector2>();
        
        Vector3 cameraForward = Vector3.ProjectOnPlane(CameraTransform.forward, Motor.CharacterUp).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(CameraTransform.right, Motor.CharacterUp).normalized;
        
        _moveInputVector = cameraForward * moveInput.y + cameraRight * moveInput.x;

        if (JumpAction.action.WasPressedThisFrame())
        {
            _jumpRequested = true;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(CameraTransform.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude > 0f)
        {
            currentRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
            Vector3 targetVelocity = _moveInputVector * MaxMoveSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-MovementSharpness * deltaTime));

            if (_jumpRequested)
            {
                _jumpRequested = false;
                Motor.ForceUnground();
                currentVelocity += Motor.CharacterUp * JumpSpeed;
            }
        }
        else
        {
            currentVelocity -= Motor.CharacterUp * Gravity * deltaTime;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
}
