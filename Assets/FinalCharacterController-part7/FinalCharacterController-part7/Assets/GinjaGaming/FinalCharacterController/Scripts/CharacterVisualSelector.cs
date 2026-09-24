using UnityEngine;

namespace GinjaGaming.FinalCharacterController
{
    public class CharacterVisualSelector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private Transform modelContainer;
        
        [Header("Auto-Configuration")]
        [SerializeField] private Animator mainAnimator;

        private void Reset()
        {
            mainAnimator = GetComponent<Animator>();
            // Try to find a child named "Mesh" or similar if Not assigned
            if (modelContainer == null) modelContainer = transform;
        }

        [ContextMenu("Swap Visuals")]
        public void SwapVisuals()
        {
            if (characterPrefab == null)
            {
                Debug.LogError("No character prefab assigned!");
                return;
            }

            if (mainAnimator == null) mainAnimator = GetComponent<Animator>();

            // 1. Hide/Destroy old children that are not important systems
            foreach (Transform child in transform)
            {
                // Logic to avoid hiding cameras or important inputs
                if (child.name == "Armature" || child.name == "Mesh" || child.name == "Armors" || child.name.Contains("man") || child.name.Contains("woman"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            // 2. Instantiate new model
            GameObject newModel = Instantiate(characterPrefab, transform);
            newModel.name = characterPrefab.name;
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;

            // 3. Update Animator Avatar
            Animator newModelAnimator = newModel.GetComponent<Animator>();
            if (newModelAnimator != null)
            {
                if (mainAnimator != null)
                {
                    mainAnimator.avatar = newModelAnimator.avatar;
                }
                newModelAnimator.enabled = false; // Disable the child animator
            }
            
            Debug.Log($"Visual swapped to: {newModel.name}. Avatar updated.");
        }

        public void SelectCharacter(int index, GameObject[] prefabs)
        {
            if (index < 0 || index >= prefabs.Length) return;
            characterPrefab = prefabs[index];
            SwapVisuals();
        }
    }
}
