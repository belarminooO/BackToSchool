using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using GinjaGaming.FinalCharacterController;
using Unity.Cinemachine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class MyCharacterController : MonoBehaviour, ICharacterController, IPlayerController
{

    [Header("Vignette Feedback")]
    public GameObject vignetteObject;
    public ChangeColor changeColorScript;

    [Header("Quiz System Data")]
    public Team team { get; set; } = Team.None;
    public bool hasAnswered { get; set; } = false;
    public int score { get; set; } = 0;

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
    public bool IsMovementEnabled { get; set; } = true;

    [Header("Look Settings")]
    public float LookSensitivity = 0.5f; 
    public float MinPitch = -89f;
    public float MaxPitch = 89f;

    private Vector3 _moveInputVector;
    private Vector2 _lookInputVector;
    private float _targetPitch;
    private float _targetYaw;
    private bool _jumpRequested;
    private GeneratedFootstepEmitter _footstepEmitter;
    private Vector3 _previousFootstepPosition;
    private bool _hasFootstepPosition;

    public bool isJailed { get; set; } = false;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;
        GeneratedQuizSfx.EnsureExists();

        _footstepEmitter = GetComponent<GeneratedFootstepEmitter>();
        if (_footstepEmitter == null)
            _footstepEmitter = gameObject.AddComponent<GeneratedFootstepEmitter>();
        _footstepEmitter.ConfigureForPlayer();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (MoveAction) MoveAction.action.Enable();
        if (LookAction) LookAction.action.Enable();
        if (JumpAction) JumpAction.action.Enable();
        if (CameraDebugViewAction) CameraDebugViewAction.action.Enable();

        _targetYaw = Motor.TransientRotation.eulerAngles.y;

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
        if (MoveAction == null || JumpAction == null || LookAction == null) {
            return;
        }

        if (!IsMovementEnabled)
        {
            _moveInputVector = Vector3.zero;
            _lookInputVector = Vector2.zero;
            return;
        }

        Vector2 moveInput = MoveAction.action.ReadValue<Vector2>();
        _lookInputVector = LookAction.action.ReadValue<Vector2>();

        _targetYaw += _lookInputVector.x * LookSensitivity;

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

        _targetPitch -= _lookInputVector.y * LookSensitivity;
        _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);

        if (CameraTarget != null)
        {
            CameraTarget.localEulerAngles = new Vector3(_targetPitch, 0f, 0f);
        }

        UpdateFootsteps();
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {

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

    public void AssignTeam(Team assignedTeam)
    {
        team = assignedTeam;
    }
    public void ChangeScore(int points)
    {
        score += points;
    }

    public void ShowVignette(bool correctAnswer)
    {
        if (vignetteObject != null && changeColorScript != null)
        {

            vignetteObject.SetActive(true);

            if (correctAnswer)
            {
                changeColorScript.ChangeToGreen();
            }
            else
            {
                changeColorScript.ChangeToRed();
            }

            Invoke(nameof(HideVignette), 2f);

        }
    }

    private void HideVignette()
    {
        if (vignetteObject != null)
        {
            vignetteObject.SetActive(false);
        }
    }

    public void TeleportTo(Vector3 position)
    {
        Motor.SetPosition(position);
        _previousFootstepPosition = position;
        _hasFootstepPosition = false;

        if (_footstepEmitter != null)
            _footstepEmitter.ResetCycle();
    }

    private void UpdateFootsteps()
    {
        if (_footstepEmitter == null)
            return;

        Vector3 currentPosition = transform.position;
        if (!_hasFootstepPosition)
        {
            _previousFootstepPosition = currentPosition;
            _hasFootstepPosition = true;
            return;
        }

        Vector3 frameDelta = currentPosition - _previousFootstepPosition;
        _previousFootstepPosition = currentPosition;

        Vector3 planarDelta = Vector3.ProjectOnPlane(frameDelta, Vector3.up);
        if (planarDelta.sqrMagnitude > 16f)
        {
            _footstepEmitter.ResetCycle();
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = planarDelta.magnitude / deltaTime;
        bool isGrounded = Motor == null || Motor.GroundingStatus.IsStableOnGround;
        _footstepEmitter.Tick(isGrounded, speed, deltaTime);
    }

}
