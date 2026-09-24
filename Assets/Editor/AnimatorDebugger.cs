using UnityEngine;
using UnityEditor;

public class AnimatorDebugger : EditorWindow
{
    [MenuItem("Tools/Make It Work (Idle)")]
    public static void MakeItWork()
    {
        GameObject target = GameObject.Find("normal man a");
        if (target == null) { Debug.LogError("Object 'normal man a' not found!"); return; }

        Animator anim = target.GetComponent<Animator>();
        if (anim == null) { Debug.LogError("No Animator on 'normal man a'!"); return; }

        // Find the avatar if missing
        if (anim.avatar == null)
        {
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Personagens/LowPolyPeople/FBX/normal-man-a.fbx");
            if (avatar != null) anim.avatar = avatar;
        }

        anim.enabled = true;
        anim.speed = 1.0f;
        
        // Take a break from Play mode if we are in it
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("Sampling animation in Editor...");
            anim.Update(1.0f); 
        }
        else
        {
            anim.Play(0, 0, 0);
        }

        Debug.Log("Executed MakeItWork on " + target.name + ". State: " + anim.GetCurrentAnimatorStateInfo(0).fullPathHash);
    }
}
