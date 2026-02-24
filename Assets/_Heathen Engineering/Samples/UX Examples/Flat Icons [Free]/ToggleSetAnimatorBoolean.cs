using UnityEngine;
using TMPro;

namespace HeathenEngineering.UX.Samples
{
    public class ToggleSetAnimatorBoolean : MonoBehaviour
    {
        public AudioSource audioSource;
        public TextMeshProUGUI text; 
        public float threshold = 0.1f;

        public Animator animator;
        public string booleanName;

        public void SetBoolean(bool value)
        {
            animator.SetBool(booleanName, value);
        }
    }
}
