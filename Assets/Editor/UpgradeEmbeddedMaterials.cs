using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class UpgradeEmbeddedMaterials
{
    [MenuItem("Tools/Fix Pink FBX Materials")]
    public static void FixMaterials()
    {
        string folderPath = "Assets/school";
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
        
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("Could not find URP Lit shader.");
            return;
        }

        int extractedCount = 0;
        int upgradedCount = 0;

        foreach (string guid in fbxGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.ToLower().EndsWith(".fbx")) continue;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null && importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                // To extract embedded materials, we need to find the embedded material assets
                IEnumerable<Object> objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool needsReimport = false;

                foreach (Object obj in objects)
                {
                    if (obj is Material mat)
                    {
                        // Check if it's embedded
                        if (AssetDatabase.IsSubAsset(mat))
                        {
                            string matFolderPath = Path.GetDirectoryName(assetPath) + "/Materials";
                            if (!AssetDatabase.IsValidFolder(matFolderPath))
                            {
                                string parent = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                                AssetDatabase.CreateFolder(parent, "Materials");
                            }

                            string newMatPath = matFolderPath + "/" + mat.name + ".mat";
                            // Make name unique if exists
                            newMatPath = AssetDatabase.GenerateUniqueAssetPath(newMatPath);

                            // Extract material
                            string error = AssetDatabase.ExtractAsset(mat, newMatPath);
                            if (string.IsNullOrEmpty(error))
                            {
                                extractedCount++;
                                needsReimport = true;
                                
                                // Now upgrade the newly extracted material
                                Material extractedMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
                                if (extractedMat != null)
                                {
                                    UpgradeMaterialToURP(extractedMat, urpLit);
                                    upgradedCount++;
                                }
                            }
                        }
                    }
                }

                // If materials were extracted, reimport the model so it links to them
                if (needsReimport)
                {
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        
        // Also upgrade any existing normal materials in the folder just in case
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        foreach (string mguid in matGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(mguid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m != null && m.shader.name != urpLit.name)
            {
                UpgradeMaterialToURP(m, urpLit);
                upgradedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed materials! Extracted: {extractedCount}, Upgraded: {upgradedCount}");
    }

    private static void UpgradeMaterialToURP(Material mat, Shader urpLit)
    {
        if (mat.shader.name == urpLit.name) return;

        Texture albedo = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        
        mat.shader = urpLit;
        
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
        }
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        EditorUtility.SetDirty(mat);
    }
}
