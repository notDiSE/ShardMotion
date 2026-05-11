#if UNITY_EDITOR
using System.Collections;
using System.IO;
using ShardMotion;
using UnityEditor;
using UnityEngine;
/// <summary>
/// Script used to generate promotional materials, captures both ingame scene and camera image
/// </summary>
public class CaptureRealAndScene : MonoBehaviour
{
    public TrackingCamera trackingCamera;
    public Camera captureCamera;
 
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) StartCoroutine(Autospoust());
    }

    IEnumerator Autospoust()
    {
        yield return new WaitForSeconds(3);
        Capture();
    }
 
    void Capture()
    {
        string path = EditorUtility.SaveFilePanel("Uloz snimky", "", "snapshot", "png");
        if (string.IsNullOrEmpty(path)) return;
        if (path.EndsWith(".png")) path = path[..^4];
 
        var real = new Texture2D(trackingCamera.tex.width, trackingCamera.tex.height, TextureFormat.RGBA32, false);
        real.SetPixels32(trackingCamera.tex.GetPixels32());
        real.Apply();
        File.WriteAllBytes(path + "_real.png", real.EncodeToPNG());
        Destroy(real);
 
        var prevFlags = captureCamera.clearFlags;
        var prevBg = captureCamera.backgroundColor;
        var prevTarget = captureCamera.targetTexture;
 
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);
 
        var rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        captureCamera.targetTexture = rt;
        captureCamera.Render();
        
        RenderTexture.active = rt;
        var virt = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        virt.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        virt.Apply();
        File.WriteAllBytes(path + "_virtual.png", virt.EncodeToPNG());
        Destroy(virt);
 
        RenderTexture.active = null;
        captureCamera.targetTexture = prevTarget;
        captureCamera.clearFlags = prevFlags;
        captureCamera.backgroundColor = prevBg;
        rt.Release();
        Destroy(rt);
    }
}
#endif
