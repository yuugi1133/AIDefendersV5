using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AIDefenders.Assets.TurretPlatform
{
    public static class TurretPlatformPrefabBuilder
    {
        [Serializable] public class Part
        {
            public string name, group, material, color;
            public bool emissive;
            public Vector3[] vertices;
            public int[] triangles;
        }
        [Serializable] public class Model { public Part[] parts; }

        [MenuItem("Tools/AI Defenders/Create Turret Platform")]
        public static void Build()
        {
            string source = null;
            foreach (string id in AssetDatabase.FindAssets("TurretPlatformMeshData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (path.EndsWith("/TurretPlatformMeshData.json", StringComparison.Ordinal))
                { source = path; break; }
            }
            if (source == null)
            {
                Debug.LogError("TurretPlatformMeshData.json is missing. Copy the complete AIDefendersPlatform folder into Assets.");
                return;
            }
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && !pipeline.GetType().Name.Contains("Universal"))
            {
                Debug.LogError("This builder supports Built-in and URP. Configure HDRP materials separately.");
                return;
            }
            Shader shader = Shader.Find(pipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("The required Lit shader was not found."); return; }
            Model model = JsonUtility.FromJson<Model>(File.ReadAllText(source));
            if (model == null || model.parts == null || model.parts.Length == 0)
                throw new InvalidDataException("The Turret platform model data is empty.");
            // Check all data before creating any assets.
            foreach (Part part in model.parts)
            {
                if (part.vertices == null || part.triangles == null || part.triangles.Length % 3 != 0)
                    throw new InvalidDataException("Invalid mesh: " + part.name);
                foreach (int index in part.triangles)
                    if (index < 0 || index >= part.vertices.Length)
                        throw new InvalidDataException("Invalid index: " + part.name);
                if (part.group != "Base" && part.group != "Yaw" && part.group != "Pitch" && part.group != "Recoil_L" && part.group != "Recoil_R")
                    throw new InvalidDataException("Invalid group: " + part.group);
            }
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersPlatformGenerated");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            var root = new GameObject("TurretPlatform");
            try
            {
                Transform baseRoot = Child("Base", root.transform, Vector3.zero);
                Child("TurretMount", root.transform, new Vector3(0f, .301f, 0f));
                var materials = new Dictionary<string, Material>();
                foreach (Part part in model.parts)
                {
                    Transform parent = baseRoot;
                    var go = new GameObject(part.name);
                    go.transform.SetParent(parent, false);
                    var mesh = new Mesh { name = part.name };
                    mesh.vertices = part.vertices;
                    mesh.triangles = part.triangles;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, folder + "/" + part.name + ".asset");
                    Material mat;
                    if (!materials.TryGetValue(part.material, out mat))
                    {
                        mat = new Material(shader) { name = part.material };
                        Color color;
                        if (!ColorUtility.TryParseHtmlString(part.color, out color)) color = Color.white;
                        mat.color = color;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", part.emissive ? 0f : .12f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .28f);
                        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .28f);
                        if (part.emissive)
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", color.linear * 1.6f);
                            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        }
                        AssetDatabase.CreateAsset(mat, folder + "/Material_" + part.material + ".mat");
                        materials.Add(part.material, mat);
                    }
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                }
                BoxCollider baseCollider = baseRoot.gameObject.AddComponent<BoxCollider>();
                baseCollider.center = new Vector3(0f, .15f, 0f);
                baseCollider.size = new Vector3(3.2f, .30f, 3.2f);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/TurretPlatform.prefab");
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("Turret platform turret created: " + folder + "/TurretPlatform.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static Transform Child(string name, Transform parent, Vector3 localPosition)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }
    }
}
