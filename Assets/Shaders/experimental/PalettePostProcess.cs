using UnityEngine;

[ExecuteInEditMode]
public class PalettePostProcess : MonoBehaviour
{
    public Material paletteMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (paletteMaterial != null)
            Graphics.Blit(src, dest, paletteMaterial);
        else
            Graphics.Blit(src, dest);
    }
}