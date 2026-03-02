using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;

using Unity.Cinemachine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class MyCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Cinemachine Cameras")]
    public CinemachineCamera FirstPersonCamera;
    public CinemachineCamera StaticCamera;

    private bool _isFirstPerson = true; 
    
    [Header("Camera Configuration")]
    [Tooltip("Assign an empty GameObject placed at the player's head. Cinemachine will follow/look at this.")]
    public Transform CameraTarget; 
    
    [Header("Input Actions")]
    public InputActionReference MoveAction;
    public InputActionReference LookAction; 
    public InputActionReference JumpAction;
    public InputActionReference CameraDebugViewAction;

    [Header("Movement Settings")]
    public float MaxMoveSpeed = 10f;
    public float MovementSharpness = 15f;
    public float JumpSpeed = 10f;
    public float Gravity = 30f;

    [Header("Look Settings")]
    public float LookSensitivity = 0.5f; // Adjust if mouse is too fast/slow
    public float MinPitch = -89f;
    public float MaxPitch = 89f;

    private Vector3 _moveInputVector;
    private Vector2 _lookInputVector;
    private float _targetPitch;
    private float _targetYaw;
    private bool _jumpRequested;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (MoveAction) MoveAction.action.Enable();
        if (LookAction) LookAction.action.Enable();
        if (JumpAction) JumpAction.action.Enable();
        if (CameraDebugViewAction) CameraDebugViewAction.action.Enable();

        // Initialize our target rotation to the character's starting rotation
        _targetYaw = Motor.TransientRotation.eulerAngles.y;

        // Static Camera Debug View
        if (FirstPersonCamera != null)
        {
            FirstPersonCamera.Priority = 10;
        }
        if (StaticCamera != null)
        {
            StaticCamera.Priority = 0;
        }
    }

    private void Update()
    {
        if (MoveAction == null || JumpAction == null || LookAction == null) return;

        // 1. Read Inputs using New Input System
        Vector2 moveInput = MoveAction.action.ReadValue<Vector2>();
        _lookInputVector = LookAction.action.ReadValue<Vector2>();

        // 2. Accumulate Yaw (Left/Right) directly from mouse delta
        _targetYaw += _lookInputVector.x * LookSensitivity;
        
        // 3. Calculate movement direction relative to our *intended* rotation
        Quaternion targetRotation = Quaternion.Euler(0, _targetYaw, 0);
        Vector3 motorForward = targetRotation * Vector3.forward;
        Vector3 motorRight = targetRotation * Vector3.right;
        
        _moveInputVector = (motorForward * moveInput.y + motorRight * moveInput.x).normalized;

        if (JumpAction.action.WasPressedThisFrame())
        {
            _jumpRequested = true;
        }

        if (CameraDebugViewAction != null && CameraDebugViewAction.action.WasPressedThisFrame())
        {
            ToggleCameraView();
        }
    }

    private void LateUpdate()
    {
        // 3. Handle First-Person Camera Pitch (Up/Down)
        _targetPitch -= _lookInputVector.y * LookSensitivity;
        _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);

        if (CameraTarget != null)
        {
            CameraTarget.localEulerAngles = new Vector3(_targetPitch, 0f, 0f);
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // 4. Set Character Yaw (Left/Right) deterministically 
        currentRotation = Quaternion.Euler(0f, _targetYaw, 0f);
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

    private void ToggleCameraView()
    {
        if (FirstPersonCamera == null || StaticCamera == null) return;
        _isFirstPerson = !_isFirstPerson;
        if (_isFirstPerson)
        {
            FirstPersonCamera.Priority = 10;
            StaticCamera.Priority = 0;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            FirstPersonCamera.Priority = 0;
            StaticCamera.Priority = 10;
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
