using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerActionsInput _actionsInput;

    private void Awake()
    {
        _actionsInput = GetComponentInParent<PlayerActionsInput>();
    }

    public void SetAttackPressedFalse()
    {
        _actionsInput?.SetAttackPressedFalse();
    }

    public void SetGatherPressedFalse()
    {
        _actionsInput?.SetGatherPressedFalse();
    }
}