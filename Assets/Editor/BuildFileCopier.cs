using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BackToSchool.Editor
{
    public class BuildFileCopier : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildOutputPath = report.summary.outputPath;
            string buildRoot = Directory.Exists(buildOutputPath)
                ? buildOutputPath
                : Path.GetDirectoryName(buildOutputPath) ?? buildOutputPath;

            CopyIfExists(Path.Combine(projectRoot, ".env"), buildRoot);
            CopyIfExists(Path.Combine(projectRoot, "steam_appid.txt"), buildRoot);

            if (buildOutputPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                string contentsRoot = Path.Combine(buildRoot, "Contents");
                CopyIfExists(Path.Combine(projectRoot, ".env"), Path.Combine(contentsRoot, "Resources"));
                CopyIfExists(Path.Combine(projectRoot, "steam_appid.txt"), Path.Combine(contentsRoot, "MacOS"));
            }
        }

        private static void CopyIfExists(string sourcePath, string destinationRoot)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[BuildFileCopier] Missing source file: {sourcePath}");
                return;
            }

            Directory.CreateDirectory(destinationRoot);

            string destinationPath = Path.Combine(destinationRoot, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, destinationPath, true);
            Debug.Log($"[BuildFileCopier] Copied {Path.GetFileName(sourcePath)} to {destinationPath}");
        }
    }
}
