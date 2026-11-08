using UnityEngine;

/// <summary>
/// Builds the enclosed graybox arena: a 40 x 20 x 40 m box with two pillars.
/// Uses baked material assets so the built player renders correctly (see DiscesaMaterials).
/// </summary>
public static class ArenaBuilder
{
    public static GameObject Build(Vector3 center, float width = 40f, float height = 20f, float depth = 40f)
    {
        var root = new GameObject("Arena");
        root.transform.position = center;

        var floor = DiscesaMaterials.ArenaFloor;
        var trim = DiscesaMaterials.ArenaTrim;

        float halfW = width * 0.5f, halfH = height * 0.5f, halfD = depth * 0.5f, t = 0.2f;

        AddBox(root.transform, "Ceiling", new Vector3(0f, halfH, 0f), new Vector3(width, t, depth), floor);
        AddBox(root.transform, "Floor", new Vector3(0f, -halfH, 0f), new Vector3(width, t, depth), trim);
        AddBox(root.transform, "WallWest", new Vector3(-halfW, 0f, 0f), new Vector3(t, height, depth), floor);
        AddBox(root.transform, "WallEast", new Vector3(halfW, 0f, 0f), new Vector3(t, height, depth), floor);
        AddBox(root.transform, "WallNorth", new Vector3(0f, 0f, halfD), new Vector3(width, height, t), trim);
        AddBox(root.transform, "WallSouth", new Vector3(0f, 0f, -halfD), new Vector3(width, height, t), floor);

        AddBox(root.transform, "PillarSW", new Vector3(-8f, 0f, -8f), new Vector3(2f, height, 2f), trim);
        AddBox(root.transform, "PillarNE", new Vector3(8f, 0f, 8f), new Vector3(2f, height, 2f), trim);

        return root;
    }

    public static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = size;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return go;
    }
}
