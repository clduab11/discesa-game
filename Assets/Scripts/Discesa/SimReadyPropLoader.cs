using UnityEngine;

/// <summary>
/// Loads the single SimReady trial prop (white crate). Visual-only, no colliders.
/// Source: codepawl/simready-glb (CC-BY-4.0, credit NVIDIA), glb/crate_white_a14.glb.
/// Returns null when the dataset file has not been downloaded/imported yet.
/// </summary>
public static class SimReadyPropLoader
{
    const string PropAssetPath = "Assets/Art/Models/simready_crate_trial.glb";
    const string PropResourcesPath = "Discesa/simready_crate_trial";

    public static GameObject TryInstantiate()
    {
#if UNITY_EDITOR
        var editorModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PropAssetPath);
        if (editorModel != null)
        {
            return Spawn(editorModel);
        }
#endif
        var prefab = Resources.Load<GameObject>(PropResourcesPath);
        if (prefab == null) return null;
        return Spawn(prefab);
    }

    static GameObject Spawn(GameObject prefab)
    {
        var go = (GameObject)Object.Instantiate(prefab);
        go.name = "SimReadyCrate";
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) Object.Destroy(c);
        return go;
    }
}
