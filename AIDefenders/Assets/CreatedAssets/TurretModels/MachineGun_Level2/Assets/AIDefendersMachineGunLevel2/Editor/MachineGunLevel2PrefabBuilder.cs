using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace AIDefenders.Assets {
public static class MachineGunLevel2PrefabBuilder {
    [Serializable] public class Part { public string name, group, color; public Vector3[] vertices; public int[] triangles; }
    [Serializable] public class Model { public Part[] parts; }
    [MenuItem("Tools/AI Defenders/Create Machine Gun Level 2")]
    public static void Build() {
        string[] ids = AssetDatabase.FindAssets("MachineGunLevel2MeshData");
        string source = null;
        foreach (string id in ids) { string p = AssetDatabase.GUIDToAssetPath(id); if (p.EndsWith("/MachineGunLevel2MeshData.json")) { source = p; break; } }
        if (source == null) { Debug.LogError("MachineGunLevel2MeshData.json was not found. Copy the complete AIDefendersMachineGunLevel2 folder into Assets."); return; }
        Shader shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("This builder supports Built-in and URP. Install/select the appropriate render pipeline first."); return; }
        Model model = JsonUtility.FromJson<Model>(File.ReadAllText(source));
        if(model==null || model.parts==null || model.parts.Length==0) throw new InvalidDataException("Empty model data.");
        foreach(Part p in model.parts) {
            if(p.vertices==null || p.triangles==null || p.triangles.Length%3!=0) throw new InvalidDataException("Invalid mesh.");
            foreach(int i in p.triangles) if(i<0 || i>=p.vertices.Length) throw new InvalidDataException("Invalid mesh index.");
        }
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersMachineGunLevel2Generated");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
        GameObject root = new GameObject("MachineGunLevel2");
        try {
            Transform baseT = Child("Base", root.transform, Vector3.zero);
            Transform yaw = Child("YawPivot", root.transform, Vector3.zero);
            Transform pitch = Child("PitchPivot", yaw, new Vector3(0,1.55f,0));
            Transform recoilLeft = Child("RecoilLeft", pitch, Vector3.zero);
            Transform recoilRight = Child("RecoilRight", pitch, Vector3.zero);
            foreach (int side in new [] { -1, 1 }) {
                Transform mount=side<0 ? recoilLeft : recoilRight;
                Child("MuzzlePoint1",mount,new Vector3(side*.50f-.12f,.11f,1.61f));
                Child("MuzzlePoint2",mount,new Vector3(side*.50f+.12f,.11f,1.61f));
                Child("MuzzlePoint3",mount,new Vector3(side*.50f,-.15f,1.61f));
            }
            foreach (Part p in model.parts) {
                Transform parent = p.group == "Base" ? baseT : p.group == "Yaw" ? yaw : p.group == "Pitch" ? pitch : p.group == "RecoilLeft" ? recoilLeft : recoilRight;
                GameObject go = new GameObject(p.name); go.transform.SetParent(parent, false);
                Mesh mesh = new Mesh(); mesh.name = p.name;
                mesh.vertices = p.vertices; mesh.triangles = p.triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, folder + "/" + p.name + ".asset");
                Material mat = new Material(shader); Color color; ColorUtility.TryParseHtmlString(p.color, out color); mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .25f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .25f);
                if (p.name == "Indicator") { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.5f); }
                AssetDatabase.CreateAsset(mat, folder + "/" + p.name + ".mat");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            BoxCollider col = root.AddComponent<BoxCollider>(); col.center = new Vector3(0,1.05f,.25f); col.size = new Vector3(2.26f,2.10f,2.72f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/MachineGunLevel2.prefab");
            AssetDatabase.SaveAssets(); Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
            Debug.Log("Turret prefab created: " + folder + "/MachineGunLevel2.prefab");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static Transform Child(string name, Transform parent, Vector3 position) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
}
}
