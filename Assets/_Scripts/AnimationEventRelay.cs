using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerActionsInput _actionsInput;

    private void Awake()
    {
        _actionsInput = GetComponentInParent<PlayerActionsInput>();
    }

    // Called by the animation event on PushMixamo
    public void SetAttackPressedFalse()
    {
        _actionsInput?.SetAttackPressedFalse();
    }

    // Called by the animation event on Gather animations (if any)
    public void SetGatherPressedFalse()
    {
        _actionsInput?.SetGatherPressedFalse();
    }
}