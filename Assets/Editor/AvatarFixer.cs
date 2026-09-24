using UnityEngine;
using UnityEditor;
using System.Linq;

public class AvatarFixer : EditorWindow
{
    [MenuItem("Tools/Fix Character Avatar")]
    public static void FixAvatar()
    {
        GameObject testBot = GameObject.Find("TestBot");
        if (testBot == null)
        {
            Debug.LogError("TestBot not found in scene!");
            return;
        }

        Animator animator = testBot.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on TestBot!");
            return;
        }

        string fbxPath = "Assets/Personagens/LowPolyPeople/FBX/normal-man-a.fbx";
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<Avatar>()
            .FirstOrDefault();

        if (avatar == null)
        {
            Debug.LogError("Avatar not found in FBX: " + fbxPath);
            return;
        }

        animator.avatar = avatar;
        EditorUtility.SetDirty(animator);
        Debug.Log("Successfully assigned Avatar " + avatar.name + " to TestBot.");
        
        // Also ensure the parent animator is disabled and the script is correctly linked
        GameObject parent = GameObject.Find("ThirdPersonController");
        if (parent != null) {
            Animator parentAnim = parent.GetComponent<Animator>();
            if (parentAnim != null) parentAnim.enabled = false;
        }
    }
}
