using UnityEngine;

public class RandomAnimationOffset : MonoBehaviour
{
    private void Start()
    {
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            animator.Play(state.fullPathHash, -1, Random.Range(0f, 1f));
        }
    }
}
