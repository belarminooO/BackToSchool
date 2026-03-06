using UnityEngine;
using GinjaGaming.FinalCharacterController;

public interface IGameState
{
    void EnterState(QuizGameManager manager);
    void UpdateState(QuizGameManager manager);
    void ExitState(QuizGameManager manager);
}