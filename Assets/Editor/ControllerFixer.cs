using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class ControllerFixer : EditorWindow
{
    [MenuItem("Tools/Rebuild Teste Controller")]
    public static void Rebuild()
    {
        string newPath = "Assets/Teste_Clean.controller";
        AnimatorController clean = AnimatorController.CreateAnimatorControllerAtPath(newPath);
        
        // Add state
        AnimatorStateMachine sm = clean.layers[0].stateMachine;
        AnimatorState state = sm.AddState("Idle");
        
        // Find motion (FBX is usually the parent of the motion internally)
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/FinalCharacterController-part7/FinalCharacterController-part7/Assets/Blink/Art/Animations/Animations_Starter_Pack/NewAnimations/Idle.fbx");
        foreach(var a in assets) {
            if (a is AnimationClip && a.name.Contains("mixamo")) {
                state.motion = (Motion)a;
                break;
            }
        }
        
        // Assign to normal man a
        GameObject target = GameObject.Find("normal man a");
        if (target != null)
        {
            Animator anim = target.GetComponent<Animator>();
            if (anim != null) {
                anim.runtimeAnimatorController = clean;
                // Re-assign avatar just in case
                Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Personagens/LowPolyPeople/FBX/normal-man-a.fbx");
                if (avatar != null) anim.avatar = avatar;
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Created and assigned Assets/Teste_Clean.controller");
    }
}
