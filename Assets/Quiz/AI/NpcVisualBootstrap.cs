using UnityEngine;

internal static class NpcVisualBootstrap
{
    private static readonly Bounds VisibleBounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(4f, 6f, 4f));

    internal static void Configure(GameObject root, Animator animator = null)
    {
        if (root == null)
            return;

        if (animator == null)
            animator = root.GetComponent<Animator>();

        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skinned.updateWhenOffscreen = true;
            skinned.localBounds = VisibleBounds;
        }
    }
}
