#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ShardMotion.Examples
{
    [InitializeOnLoad]
    public static class ImportTMPEssentials
    {
        static ImportTMPEssentials()
        {
            EditorApplication.delayCall += Check;
        }

        static void Check()
        {
            EditorApplication.delayCall -= Check;

            if (!IsTMPInstalled()) return;
            if (AreTMPEssentialsImported()) return;

            bool open = EditorUtility.DisplayDialog(
                "TMP Essentials not imported",
                "The Game Example uses TextMeshPro Essentials. You need to import TMP Essential Resources.",
                "Open Importer",
                "Skip"
            );

            if (open) EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }

        static bool IsTMPInstalled()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Unity.TextMeshPro") return true;
            }
            return false;
        }

        static bool AreTMPEssentialsImported()
        {
            return Directory.Exists(Path.Combine(Application.dataPath, "TextMesh Pro"));
        }
    }
}
#endif