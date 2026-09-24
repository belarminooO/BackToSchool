using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GinjaGaming.FinalCharacterController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [Header("Quiz System Data")]
        public Team team { get; set; } = Team.None;
        public bool hasAnswered { get; set; } = false;
        public int score { get; set; } = 0;
        public bool isJailed { get; set; } = false;
        
        public GameObject vignetteObject;
        public ChangeColor changeColorScript;

        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Camera _playerCamera;
        public Camera PlayerCamera => _playerCamera;
        [SerializeField] private Transform _cameraTarget;
        public Transform CameraTarget => _cameraTarget;
        private CapsuleCollider _bodyCollider;

        public float RotationMismatch { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;

        [Header("Base Movement")]
        public float walkAcceleration = 25f;
        public float walkSpeed = 2f;
        public float runAcceleration = 35f;
        public float runSpeed = 4f;
        public float sprintAcceleration = 50f;
        public float sprintSpeed = 7f;
        public float crouchAcceleration = 45f; // Boosted even more
        public float crouchSpeed = 2.0f; // Slightly faster for testing
        public float inAirAcceleration = 25f;
        public float drag = 20f;
        public float inAirDrag = 5f;
        public float gravity = 25f;
        public float terminalVelocity = 50f;
        public float jumpSpeed = 0.8f;
        public float movingThreshold = 0.01f;

        [Header("Height Settings")]
        public float standingHeight = 1.8f;
        public float crouchingHeight = 1.2f;
        [Tooltip("Radius of the collision body (CapsuleCollider) used for player-to-player pushing")]
        public float bodyColliderRadius = 0.3f;

        [Header("State Data")]
        public PlayerMovementState playerMovementState;
        [Header("Animation")]
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.67f;

        [Header("Impact Settings")]
        public float maxExternalForce = 18f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        [Header("Environment Details")]
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _interactionRadius = 1.2f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerActionsInput _playerActionsInput;
        private PlayerState _playerState;
        private ChairInteraction _currentChair;

        private Vector3 _velocity = Vector3.zero;
        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private bool _jumpedLastFrame = false;
        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        private float _antiBump;
        private float _stepOffset;

        private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;
        private float _interactionCooldownTimer = 0f;
        private Vector3 _externalForce = Vector3.zero;

        public void ApplyKnockback(Vector3 force)
        {
            _externalForce = Vector3.ClampMagnitude(_externalForce + force, maxExternalForce);
        }

        public bool IsMovementEnabled
        {
        get => enabled;
        set => enabled = value;
        }

        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerActionsInput = GetComponent<PlayerActionsInput>();
            _playerState = GetComponent<PlayerState>();
            _bodyCollider = GetComponent<CapsuleCollider>();

            _antiBump = sprintSpeed;
            _stepOffset = _characterController.stepOffset;

            if (_playerCamera == null)
                _playerCamera = Camera.main;


            // // Hide player layer from camera (wasn't working in editor)
            // if (Camera.main != null) {
            //     int playerLayer = LayerMask.NameToLayer("Player");
            //     Camera.main.cullingMask &= ~(1 << playerLayer);
            // }
        }
        #endregion

        // Cursor Locking
        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #region Update Logic
        private void Update()
        {
            HandleInteraction();

            if (_playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting)
                return;

            UpdateMovementState();

            HandleVerticalMovement();
            HandleLateralMovement();
        }

        private void HandleInteraction()
        {
            if (_interactionCooldownTimer > 0)
                _interactionCooldownTimer -= Time.deltaTime;

            if (!_playerActionsInput.GatherPressed) return;

            if (_interactionCooldownTimer > 0)
            {
                _playerActionsInput.SetGatherPressedFalse();
                return;
            }

            _playerActionsInput.SetGatherPressedFalse();
            _interactionCooldownTimer = 0.4f;
            // Chair sit/stand is fully handled by Interactor to avoid same-frame double-fire.
        }

        public void SetCurrentChair(ChairInteraction chair) { _currentChair = chair; }
        public ChairInteraction CurrentChair => _currentChair;
        public bool IsSitting => _currentChair != null;

        public void TryStandUp()
        {
            if (_currentChair != null)
            {
                _currentChair.StandUp();
                _currentChair = null;
            }
        }

        private void UpdateMovementState()
        {
            _lastMovementState = _playerState.CurrentPlayerMovementState;

            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;             //order
            bool isMovingLaterally = IsMovingLaterally();                                            //matters
            bool isSprinting = _playerLocomotionInput.SprintToggledOn && isMovingLaterally;          //order
            bool isWalking = isMovingLaterally && (!canRun || _playerLocomotionInput.WalkToggledOn); //matters
            bool isCrouching = _playerLocomotionInput.CrouchToggledOn;
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState = isCrouching ? PlayerMovementState.Crouching :
                                               isWalking ? PlayerMovementState.Walking :
                                               isSprinting ? PlayerMovementState.Sprinting :
                                               isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;

            _playerState.SetPlayerMovementState(lateralState);
            playerMovementState = _playerState.CurrentPlayerMovementState;

            if (_playerLocomotionInput.CrouchToggledOn)
            {
                // Temporarily disable airborne overwrite when crouching to test consistency
                // (or just log it)
                // Debug.Log($"[StateDebug] isCrouching Input: true, lateralState: {lateralState}, finalState: {_playerState.CurrentPlayerMovementState}, isGrounded: {isGrounded}");
            }

            // Control Airborn State
            if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else
            {
                _characterController.stepOffset = _stepOffset;
            }
        }

        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.InGroundedState();

            _verticalVelocity -= gravity * Time.deltaTime;

            if (isGrounded && _verticalVelocity < 0)
                _verticalVelocity = -_antiBump;

            if (_playerLocomotionInput.JumpPressed && isGrounded)
            {
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                _jumpedLastFrame = true;
            }

            if (_playerState.IsStateGroundedState(_lastMovementState) && !isGrounded)
            {
                _verticalVelocity += _antiBump;
            }

            // Clamp at terminal velocity
            if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
            {
                _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
            }
        }

        private void HandleLateralMovement()
        {
            // Create quick references for current state
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = _playerState.InGroundedState();
            bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;
            bool isCrouching = _playerState.CurrentPlayerMovementState == PlayerMovementState.Crouching; // Added for consistency

            // State dependent acceleration and speed
            float lateralAcceleration = !isGrounded ? inAirAcceleration :
                                        isCrouching ? crouchAcceleration :
                                        isWalking ? walkAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;

            float clampLateralMagnitude = !isGrounded ? sprintSpeed :
                                          isCrouching ? crouchSpeed :
                                          isWalking ? walkSpeed :
                                          isSprinting ? sprintSpeed : runSpeed;

            Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + cameraForwardXZ * _playerLocomotionInput.MovementInput.y;

            _velocity.y = 0;
            _velocity += movementDirection * lateralAcceleration * Time.deltaTime;

            float dragMagnitude = isGrounded ? drag : inAirDrag;
            Vector3 currentDrag = _velocity.normalized * dragMagnitude * Time.deltaTime;
            _velocity = (_velocity.magnitude > dragMagnitude * Time.deltaTime) ? _velocity - currentDrag : Vector3.zero;
            _velocity = Vector3.ClampMagnitude(_velocity, clampLateralMagnitude);

            Vector3 finalVelocity = _velocity;
            finalVelocity.y = _verticalVelocity;
            finalVelocity = !isGrounded ? HandleSteepWalls(finalVelocity) : finalVelocity;

            if (_externalForce.magnitude > 0.1f)
            {
                finalVelocity += _externalForce;
                _externalForce = Vector3.Lerp(_externalForce, Vector3.zero, 5f * Time.deltaTime);
            }
            else
            {
                _externalForce = Vector3.zero;
            }

            // Move character (Unity suggests only calling this once per tick)
            _characterController.Move(finalVelocity * Time.deltaTime);
            
            // Sync internal velocity with real velocity if we hit something
            // if (_characterController.velocity.magnitude < _velocity.magnitude * 0.5f)
            // {
            //     _velocity = _characterController.velocity;
            //     _velocity.y = 0;
            // }

            // Handle CharacterController height
            bool isCrouchingState = playerMovementState == PlayerMovementState.Crouching;
            float targetHeight = isCrouchingState ? crouchingHeight : standingHeight;
            _characterController.height = Mathf.MoveTowards(_characterController.height, targetHeight, 10f * Time.deltaTime);
            _characterController.center = Vector3.up * (_characterController.height / 2f);
            
            // Sync extra body collider if it exists (for player-to-player collision)
            if (_bodyCollider != null)
            {
                _bodyCollider.height = _characterController.height;
                _bodyCollider.center = _characterController.center;
                _bodyCollider.radius = bodyColliderRadius;
            }
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;

            if (!validAngle && _verticalVelocity < 0f)
                velocity = Vector3.ProjectOnPlane(velocity, normal);

            return velocity;
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            UpdateCameraRotation();
        }

        private void UpdateCameraRotation()
        {
            _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

            _playerTargetRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;

            float rotationTolerance = 90f;
            bool isSitting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting;
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = _rotatingToTargetTimer > 0;

            if (isSitting)
            {
                // Skip player rotation while sitting
            }
            else if (!isIdling)
            {
                RotatePlayerToTarget();
            }
            // If rotation mismatch not within tolerance, or rotate to target is active, ROTATE
            else if (Mathf.Abs(RotationMismatch) > rotationTolerance || IsRotatingToTarget)
            {
                UpdateIdleRotation(rotationTolerance);
            }

            // _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

            if (_cameraTarget != null)
                _cameraTarget.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
            
            if (_cameraTarget != null)
            {
                bool isCrouching = _playerState.CurrentPlayerMovementState == PlayerMovementState.Crouching;
                float targetCameraY = isCrouching ? crouchingHeight - 0.3f : standingHeight - 0.2f;
                
                Vector3 localPos = _cameraTarget.localPosition;
                localPos.y = Mathf.MoveTowards(localPos.y, targetCameraY, 5f * Time.deltaTime);
                _cameraTarget.localPosition = localPos;
            }
            
            
            // Get angle between camera and player
            Vector3 camForwardProjectedXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            RotationMismatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            // Initiate new rotation direction
            if (Mathf.Abs(RotationMismatch) > rotationTolerance)
            {
                _rotatingToTargetTimer = rotateToTargetTime;
                _isRotatingClockwise = RotationMismatch > rotationTolerance;
            }
            _rotatingToTargetTimer -= Time.deltaTime;

            // Rotate player
            if (_isRotatingClockwise && RotationMismatch > 0f ||
                !_isRotatingClockwise && RotationMismatch < 0f)
            {
                RotatePlayerToTarget();
            }
        }

        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
        }
        #endregion

        #region State Checks
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);

            return lateralVelocity.magnitude > movingThreshold;
        }

        private bool IsGrounded()
        {
            bool grounded = _playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

            return grounded;
        }

        private bool IsGroundedWhileGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.radius, transform.position.z);

            bool grounded = Physics.CheckSphere(spherePosition, _characterController.radius, _groundLayers, QueryTriggerInteraction.Ignore);

            return grounded;
        }

        private bool IsGroundedWhileAirborne()
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;

            return _characterController.isGrounded && validAngle;
        }

        private bool CanRun()
        {
            return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
        }
        #endregion

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
                if (correctAnswer) changeColorScript.ChangeToGreen();
                else changeColorScript.ChangeToRed();
                Invoke(nameof(HideVignette), 2f);
            }
        }

        private void HideVignette()
        {
            if (vignetteObject != null) 
                vignetteObject.SetActive(false);
        }

        public void TeleportTo(Vector3 position)
        {
            ClearSittingStateBeforeTeleport();

            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;

            _velocity = Vector3.zero;
            _verticalVelocity = 0f;
            _externalForce = Vector3.zero;
            _jumpedLastFrame = false;
            _interactionCooldownTimer = 0f;
            _rotatingToTargetTimer = 0f;

            if (_playerState != null && _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting)
                _playerState.SetPlayerMovementState(PlayerMovementState.Idling);
        }

        private void ClearSittingStateBeforeTeleport()
        {
            if (_currentChair != null)
            {
                _currentChair.StandUp();
                _currentChair = null;
                return;
            }

            if (_playerState != null && _playerState.CurrentPlayerMovementState == PlayerMovementState.Sitting)
                _playerState.SetPlayerMovementState(PlayerMovementState.Idling);

            if (_characterController != null && !_characterController.enabled)
                _characterController.enabled = true;
        }
    }
}
