using UnityEngine;
using UnityEditor;

public class UpgradeURP
{
    [MenuItem("Tools/Upgrade All Project Materials to URP")]
    public static void UpgradeAllMaterials()
    {
        // Find all materials in the project
        string[] guids = AssetDatabase.FindAssets("t:Material");
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("Could not find URP Lit shader.");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Skip packages and read-only folders
            if (path.StartsWith("Packages/")) continue;
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // Only upgrade if it's using a built-in standard shader
            if (mat != null && (mat.shader.name == "Standard" || mat.shader.name == "Standard (Specular setup)" || mat.shader.name == "Legacy Shaders/Diffuse"))
            {
                Texture albedo = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                
                mat.shader = urpLit;
                
                if (albedo != null)
                {
                    mat.SetTexture("_BaseMap", albedo);
                }
                
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);

                EditorUtility.SetDirty(mat);
                Debug.Log("Upgraded material to URP Lit: " + path);
                count++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Finished upgrading {count} materials in the project.");
    }
}
