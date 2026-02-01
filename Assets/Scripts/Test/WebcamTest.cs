using UnityEditor;
using UnityEngine;

using UnityEngine;

public class WebcamTest : MonoBehaviour
{
    public WebCamTexture cam;
    public WebCamTexture cam2;

    void Start()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) return;

        cam = new WebCamTexture(devices[0].name);
        cam2 = new WebCamTexture(devices[1].name);
        cam.Play();
        cam2.Play();
    }

    void OnDisable()
    {
        if (cam != null && cam.isPlaying)
            cam.Stop();
        if (cam2 != null && cam2.isPlaying)
            cam2.Stop();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(WebcamTest))]
public class WebcamTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = (WebcamTest)target;

        DrawCamPreview(t.cam, "Cam 1");
        DrawCamPreview(t.cam2, "Cam 2");

        Repaint();
    }

    void DrawCamPreview(WebCamTexture cam, string label)
    {
        if (cam == null || !cam.isPlaying || cam.width <= 16) return;

        GUILayout.Label(label, EditorStyles.boldLabel);

        float aspect = (float)cam.width / cam.height;
        Rect r = GUILayoutUtility.GetAspectRect(aspect, GUILayout.Height(200));

        EditorGUI.DrawPreviewTexture(r, cam);
    }
}
#endif
