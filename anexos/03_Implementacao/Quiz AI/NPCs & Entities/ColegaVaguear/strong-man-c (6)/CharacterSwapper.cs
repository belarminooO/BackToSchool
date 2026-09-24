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
        private const string CharacterModelsFolder = "Assets/Personagens/LowPolyPeople/FBX";
        private const int IgnoreRaycastLayer = 2;
        private const int MeshLayer = 3;

        [Header("Character Selection")]
        [Tooltip("Select the character model index.")]
        [HideInInspector]
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
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { CharacterModelsFolder });

            System.Array.Sort(
                guids,
                (a, b) => string.Compare(
                    AssetDatabase.GUIDToAssetPath(a),
                    AssetDatabase.GUIDToAssetPath(b),
                    System.StringComparison.Ordinal));

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    characterModels.Add(model);
                }
            }
#endif
        }

        public void SwapCharacter(int index)
        {
#if UNITY_EDITOR
            if (!TryGetCharacterModel(index, out GameObject newModel))
            {
                return;
            }

            rootTransform = ResolveRootTransform();
            if (!TryFindCurrentCharacter(out Transform oldSkeleton, out Transform oldMesh))
            {

                return;
            }

            Transform oldHead = oldSkeleton != null ? FindDeepChild(oldSkeleton, "Head") : null;
            GameObject newInstance = InstantiateCharacterModel(newModel);
            Transform newHead = FindDeepChild(newInstance.transform, "Head");

            UpdateMainAnimatorAvatar(newInstance);
            ConfigureCharacterLayers(newInstance);
            MoveHeadAttachments(oldHead, newHead);
            RefreshBodyVisibility();
            CleanupOldCharacter(oldMesh, oldSkeleton);
            MarkDirty();
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

#if UNITY_EDITOR
        private bool TryGetCharacterModel(int index, out GameObject model)
        {
            model = null;

            if (characterModels == null || characterModels.Count == 0)
            {
                LoadModels();
            }

            if (characterModels == null || characterModels.Count == 0)
            {
                return false;
            }

            if (index < 0 || index >= characterModels.Count)
            {
                return false;
            }

            model = characterModels[index];
            return model != null;
        }

        private Transform ResolveRootTransform()
        {
            if (rootTransform == null)
            {
                rootTransform = transform.Find("Root") ?? transform;
            }

            return rootTransform;
        }

        private bool TryFindCurrentCharacter(out Transform skeleton, out Transform mesh)
        {
            skeleton = null;
            mesh = null;

            foreach (Transform child in rootTransform)
            {
                if (child.name == "CameraTarget" || child.name == "Canvas")
                {
                    continue;
                }

                if (child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    mesh = child;
                }
                else
                {
                    skeleton = child;
                }
            }

            return skeleton != null || mesh != null;
        }

        private GameObject InstantiateCharacterModel(GameObject model)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, rootTransform);
            if (instance == null)
            {
                instance = Instantiate(model, rootTransform);
            }

            instance.name = model.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private void UpdateMainAnimatorAvatar(GameObject newInstance)
        {
            Animator newAnimator = newInstance.GetComponent<Animator>();
            if (newAnimator == null)
            {
                return;
            }

            if (mainAnimator == null)
            {
                mainAnimator = rootTransform.GetComponent<Animator>() ?? GetComponent<Animator>();
            }

            if (mainAnimator != null)
            {
                mainAnimator.avatar = newAnimator.avatar;
                mainAnimator.Rebind();
            }

            newAnimator.enabled = false;
        }

        private void ConfigureCharacterLayers(GameObject characterInstance)
        {
            SetLayerRecursively(characterInstance, IgnoreRaycastLayer);

            foreach (var colliderComponent in characterInstance.GetComponentsInChildren<Collider>(true))
            {
                Undo.DestroyObjectImmediate(colliderComponent);
            }

            foreach (var renderer in characterInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.gameObject.layer = MeshLayer;
            }
        }

        private void MoveHeadAttachments(Transform oldHead, Transform newHead)
        {
            if (oldHead == null || newHead == null)
            {
                return;
            }

            List<Transform> attachmentsToMove = new List<Transform>();
            foreach (Transform child in oldHead)
            {
                if (!IsBoneEnd(child.name))
                {
                    attachmentsToMove.Add(child);
                }
            }

            foreach (Transform attachment in attachmentsToMove)
            {
                Vector3 localPosition = attachment.localPosition;
                Quaternion localRotation = attachment.localRotation;
                Vector3 localScale = attachment.localScale;

                Undo.SetTransformParent(attachment, newHead, "Reparent");
                attachment.localPosition = localPosition;
                attachment.localRotation = localRotation;
                attachment.localScale = localScale;

                if (IsInteractorSourceCandidate(attachment))
                {
                    UpdateInteractorSource(attachment);
                }
            }
        }

        private static bool IsBoneEnd(string objectName)
        {
            return objectName.IndexOf("end", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsInteractorSourceCandidate(Transform item)
        {
            return (item.name.Contains("Camera Target") && item.name != "Main Camera") ||
                   item.GetComponent<Camera>() != null;
        }

        private void UpdateInteractorSource(Transform sourceTransform)
        {
            Interactor interactor = GetComponent<Interactor>();
            if (interactor == null)
            {
                return;
            }

            Undo.RecordObject(interactor, "Update Interactor Source");
            interactor.InteractorSource = sourceTransform;
        }

        private void RefreshBodyVisibility()
        {
            GetComponent<FirstPersonNetworkState>()?.RefreshBodyVisibility();
        }

        private void CleanupOldCharacter(Transform oldMesh, Transform oldSkeleton)
        {
            if (oldMesh != null)
            {
                Undo.DestroyObjectImmediate(oldMesh.gameObject);
            }

            if (oldSkeleton != null)
            {
                Undo.DestroyObjectImmediate(oldSkeleton.gameObject);
            }
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
            if (mainAnimator != null)
            {
                EditorUtility.SetDirty(mainAnimator);
            }
        }
#endif

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

                }
                else
                {

                }
                Debug.DrawRay(source.position, source.forward * 10f, Color.cyan, 2f);
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {

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
