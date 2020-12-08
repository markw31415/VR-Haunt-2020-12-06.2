using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

[CustomEditor(typeof(FenceMeshMerge))]
public class FenceMeshMergeEditor : Editor {

	public FenceMeshMerge 		 fenceMeshMerge;
	bool showHelp = false;
	void OnEnable()    
	{
		fenceMeshMerge = (FenceMeshMerge)target;
	}

	//------------------------------------------
	public override void OnInspectorGUI() 
	{
		serializedObject.Update();
		List<GameObject> finishedMergedObjects = new List<GameObject>();

		if( GUILayout.Button("Create Merged-Mesh Copy", GUILayout.Width(200)) ){ 

			//-- Cretae New Folder for the copy -----
			GameObject mergedCopyFolder = new GameObject(fenceMeshMerge.gameObject.name + " Merged Mesh Copy");
			//Vector3 adjustedPosition = Vector3.zero; // we'll set the final position of everything to the first post position
			mergedCopyFolder.transform.position = fenceMeshMerge.gameObject.transform.position;
			//=========== Rails ==============
			List<Transform> railsMergedFolders = GetAllDividedFolders("Rails");
			for(int i=0; i< railsMergedFolders.Count; i++){
				List<GameObject> allRails = GetAllGameObjectsFromDividedFolder(railsMergedFolders[i]);
                Debug.Log("Merging " + allRails.Count + " Rails");

                if (allRails != null && allRails.Count > 0)
				{
					GameObject mergedObj = CombineNestedGameObjects(allRails, "Rails Merged " + i, fenceMeshMerge.gameObject.transform.position);
					mergedObj.transform.parent = mergedCopyFolder.transform;
					finishedMergedObjects.Add (mergedObj);
					//Creating Colliders
                    for(int j=0; j<allRails.Count; j++){
						GameObject thisRail = allRails[j];
						BoxCollider coll = thisRail.GetComponent<BoxCollider>(); // does the original have a collider
						if(coll != null){
							//Create Colliders by duplicating the rails, then destroying everything except the new colliders
							// this will only work on unnested parts that have box colliders. Else makes more sense to add custom colliders
							GameObject colliderDummy = Instantiate (thisRail) as GameObject; // debugging
							colliderDummy.name = thisRail.name + "_BoxCollider";
							colliderDummy.transform.parent = mergedObj.transform;
							Vector3 pos = colliderDummy.transform.position;
							colliderDummy.transform.position = fenceMeshMerge.gameObject.transform.position+pos;
							MeshFilter mf = colliderDummy.GetComponent<MeshFilter>();//debug
							if(mf)
								DestroyImmediate(mf);
							MeshRenderer mr = colliderDummy.GetComponent<MeshRenderer>();
							if(mr)
								DestroyImmediate(mr);
						}
					}
				}
			}
			//=========== Posts ==============
			List<Transform> postsMergedFolders = GetAllDividedFolders("Posts");
			if(postsMergedFolders != null)
			{
				for(int i=0; i< postsMergedFolders.Count; i++){
					List<GameObject> allPosts = GetAllGameObjectsFromDividedFolder(postsMergedFolders[i]);
					if(allPosts != null && allPosts.Count > 0)
					{
						GameObject mergedObj = CombineNestedGameObjects(allPosts, "Posts Merged " + i, fenceMeshMerge.gameObject.transform.position);
						if(mergedObj != null){
							mergedObj.transform.parent = mergedCopyFolder.transform;
							finishedMergedObjects.Add (mergedObj);
						}
					}
				}
			}
			//=========== Subs ==============
			List<Transform> subsMergedFolders = GetAllDividedFolders("Subs");
			for(int i=0; i< subsMergedFolders.Count; i++){
				List<GameObject> allSubs = GetAllGameObjectsFromDividedFolder(subsMergedFolders[i]);
				if(allSubs != null && allSubs.Count > 0)
				{
					GameObject mergedObj = CombineNestedGameObjects(allSubs, "Subposts Merged " + i, fenceMeshMerge.gameObject.transform.position);
					if(mergedObj != null){
						mergedObj.transform.parent = mergedCopyFolder.transform;
						finishedMergedObjects.Add (mergedObj);
					}
				}
			}
			//=========== Extras ==============
			List<Transform> extrasMergedFolders = GetAllDividedFolders("Extras");
			for(int i=0; i< extrasMergedFolders.Count; i++){
				List<GameObject> allExtras = GetAllGameObjectsFromDividedFolder(extrasMergedFolders[i]);
				if(allExtras != null && allExtras.Count > 0)
				{
					GameObject mergedObj = CombineNestedGameObjects(allExtras, "Extras Merged " + i, fenceMeshMerge.gameObject.transform.position);
					if(mergedObj != null){
						mergedObj.transform.parent = mergedCopyFolder.transform;
						finishedMergedObjects.Add (mergedObj);
					}
				}
			}

			SaveMergedMeshes(finishedMergedObjects);
		}
		EditorGUILayout.Separator();
		showHelp = EditorGUILayout.Foldout(showHelp, "Show Merged-Mesh-Copy Help");
		if(showHelp){
			EditorGUILayout.LabelField("This will create a copy of the Finished folder with each sub-group");
			EditorGUILayout.LabelField("of meshes merged in to a single mesh. Each merged mesh will take 1 drawcall.");
			EditorGUILayout.LabelField("(As with any Game Object, this can increase with Lights/Shadows/Quality-Settings)");
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("In many cases you don't need to use this, as each group has a Combine script");
			EditorGUILayout.LabelField("which will achieve the same thing at Runtime, while retaining the flexibility of separate meshes.");
			EditorGUILayout.LabelField("However, it can be useful depending on your dynamic/static batching settings,");
			EditorGUILayout.LabelField("and when needing a particular setup for prefab creation or lighmapping.");
		}
	}
	//--------------------------------
	public static List<Mesh> GetAllMeshesFromGameObject(GameObject inGO){

		Transform[] allObjects = inGO.GetComponentsInChildren<Transform>(true);
		List<Mesh> meshes = new List<Mesh>();
		foreach (Transform child in allObjects) {
			MeshFilter mf = (MeshFilter) child.gameObject.GetComponent<MeshFilter>(); // you have to check the filter first, otherwise Unity gives an error
			if(mf != null){
				Mesh thisObjectMesh = child.gameObject.GetComponent<MeshFilter>().sharedMesh;
				if(thisObjectMesh != null)
					meshes.Add(thisObjectMesh);
			}
		}
		return meshes;
	}
	//----------
	public static List<GameObject> GetAllMeshGameObjectsFromGameObject(GameObject inGO){

		Transform[] allObjects = inGO.GetComponentsInChildren<Transform>(true);
		List<GameObject> allGameObjects = new List<GameObject>();
		foreach (Transform child in allObjects) {
			MeshFilter mf = (MeshFilter) child.gameObject.GetComponent<MeshFilter>(); // you have to check the filter first, otherwise Unity gives an error
			if(mf != null){
				allGameObjects.Add(child.gameObject);
			}
		}
		return allGameObjects;
	}

