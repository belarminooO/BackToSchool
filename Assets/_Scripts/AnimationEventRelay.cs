using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerActionsInput _actionsInput;

    private PlayerActionsInput ActionsInput
    {
        get
        {
            if (_actionsInput == null)
                _actionsInput = GetComponentInParent<PlayerActionsInput>(true); 
            return _actionsInput;
        }
    }

    public void SetAttackPressedFalse()
    {
        ActionsInput?.SetAttackPressedFalse();
    }

    public void SetGatherPressedFalse()
    {
        ActionsInput?.SetGatherPressedFalse();
    }
}
