using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

[CustomEditor(typeof(CustomObjectCombineTool))]
public class CustomObjectCombineToolEditor : Editor {

	private SerializedProperty	 objectToCombine;

	public CustomObjectCombineTool 		combineTool;

	void OnEnable()    
	{
		combineTool = (CustomObjectCombineTool)target;
		objectToCombine = serializedObject.FindProperty("objectToCombine");
	}

	//------------------------------------------
	public override void OnInspectorGUI() 
	{
		EditorGUILayout.Separator();EditorGUILayout.Separator();

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(objectToCombine);
		if (EditorGUI.EndChangeCheck()){

			combineTool.objectToCombine = (GameObject)objectToCombine.objectReferenceValue;
		}
		EditorGUILayout.Separator();EditorGUILayout.Separator();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("findLocalPosition"), new GUIContent("Find Local Position"));
        EditorGUILayout.LabelField("(Sets position to the first submesh - can be useful if the submeshes are far from the parent)");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("remove_Average_Child_Z_Offsets"), new GUIContent("Remove Average Child Z Offsets"));
        EditorGUILayout.LabelField("(Ensures correctly centered Z offsets for use in AFWB)");

        EditorGUILayout.Separator();EditorGUILayout.Separator();
		if(GUILayout.Button("Combine")){ 


			if(combineTool.objectToCombine == null){
				Debug.Log("You need to attach a GameObject in the 'Object to Combine' slot first");
			}
			else{
				Transform[] allObjects = combineTool.objectToCombine.GetComponentsInChildren<Transform>(true);
				List<GameObject> allGameObjects = new List<GameObject>();
				foreach (Transform child in allObjects) {
					MeshFilter mf = (MeshFilter) child.gameObject.GetComponent<MeshFilter>(); // you have to check the filter first, otherwise Unity gives an error
					if(mf != null && child.gameObject.activeInHierarchy == true && child.gameObject.activeSelf == true && child.name.Contains("FenceManagerMarker_") == false){
						allGameObjects.Add(child.gameObject);
					}
				}
				if(allGameObjects.Count > 0){

					Vector3 pos = combineTool.objectToCombine.transform.position;
					if(combineTool.findLocalPosition == true) // in this case use the position of the first object. Useful for when the parent folder is far from the child objects
						pos = allGameObjects[0].transform.position;

                    GameObject mergedObjects = CombineNestedGameObjects(allGameObjects, combineTool.objectToCombine.name+"[Combined]",  pos, combineTool.remove_Average_Child_Z_Offsets);
                    SaveMergedMeshes(mergedObjects);
                }
				else
					Debug.Log(combineTool.objectToCombine.name + " contained no valid meshes to combine");
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
    //------------------------------------- 
    void SaveMergedMeshes(GameObject mergedObjects)
    {
        string dateStr = GetPartialTimeString(true);
        string path, folderName = "Meshes-Merged  " + dateStr;

        if (!Directory.Exists("Assets/Auto Fence Builder/UserGeneratedRailMeshes"))
        {
            AssetDatabase.CreateFolder("Assets/Auto Fence Builder", "UserGeneratedRailMeshes");
        }
        AssetDatabase.CreateFolder("Assets/Auto Fence Builder/UserGeneratedRailMeshes", folderName);
        path = "Assets/Auto Fence Builder/UserGeneratedRailMeshes/" + folderName + "/";

       //int numObjects = finishedGameObjects.Count;
        //for (int i = 0; i < numObjects; i++)
        //{
            Mesh mesh = mergedObjects.GetComponent<MeshFilter>().sharedMesh;
            if (mergedObjects != null && mesh != null)
            {
                if (Directory.Exists(path))
                {
                    AssetDatabase.CreateAsset(mesh, path + mesh.name + ".asset");
                }
            }
        //}
        AssetDatabase.SaveAssets();
    }
    //---------------------------------
    public static GameObject CombineNestedGameObjects(List<GameObject> allGameObjects, string name, Vector3 positionOffset = default(Vector3), bool removeAvgChildZOffsets = true)
	{
		GameObject combinedObject = new GameObject(name);
		ArrayList combineInstanceArrays = new ArrayList();
		ArrayList mats = new ArrayList();

		GameObject thisGO = null;
        float avgZ = 0;

        if (removeAvgChildZOffsets){
            for (int i = 0; i < allGameObjects.Count; i++){
                avgZ += allGameObjects[i].transform.localPosition.z;
            }
            avgZ /= allGameObjects.Count;
        }
        for (int i=0; i< allGameObjects.Count; i++)
		{
			

            thisGO = allGameObjects[i];
			if(thisGO == null)
				continue;
            if(removeAvgChildZOffsets)
                thisGO.transform.localPosition -=  new Vector3(0,0, avgZ);
            thisGO.transform.position -= positionOffset;
			MeshFilter[] meshFilters = thisGO.GetComponentsInChildren<MeshFilter>(true);

			//int count = 0;
			foreach( MeshFilter meshFilter in meshFilters )
			{
				MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
				if(!meshRenderer) { 
					Debug.LogError("Missing MeshRenderer."); 
					continue; 
				}
				if(meshRenderer.sharedMaterials.Length != meshFilter.sharedMesh.subMeshCount) { 
					Debug.LogError("Incorrect materials count"); 
					continue; 
				}

				for (int s = 0; s < meshFilter.sharedMesh.subMeshCount; s++) {
					int materialArrayIndex = MaterialsContain(mats, meshRenderer.sharedMaterials [s].name);
					if (materialArrayIndex == -1) {
						mats.Add (meshRenderer.sharedMaterials [s]);
						materialArrayIndex = mats.Count - 1;
					} 
					combineInstanceArrays.Add (new ArrayList ());
					CombineInstance combineInstance = new CombineInstance ();
					combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
					combineInstance.subMeshIndex = s;
					combineInstance.mesh = meshFilter.sharedMesh;
					(combineInstanceArrays [materialArrayIndex] as ArrayList).Add (combineInstance);
				}
			}
			thisGO.transform.position += positionOffset; // because we temorarily modified the source to get the local position
		}


		MeshFilter meshFilterCombine = combinedObject.GetComponent<MeshFilter>();
		if(!meshFilterCombine)
			meshFilterCombine = combinedObject.AddComponent<MeshFilter>();

		Mesh[] meshes = new Mesh[mats.Count];
		CombineInstance[] combineInstances = new CombineInstance[mats.Count];

		for( int m = 0; m < mats.Count; m++ )
		{
			CombineInstance[] combineInstanceArray = (combineInstanceArrays[m] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
			meshes[m] = new Mesh();
			meshes[m].CombineMeshes( combineInstanceArray, true, true );

			combineInstances[m] = new CombineInstance();
			combineInstances[m].mesh = meshes[m];
			combineInstances[m].subMeshIndex = 0;
		}
		//Combine 
		meshFilterCombine.sharedMesh = new Mesh();
		meshFilterCombine.sharedMesh.CombineMeshes( combineInstances, false, false );
		meshFilterCombine.sharedMesh.name = name;

		foreach( Mesh mesh in meshes ){
			mesh.Clear();
			DestroyImmediate(mesh);
		}
		MeshRenderer meshRendererCombine = combinedObject.GetComponent<MeshRenderer>();
		if(!meshRendererCombine)
			meshRendererCombine = combinedObject.AddComponent<MeshRenderer>();    

		Material[] materialsArray = mats.ToArray(typeof(Material)) as Material[];
		meshRendererCombine.materials = materialsArray;    

		combinedObject.transform.position += positionOffset;
		return combinedObject;
		// Part of this material combine code was gratefuly lifted from a Unity forums post!
	}
	//----
	private static int MaterialsContain (ArrayList matArray, string searchName)
	{
		for (int i = 0; i < matArray.Count; i++) {
			if (((Material)matArray [i]).name == searchName) {
				return i;
			}
		}
		return -1;
	}
    //---------------------------
    string GetPartialTimeString(bool includeDate = false)
    {
        DateTime currentDate = System.DateTime.Now;
        string timeString = currentDate.ToString();
        timeString = timeString.Replace("/", "-"); // because the / in that will upset the path
        timeString = timeString.Replace(":", "-"); // because the / in that will upset the path
        if (timeString.EndsWith(" AM") || timeString.EndsWith(" PM"))
        { // windows??
            timeString = timeString.Substring(0, timeString.Length - 3);
        }
        if (includeDate == false)
            timeString = timeString.Substring(timeString.Length - 8);
        return timeString;
    }

}
