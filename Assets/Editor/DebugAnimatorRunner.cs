using UnityEngine;
using UnityEditor;

public class DebugAnimatorRunner : EditorWindow
{
    [MenuItem("Tools/Debug and Fix Animator")]
    public static void Run()
    {
        GameObject target = GameObject.Find("normal man a");
        if (target == null) target = GameObject.Find("HumanMale_Character_FREE");
        
        if (target == null) { Debug.LogError("Target not found!"); return; }
        
        Debug.Log("Checking " + target.name);
        
        // Remove children Animators
        foreach(var a in target.GetComponentsInChildren<Animator>()) {
            if (a.gameObject != target) {
                Debug.Log("Removing Animator from child: " + a.gameObject.name);
                DestroyImmediate(a);
            }
        }
        
        Animator anim = target.GetComponent<Animator>();
        if (anim == null) anim = target.AddComponent<Animator>();
        
        anim.enabled = true;
        anim.speed = 1.0f;
        
        // Assign Avatar
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Personagens/LowPolyPeople/FBX/normal-man-a.fbx");
        if (avatar != null) {
            anim.avatar = avatar;
            Debug.Log("Assigned Avatar: " + avatar.name);
        } else {
            Debug.LogError("Avatar not found at path!");
        }
        
        // Assign Controller
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Teste_Clean.controller");
        if (controller == null) {
            controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Personagens/LowPolyPeople/Prefabs/Animations/normal.controller");
        }
        
        if (controller != null) {
            anim.runtimeAnimatorController = controller;
            Debug.Log("Assigned Controller: " + controller.name);
        }
        
        // Force Update
        anim.Update(1.0f);
        
        // Check if any bone moved from localPosition zero?
        Transform stomach = target.transform.Find("model/skeleton/Stomach");
        if (stomach == null) stomach = target.transform.Find("skeleton/Stomach");
        
        if (stomach != null) {
            Debug.Log("Stomach Position: " + stomach.localPosition);
        } else {
             Debug.LogWarning("Could not find Stomach bone");
        }
    }
}
