using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GinjaGaming.FinalCharacterController
{
    [ExecuteInEditMode]
    public class CharacterSwapper : MonoBehaviour
    {
        [Header("Character Selection")]
        [Tooltip("Select the character model index.")]
        public int characterIndex = 0;

        [HideInInspector]
        public int lastIndex = -1;

        [HideInInspector]
        public List<GameObject> characterModels = new List<GameObject>();

        [Header("References")]
        public Transform rootTransform;
        public Animator mainAnimator;

        public void LoadModels()
        {
#if UNITY_EDITOR
            characterModels.Clear();
            string folderPath = "Assets/Personagens/LowPolyPeople/FBX";
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            // Sort by path string so they remain in a fixed alphabetical order
            System.Array.Sort(guids, (a, b) => string.Compare(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().EndsWith(".fbx"))
                {
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model != null)
                    {
                        characterModels.Add(model);
                    }
                }
            }
#endif
        }

        public void SwapCharacter(int index)
        {
#if UNITY_EDITOR
            if (characterModels == null || characterModels.Count == 0) LoadModels();
            if (characterModels.Count == 0 || index < 0 || index >= characterModels.Count) return;

            GameObject newFbx = characterModels[index];
            if (newFbx == null) return;

            if (rootTransform == null)
            {
                Transform foundRoot = transform.Find("Root");
                rootTransform = foundRoot != null ? foundRoot : transform;
            }

            // 1. Identify existing model
            Transform oldSkeleton = null;
            Transform oldMesh = null;

            foreach (Transform child in rootTransform)
            {
                if (child.name != "CameraTarget" && child.name != "Canvas")
                {
                    if (child.GetComponent<SkinnedMeshRenderer>() != null)
                        oldMesh = child;
                    else
                        oldSkeleton = child;
                }
            }

            if (oldSkeleton == null && oldMesh == null)
            {
                Debug.LogWarning("No old character found to swap!");
                return;
            }

            Transform oldHead = null;
            if (oldSkeleton != null) oldHead = FindDeepChild(oldSkeleton, "Head");

            // 2. Instantiate new model
            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(newFbx, rootTransform);
            if (newInstance == null) newInstance = Instantiate(newFbx, rootTransform);

            newInstance.name = newFbx.name; // Strip the "(Clone)" tag if any
            newInstance.transform.localPosition = Vector3.zero;
            newInstance.transform.localRotation = Quaternion.identity;
            newInstance.transform.localScale = Vector3.one;

            Transform newHead = FindDeepChild(newInstance.transform, "Head");

            // 4. Update Animator Avatar
            Animator newAnim = newInstance.GetComponent<Animator>();
            if (newAnim != null)
            {
                if (mainAnimator == null)
                {
                    // The core animator that PlayerAnimation talks to is typically on the Root object.
                    mainAnimator = rootTransform.GetComponent<Animator>();
                    if (mainAnimator == null) mainAnimator = GetComponent<Animator>();
                }

                if (mainAnimator != null)
                {
                    mainAnimator.avatar = newAnim.avatar;
                    // Rebind Animator as the skeleton has changed while running
                    mainAnimator.Rebind();
                }
                // Disable animator on child to prevent fighting main animator
                newAnim.enabled = false;
            }

            // 5. Physics & Interaction Fix
            // Force the entire new character to be on Layer 2 (Ignore Raycast) or Layer 3 (Original Mesh)
            // This prevents the character's own body from blocking the Interactor raycast.
            int ignoreRaycastLayer = 2; 
            int meshLayer = 3; 
            
            SetLayerRecursively(newInstance, ignoreRaycastLayer);
            
            // Specifically find SkinnedMeshRenderers and set them to meshLayer or IgnoreRaycast
            // Also, strip any colliders that might have come with the FBX to be safe
            foreach (var col in newInstance.GetComponentsInChildren<Collider>(true))
            {
                Undo.DestroyObjectImmediate(col);
            }
            
            foreach (var smr in newInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.gameObject.layer = meshLayer;
            }

            // 6. Move Cameras and other Head attachments
            if (oldHead != null && newHead != null)
            {
                List<Transform> itemsToMove = new List<Transform>();
                foreach (Transform child in oldHead)
                {
                    if (!child.name.ToLower().Contains("end"))
                        itemsToMove.Add(child);
                }
                foreach (Transform item in itemsToMove)
                {
                    Vector3 localPos = item.localPosition;
                    Quaternion localRot = item.localRotation;
                    Vector3 localScale = item.localScale;

                    Undo.SetTransformParent(item, newHead, "Reparent");

                    item.localPosition = localPos;
                    item.localRotation = localRot;
                    item.localScale = localScale;

                    if ((item.name.Contains("Camera Target") && !item.name.Equals("Main Camera")) || item.GetComponent<Camera>() != null)
                    {
                        var interactor = GetComponent<Interactor>();
                        if (interactor != null)
                        {
                            interactor.InteractorSource = item;
                            Undo.RecordObject(interactor, "Update Interactor Source");
                        }
                    }
                }
            }

            // FINAL SYNC: Tell the network state to refresh body visibility.
            // This ensures if we are the LocalPlayer, the new body is hidden from our eyes.
            var networkState = GetComponent<FirstPersonNetworkState>();
            if (networkState != null)
            {
                networkState.RefreshBodyVisibility();
            }

            // 7. Cleanup old objects
            if (oldMesh != null) Undo.DestroyObjectImmediate(oldMesh.gameObject);
            if (oldSkeleton != null) Undo.DestroyObjectImmediate(oldSkeleton.gameObject);

            // Mark dirty
            EditorUtility.SetDirty(this);
            if (mainAnimator != null) EditorUtility.SetDirty(mainAnimator);
#endif
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public void DiagnoseInteraction()
        {
            Transform source = null;
            var interactor = GetComponent<Interactor>();
            if (interactor != null) source = interactor.InteractorSource;
            if (source == null && Camera.main != null) source = Camera.main.transform;

            if (source != null)
            {
                Ray r = new Ray(source.position, source.forward);
                if (Physics.Raycast(r, out RaycastHit hit, 10f))
                {
                    Debug.Log($"<color=cyan>Interaction Diagnostic:</color> Your raycast is hitting <b>{hit.collider.gameObject.name}</b> on layer {hit.collider.gameObject.layer}. Tag: {hit.collider.gameObject.tag}");
                }
                else
                {
                    Debug.Log("<color=orange>Interaction Diagnostic:</color> Raycast hit nothing within 10 meters.");
                }
                Debug.DrawRay(source.position, source.forward * 10f, Color.cyan, 2f);
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                // Better matching for different FBX naming conventions
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) || 
                    child.name.EndsWith(":" + name, System.StringComparison.OrdinalIgnoreCase)) 
                    return child;
                
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CharacterSwapper))]
    public class CharacterSwapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterSwapper swapper = (CharacterSwapper)target;

            if (GUILayout.Button("Refresh Character List") || swapper.characterModels == null || swapper.characterModels.Count == 0)
            {
                swapper.LoadModels();
            }

            if (swapper.characterModels != null && swapper.characterModels.Count > 0)
            {
                string[] options = new string[swapper.characterModels.Count];
                for (int i = 0; i < swapper.characterModels.Count; i++)
                {
                    options[i] = swapper.characterModels[i] != null ? swapper.characterModels[i].name : "Missing Model";
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Selected Character", swapper.characterIndex, options);
                if (EditorGUI.EndChangeCheck() || swapper.characterIndex != swapper.lastIndex)
                {
                    Undo.RecordObject(swapper, "Change Character Index");
                    swapper.characterIndex = newIndex;
                    swapper.lastIndex = newIndex;
                    swapper.SwapCharacter(newIndex);

                    EditorUtility.SetDirty(swapper.gameObject);

#if UNITY_2021_1_OR_NEWER
                    var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    }
#endif
                }

                if (GUILayout.Button("Diagnose Interaction Raycast"))
                {
                    swapper.DiagnoseInteraction();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No character models found in the specified folder.", MessageType.Warning);
            }

            DrawDefaultInspector();
        }
    }
#endif


}
