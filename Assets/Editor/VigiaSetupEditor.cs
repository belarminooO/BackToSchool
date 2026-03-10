using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;
using System.Collections.Generic;

namespace QuizAI.Editor
{
    public class VigiaSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Quiz AI/Auto Setup Vigia")]
        public static void SetupVigia()
        {
            GameObject vigiaGO = FindVigiaObject();

            if (vigiaGO == null)
            {
                Debug.LogError("Setup Vigia: Não foi encontrado o objeto 'strong man c' na cena ativa. Certifica-te de que o personagem está na cena.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(vigiaGO, "Setup Vigia");

            // 1. Configurar NavMeshAgent
            NavMeshAgent agent = vigiaGO.GetComponent<NavMeshAgent>();
            if (agent == null) agent = vigiaGO.AddComponent<NavMeshAgent>();
            
            agent.speed = 2.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.8f;
            agent.radius = 0.4f;
            agent.height = 1.8f;

            // 2. Configurar Vigia Script
            Vigia vigiaScript = vigiaGO.GetComponent<Vigia>();
            if (vigiaScript == null) vigiaScript = vigiaGO.AddComponent<Vigia>();
            
            vigiaScript.FindWaypointsInScene();
            
            // 3. Configurar Animator e Controller
            Animator animator = vigiaGO.GetComponent<Animator>();
            if (animator == null) animator = vigiaGO.AddComponent<Animator>();

            SetupAnimatorController(animator);

            Selection.activeGameObject = vigiaGO;
            Debug.Log("<color=green>Setup Vigia:</color> Concluído com sucesso no objeto: " + vigiaGO.name);
        }

        private static GameObject FindVigiaObject()
        {
            // Tenta nomes comuns do prefab
            string[] possibleNames = { "strong man c", "strong-man-c", "Vigia", "Watchman" };
            
            foreach (var name in possibleNames)
            {
                GameObject go = GameObject.Find(name);
                if (go != null) return go;
            }

            // Procura por componente se o nome falhar
            Vigia existing = Object.FindAnyObjectByType<VigilaWaypoint>()?.GetComponentInParent<Vigia>();
            if (existing != null) return existing.gameObject;

            return null;
        }

        private static void SetupAnimatorController(Animator animator)
        {
            string targetPath = "Assets/Quiz/AI/VigiaAnimator.controller";
            
            if (animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.name == "VigiaAnimator")
            {
                return; // Já está configurado
            }

            if (File.Exists(targetPath))
            {
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(targetPath);
                return;
            }

            // Tenta encontrar o controller do FinalCharacterController Part 7 como base
            string sourcePath = "Assets/FinalCharacterController-part7/FinalCharacterController-part7/Assets/GinjaGaming/FinalCharacterController/Animation/PlayerAnimator.controller";
            
            if (File.Exists(sourcePath))
            {
                if (!Directory.Exists("Assets/Quiz/AI")) Directory.CreateDirectory("Assets/Quiz/AI");
                AssetDatabase.CopyAsset(sourcePath, targetPath);
                AssetDatabase.Refresh();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(targetPath);
                Debug.Log("<color=cyan>Setup Vigia:</color> Criado novo Animator Controller baseado no PlayerAnimator.");
            }
            else
            {
                Debug.LogWarning("Setup Vigia: Não foi possível encontrar o controller base em: " + sourcePath + ". Por favor atribui um Animator Controller manualmente.");
            }
        }
    }
}
