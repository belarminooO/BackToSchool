using UnityEngine;

public interface IPlayerController
{
    Team team { get; set; }
    bool hasAnswered { get; set; }
    int score { get; set; }
    bool IsMovementEnabled { get; set; }

    void AssignTeam(Team team);
    void ChangeScore(int points);
    void ShowVignette(bool correctAnswer);
    void TeleportTo(Vector3 position);

    bool isJailed { get; set; }

    Transform transform { get; }
    GameObject gameObject { get; }
}