	//---------------------------
	public static GameObject CombineNestedGameObjects(List<GameObject> allGameObjects, string name, Vector3 positionOffset = default(Vector3))
	{
		GameObject combinedObject = new GameObject(name);
		ArrayList combineInstanceArrays = new ArrayList();
		ArrayList mats = new ArrayList();

		GameObject thisGO = null;

		for(int i=0; i< allGameObjects.Count; i++)
		{
			thisGO = allGameObjects[i];
			if(thisGO == null)
            {
                Debug.LogWarning("CombineNestedGameObjects():  ** GameObject Missing **  [" + i +"]");
                continue;
            }
            //else
               // Debug.Log("CombineNestedGameObjects():  GameObject OK   " + thisGO.name + "[" + i + "]");

            thisGO.transform.position -= positionOffset;
			MeshFilter[] meshFilters = thisGO.GetComponentsInChildren<MeshFilter>(true);

            if(meshFilters == null || meshFilters.Length == 0)
            {
                Debug.LogWarning("CombineNestedGameObjects():  ** meshFilters Count is 0 for GameObject [" + i + "]");
                continue;
            }


            //int count = 0;
            //foreach (MeshFilter meshFilter in meshFilters)
            for (int j=0; j< meshFilters.Length; j++)
			{
				MeshRenderer meshRenderer = meshFilters[j].GetComponent<MeshRenderer>();
				if(!meshRenderer) { 
					Debug.LogWarning("Missing MeshRenderer."); 
					continue; 
				}

                if (meshFilters[j] == null)
                {
                    Debug.LogWarning("CombineNestedGameObjects():  ** MeshFilter Missing! **   " + thisGO.name + "[" + i + "] meshFilters[" + j + "] was null");
                    continue;
                }
                //else
                    //Debug.Log("CombineNestedGameObjects():  MeshFilter OK   " + thisGO.name + "[" + i + "] meshFilters[" + j + "]");


                if (meshFilters[j].sharedMesh == null)
                {   
                    Debug.LogWarning("CombineNestedGameObjects():  ** Mesh Missing! **   " + thisGO.name + "[" + i + "] mesh[" + j + "] was null");
                    continue;
                }
               //else
                    //Debug.Log("CombineNestedGameObjects():  Mesh OK   " + thisGO.name + "[" + i + "] mesh[" + j + "]");



                if (meshRenderer.sharedMaterials.Length != meshFilters[j].sharedMesh.subMeshCount) { 
					Debug.LogWarning("Incorrect materials count: " + meshRenderer.sharedMaterials.Length + " Materials,  " + 
                    meshFilters[j].sharedMesh.subMeshCount + " subMeshes"); 
					continue; 
				}

				for (int s = 0; s < meshFilters[j].sharedMesh.subMeshCount; s++) {
                    //Debug.Log("i=" + i + " j=" + j + "   s = " + s + "/" + meshFilters[j].sharedMesh.subMeshCount);
    
                    int materialArrayIndex = MaterialsContain(mats, meshRenderer.sharedMaterials [s].name);
					if (materialArrayIndex == -1) {
						mats.Add (meshRenderer.sharedMaterials[s]);
						materialArrayIndex = mats.Count - 1;
					} 
					combineInstanceArrays.Add (new ArrayList ());
					CombineInstance combineInstance = new CombineInstance ();
					combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
					combineInstance.subMeshIndex = s;
					combineInstance.mesh = meshFilters[j].sharedMesh;
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
	//-------------------------------------	
	//Saves the procedurally generated Rail meshes produced when using Sheared mode as prefabs, in order to create a working prefab from the Finished AutoFence
	void SaveMergedMeshes(List<GameObject> finishedGameObjects){
		
		string dateStr = GetPartialTimeString(true);
		string path, folderName = "Meshes-Merged  " + dateStr;
		
		if(!Directory.Exists("Assets/Auto Fence Builder/UserGeneratedRailMeshes")){
			AssetDatabase.CreateFolder("Assets/Auto Fence Builder", "UserGeneratedRailMeshes");
		}
		AssetDatabase.CreateFolder("Assets/Auto Fence Builder/UserGeneratedRailMeshes", folderName);
		path = "Assets/Auto Fence Builder/UserGeneratedRailMeshes/" + folderName + "/";

		int numObjects = finishedGameObjects.Count;
		for(int i=0; i<numObjects; i++){
			Mesh mesh = finishedGameObjects[i].GetComponent<MeshFilter>().sharedMesh;
			if(finishedGameObjects[i] != null && mesh != null){
				if(Directory.Exists(path) ){
					AssetDatabase.CreateAsset(mesh, path + mesh.name + ".asset");
				}
			}	
		}
		AssetDatabase.SaveAssets();
	}
	//------------------------------------------
	List<GameObject> GetAllGameObjectsFromDividedFolder(Transform dividedFolder){

		int numChildren = dividedFolder.childCount;
		List<GameObject> goList = new List<GameObject>(); 
		for(int i=0; i<numChildren; i++){
			goList.Add (dividedFolder.GetChild(i).gameObject);
		}
		return goList;
	}
	//------------------------------------------
	List<Transform> GetAllDividedFolders(string folderName) 
	{
		GameObject masterFolder = fenceMeshMerge.gameObject;

		Transform mainFolder = masterFolder.transform.Find(folderName);

		if(mainFolder == null) return null;
		
		int numChildren = mainFolder.childCount;
		
		Transform thisChild;
		List<Transform> dividedFolders = new List<Transform>(); 
		for(int i=0; i<numChildren; i++){
			
			thisChild = mainFolder.GetChild(i);
			if(folderName == "Rails" &&  thisChild.name.StartsWith("RailsAGroupedFolder") ){
				
				dividedFolders.Add(thisChild);
			}
			if(folderName == "Rails" &&  thisChild.name.StartsWith("RailsBGroupedFolder") ){
				
				dividedFolders.Add(thisChild);
			}
			else if(folderName == "Posts" &&  thisChild.name.StartsWith("PostsGroupedFolder") ){
				
				dividedFolders.Add(thisChild);
			}
			else if(folderName == "Subs" &&  thisChild.name.StartsWith("SubsGroupedFolder") ){
				
				dividedFolders.Add(thisChild);
			}
			else if(folderName == "Extras" &&  thisChild.name.StartsWith("ExtrasGroupedFolder") ){

				dividedFolders.Add(thisChild);
			}
		}

		return dividedFolders;
	}
	//---------------------------
	string GetPartialTimeString(bool includeDate = false)
	{
		DateTime currentDate = System.DateTime.Now;
		string timeString = currentDate.ToString();
		timeString = timeString.Replace("/", "-"); // because the / in that will upset the path
		timeString = timeString.Replace(":", "-"); // because the / in that will upset the path
		if (timeString.EndsWith (" AM") || timeString.EndsWith (" PM")) { // windows??
			timeString = timeString.Substring (0, timeString.Length - 3 );
		}
		if(includeDate == false)
			timeString = timeString.Substring (timeString.Length - 8);
		return timeString;
	}


}
