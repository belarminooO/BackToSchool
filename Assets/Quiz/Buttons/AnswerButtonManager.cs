using UnityEngine;
using PurrNet;
using GinjaGaming.FinalCharacterController;

public class AnswerButtonManager : NetworkBehaviour
{
    public AnswerButton[] answerButtons;

    public SyncVar<NetworkIdentity> ownerPlayer = new SyncVar<NetworkIdentity>();

    public AnswerButton currentSelectedButton;

    public int lockedAnswerIndex { get; private set; } = -1; // -1 = no answer

    // [ObserversRpc]
    // public void RPC_SetOwner(NetworkIdentity newOwner)
    // {
    //     ownerPlayer.value = newOwner;
    //     lockedAnswerIndex = -1;

    //     // Update a text canvas here to show the player's name or other info if needed <-----------
    // }

    [ObserversRpc]
    public void RPC_SetOwner(NetworkIdentity newOwner)
    {
        if (isServer)
            ownerPlayer.value = newOwner; 
        lockedAnswerIndex = -1;
    }

    public void SubmitAnswer(AnswerButton newButton, GameObject whoPressedIt) 
    {
        if (ownerPlayer == null)
        {
            Debug.LogError("Owner player is not set for this AnswerButtonManager.");
            return;
        }

        if (currentSelectedButton == newButton)
        {
            return; // No change in selection
        }

        RPC_SubmitAnswerToServer(newButton.answerIndex, whoPressedIt.name);
    }

    [ServerRpc(requireOwnership: false)]
    private void RPC_SubmitAnswerToServer(int answerIndex, string whoPressedItName)
    {
        lockedAnswerIndex = answerIndex;
        Debug.Log($"Player {whoPressedItName} submitted answer index {answerIndex} for player {ownerPlayer.name}");

        // TODO - Process the answer, update scores, and notify clients as needed

        RPC_AnimateButton(answerIndex);
    }

    [ObserversRpc]
    private void RPC_AnimateButton(int answerIndex)
    {
        AnswerButton buttonToPress = null;

        foreach(AnswerButton button in answerButtons)
        {
            if (button.answerIndex == answerIndex)
            {
                buttonToPress = button;
                break;
            }
        }

        if (buttonToPress == null)
        {
            return;
        }

        if(currentSelectedButton != null && currentSelectedButton != buttonToPress)
        {
            currentSelectedButton.ReleaseUp();
        }
        currentSelectedButton = buttonToPress;
        currentSelectedButton.PressDown();
    }

    [ObserversRpc]
    public void RPC_ResetButtons()
    {
        lockedAnswerIndex = -1;
        if (currentSelectedButton != null)
        {
            currentSelectedButton.ReleaseUp();
            currentSelectedButton = null;
        }
    }

    [ObserversRpc]
    public void RPC_ShowVignette(bool correctAnswer)
    {
        if (ownerPlayer.value != null && ownerPlayer.value.isOwner)
        {
            GeneratedQuizSfx.PlayAnswerResult(correctAnswer);

            var player = ownerPlayer.value.GetComponent<IPlayerController>();
            if (player != null)
                player.ShowVignette(correctAnswer);
        }
    }
}
