using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace AIDefenders.Assets {
public static class LaserTurretPrefabBuilder {
    [Serializable] public class Part { public string name, group, color; public Vector3[] vertices; public int[] triangles; }
    [Serializable] public class Model { public Part[] parts; }
    [MenuItem("Tools/AI Defenders/Create Laser Turret")]
    public static void Build() {
        string[] ids = AssetDatabase.FindAssets("LaserTurretMeshData");
        string source = null;
        foreach (string id in ids) { string p = AssetDatabase.GUIDToAssetPath(id); if (p.EndsWith("/LaserTurretMeshData.json")) { source = p; break; } }
        if (source == null) { Debug.LogError("LaserTurretMeshData.json was not found. Copy the complete AIDefendersLaserTurret folder into Assets."); return; }
        Shader shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("This builder supports Built-in and URP. Install/select the appropriate render pipeline first."); return; }
        Model model = JsonUtility.FromJson<Model>(File.ReadAllText(source));
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersLaserTurretGenerated");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
        GameObject root = new GameObject("LaserTurret");
        try {
            Transform baseT = Child("Base", root.transform, Vector3.zero);
            Transform yaw = Child("YawPivot", root.transform, Vector3.zero);
            Transform pitch = Child("PitchPivot", yaw, new Vector3(0,1.55f,0));
            Child("LaserOrigin", pitch, new Vector3(0,0,1.59f));
            foreach (Part p in model.parts) {
                Transform parent = p.group == "Base" ? baseT : p.group == "Yaw" ? yaw : pitch;
                GameObject go = new GameObject(p.name); go.transform.SetParent(parent, false);
                Mesh mesh = new Mesh(); mesh.name = p.name;
                mesh.vertices = p.vertices; mesh.triangles = p.triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, folder + "/" + p.name + ".asset");
                Material mat = new Material(shader); Color color; ColorUtility.TryParseHtmlString(p.color, out color); mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .25f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .25f);
                if (p.color == "#24eee0") { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.5f); }
                AssetDatabase.CreateAsset(mat, folder + "/" + p.name + ".mat");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            BoxCollider col = root.AddComponent<BoxCollider>(); col.center = new Vector3(0,1.05f,.24f); col.size = new Vector3(2.26f,2.10f,2.70f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/LaserTurret.prefab");
            AssetDatabase.SaveAssets(); Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
            Debug.Log("Turret prefab created: " + folder + "/LaserTurret.prefab");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static Transform Child(string name, Transform parent, Vector3 position) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
}
}
