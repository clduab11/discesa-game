using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor camera helper: captures Game View screenshots for visual-check evidence.
/// Writes to Screenshots/ (gitignored) with a timestamp. No runtime cost in builds
/// unless the key combo is pressed in Play mode.
///
/// Menu: Discesa > Capture Game View Screenshot
/// Play mode: Shift+F12 captures one frame.
/// </summary>
public static class ScreenshotTool
{
    const string Folder = "Screenshots";

    [MenuItem("Discesa/Capture Game View Screenshot")]
    public static void CaptureFromMenu()
    {
        var cam = FindCamera();
        if (cam == null)
        {
            Debug.LogWarning("[Discesa] Screenshot: no Camera found; open a scene and press Play first.");
            return;
        }
        Capture(cam, "menu");
    }

    public static void CaptureFromPlayMode(Camera cam, string tag)
    {
        if (cam == null) cam = FindCamera();
        if (cam == null) return;
        Capture(cam, tag);
    }

    static Camera FindCamera()
    {
        var cam = Camera.main;
        if (cam != null) return cam;
        return UnityEngine.Object.FindAnyObjectByType<Camera>();
    }

    static void Capture(Camera cam, string tag)
    {
        Directory.CreateDirectory(Folder);
        int w = Mathf.Min(Screen.width > 0 ? Screen.width : 1280, 1920);
        int h = Mathf.Min(Screen.height > 0 ? Screen.height : 720, 1080);
        if (w <= 0) w = 1280;
        if (h <= 0) h = 720;

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevRt = RenderTexture.active;
        var prevTarget = cam.targetTexture;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            string name = $"discesa_{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            File.WriteAllBytes(Path.Combine(Folder, name), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[Discesa] Screenshot saved: {Path.Combine(Folder, name)}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Discesa] Screenshot failed: {e.Message}");
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevRt;
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}

/// <summary>
/// Play-mode screenshot key: Shift+F12 saves an evidence frame to Screenshots/.
/// Attach to any object in the scene; GameManager bootstrap does not add it
/// automatically so normal play is untouched unless a human opts in.
/// Kept in the Editor assembly on purpose: the Editor assembly references the
/// Input System, the runtime assembly must not need it.
/// </summary>
public class PlayModeScreenshotKey : MonoBehaviour
{
    void Update()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        // Editor assembly references Unity.InputSystem; runtime does not need it.
        var kbType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        if (kbType != null)
        {
            dynamic kb = kbType.GetProperty("current")?.GetValue(null);
            if (kb != null && kb.shiftKey.isPressed && kb.f12Key.wasPressedThisFrame) pressed = true;
        }
#endif
        if (!pressed && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            pressed = Input.GetKeyDown(KeyCode.F12);
        if (pressed) ScreenshotTool.CaptureFromPlayMode(Camera.main, "play");
    }
}
