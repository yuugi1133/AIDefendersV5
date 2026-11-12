using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class GrasslandSceneryBuilder
{
    const string Folder = "Assets/Scenes/GameStages/GrassField/SceneryUpgrade";
    const string RootName = "Grassland_Scenery_Upgrade";
    const string Pines = "Assets/AssetStore/Pine Trees/Prefabs/";
    const string Farm = "Assets/AssetStore/Easy3D/Stylized Farm Asset Pack - Small/Prefabs/";
    static Terrain terrain;
    static System.Random random;
    static readonly List<Vector2[]> routes = new List<Vector2[]>();
    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    [MenuItem("Tools/AI Defenders/Upgrade Grassland Scenery")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before upgrading scenes.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        prefabs.Clear();
        Scene original = SceneManager.GetActiveScene();
        string sourcePath = ScenePath(1);
        Scene source = SceneManager.GetSceneByPath(sourcePath);
        bool opened = !source.isLoaded;
        if (!opened && source.isDirty) throw new InvalidOperationException("The grassland scene has unsaved edits; save it before rebuilding scenery.");
        if (opened) source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
        GameObject root = null;
        try
        {
            SceneManager.SetActiveScene(source);
            foreach (GameObject go in source.GetRootGameObjects())
                if (go.GetComponent<Terrain>() != null) terrain = go.GetComponent<Terrain>();
            if (terrain == null) throw new InvalidOperationException("Grassland Terrain missing.");
            CollectRoutes(source);
            random = new System.Random(61016);
            root = new GameObject(RootName);
            BuildNature(root.transform);
            BuildRanchWorkyard(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/GrasslandScenery.prefab");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(source, true);
        }

        for (int i = 1; i <= 3; i++)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath(i));
            bool open = !scene.isLoaded;
            if (!open && scene.isDirty) throw new InvalidOperationException("Save grassland scene edits before applying the upgrade.");
            if (open) scene = EditorSceneManager.OpenScene(ScenePath(i), OpenSceneMode.Additive);
            try
            {
                foreach (GameObject go in scene.GetRootGameObjects())
                    if (go.name == RootName) UnityEngine.Object.DestroyImmediate(go);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/GrasslandScenery.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = RootName;
                foreach (Light lamp in instance.GetComponentsInChildren<Light>(true))
                {
                    lamp.enabled = i > 1;
                    lamp.intensity = i == 3 ? 2.5f : 1.2f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (open) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Grassland scenery applied to day, dusk and night. Paths, goals and Terrain data preserved.");
    }

    static string ScenePath(int index) { return "Assets/Scenes/GameStages/GrassField/Grassfield_" + index + ".unity"; }

    static void CollectRoutes(Scene scene)
    {
        routes.Clear();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Path_sp", StringComparison.Ordinal) && t.childCount > 0)
                {
                    var points = new List<Vector2>();
                    points.Add(new Vector2(root.transform.position.x, root.transform.position.z));
                    foreach (Transform waypoint in t) points.Add(new Vector2(waypoint.position.x, waypoint.position.z));
                    points.Add(new Vector2(.2f, 58.9f));
                    routes.Add(points.ToArray());
                }
    }

    static float Height(float x, float z) { return terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y; }
    static float Range(float min, float max) { return min + (float)random.NextDouble() * (max - min); }

    static bool ClearGround(float x, float z, float clearance)
    {
        Vector2 p = new Vector2(x, z);
        if (z > 43 && Mathf.Abs(x) < 42) return false; // Existing ranch, goal and new workyard.
        if (Height(x, z) < -.15f) return false; // Lake and wet bank.
        if (terrain.terrainData.GetSteepness((x - terrain.transform.position.x) / terrain.terrainData.size.x,
            (z - terrain.transform.position.z) / terrain.terrainData.size.z) > 26) return false;
        foreach (Vector2[] path in routes)
            for (int i = 1; i < path.Length; i++)
            {
                Vector2 line = path[i] - path[i - 1];
                float t = line.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(p - path[i - 1], line) / line.sqrMagnitude) : 0;
                if (Vector2.Distance(p, path[i - 1] + line * t) < clearance) return false;
            }
        return true;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }

    static GameObject Place(Transform parent, string path, float x, float z, float scale, float yaw)
    {
        GameObject prefab;
        if (!prefabs.TryGetValue(path, out prefab))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Scenery prefab missing: " + path);
            prefabs.Add(path, prefab);
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = new Vector3(x, Height(x, z), z);
        instance.transform.rotation = Quaternion.Euler(0, yaw, 0);
        instance.transform.localScale = Vector3.one * scale;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
        return instance;
    }

    static void BuildNature(Transform root)
    {
        Transform nature = Group(root, "Meadow_Groves_And_RockGardens");
        Vector2[] centers = { new Vector2(-6,-124), new Vector2(-8,-74), new Vector2(9,-39), new Vector2(85,-92),
            new Vector2(94,-33), new Vector2(52,28), new Vector2(-37,-116), new Vector2(35,-158), new Vector2(-47,-14) };
        string[] bushes = { "bush_01.prefab", "bush_02.prefab", "bush_03.prefab" };
        for (int i = 0; i < centers.Length; i++)
        {
            Transform cluster = Group(nature, "Natural_Island_" + (i + 1));
            for (int j = 0; j < 20; j++)
            {
                float x = centers[i].x + Range(-11, 11), z = centers[i].y + Range(-10, 10);
                if (!ClearGround(x, z, 13)) continue;
                if (j < 3) Place(cluster, Pines + "pine_tree_" + (j % 3 + 1) + ".prefab", x, z, Range(.7f, 1.15f), Range(0,360));
                else if (j < 8) Place(cluster, Pines + (j % 2 == 0 ? "rock4.prefab" : "rock5.prefab"), x, z, Range(.8f, 1.8f), Range(0,360));
                else Place(cluster, Pines + bushes[j % 3], x, z, Range(2f, 3.6f), Range(0,360));
            }
        }
        Transform grass = Group(root, "Grass_And_FallenWood");
        for (int i = 0; i < 460; i++)
        {
            float x = Range(-70, 105), z = Range(-170, 95);
            if (!ClearGround(x, z, 9)) continue;
            Place(grass, Pines + (i % 37 == 0 ? "stump7.prefab" : "grass_1.prefab"), x, z, Range(1.4f, 3.2f), Range(0,360));
        }
        Place(grass, Pines + "broken_pine.prefab", -8, -106, 1.1f, 64);
        BuildFlowers(root);
    }

    static Material Material(string name, Color color, bool glow = false)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.color = color;
        material.SetFloat("_Smoothness", .08f);
        if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.5f); }
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.layer = 2;
        return go;
    }

    static void BuildRanchWorkyard(Transform root)
    {
        Transform yard = Group(root, "Ranch_Workyard");
        Place(yard, Farm + "Wheel.prefab", 16.8f, 77, 2.2f, 80);
        Place(yard, Farm + "Pitchfork.prefab", 17.2f, 76, 1.6f, 24);
        Place(yard, Farm + "Rake.prefab", 18.2f, 76.4f, 1.6f, 16);
        const string crate = "Assets/AssetStore/Blacksmith Pack/Prefabs/Large Objects/Furniture&Decor/Crate.prefab";
        Place(yard, crate, 22, 78, 1.4f, 12);
        Place(yard, crate, 23.6f, 78, 1.1f, -8);
        Place(yard, Farm + "Wooden Sign - Farm.prefab", 19, 72, 1.4f, 180);
        Place(yard, Farm + "Fence Fixed.prefab", 23, 82, 1.5f, 90);
        Place(yard, Farm + "Fence Fixed.prefab", 26, 82, 1.5f, 90);
        Material hay = Material("Meadow_Hay", new Color(.77f,.58f,.19f));
        Transform stack = Group(yard, "Hay_Bale_Stack");
        stack.position = new Vector3(18, Height(18,83), 83);
        for (int i = 0; i < 5; i++)
        {
            var bale = Shape(stack, "Hay_Bale", PrimitiveType.Cylinder, new Vector3((i % 3) * 1.35f, i < 3 ? .65f : 1.8f, i < 3 ? 0 : .2f), new Vector3(1.2f,.7f,1.2f), hay);
            bale.transform.localRotation = Quaternion.Euler(90,0,0);
        }
        AddLantern(yard, 18, 72);
        BuildWell(yard, 15, 89);
    }

    static void BuildWell(Transform parent, float x, float z)
    {
        Transform well = Group(parent, "Ranch_Stone_Well");
        well.position = new Vector3(x, Height(x,z), z);
        Material stone = Material("Meadow_Well_Stone", new Color(.48f,.51f,.46f));
        Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/GameStages/GrassField/Ranch_Wood.mat");
        Material roof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/GameStages/GrassField/Ranch_Red.mat");
        Material water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/GameStages/GrassField/Ranch_Water.mat");
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 10; i++)
            {
                float angle = (i + row * .5f) * 36;
                var block = Shape(well, "Well_Stone", PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*1.1f,.3f+row*.48f,Mathf.Cos(angle*Mathf.Deg2Rad)*1.1f), new Vector3(.72f,.45f,.40f), stone);
                block.transform.localRotation = Quaternion.Euler(0,angle,0);
            }
        Shape(well, "Water", PrimitiveType.Cylinder, new Vector3(0,.48f,0), new Vector3(1.8f,.03f,1.8f), water);
        Shape(well, "Left_Post", PrimitiveType.Cube, new Vector3(-1.45f,1.65f,0), new Vector3(.18f,3.3f,.18f), wood);
        Shape(well, "Right_Post", PrimitiveType.Cube, new Vector3(1.45f,1.65f,0), new Vector3(.18f,3.3f,.18f), wood);
        Shape(well, "Beam", PrimitiveType.Cube, new Vector3(0,2.4f,0), new Vector3(3,.15f,.15f), wood);
        for (int side = -1; side <= 1; side += 2)
        {
            var panel = Shape(well, "Roof", PrimitiveType.Cube, new Vector3(side*.73f,3.27f,0), new Vector3(1.72f,.16f,2.5f), roof);
            panel.transform.localRotation = Quaternion.Euler(0,0,-side*25);
        }
    }

    static void AddLantern(Transform parent, float x, float z)
    {
        Transform lamp = Group(parent, "Warm_Ranch_Lantern");
        lamp.position = new Vector3(x, Height(x,z), z);
        Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/GameStages/GrassField/Ranch_Wood.mat");
        Material light = Material("Meadow_Lantern", new Color(1,.63f,.20f), true);
        Shape(lamp, "Post", PrimitiveType.Cube, new Vector3(0,1.5f,0), new Vector3(.18f,3,.18f), wood);
        Shape(lamp, "Lantern", PrimitiveType.Cube, new Vector3(0,3.05f,0), new Vector3(.45f,.65f,.45f), light);
        var bulb = new GameObject("Night_Light").AddComponent<Light>();
        bulb.transform.SetParent(lamp,false); bulb.transform.localPosition = new Vector3(0,3.05f,0);
        bulb.type = LightType.Point; bulb.color = new Color(1,.72f,.35f); bulb.range = 11;
        bulb.shadows = LightShadows.None; bulb.enabled = false;
    }

    static void BuildFlowers(Transform root)
    {
        Material[] materials = { Material("Wildflower_Cream", new Color(.96f,.9f,.65f)), Material("Wildflower_Gold", new Color(1,.7f,.12f)), Material("Wildflower_Lilac", new Color(.64f,.48f,.82f)) };
        // A single shared mesh per color keeps the little flowers inexpensive.
        for (int color = 0; color < materials.Length; color++)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var stems = new List<int>();
            for (int i = 0; i < 220; i++)
            {
                float x = Range(-55,90), z = Range(-140,38);
                if (!ClearGround(x,z,10)) continue;
                float height = Height(x,z) + Range(.22f,.48f);
                int start = vertices.Count;
                vertices.Add(new Vector3(x,height,z));
                for (int j = 0; j < 10; j++)
                {
                    float angle = j * Mathf.PI / 5;
                    float radius = j % 2 == 0 ? .34f : .15f;
                    vertices.Add(new Vector3(x + Mathf.Cos(angle)*radius,height,z + Mathf.Sin(angle)*radius));
                }
                for (int j = 0; j < 10; j++) { triangles.Add(start); triangles.Add(start + (j+1)%10+1); triangles.Add(start+j+1); }
                int stem = vertices.Count;
                float ground = Height(x,z);
                vertices.Add(new Vector3(x-.025f,ground,z)); vertices.Add(new Vector3(x+.025f,ground,z));
                vertices.Add(new Vector3(x+.025f,height,z)); vertices.Add(new Vector3(x-.025f,height,z));
                stems.AddRange(new[] { stem,stem+2,stem+1,stem,stem+3,stem+2,stem,stem+1,stem+2,stem,stem+2,stem+3 });
            }
            var mesh = new Mesh(); mesh.name = "Meadow_Wildflowers_" + color;
            mesh.SetVertices(vertices); mesh.subMeshCount=2; mesh.SetTriangles(triangles,0); mesh.SetTriangles(stems,1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Folder + "/Wildflowers_" + color + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
            else AssetDatabase.CreateAsset(mesh,path);
            Transform group = Group(root, "Wildflower_Patches_" + color);
            group.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = group.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = new[] { materials[color], Material("Wildflower_Stem",new Color(.22f,.39f,.10f)) }; renderer.shadowCastingMode = ShadowCastingMode.Off;
            group.gameObject.layer = 2;
        }
    }
}
