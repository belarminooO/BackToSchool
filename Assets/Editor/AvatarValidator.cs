using UnityEngine;
using UnityEditor;

public class AvatarValidator : EditorWindow
{
    [MenuItem("Tools/Validate Avatar Mapping")]
    public static void Validate()
    {
        GameObject testBot = GameObject.Find("TestBot");
        if (testBot == null) return;

        Animator animator = testBot.GetComponent<Animator>();
        if (animator == null) return;

        Avatar avatar = animator.avatar;
        if (avatar == null)
        {
            Debug.LogError("No Avatar assigned to TestBot!");
            return;
        }

        if (!avatar.isHuman)
        {
            Debug.LogError("Avatar is NOT Humanoid!");
            return;
        }

        // Check if required bones are mapped
        HumanDescription desc = animator.avatar.humanDescription;
        Debug.Log("Avatar: " + avatar.name + " | Human bones mapped: " + desc.human.Length);
        
        foreach (var bone in desc.human)
        {
            Debug.Log("Bone: " + bone.humanName + " -> " + bone.boneName);
        }
    }
}
