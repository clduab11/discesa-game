using UnityEngine;

/// <summary>
/// Spawns the Blender-authored ship visual as a child of the ship GameObject.
/// Gameplay collision stays with the ShipController's swept box + trigger sphere;
/// the imported mesh never carries colliders.
///
/// The GLB lives at Assets/Art/Models/ship_graybox_v1.glb. GLTFast imports it
/// as a prefab (ModelImporter scripted importer). This loader instantiates the
/// model prefab at runtime so the bootstrap needs no scene wiring. Falls back
/// to graybox primitives when the asset is missing (e.g. in a fresh checkout
/// before the Editor has imported it).
/// </summary>
public static class ShipVisualLoader
{
    const string ShipModelAssetPath = "Assets/Art/Models/ship_graybox_v1.glb";
    const string ShipModelResourcesPath = "Discesa/ship_graybox_v1";

    public static GameObject TryInstantiate(Transform parent)
    {
#if UNITY_EDITOR
        // Editor: load via AssetDatabase so the mesh renders in Play mode without
        // needing a Resources copy. Works for both the Models source and the
        // Resources copy (both are GLTFast ScriptedImporter assets).
        var editorModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ShipModelAssetPath);
        if (editorModel != null)
        {
            var instance = (GameObject)Object.Instantiate(editorModel);
            instance.name = "ShipVisual";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            StripColliders(instance);
            return instance;
        }
#endif
        // Build: Resources.Load works when the .glb was copied to Resources with
        // its GLTFast importer (see DiscesaAssetWire). Load returns the prefab root.
        var prefab = Resources.Load<GameObject>(ShipModelResourcesPath);
        if (prefab == null) return null;

        var go = (GameObject)Object.Instantiate(prefab);
        go.name = "ShipVisual";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        StripColliders(go);
        return go;
    }

    static void StripColliders(GameObject root)
    {
        var colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) Object.Destroy(c);
    }
}
