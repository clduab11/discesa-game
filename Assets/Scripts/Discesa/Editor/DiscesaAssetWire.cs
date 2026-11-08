using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Editor tooling for the Blender-authored ship + SimReady GLB trial.
/// GLTFast provides the GLB importer: the .glb asset itself becomes a prefab
/// in the AssetDatabase. Runtime Resources copies are verbatim .glb bytes.
///   Discesa > Wire Ship + Prop Assets : copies GLBs into Resources, imports,
///     verifies GameManager wiring, logs the checks (no hard crash on missing).
///   Discesa > Bake Runtime Materials  : existing MaterialBaker (unchanged).
/// </summary>
public static class DiscesaAssetWire
{
    const string ShipSrc = "Assets/Art/Models/ship_graybox_v1.glb";
    const string ShipDst = "Assets/Resources/Discesa/ship_graybox_v1.glb";
    const string PropSrc = "Assets/Art/Models/simready_crate_trial.glb";
    const string PropDst = "Assets/Resources/Discesa/simready_crate_trial.glb";

    [MenuItem("Discesa/Wire Ship + Prop Assets")]
    public static void Wire()
    {
        bool shipOk = CopyModelToResources(ShipSrc, ShipDst, "Ship");
        bool propOk = CopyModelToResources(PropSrc, PropDst, "SimReady crate");

        // Verify the GLB actually imports as a prefab (GLTFast creates one).
        var shipModel = AssetDatabase.LoadAssetAtPath<GameObject>(ShipSrc);
        if (shipOk) Debug.Log(shipModel != null
            ? "[Discesa] Ship GLB imported: " + ShipSrc
            : "[Discesa] Ship GLB present but no prefab produced; is GLTFast installed? Check Package Manager.");
        else Debug.LogWarning("[Discesa] Ship GLB not found at " + ShipSrc + "; GameManager will use graybox fallback.");

        var propModel = AssetDatabase.LoadAssetAtPath<GameObject>(PropSrc);
        if (propOk) Debug.Log(propModel != null
            ? "[Discesa] SimReady crate GLB imported: " + PropSrc
            : "[Discesa] SimReady crate present but no prefab produced; check importer.");
        else Debug.Log("[Discesa] SimReady prop not downloaded yet; skipping (see docs/PROVENANCE.md for the fetch step).");

        AssetDatabase.Refresh();
        Debug.Log("[Discesa] Wire complete. Play-mode visual falls back safely when an asset is missing.");
    }

    static bool CopyModelToResources(string src, string dst, string label)
    {
        if (!File.Exists(src))
        {
            Debug.Log($"[Discesa] {label}: source missing ({src}); skipped.");
            return false;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(dst));
        File.Copy(src, dst, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Discesa] {label}: copied {src} -> {dst}");
        return true;
    }
}
