using UnityEditor;
using UnityEngine;

namespace ShardMotion.Calibration
{
    /// <summary>
    /// Calibration device scriptable object, holds data about single calibraiton device
    /// </summary>
    [CreateAssetMenu(fileName = "CalibrationDevice", menuName = "Shard Motion/CalibrationDevice")]
    public class CalibrationDevice : ScriptableObject
    {
        [Header("Sizes")]
        public float codeSize;
        public float cubeSize;

        [Header("IDs per direction")] 
        public int forwardID = 0;
        public int rightID = 1;
        public int backwardID = 2;
        public int leftID = 3;
    
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(CalibrationDevice))]
    public class CalibrationDeviceEditor : Editor
    {
        static bool previewEnabled;

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            previewEnabled = GUILayout.Toggle(previewEnabled, "Preview in Scene", "Button");
        }
        
        void OnSceneGUI(SceneView sceneView)
        {
            if (!previewEnabled) return;
            
            // Calibration device can be drawn in scene

            var data = (CalibrationDevice)target;
            if (data == null) return;

            Vector3 origin = sceneView.pivot;

            Handles.matrix = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one);

            DrawCube(data.cubeSize); // draws the base cube
            
            // Draws all the faces, to match ids
            DrawCodeFace(Vector3.forward, Quaternion.identity, data.forwardID, data);
            DrawCodeFace(Vector3.right, Quaternion.Euler(0, 90, 0), data.rightID, data);
            DrawCodeFace(Vector3.back, Quaternion.Euler(0, 180, 0), data.backwardID, data);
            DrawCodeFace(Vector3.left, Quaternion.Euler(0, -90, 0), data.leftID, data);
        }

        void DrawCube(float size)
        {
            // draw all faces of cube
            DrawCubeFace(Vector3.forward, Quaternion.identity, size);
            DrawCubeFace(Vector3.back, Quaternion.Euler(0, 180, 0), size);
            DrawCubeFace(Vector3.right, Quaternion.Euler(0, 90, 0), size);
            DrawCubeFace(Vector3.left, Quaternion.Euler(0, -90, 0), size);
            DrawCubeFace(Vector3.up, Quaternion.Euler(-90, 0, 0), size);
            DrawCubeFace(Vector3.down, Quaternion.Euler(90, 0, 0), size);
        }

        // Draw one face
        void DrawCubeFace(Vector3 dir, Quaternion rot, float size)
        {
            Vector3 center = dir * size * 0.5f;

            Matrix4x4 m = Matrix4x4.TRS(center, rot, Vector3.one);
            using (new Handles.DrawingScope(m))
            {
                float h = size * 0.5f;

                Vector3[] quad =
                {
                    new Vector3(-h, -h, 0),
                    new Vector3(-h,  h, 0),
                    new Vector3( h,  h, 0),
                    new Vector3( h, -h, 0),
                };

                Handles.DrawSolidRectangleWithOutline(
                    quad,
                    new Color(1, 1, 1, 0.06f),
                    new Color(1, 1, 1, 0.35f)
                );
            }
        }

        // Draw face with marker ID
        void DrawCodeFace(Vector3 dir, Quaternion rot, int id, CalibrationDevice data)
        {
            Vector3 center = dir * data.cubeSize * 0.5f;

            Matrix4x4 m = Matrix4x4.TRS(center, rot, Vector3.one);
            using (new Handles.DrawingScope(m))
            {
                float h = data.codeSize * 0.5f;

                Vector3[] quad =
                {
                    new Vector3(-h, -h, 0),
                    new Vector3(-h,  h, 0),
                    new Vector3( h,  h, 0),
                    new Vector3( h, -h, 0),
                };

                Handles.DrawSolidRectangleWithOutline(
                    quad,
                    new Color(1, 0, 0, 0.18f),
                    Color.red
                );

                Handles.Label(Vector3.zero, id.ToString(), EditorStyles.boldLabel);
            }
        }
    }
    #endif
}