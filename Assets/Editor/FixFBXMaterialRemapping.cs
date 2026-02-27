using UnityEngine;
using UnityEditor;

public class FixFBXMaterialRemapping
{
    [MenuItem("Tools/Remap FBX Materials")]
    public static void RemapMaterials()
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/school" });
        int remappedCount = 0;

        foreach (string guid in fbxGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.ToLower().EndsWith(".fbx")) continue;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;

                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.RecursiveUp);

                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                remappedCount++;
            }
        }
        
        Debug.Log($"Remapped materials for {remappedCount} FBX models.");
    }
}
