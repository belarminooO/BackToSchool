// removed Cinemachine namespace as it's not used here and is breaking build with 3.x versioning
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GinjaGaming.FinalCharacterController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionsMapActions
    {
        #region Class Variables
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private FirstPersonNetworkState _networkState;
        public bool GatherPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public event Action AttackStarted;
        private PlayerControls _ownControls;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _networkState = GetComponent<FirstPersonNetworkState>();
        }

        private void OnEnable()
        {
            TryInitializeControls();
        }

        private void TryInitializeControls()
        {
            if (_ownControls != null)
                return;

            if (_networkState != null && !_networkState.isOwner)
                return;

            _ownControls = new PlayerControls();
            _ownControls.PlayerActionsMap.Enable();
            _ownControls.PlayerActionsMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (_ownControls == null)
                return;
            _ownControls?.PlayerActionsMap.Disable();
            _ownControls?.PlayerActionsMap.RemoveCallbacks(this);
            _ownControls?.Dispose();
            _ownControls = null;
        }
        // private void OnEnable()
        // {
        //     if (PlayerInputManager.Instance?.PlayerControls == null)
        //     {
        //         Debug.LogError("Player controls is not initialized - cannot enable");
        //         return;
        //     }

        //     PlayerInputManager.Instance.PlayerControls.PlayerActionsMap.Enable();
        //     PlayerInputManager.Instance.PlayerControls.PlayerActionsMap.SetCallbacks(this);
        // }

        // private void OnDisable()
        // {
        //     if (PlayerInputManager.Instance?.PlayerControls == null)
        //     {
        //         Debug.LogError("Player controls is not initialized - cannot disable");
        //         return;
        //     }

        //     PlayerInputManager.Instance.PlayerControls.PlayerActionsMap.Disable();
        //     PlayerInputManager.Instance.PlayerControls.PlayerActionsMap.RemoveCallbacks(this);
        // }
        #endregion

        #region Update
        private void Update()
        {
            TryInitializeControls();

            if (_playerLocomotionInput.MovementInput != Vector2.zero ||
                _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling)
            {
                GatherPressed = false;
            }
        }

        public void SetGatherPressedFalse()
        {
            GatherPressed = false;
        }

        public void SetAttackPressedFalse()
        {
            Debug.Log($"[ActionsInput] SetAttackPressedFalse on '{gameObject.name}'", gameObject);
            AttackPressed = false;
        }
        #endregion

        #region Input Callbacks
        public void OnGathering(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            GatherPressed = true;
        }

        public void OnAttacking(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Debug.Log($"[ActionsInput] OnAttacking PERFORMED on '{gameObject.name}'", gameObject);

            AttackPressed = true;
            AttackStarted?.Invoke();
            CancelInvoke(nameof(SetAttackPressedFalse));
            Invoke(nameof(SetAttackPressedFalse), 0.8f);
        }
        #endregion
    }
}
