using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using UnityEngine;
using UnityEngine.Rendering;

namespace GinjaGaming.FinalCharacterController
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float locomotionBlendSpeed = 4f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private PlayerController _playerController;
        private PlayerActionsInput _playerActionsInput;
        private FirstPersonNetworkState _networkState;

        // Locomotion
        private static int inputXHash = Animator.StringToHash("inputX");
        private static int inputYHash = Animator.StringToHash("inputY");
        private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
        private static int isIdlingHash = Animator.StringToHash("isIdling");
        private static int isGroundedHash = Animator.StringToHash("isGrounded");
        private static int isFallingHash = Animator.StringToHash("isFalling");
        private static int isJumpingHash = Animator.StringToHash("isJumping");
        private static int isCrouchingHash = Animator.StringToHash("isCrouching");
        private static int isSittingHash = Animator.StringToHash("isSittingBool");

        // Actions
        private static int isAttackingHash = Animator.StringToHash("isAttacking");
        private static int isGatheringHash = Animator.StringToHash("isGathering");
        private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
        private int[] actionHashes;

        // Camera/Rotation
        private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
        private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");

        private Vector3 _currentBlendInput = Vector3.zero;
        private float _sprintMaxBlendValue = 1.5f;
        private float _runMaxBlendValue = 1.0f;
        private float _walkMaxBlendValue = 0.5f;

        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _playerController = GetComponent<PlayerController>();
            _playerActionsInput = GetComponent<PlayerActionsInput>();
            _networkState = GetComponent<FirstPersonNetworkState>();

            // Must match PurrNet.NetworkAnimator on the same object, or auto-sync will never see parameter changes.
            var netAnimator = GetComponent<NetworkAnimator>();
            if (netAnimator != null && netAnimator.animator != null)
                _animator = netAnimator.animator;
            
            actionHashes = new int[] { isGatheringHash };
        }

        private void Update()
        {
            UpdateAnimationState();
        }

        // private void UpdateAnimationState()
        // {
           
        //     PlayerMovementState currentState = (_networkState != null && !_networkState.isOwner)
        //         ? _networkState.SyncedMovementState
        //         : _playerState.CurrentPlayerMovementState;

        //     bool isIdling = currentState == PlayerMovementState.Idling;
        //     bool isRunning = currentState == PlayerMovementState.Running;
        //     bool isSprinting = currentState == PlayerMovementState.Sprinting;
        //     bool isJumping = currentState == PlayerMovementState.Jumping;
        //     bool isFalling = currentState == PlayerMovementState.Falling;
        //     bool isCrouching = currentState == PlayerMovementState.Crouching;
        //     bool isSitting = currentState == PlayerMovementState.Sitting;
        //     bool isGrounded = _playerState.InGroundedState();
        //     bool isPlayingAction = actionHashes.Any(hash => _animator.GetBool(hash));

        //     // DEBUG
        //     // if (isCrouching || _playerLocomotionInput.CrouchToggledOn)
        //     // {
        //     //     Debug.Log($"[CrouchDebug] State: {_playerState.CurrentPlayerMovementState}, InputToggle: {_playerLocomotionInput.CrouchToggledOn}, isGrounded: {isGrounded}");
        //     // }

        //     if (isCrouching) {
        //         Debug.Log($"[CrouchInput] MovementInput: {_playerLocomotionInput.MovementInput}");
        //     }

        //     bool isRunBlendValue = isRunning || isJumping || isFalling;

        //     Vector2 moveInput = _playerLocomotionInput.MovementInput;
        //     if (_networkState != null && !_networkState.isOwner)
        //     {
        //         moveInput = _networkState.SyncedMovementInput;
        //     }

        //     Vector2 inputTarget = isSprinting ? moveInput * _sprintMaxBlendValue : isRunBlendValue ? moveInput * _runMaxBlendValue : moveInput * _walkMaxBlendValue;

        //     // Vector2 inputTarget = isSprinting ? _playerLocomotionInput.MovementInput * _sprintMaxBlendValue :
        //     //                       isRunBlendValue ? _playerLocomotionInput.MovementInput * _runMaxBlendValue : 
        //     //                                         _playerLocomotionInput.MovementInput * _walkMaxBlendValue;

        //     _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

        //     _animator.SetBool(isGroundedHash, isGrounded);
        //     _animator.SetBool(isIdlingHash, isIdling);
        //     _animator.SetBool(isFallingHash, isFalling);
        //     _animator.SetBool(isJumpingHash, isJumping);
        //     _animator.SetBool(isCrouchingHash, isCrouching);
        //     _animator.SetBool(isSittingHash, isSitting);

        //     //_animator.SetBool(isRotatingToTargetHash, _playerController.IsRotatingToTarget);
            
        //     bool isRotating = _playerController != null && _playerController.enabled && _playerController.IsRotatingToTarget;
        //     _animator.SetBool(isRotatingToTargetHash, isRotating);

        //     _animator.SetBool(isAttackingHash, _playerActionsInput.AttackPressed);
        //     _animator.SetBool(isGatheringHash, _playerActionsInput.GatherPressed);
        //     _animator.SetBool(isPlayingActionHash, isPlayingAction);

        //     _animator.SetFloat(inputXHash, _currentBlendInput.x);
        //     _animator.SetFloat(inputYHash, _currentBlendInput.y);
        //     _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
            
        //     //_animator.SetFloat(rotationMismatchHash, _playerController.RotationMismatch);

        //     float rotMismatch = _playerController != null && _playerController.enabled ? _playerController.RotationMismatch : 0f;
        //     _animator.SetFloat(rotationMismatchHash, rotMismatch);
        
        // }
        private void UpdateAnimationState()
        {
            bool isOwner = _networkState == null || _networkState.isOwner;
            PlayerMovementState currentState = isOwner ? _playerState.CurrentPlayerMovementState : _networkState.SyncedMovementState;
            
            bool isIdling = currentState == PlayerMovementState.Idling;
            bool isRunning = currentState == PlayerMovementState.Running;
            bool isSprinting = currentState == PlayerMovementState.Sprinting;
            bool isJumping = currentState == PlayerMovementState.Jumping;
            bool isFalling = currentState == PlayerMovementState.Falling;
            bool isCrouching = currentState == PlayerMovementState.Crouching;
            bool isSitting = currentState == PlayerMovementState.Sitting;
            bool isGrounded = _playerState.InGroundedState();

            Vector2 moveInput = isOwner ? (_playerLocomotionInput != null ? _playerLocomotionInput.MovementInput : Vector2.zero) : _networkState.SyncedMovementInput;
            bool isRunBlendValue = isRunning || isJumping || isFalling;
            Vector2 inputTarget = isSprinting ? moveInput * _sprintMaxBlendValue : isRunBlendValue ? moveInput * _runMaxBlendValue : moveInput * _walkMaxBlendValue;
            _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
            
            _animator.SetBool(isGroundedHash, isGrounded);
            _animator.SetBool(isIdlingHash, isIdling);
            _animator.SetBool(isFallingHash, isFalling);
            _animator.SetBool(isJumpingHash, isJumping);
            _animator.SetBool(isCrouchingHash, isCrouching);
            _animator.SetBool(isSittingHash, isSitting);
            _animator.SetFloat(inputXHash, _currentBlendInput.x);
            _animator.SetFloat(inputYHash, _currentBlendInput.y);
            _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);

            bool attackPressed = isOwner ? (_playerActionsInput != null && _playerActionsInput.AttackPressed) : (_networkState != null && _networkState.SyncedAttack);            
            bool gatherPressed = isOwner ? (_playerActionsInput != null && _playerActionsInput.GatherPressed) : (_networkState != null && _networkState.SyncedGather);
            bool isPlayingAction = actionHashes.Any(hash => _animator.GetBool(hash));
            
            if (attackPressed)
            {
                Debug.Log($"[PlayerAnimation] isAttacking=TRUE | isOwner={isOwner} | RawAttack={_playerActionsInput?.AttackPressed} | SyncedAttack={_networkState?.SyncedAttack} | networkStateNull={_networkState == null}", gameObject);
            }

            _animator.SetBool(isAttackingHash, attackPressed);
            _animator.SetBool(isGatheringHash, gatherPressed);
            _animator.SetBool(isPlayingActionHash, isPlayingAction);
            
            bool isRotating = isOwner && _playerController != null && _playerController.enabled && _playerController.IsRotatingToTarget;
            
            float rotMismatch = isOwner && _playerController != null && _playerController.enabled ? _playerController.RotationMismatch : 0f;
            _animator.SetBool(isRotatingToTargetHash, isRotating);
            _animator.SetFloat(rotationMismatchHash, rotMismatch);
        }
    
    }
}
