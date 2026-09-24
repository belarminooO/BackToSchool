using UnityEngine;
using UnityEditor;
using PurrNet;
using GinjaGaming.FinalCharacterController;

namespace QuizAI.Editor
{
    public class PlayerSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Quiz AI/Auto Setup Player")]
        public static void SetupPlayerFPC()
        {
            string oldPlayerPath = "Assets/_Prefabs/@Player.prefab";
            string baseFpcPath = "Assets/_Prefabs/FirstPersonController.prefab";
            string newPlayerPath = "Assets/_Prefabs/Player_FPC.prefab";

            GameObject oldPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(oldPlayerPath);
            GameObject baseFpcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseFpcPath);

            if (oldPlayerPrefab == null)
            {
                Debug.LogError($"[PlayerSetup] Could not find {oldPlayerPath}");
                return;
            }

            if (baseFpcPrefab == null)
            {
                Debug.LogError($"[PlayerSetup] Could not find {baseFpcPath}");
                return;
            }

            // Instantiate both to manipulate them
            GameObject newPlayerInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseFpcPrefab);
            GameObject oldPlayerInstance = (GameObject)PrefabUtility.InstantiatePrefab(oldPlayerPrefab);

            if (newPlayerInstance == null || oldPlayerInstance == null)
            {
                Debug.LogError("[PlayerSetup] Failed to instantiate prefabs for setup.");
                return;
            }

            // Unpack both so we can extract and add children safely without Prefab Variant/Override issues
            PrefabUtility.UnpackPrefabInstance(oldPlayerInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            PrefabUtility.UnpackPrefabInstance(newPlayerInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            newPlayerInstance.name = "Player_FPC";

            // 1. Copy Voice/Net components from oldPlayer to newPlayer
            // Note: In Unity, to copy a component, we use EditorUtility.CopySerialized and PasteComponentValues.
            // Since we can't easily auto-copy all components safely without knowing their exact types and avoiding duplicates,
            // we will instead extract the Voice/UI child objects.
            
            // Transfer UI child
            Transform uiChild = oldPlayerInstance.transform.Find("UI");
            if (uiChild != null)
            {
                uiChild.SetParent(newPlayerInstance.transform);
                uiChild.localPosition = Vector3.zero;
                uiChild.localRotation = Quaternion.identity;
                Debug.Log("[PlayerSetup] Transferred UI child object.");
            }
            else
            {
                // Fallback: search for nametag
                PlayerNameTag nametag = oldPlayerInstance.GetComponentInChildren<PlayerNameTag>(true);
                if (nametag != null && nametag.transform.parent != oldPlayerInstance.transform)
                {
                    Transform nametagCanvas = nametag.transform.parent;
                    nametagCanvas.SetParent(newPlayerInstance.transform);
                    nametagCanvas.localPosition = new Vector3(0, 2f, 0); // Above head
                    Debug.Log("[PlayerSetup] Transferred Nametag Canvas.");
                }
            }

            // Transfer VoiceChat child
            Transform voiceChild = oldPlayerInstance.transform.Find("VoiceChat");
            if (voiceChild != null)
            {
                voiceChild.SetParent(newPlayerInstance.transform);
                voiceChild.localPosition = Vector3.zero;
                voiceChild.localRotation = Quaternion.identity;
                voiceChild.gameObject.SetActive(true); // VERY IMPORTANT: Required for MetaVoiceChat Coroutines
                Debug.Log("[PlayerSetup] Transferred VoiceChat child object.");
            }

            // Make sure the new player has a NetworkIdentity
            NetworkIdentity newNetId = newPlayerInstance.GetComponent<NetworkIdentity>();
            if (newNetId == null)
            {
                newNetId = newPlayerInstance.AddComponent<NetworkIdentity>();
                Debug.Log("[PlayerSetup] Added NetworkIdentity.");
            }

            // Transfer NetworkTransform
            var oldNetTransform = oldPlayerInstance.GetComponent<PurrNet.NetworkTransform>();
            if (oldNetTransform != null)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(oldNetTransform);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newPlayerInstance);
                Debug.Log("[PlayerSetup] Transferred NetworkTransform.");
            }
            else
            {
                newPlayerInstance.AddComponent<PurrNet.NetworkTransform>();
                Debug.Log("[PlayerSetup] Added NetworkTransform fallback.");
            }

            // Copy PurrNet settings if needed, but PurrNet NetworkIdentity uses automatic generation mostly.
            
            // Transfer PlayerVoiceState if it exists on root
            var oldVoiceState = oldPlayerInstance.GetComponent("PlayerVoiceState");
            if (oldVoiceState != null)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(oldVoiceState);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newPlayerInstance);
                Debug.Log("[PlayerSetup] Transferred PlayerVoiceState component.");
            }

            // Make sure PlayerController is setup
            PlayerController pc = newPlayerInstance.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.team = Team.None;
                // Hook up vignette if present
                Transform vignette = newPlayerInstance.transform.Find("UI/Vignette") ?? newPlayerInstance.transform.Find("Vignette");
                if (vignette != null)
                {
                    pc.vignetteObject = vignette.gameObject;
                    pc.changeColorScript = vignette.GetComponent("ChangeColor") as ChangeColor;
                }
                Debug.Log("[PlayerSetup] Configured Quiz fields on PlayerController.");
            }

            // Transfer Interactor if it exists on root
            var oldInteractor = oldPlayerInstance.GetComponent<Interactor>();
            if (oldInteractor != null)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(oldInteractor);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newPlayerInstance);
                Interactor newInteractor = newPlayerInstance.GetComponent<Interactor>();
                if (newInteractor != null && oldInteractor.e_indicator != null)
                {
                    // Find the equivalent e_indicator in the newly copied UI
                    Transform[] allUiChildren = newPlayerInstance.GetComponentsInChildren<Transform>(true);
                    foreach(var child in allUiChildren) {
                        if (child.name == oldInteractor.e_indicator.name) {
                            newInteractor.e_indicator = child.gameObject;
                            break;
                        }
                    }
                }
                // Assign InteractorSource to the CameraTarget (the transform the player actually looks through)
                if (newInteractor != null)
                {
                    Transform cameraTarget = null;
                    foreach (var t in newPlayerInstance.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "CameraTarget" || t.name.Contains("CameraTarget"))
                        {
                            cameraTarget = t;
                            break;
                        }
                    }
                    // Fallback: find any camera component that isn't tagged MainCamera
                    if (cameraTarget == null)
                    {
                        foreach (var cam in newPlayerInstance.GetComponentsInChildren<Camera>(true))
                        {
                            cameraTarget = cam.transform;
                            break;
                        }
                    }
                    newInteractor.InteractorSource = cameraTarget;
                }
                Debug.Log("[PlayerSetup] Transferred Interactor component.");
            }

            // Transfer PlayerNameTag if it exists on root
            var oldNameTag = oldPlayerInstance.GetComponent<PlayerNameTag>();
            if (oldNameTag != null)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(oldNameTag);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newPlayerInstance);
                PlayerNameTag newNameTag = newPlayerInstance.GetComponent<PlayerNameTag>();
                if (newNameTag != null)
                {
                    // Find the equivalent nameText in the newly copied UI by name (usually "NameText")
                    Transform[] allUiChildren = newPlayerInstance.GetComponentsInChildren<Transform>(true);
                    foreach(var child in allUiChildren) {
                        if (child.name == "NameText" || child.GetComponent<TMPro.TextMeshProUGUI>() != null) {
                            var textComp = child.GetComponent<TMPro.TextMeshProUGUI>();
                            if (textComp != null) {
                                // Assign using SerializedObject since the field is private
                                SerializedObject so = new SerializedObject(newNameTag);
                                var prop = so.FindProperty("nameText");
                                if (prop != null) {
                                    prop.objectReferenceValue = textComp;
                                    so.ApplyModifiedProperties();
                                }
                                break;
                            }
                        }
                    }
                }
                Debug.Log("[PlayerSetup] Transferred PlayerNameTag component.");
            }

            // Add NetworkAnimator to sync animations
            var anim = newPlayerInstance.GetComponent<Animator>();
            if (anim != null)
            {
                var netAnim = newPlayerInstance.GetComponent<PurrNet.NetworkAnimator>();
                if (netAnim == null)
                {
                    netAnim = newPlayerInstance.AddComponent<PurrNet.NetworkAnimator>();
                    // NetworkAnimator.animator is read-only, we should try using reflection or SerializedObject
                    SerializedObject so = new SerializedObject(netAnim);
                    var prop = so.FindProperty("animator");
                    if (prop != null) {
                        prop.objectReferenceValue = anim;
                        so.ApplyModifiedProperties();
                    }
                    Debug.Log("[PlayerSetup] Added NetworkAnimator.");
                }
            }

            // Save the new prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(newPlayerInstance, newPlayerPath, InteractionMode.UserAction);
            if (savedPrefab != null)
            {
                Debug.Log($"<color=green>[PlayerSetup]</color> Successfully created {newPlayerPath}!");
            }
            else
            {
                Debug.LogError("[PlayerSetup] Failed to save the new prefab.");
            }

            // Cleanup scene instances
            DestroyImmediate(newPlayerInstance);
            DestroyImmediate(oldPlayerInstance);

            AssetDatabase.Refresh();
        }
    }
}
