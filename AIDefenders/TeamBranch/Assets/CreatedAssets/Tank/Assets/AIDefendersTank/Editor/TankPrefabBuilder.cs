using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace AIDefenders.Assets {
public static class TankPrefabBuilder {
 [Serializable] public class Part { public string name,group,color; public Vector3[] vertices; public int[] triangles; }
 [Serializable] public class Model { public Part[] parts; }
 class Batch { public string group,color; public List<Vector3> vertices=new List<Vector3>(); public List<int> triangles=new List<int>(); }
 [MenuItem("Tools/AI Defenders/Create Desert Tank")]
 public static void Build() {
  string source=null;
  foreach(string id in AssetDatabase.FindAssets("TankMeshData")) {string p=AssetDatabase.GUIDToAssetPath(id);if(p.EndsWith("/TankMeshData.json")){source=p;break;}}
  if(source==null){Debug.LogError("Copy the complete AIDefendersTank folder into Assets.");return;}
  Shader shader=GraphicsSettings.currentRenderPipeline==null?Shader.Find("Standard"):Shader.Find("Universal Render Pipeline/Lit");
  if(shader==null){Debug.LogError("Built-in or URP shader required.");return;}
  Model data=JsonUtility.FromJson<Model>(File.ReadAllText(source));
  string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/AIDefendersTankGenerated");AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder));
  GameObject root=new GameObject("DesertTank");
  try {
   Transform hull=Child("Hull",root.transform,Vector3.zero);
   Transform left=Child("LeftTrack",root.transform,Vector3.zero),right=Child("RightTrack",root.transform,Vector3.zero);
   Transform yaw=Child("YawPivot",root.transform,new Vector3(0,1.25f,-.22f));
   Transform pitch=Child("PitchPivot",yaw,new Vector3(0,.32f,.88f));
   Transform recoil=Child("Recoil",pitch,Vector3.zero);
   Child("MuzzlePoint",recoil,new Vector3(0,0,2.59f));
   var batches=new Dictionary<string,Batch>();
   foreach(Part p in data.parts){string key=p.group+"_"+p.color.Substring(1);Batch b;if(!batches.TryGetValue(key,out b)){b=new Batch{group=p.group,color=p.color};batches.Add(key,b);}int offset=b.vertices.Count;b.vertices.AddRange(p.vertices);foreach(int index in p.triangles)b.triangles.Add(index+offset);}
   var materials=new Dictionary<string,Material>();
   foreach(var entry in batches){Batch b=entry.Value;Transform parent=b.group=="Hull"?hull:b.group=="TrackL"?left:b.group=="TrackR"?right:b.group=="Yaw"?yaw:b.group=="Pitch"?pitch:recoil;
    GameObject go=new GameObject(entry.Key);go.transform.SetParent(parent,false);
    Mesh mesh=new Mesh();mesh.name=entry.Key;if(b.vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
    mesh.SetVertices(b.vertices);mesh.SetTriangles(b.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/"+entry.Key+".asset");
    Material mat;if(!materials.TryGetValue(b.color,out mat)){mat=new Material(shader);Color color;ColorUtility.TryParseHtmlString(b.color,out color);mat.color=color;if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",.2f);if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.2f);AssetDatabase.CreateAsset(mat,folder+"/Material_"+b.color.Substring(1)+".mat");materials.Add(b.color,mat);}
    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
   }
   BoxCollider col=hull.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.66f,0);col.size=new Vector3(2.65f,1.32f,3.94f);
   BoxCollider turretCol=yaw.gameObject.AddComponent<BoxCollider>();turretCol.center=new Vector3(0,.40f,-.20f);turretCol.size=new Vector3(1.90f,.85f,2.2f);
   GameObject prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/DesertTank.prefab");AssetDatabase.SaveAssets();Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
   Debug.Log("Desert tank created: "+folder+"/DesertTank.prefab");
  } finally {UnityEngine.Object.DestroyImmediate(root);}
 }
 static Transform Child(string name,Transform parent,Vector3 local){Transform t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
}
}
