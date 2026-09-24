using UnityEngine;
using PurrNet;
using GinjaGaming.FinalCharacterController;

public class AnswerButtonManager : NetworkBehaviour
{
    public AnswerButton[] answerButtons;

    public SyncVar<NetworkIdentity> ownerPlayer = new SyncVar<NetworkIdentity>();

    public AnswerButton currentSelectedButton;

    public int lockedAnswerIndex { get; private set; } = -1; 

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

            return;
        }

        if (currentSelectedButton == newButton)
        {
            return; 
        }

        RPC_SubmitAnswerToServer(newButton.answerIndex, whoPressedIt.name);
    }

    [ServerRpc(requireOwnership: false)]
    private void RPC_SubmitAnswerToServer(int answerIndex, string whoPressedItName)
    {
        lockedAnswerIndex = answerIndex;

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
