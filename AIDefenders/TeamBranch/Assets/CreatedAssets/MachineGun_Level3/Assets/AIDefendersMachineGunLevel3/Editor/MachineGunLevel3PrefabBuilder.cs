using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace AIDefenders.Assets {
public static class MachineGunLevel3PrefabBuilder {
    [Serializable] public class Part { public string name, group, color; public Vector3[] vertices; public int[] triangles; }
    [Serializable] public class Model { public Part[] parts; }
    [MenuItem("Tools/AI Defenders/Create Machine Gun Level 3")]
    public static void Build() {
        string[] ids = AssetDatabase.FindAssets("MachineGunLevel3MeshData");
        string source = null;
        foreach (string id in ids) { string p = AssetDatabase.GUIDToAssetPath(id); if (p.EndsWith("/MachineGunLevel3MeshData.json")) { source = p; break; } }
        if (source == null) { Debug.LogError("MachineGunLevel3MeshData.json was not found. Copy the complete AIDefendersMachineGunLevel3 folder into Assets."); return; }
        Shader shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("This builder supports Built-in and URP. Install/select the appropriate render pipeline first."); return; }
        Model model = JsonUtility.FromJson<Model>(File.ReadAllText(source));
        if(model==null || model.parts==null || model.parts.Length==0) throw new InvalidDataException("Empty model data.");
        foreach(Part p in model.parts) {
            if(p.vertices==null || p.triangles==null || p.triangles.Length%3!=0) throw new InvalidDataException("Invalid mesh.");
            foreach(int i in p.triangles) if(i<0 || i>=p.vertices.Length) throw new InvalidDataException("Invalid mesh index.");
        }
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersMachineGunLevel3Generated");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
        GameObject root = new GameObject("MachineGunLevel3");
        try {
            Transform baseT = Child("Base", root.transform, Vector3.zero);
            Transform yaw = Child("YawPivot", root.transform, Vector3.zero);
            Transform pitch = Child("PitchPivot", yaw, new Vector3(0,1.55f,0));
            Transform[] recoils = new Transform[3];
            string[] sides={"Left","Center","Right"};
            for(int pod=0;pod<3;pod++) {
                recoils[pod]=Child("Recoil"+sides[pod],pitch,Vector3.zero);
                for(int i=0;i<6;i++) {
                    float a=i*Mathf.PI/3f;
                    Child("MuzzlePoint"+(i+1),recoils[pod],new Vector3((pod-1)*.72f+.162f*Mathf.Cos(a)*.62f,.162f*Mathf.Sin(a)-.08f,1.67f));
                }
            }
            foreach (Part p in model.parts) {
                Transform parent = p.group == "Base" ? baseT : p.group == "Yaw" ? yaw : p.group == "Pitch" ? pitch : p.group == "RecoilLeft" ? recoils[0] : p.group == "RecoilCenter" ? recoils[1] : recoils[2];
                GameObject go = new GameObject(p.name); go.transform.SetParent(parent, false);
                Mesh mesh = new Mesh(); mesh.name = p.name;
                mesh.vertices = p.vertices; mesh.triangles = p.triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, folder + "/" + p.name + ".asset");
                Material mat = new Material(shader); Color color; ColorUtility.TryParseHtmlString(p.color, out color); mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .25f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .25f);
                if (p.name.StartsWith("CyanBand",StringComparison.Ordinal)) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.5f); }
                AssetDatabase.CreateAsset(mat, folder + "/" + p.name + ".mat");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            BoxCollider col = root.AddComponent<BoxCollider>(); col.center = new Vector3(0,1.05f,.25f); col.size = new Vector3(2.26f,2.10f,2.80f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/MachineGunLevel3.prefab");
            AssetDatabase.SaveAssets(); Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
            Debug.Log("Turret prefab created: " + folder + "/MachineGunLevel3.prefab");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static Transform Child(string name, Transform parent, Vector3 position) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
}
}
