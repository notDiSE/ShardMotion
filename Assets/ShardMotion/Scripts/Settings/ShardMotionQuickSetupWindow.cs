#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShardMotion.Settings
{
    public class ShardMotionQuickSetupWindow : EditorWindow
    {
        const string SettingsPath = "Assets/Resources/ShardMotionGlobalSettings.asset";
        const string HeaderImagePath = "Assets/ShardMotion/Editor/Resources/Header.png";

        const float Margin = 16f;
        const float RowHeight = 50f;
        const float StatusSize = 36f;
        const float Gap = 8f;
        const float LabelHeight = 16f;
        const float RowSpacing = 12f;

        [MenuItem("Tools/ShardMotion/Quick Setup")]
        public static void Open()
        {
            var w = GetWindow<ShardMotionQuickSetupWindow>(true, "ShardMotion Quick Setup", true);
            w.minSize = new Vector2(460, 560);
            w.maxSize = new Vector2(460, 560);
            w.ShowUtility();
        }

        void OnGUI()
        {
            float y = Margin;
            float w = position.width - Margin * 2f;
            float x = Margin;

            Rect imgRect = new Rect(x, y, w, 180f);
            var header = AssetDatabase.LoadAssetAtPath<Texture2D>(HeaderImagePath);
            if (header != null)
                GUI.DrawTexture(imgRect, header, ScaleMode.ScaleToFit);
            y += imgRect.height + Margin;

            var settings = AssetDatabase.LoadAssetAtPath<ShardMotionSettings>(SettingsPath);
            bool hasSettings = settings != null;

            float btnWidth = w - StatusSize - Gap;

            GUI.Label(new Rect(x, y, w, LabelHeight), "Read the manual before usage", LabelStyle());
            y += LabelHeight;

            Rect helpBtn = new Rect(x, y, btnWidth, RowHeight);
            var helpIcon = EditorGUIUtility.IconContent("d__Help");
            var helpLabel = new GUIContent("  Open Manual", helpIcon.image);
            if (GUI.Button(helpBtn, helpLabel, RowButtonStyle()))
            {
                string manualPath = Path.GetFullPath("Assets/ShardMotion/Manual.html");
    
                if (File.Exists(manualPath))
                {
                    Application.OpenURL(new System.Uri(manualPath).AbsoluteUri);
                }
                else
                {
                    Debug.LogError("Manual not found at: " + manualPath);
                }
            }
            y += RowHeight + RowSpacing;

            GUI.Label(new Rect(x, y, w, LabelHeight), "Shard Motion global settings", LabelStyle());
            y += LabelHeight;

            Rect settingsBtn = new Rect(x, y, btnWidth, RowHeight);
            Rect statusRect = new Rect(
                x + btnWidth + Gap,
                y + (RowHeight - StatusSize) * 0.5f,
                StatusSize,
                StatusSize);

            var settingsIcon = EditorGUIUtility.IconContent(hasSettings ? "d_SettingsIcon" : "d_CreateAddNew");
            var settingsLabel = new GUIContent("  " + (hasSettings ? "Open Global Settings" : "Setup Global Settings"), settingsIcon.image);

            if (GUI.Button(settingsBtn, settingsLabel, RowButtonStyle()))
            {
                if (hasSettings)
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
                else
                {
                    CreateGlobalSettings();
                }
            }

            DrawStatusIcon(statusRect, hasSettings);
        }

        private void OnDestroy()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ShardMotionSettings>(SettingsPath);
            if (settings != null) return;
            
            bool confirm = EditorUtility.DisplayDialog("Missing Settings file", "ShardMotion won't work without settings file, are you sure you want to continue?", "Yes", "No");

            if (!confirm) EditorApplication.delayCall += Open;
        }

        static void CreateGlobalSettings()
        {
            const string resDir = "Assets/Resources";
            if (!System.IO.Directory.Exists(resDir))
                System.IO.Directory.CreateDirectory(resDir);

            var asset = ScriptableObject.CreateInstance<ShardMotionSettings>();
            AssetDatabase.CreateAsset(asset, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        static void DrawStatusIcon(Rect r, bool on)
        {
            var s = new GUIStyle();
            s.alignment = TextAnchor.MiddleCenter;
            s.fontStyle = FontStyle.Bold;
            s.fontSize = 28;
            s.normal.textColor = on ? new Color(0.29f, 0.78f, 0.35f) : new Color(0.85f, 0.25f, 0.25f);

            GUI.Label(r, on ? "✔" : "✘", s);
        }

        static GUIStyle RowButtonStyle()
        {
            var s = new GUIStyle(GUI.skin.button);
            s.fontSize = 13;
            s.fontStyle = FontStyle.Bold;
            s.border = new RectOffset(8, 8, 8, 8);
            s.alignment = TextAnchor.MiddleCenter;
            return s;
        }

        static GUIStyle LabelStyle()
        {
            var s = new GUIStyle(EditorStyles.miniBoldLabel);
            s.fontSize = 11;
            return s;
        }
    }
}
#endif