using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerActionsInput _actionsInput;

    private void Awake() => TryFindActionsInput();
    
    public void TryFindActionsInput()
    {
        _actionsInput = GetComponentInParent<PlayerActionsInput>();
        Debug.Log($"[AnimationEventRelay] Awake on '{gameObject.name}' | _actionsInput={(_actionsInput == null ? "NULL ← BUG" : _actionsInput.gameObject.name)}", gameObject);
        if (_actionsInput == null)
            Debug.LogWarning($"[AnimationEventRelay] Could not find PlayerActionsInput in parent of '{gameObject.name}'!");
    }

    public void SetAttackPressedFalse() => _actionsInput?.SetAttackPressedFalse();
    public void SetGatherPressedFalse() => _actionsInput?.SetGatherPressedFalse();
}
