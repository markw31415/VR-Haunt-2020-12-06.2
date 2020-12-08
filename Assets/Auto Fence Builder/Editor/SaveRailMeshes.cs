using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class SaveRailMeshes : MonoBehaviour {

	//----------------------------------------------------------------------------------------  
    //Saves the procedurally generated Rail meshes produced when using Sheared mode as prefabs, in order to create a working prefab from the Finished AutoFence
    public static bool SaveProcRailMeshesAsAssets(AutoFenceCreator af)
    {//Debug.Log("SaveProcRailMeshesAsAssets()\n");

        if(af.railACounter == 0 && af.railBCounter == 0)
            Debug.Log("No rail meshes needed saving \n");
        
        List<Transform> rails = af.railsA;
        int numRails = 0;
        string dateStr = af.GetPartialTimeString(true);
        int dateStrLength = dateStr.Length;
        
        string hourMinSec = dateStr.Substring(dateStrLength-8, 8);
        string dirPath, folderName = "NewGeneratedRailMeshes " + dateStr;
        bool cancelled = false;
        int numCreatedA = 0, numUpdatedA = 0, numCreatedB = 0, numUpdatedB = 0; 

        string dir = af.currAutoFenceBuilderDirLocation + "/UserGeneratedRailMeshes";
        if (!Directory.Exists(dir))
        {
            AssetDatabase.CreateFolder(af.currAutoFenceBuilderDirLocation, "UserGeneratedRailMeshes");
        }
        
        //Do the meshes already exist, if so might not need to create folder
        Mesh meshA, meshB;
        bool meshAExists = false, meshBExists = false, createdFolder = false;
        if(af.railsA.Count > 0)
        {
            List<Mesh> meshesA = MeshUtilitiesAFB.GetAllMeshesFromGameObject(af.railsA[0].gameObject);
            if (meshesA.Count > 0)
            {
                meshA = meshesA[0];
                meshAExists = AssetDatabase.Contains(meshA);
            }
        }
        if(af.railsB.Count > 0)
        {
            List<Mesh> meshesB = MeshUtilitiesAFB.GetAllMeshesFromGameObject(af.railsB[0].gameObject);
            if (meshesB.Count > 0)
            {
                meshB = meshesB[0];
                meshBExists = AssetDatabase.Contains(meshB);
            }
        }
        if (!Directory.Exists(dir))
        {
            if (meshAExists == false && meshBExists == false)
            {
                createdFolder = true;
                AssetDatabase.CreateFolder(af.currAutoFenceBuilderDirLocation + "/UserGeneratedRailMeshes", folderName);
            }
        }

        string railSetStr = "", meshNumStr = "";
        AssetDatabase.StartAssetEditing();
        for (int k = 0; k < 2; k++)
        {
            if (k == 0)
            {
                rails = af.railsA;
                railSetStr = " A";
                numRails = af.railACounter;
            }
            else if (k == 1)
            {
                rails = af.railsB;
                railSetStr = " B";
                numRails = af.railBCounter;
            }
            if (numRails > 0 && rails[0] != null )
            {
                for (int i = 0; i < numRails; i++)
                {
                    List<Mesh> meshes = MeshUtilitiesAFB.GetAllMeshesFromGameObject(rails[i].gameObject);
                    int meshCount = meshes.Count;

                    if (k == 0)
                        cancelled = EditorUtility.DisplayCancelableProgressBar("Saving Rail-A Meshes...", i.ToString() + " of " + numRails, (float)i / numRails);
                    else if (k == 1)
                        cancelled = EditorUtility.DisplayCancelableProgressBar("Saving Rail-B Meshes...", i.ToString() + " of " + numRails, (float)i / numRails);

                    if (cancelled)
                    {
                        EditorUtility.ClearProgressBar();
                        return false;
                    }
                    
                    if (rails[i] != null && meshCount > 0)
                    {
                        for (int m = 0; m < meshCount; m++)
                        {
                            Mesh mesh = meshes[m];
                            if (mesh == null)
                            {
                                Debug.LogWarning(rails[i].gameObject.name + ": Mesh " + m + " was null. Not saved");
                                continue;
                            }
                            if (meshCount == 1)
                                meshNumStr = "";
                            else
                                meshNumStr = "(m" + m.ToString() + ")";

                            string meshName = mesh.name;
                            if (meshName == "")
                            { // a sheared mesh was not made because it intersected with the ground, so omit it (set in 'Auto Hide Buried Rails')
                                continue;
                            }
                            else
                            {
                                //mesh.name = mesh.name.Remove(mesh.name.IndexOf("[Dup]"));
                                //mesh.name = mesh.name.Remove(mesh.name.IndexOf("[+]"));
                                string newMeshName = mesh.name + "[" + GetRailNameWithoutSuffix(rails[i]) + railSetStr + "] " + i + meshNumStr + "-" + hourMinSec;
                                
                                try
                                {
                                    if (AssetDatabase.Contains(mesh))
                                    {
                                        AssetDatabase.SaveAssets();
                                        if (k == 0)
                                            numUpdatedA++;
                                        else if (k == 1)
                                            numUpdatedB++;
                                    }
                                    else
                                    {
                                        if (createdFolder == false)
                                        {
                                            AssetDatabase.CreateFolder(af.currAutoFenceBuilderDirLocation + "/UserGeneratedRailMeshes", folderName);
                                            createdFolder = true;
                                        }
                                        dirPath = af.currAutoFenceBuilderDirLocation + "/UserGeneratedRailMeshes/" + folderName + "/";
                                        if (Directory.Exists(dirPath) == false)
                                        {
                                            EditorUtility.ClearProgressBar();
                                            Debug.Log("Directory Missing! : " + dirPath  + " Meshes not saved.");
                                        }   
                                        AssetDatabase.CreateAsset(mesh, dirPath + "/" + newMeshName + ".asset");
                                        
                                        if (k == 0)
                                            numCreatedA++;
                                        else if (k == 1)
                                            numCreatedB++;
                                    }
                                }
                                catch (System.Exception e){
                                    Debug.LogWarning("Problem Creating mesh asset. " + e.ToString() + "\n");
                                    ReportSavedMeshes(numUpdatedA, numUpdatedB, numCreatedA, numCreatedB, af.railACounter, af.railBCounter);
                                    AssetDatabase.StopAssetEditing();
                                    EditorUtility.ClearProgressBar();
                                    return false;
                                }
                            }
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }
            if (k == 0 && numRails > 0)
            {
                if (numUpdatedA != numRails && numCreatedA != numRails)
                {
                    Debug.LogWarning("Expected " + numRails + " Rails A.    Created: " + numCreatedA + "   Updated; " + numUpdatedA + "\n");
                }
            }
            if (k == 1 && numRails > 0)
            {
                if (numUpdatedB != numRails && numCreatedB != numRails)
                {
                    Debug.LogWarning("Expected " + numRails + " Rails B.    Created: " + numCreatedB + "   Updated; " + numUpdatedB + "\n");
                }
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.StopAssetEditing();
        ReportSavedMeshes(numUpdatedA, numUpdatedB, numCreatedA, numCreatedB, af.railACounter, af.railBCounter);
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        return true;
    }
    //-------------------
    private  static void ReportSavedMeshes(int numUpdatedA, int numUpdatedB, int numCreatedA, int numCreatedB, int railsCountA, int railsCountB)
    {
        if(numUpdatedA == 0 && numUpdatedB == 0 && numCreatedA == 0 && numCreatedB == 0)
        {
            Debug.Log("No meshes were created or updated\n");
            return;
        }

        string expectedStr = "Expected:  " + railsCountA + " Rails A  &  " + railsCountB + " Rails B.        ";
        
        //Debug.Log("Expected:  " + railsCountA + " Rails A  &  " + railsCountB  + " Rails B \n");
        
        if (numUpdatedA > 0)
            Debug.Log(expectedStr + "Updated " + numUpdatedA + " Rails A \n");
        if (numUpdatedB > 0)
            Debug.Log(expectedStr + "Updated " + numUpdatedB + " Rails B \n");
        if (numCreatedA > 0)
            Debug.Log(expectedStr + "Created " + numCreatedA + " Rails A \n");
        if (numCreatedB > 0)
            Debug.Log(expectedStr + "Created " + numCreatedB + " Rails B \n");
    }

    //-------------------
    private static string GetRailNameWithoutSuffix(Transform rail)
    {
        int index = rail.gameObject.name.IndexOf("_Panel_Rail");
        if (index == -1)
            index = rail.gameObject.name.IndexOf("_Rail");
        if (index == -1)
            index = rail.gameObject.name.Length > 10 ? 9 : rail.gameObject.name.Length - 1;

        string newMeshName = rail.gameObject.name.Remove(index);
        return newMeshName;
    }
}
