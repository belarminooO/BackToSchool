using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class NetworkCharacterSwapper : NetworkBehaviour
{
    [Header("Character Models — same list & order as LobbyCharacterDisplay.characterPrefabs")]
    [SerializeField] private List<GameObject> characterModels = new();

    [Header("References — same as CharacterSwapper")]
    public Transform rootTransform;
    public Animator mainAnimator;

    private readonly SyncVar<int> _index = new SyncVar<int>(ownerAuth: true);

    protected override void OnSpawned()
    {
        _index.onChanged += OnIndexChanged;

        var skinSel = SkinSelection.Instance;
        Debug.Log($"[NetworkCharacterSwapper] OnSpawned | isOwner={isOwner} | SkinSelection={(skinSel == null ? "NULL — not in scene or destroyed!" : "OK")} | idx={(skinSel != null ? skinSel.SkinIndex.ToString() : "—")} | characterModels.Count={characterModels.Count} | rootTransform={(rootTransform == null ? "NULL — assign Root in Inspector!" : rootTransform.name)} | mainAnimator={(mainAnimator == null ? "NULL — assign Animator in Inspector!" : "OK")}");

        if (isOwner)
        {
            int idx = SkinSelection.Instance != null ? SkinSelection.Instance.SkinIndex : 0;
            _index.value = idx;
            SwapCharacter(idx);
        }
        else
        {
            if (_index.value > 0 || characterModels.Count > 0)
                SwapCharacter(_index.value);
        }
    }

    protected override void OnDespawned()
    {
        _index.onChanged -= OnIndexChanged;
    }

    private void OnIndexChanged(int newIndex) => SwapCharacter(newIndex);

    private void SwapCharacter(int index)
    {
        if (characterModels == null || characterModels.Count == 0) { Debug.LogError("[SwapCharacter] characterModels is empty — fill the list in Inspector!"); return; }
        if (index < 0 || index >= characterModels.Count) { Debug.LogError($"[SwapCharacter] index {index} out of range (count={characterModels.Count})"); return; }

        GameObject newFbx = characterModels[index];
        if (newFbx == null) return;

        if (rootTransform == null)
        {
            Transform found = transform.Find("Root");
            rootTransform = found != null ? found : transform;
        }

        Transform oldSkeleton = null;
        Transform oldMesh = null;

        foreach (Transform child in rootTransform)
        {
            if (child.name == "CameraTarget" || child.name == "Canvas") continue;
            if (child.GetComponent<SkinnedMeshRenderer>() != null)
                oldMesh = child;
            else
                oldSkeleton = child;
        }

        Debug.Log($"[SwapCharacter] index={index} | oldSkeleton={(oldSkeleton == null ? "null" : oldSkeleton.name)} | oldMesh={(oldMesh == null ? "null" : oldMesh.name)}");

        Transform oldHead = oldSkeleton != null ? FindDeepChild(oldSkeleton, "Head") : null;

        GameObject newInstance = Instantiate(newFbx, rootTransform);
        newInstance.name = newFbx.name;
        newInstance.transform.localPosition = Vector3.zero;
        newInstance.transform.localRotation = Quaternion.identity;
        newInstance.transform.localScale = Vector3.one;

        Transform newHead = FindDeepChild(newInstance.transform, "Head");

        Animator newAnim = newInstance.GetComponent<Animator>();
        if (newAnim != null)
        {
            if (mainAnimator == null)
            {
                mainAnimator = rootTransform.GetComponent<Animator>();
                if (mainAnimator == null) mainAnimator = GetComponent<Animator>();
            }

            if (mainAnimator != null)
            {
                mainAnimator.avatar = newAnim.avatar;

                bool wasGrounded = mainAnimator.GetBool("isGrounded");
                bool wasIdling   = mainAnimator.GetBool("isIdling");

                mainAnimator.Rebind();

                mainAnimator.SetBool("isGrounded",  wasGrounded);
                mainAnimator.SetBool("isIdling",    wasIdling);
                mainAnimator.SetBool("isAttacking", false);
                mainAnimator.SetBool("isGathering", false);
                mainAnimator.Update(0f);
            }

            newAnim.enabled = false;
        }

        if (oldHead != null && newHead != null)
        {
            var toMove = new List<Transform>();
            foreach (Transform child in oldHead)
                if (!child.name.ToLower().Contains("end"))
                    toMove.Add(child);

            foreach (var item in toMove)
            {
                item.GetLocalPositionAndRotation(out Vector3 lp, out Quaternion lr);
                Vector3 ls = item.localScale;
                item.SetParent(newHead, false);
                item.SetLocalPositionAndRotation(lp, lr);
                item.localScale = ls;
            }
        }

        SetLayerRecursively(newInstance, 2);
        foreach (var smr in newInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.gameObject.layer = 3;

        if (isOwner)
            foreach (var smr in newInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (oldMesh != null) Destroy(oldMesh.gameObject);
        if (oldSkeleton != null) Destroy(oldSkeleton.gameObject);
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase) ||
                child.name.EndsWith(":" + childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
