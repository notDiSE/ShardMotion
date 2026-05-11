using System.ComponentModel;
using UnityEngine;
using ShardMotion.Settings;
using UnityEditor;

namespace ShardMotion.Editor
{
    [InitializeOnLoad]
    public static class Startup
    {
        /// <summary>
        /// Is called when the editor application is opended, and opens quick setup if settings file is not found (probably the first time, or incorrect installation)
        /// </summary>
        static Startup()
        {
            EditorApplication.delayCall += () =>
            {
                var settings = AssetDatabase.LoadAssetAtPath<ShardMotionSettings>(ShardMotionQuickSetupWindow.SettingsPath);
                if (settings == null) ShardMotionQuickSetupWindow.Open();
            };
        }
    }
}
