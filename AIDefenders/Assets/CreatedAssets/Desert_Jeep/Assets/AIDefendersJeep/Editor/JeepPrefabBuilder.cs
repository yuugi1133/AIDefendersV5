using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace AIDefenders.Assets {
public static class JeepPrefabBuilder {
 [Serializable] public class Part { public string name,group,color; public Vector3[] vertices; public int[] triangles; }
 [Serializable] public class Model { public Part[] parts; }
 class Batch { public string group,color; public List<Vector3> vertices=new List<Vector3>(); public List<int> triangles=new List<int>(); }
 [MenuItem("Tools/AI Defenders/Create Desert Jeep")]
 public static void Build() {
  string source=null;
  foreach(string id in AssetDatabase.FindAssets("JeepMeshData")) {string p=AssetDatabase.GUIDToAssetPath(id);if(p.EndsWith("/JeepMeshData.json")){source=p;break;}}
  if(source==null){Debug.LogError("Copy the complete AIDefendersJeep folder into Assets.");return;}
  Shader shader=GraphicsSettings.currentRenderPipeline==null?Shader.Find("Standard"):Shader.Find("Universal Render Pipeline/Lit");
  if(shader==null){Debug.LogError("Built-in or URP shader required.");return;}
  Model data=JsonUtility.FromJson<Model>(File.ReadAllText(source));
  string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersJeepGenerated");AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder));
  GameObject root=new GameObject("DesertJeep");
  try {
   var parents=new Dictionary<string,Transform>();
   parents.Add("Body",Child("Body",root.transform,Vector3.zero));
   foreach(string side in new[]{"L","R"}) {
    float x=side=="L"?-.98f:.98f;
    Transform steer=Child("Steer_F"+side,root.transform,new Vector3(x,.51f,1.20f));
    parents.Add("WheelF"+side,Child("Wheel_F"+side,steer,Vector3.zero));
    parents.Add("WheelR"+side,Child("Wheel_R"+side,root.transform,new Vector3(x,.51f,-1.20f)));
   }
   var batches=new Dictionary<string,Batch>();
   foreach(Part p in data.parts){string key=p.group+"_"+p.color.Substring(1);Batch b;if(!batches.TryGetValue(key,out b)){b=new Batch{group=p.group,color=p.color};batches.Add(key,b);}int offset=b.vertices.Count;b.vertices.AddRange(p.vertices);foreach(int index in p.triangles)b.triangles.Add(index+offset);}
   var materials=new Dictionary<string,Material>();
   foreach(var entry in batches){Batch b=entry.Value;Transform parent=parents[b.group];
    GameObject go=new GameObject(entry.Key);go.transform.SetParent(parent,false);
    Mesh mesh=new Mesh();mesh.name=entry.Key;if(b.vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
    mesh.SetVertices(b.vertices);mesh.SetTriangles(b.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/"+entry.Key+".asset");
    Material mat;if(!materials.TryGetValue(b.color,out mat)){mat=new Material(shader);Color color;ColorUtility.TryParseHtmlString(b.color,out color);mat.color=color;if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",.2f);if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.2f);AssetDatabase.CreateAsset(mat,folder+"/Material_"+b.color.Substring(1)+".mat");materials.Add(b.color,mat);}
    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
   }
   BoxCollider bodyCol=parents["Body"].gameObject.AddComponent<BoxCollider>();bodyCol.center=new Vector3(0,1.15f,-.05f);bodyCol.size=new Vector3(1.85f,.65f,3.60f);
   GameObject cabin=new GameObject("CabinCollider");cabin.transform.SetParent(parents["Body"],false);
   BoxCollider roofCol=cabin.AddComponent<BoxCollider>();roofCol.center=new Vector3(0,1.83f,-.58f);roofCol.size=new Vector3(1.85f,.77f,2.5f);
   GameObject prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/DesertJeep.prefab");AssetDatabase.SaveAssets();Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
   Debug.Log("Desert jeep created: "+folder+"/DesertJeep.prefab");
  } finally {UnityEngine.Object.DestroyImmediate(root);}
 }
 static Transform Child(string name,Transform parent,Vector3 local){Transform t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
}
}
