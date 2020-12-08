using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework.Constraints;

/* This is called from AutoFenceEditor */
public class FencePrefabLoader
{
    public bool LoadAllFencePrefabs(AutoFenceEditor ed, List<GameObject> extraPrefabs, List<GameObject> postPrefabs, List<GameObject> subPrefabs,
        List<GameObject> railPrefabs, List<GameObject> subJoinerPrefabs, ref GameObject clickMarkerObj)
    {//Debug.Log("LoadAllFencePrefabs\n");

        string[] prefabsPaths = AssetDatabase.FindAssets("FencePrefabs_AFWB");
        string prefabsFolderPath;

        if (prefabsPaths.Length == 0 || prefabsPaths[0] == "")
        {
            Debug.LogWarning("Couldn't find prefabsFolderPath   Length " + prefabsPaths.Length + "\n");
            return false;
        }
        else
        {
            prefabsFolderPath = AssetDatabase.GUIDToAssetPath(prefabsPaths[0]);

            if (prefabsFolderPath != ed.af.prefabsDefaultDirLocation && prefabsFolderPath != ed.af.currPrefabsDirLocation)
            {
                Debug.LogWarning("prefabsFolderPath is not at Current OR Default prefabs path" + "\n");
                ed.CheckDirectoryLocations(false);
            }
        }

        string[] filePaths = null, postFilePaths = null, railFilePaths = null, extrasFilePaths = null;
        try
        {
            filePaths = Directory.GetFiles(prefabsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing FencePrefabs Folder. The FencePrefabs folder must be at Assets/Auto Fence Builder/FencePrefabs   " + e.ToString());
            return false;
        }

        string postPrefabsFolderPath = prefabsFolderPath + "/_Posts_AFWB/";
        try
        {
            postFilePaths = Directory.GetFiles(postPrefabsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing FencePrefabs Posts Folder. The _Posts_AFWB folder must be within [...]/Auto Fence Builder/FencePrefabs_AFWB/  " + e.ToString());
            return false;
        }

        string railPrefabsFolderPath = prefabsFolderPath + "/_Rails_AFWB/";
        try
        {
            railFilePaths = Directory.GetFiles(railPrefabsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing FencePrefabs Rails Folder. The _Rails_AFWB folder must be within [...]/Auto Fence Builder/FencePrefabs_AFWB/  " + e.ToString());
            return false;
        }
        string extrasPrefabsFolderPath = prefabsFolderPath + "/_Extras_AFWB/";
        try
        {
            extrasFilePaths = Directory.GetFiles(extrasPrefabsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing FencePrefabs Extras Folder. The _Extras_AFWB folder must be within [...]/Auto Fence Builder/FencePrefabs_AFWB/  " + e.ToString());
        }

        //-- Load Misc first
        foreach (string filePath in filePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                GameObject go = AssetDatabase.LoadMainAssetAtPath(prefabsFolderPath + "/" + fileName) as GameObject;
                if (go != null && fileName.Contains("ClickMarkerObj"))
                    clickMarkerObj = go;
                else if (go != null && fileName.Contains("_SubJoiner"))
                    subJoinerPrefabs.Add(go);
            }
        }
        //=========== Load Posts ============
        foreach (string filePath in postFilePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                GameObject go = AssetDatabase.LoadMainAssetAtPath(postPrefabsFolderPath + fileName) as GameObject;
                bool nameEndsWithPost = go.name.EndsWith("_Post");
                if (go != null && nameEndsWithPost)
                {
                    if (MeshUtilitiesAFB.GetFirstMeshInGameObject(go) != null)
                    {
                        postPrefabs.Add(go);
                        subPrefabs.Add(go); //Posts are also used as subposts
                        extraPrefabs.Add(go);  //Posts are also used as extras
                    }
                }
                else
                {
                    Debug.LogWarning("   ***   Problem with Posts folder   ***     " + fileName + "\n");
                    if (nameEndsWithPost == false)
                        Debug.LogWarning("Post Prefabs that are manually placed in the _Posts_AFWB folder must have name ending with '_Post' \n");
                }
                if (go == null)
                    Debug.Log("Load Posts GameObject was null \n");
            }
        }
        //========== Load Rails ================
        foreach (string filePath in railFilePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                GameObject go = AssetDatabase.LoadMainAssetAtPath(railPrefabsFolderPath + fileName) as GameObject;
                bool nameEndsWithRail = go.name.EndsWith("_Rail");
                if (go != null && nameEndsWithRail)
                {
                    if (MeshUtilitiesAFB.GetFirstMeshInGameObject(go) != null)
                    {
                        railPrefabs.Add(go);
                        extraPrefabs.Add(go);//Rails are also used as extras
                    }
                }
                else
                {
                    Debug.LogWarning("   ***   Problem with Rails folder   ***     " + fileName + "\n");
                    if (nameEndsWithRail == false)
                        Debug.LogWarning("Rail Prefabs that are manually placed in the _Rails_AFWB folder must have name ending with '_Panel_Rail' \n");
                }
                if (go == null)
                    Debug.Log("Load Rails: GameObject was null \n");
            }
        }
        //============ Load Extras ==============
        foreach (string filePath in extrasFilePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                GameObject go = AssetDatabase.LoadMainAssetAtPath(extrasPrefabsFolderPath + fileName) as GameObject;
                if (go != null && go.name.EndsWith("_Extra"))
                {
                    if (MeshUtilitiesAFB.GetFirstMeshInGameObject(go) != null)
                    {
                        extraPrefabs.Add(go);
                    }
                }
                else
                    Debug.Log("   ***   Problem with Extras folder   ***   " + fileName);
                if (go == null)
                    Debug.Log("Load Extras: GameObject was null \n");
            }
        }
        return true;
    }
    //----------------------------
    // For use when creating a minimal setup with zero prefab content
    public bool LoadAllFencePrefabsMinimal(AutoFenceEditor ed, List<GameObject> extraPrefabs, List<GameObject> postPrefabs, List<GameObject> subPrefabs,
        List<GameObject> railPrefabs, List<GameObject> subJoinerPrefabs, ref GameObject clickMarkerObj)
    {

        string[] zeroContentPaths = AssetDatabase.FindAssets("ZeroPrefabContentVersion");
        string zeroContentsFolderPath;

        if (zeroContentPaths.Length == 0 || zeroContentPaths[0] == "")
        {
            Debug.LogWarning("Couldn't find ZeroPrefabContentVersion   Length " + zeroContentPaths.Length + "\n");
            return false;
        }
        else
        {
            zeroContentsFolderPath = AssetDatabase.GUIDToAssetPath(zeroContentPaths[0]);
            if (zeroContentsFolderPath == "")
            {
                Debug.LogWarning("Couldn't create zeroContentsFolderPath \n");
            }
        }

        string[] filePaths = null;//, postFilePaths = null, railFilePaths = null, extrasFilePaths = null;
        try
        {
            filePaths = Directory.GetFiles(zeroContentsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing ZeroPrefabContentVersion Folder. The ZeroPrefabContentVersion folder must be at Auto Fence Builder/ZeroPrefabContentVersion   " + e.ToString());
            return false;
        }
        //===================================================
        //         Create new prefabs folders if necessary
        //===================================================
        string mainPrefabsFolderPath = ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB";
        bool folderExists = AssetDatabase.IsValidFolder(mainPrefabsFolderPath);
        if (folderExists == false)
        {
            string guid = AssetDatabase.CreateFolder(ed.af.currAutoFenceBuilderDirLocation, "FencePrefabs_AFWB");
            mainPrefabsFolderPath = AssetDatabase.GUIDToAssetPath(guid);
            if (mainPrefabsFolderPath == "")
            {
                Debug.LogWarning("Couldn't create FencePrefabs_AFWB folder \n");
                return false;
            }
        }


        string postsFolderPath = ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Posts_AFWB";
        folderExists = AssetDatabase.IsValidFolder(postsFolderPath);
        if (folderExists == false)
        {
            string guid = AssetDatabase.CreateFolder(ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB", "_Posts_AFWB");
            postsFolderPath = AssetDatabase.GUIDToAssetPath(guid);
            if (postsFolderPath == "")
                Debug.LogWarning("Couldn't create _Posts_AFWB folder \n");
        }

        string railsFolderPath = ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Rails_AFWB";
        folderExists = AssetDatabase.IsValidFolder(railsFolderPath);
        if (folderExists == false)
        {
            string guid = AssetDatabase.CreateFolder(ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB", "_Rails_AFWB");
            railsFolderPath = AssetDatabase.GUIDToAssetPath(guid);
        }

        string extrasFolderPath = ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Extras_AFWB";
        folderExists = AssetDatabase.IsValidFolder(extrasFolderPath);
        if (folderExists == false)
        {
            string guid = AssetDatabase.CreateFolder(ed.af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB", "_Extras_AFWB");
            extrasFolderPath = AssetDatabase.GUIDToAssetPath(guid);
        }

        //===============================================================
        //       Load the minimal prefabs from ZeroContent Folder
        //===============================================================

        foreach (string filePath in filePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                GameObject go = AssetDatabase.LoadMainAssetAtPath(zeroContentsFolderPath + "/" + fileName) as GameObject;
                if (go != null && fileName.Contains("ClickMarkerObj"))
                    clickMarkerObj = go;
                else if (go != null && fileName.Contains("_SubJoiner"))
                    subJoinerPrefabs.Add(go);
                else if (go != null && fileName.Contains("CorePost_Post"))
                {
                    postPrefabs.Add(go);
                    subPrefabs.Add(go);
                    extraPrefabs.Add(go);
                }
                else if (go != null && fileName.Contains("Marker_Post"))
                {
                    postPrefabs.Add(go);
                    subPrefabs.Add(go);
                    extraPrefabs.Add(go);
                }

                else if (go != null && fileName.Contains("CoreRail_Panel_Rail"))
                {
                    railPrefabs.Add(go);
                    extraPrefabs.Add(go);
                }
                else
                {
                    Debug.LogWarning(" Unknown prefab in ZeroPrefabContentVersion folder:  " + fileName + "\n");
                }
                if (go == null)
                    Debug.Log("GameObject was null \n");
            }
        }
        //==================================================================================
        //       Resave the minimal prefabs back to the regular FencePrefabs_AFWB folders
        //==================================================================================
        for (int i = 0; i < postPrefabs.Count; i++)
        {
            GameObject go = postPrefabs[i];
            if (go != null)
            {
                CopyPrefabToDirectory(go, postsFolderPath);
            }
        }
        for (int i = 0; i < railPrefabs.Count; i++)
        {
            GameObject go = railPrefabs[i];
            if (go != null)
            {
                CopyPrefabToDirectory(go, railsFolderPath);
            }
        }

        if (clickMarkerObj != null)
            CopyPrefabToDirectory(clickMarkerObj, mainPrefabsFolderPath);
        if (subJoinerPrefabs.Count > 0)
            CopyPrefabToDirectory(subJoinerPrefabs[0], mainPrefabsFolderPath);

        AssetDatabase.Refresh();

        return true;
    }
    //-------------
    // ATM, you can only save from an instanciated go
    public static bool CopyPrefabToDirectory(GameObject go, string savePath)
    {
        GameObject instantiatedGO = GameObject.Instantiate(go);
        instantiatedGO.name = go.name; //we don't want "(CLone)"
        savePath += "/" + instantiatedGO.name + ".prefab";

        bool fileExists = File.Exists(savePath);
        if (fileExists)
        {
            Debug.LogWarning("File already exists " + go.name + "   " + savePath);
            if (instantiatedGO)
                GameObject.DestroyImmediate(instantiatedGO);
            return true;
        }

        //GameObject prefab = PrefabUtility.CreatePrefab(savePath, instantiatedGO);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instantiatedGO, savePath);

        /*try {
            PrefabUtility.ReplacePrefab(go, prefab, ReplacePrefabOptions.Default);
        }
        catch (System.Exception e){
            Debug.LogWarning("Couldn't ReplacePrefab " + go.name + " to " + savePath + "  " + e.ToString());
            return false;
        }*/
        if (instantiatedGO)
            GameObject.DestroyImmediate(instantiatedGO);

        return true;
    }
    //--------------------------------------
    // Will replace if it already exists
    public GameObject LoadSinglePostPrefabWithName(List<GameObject> postPrefabs, string postName, bool replace = false)
    {
        string prefabsFolderPath = "Assets/Auto Fence Builder/FencePrefabs/_Posts";
        string[] filePaths = null;
        try
        {
            filePaths = Directory.GetFiles(prefabsFolderPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Missing FencePrefabs Posts Folder. The FencePrefabs folder must be at Assets/Auto Fence Builder/FencePrefabs   " + e.ToString());
            return null;
        }
        postName = postName + ".prefab";
        foreach (string filePath in filePaths)
        {
            if (filePath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.Equals(postName))
                {
                    Object[] data = AssetDatabase.LoadAllAssetsAtPath(prefabsFolderPath + fileName);
                    GameObject newPost = data[0] as GameObject;
                    if (replace == true && newPost != null && MeshUtilitiesAFB.GetFirstMeshInGameObject(newPost) != null)
                    {
                        ReplacePrefabInList(postPrefabs, newPost);
                    }
                    return newPost;
                }
            }
        }
        return null;
    }
    //---------------
    public bool ReplacePrefabInList(List<GameObject> prefabs, GameObject replacement)
    {
        GameObject removeOld = prefabs.Where(obj => obj.name == replacement.name).First();
        var index = prefabs.IndexOf(removeOld);
        if (index != -1)
        {
            prefabs[index] = replacement;
            return true;
        }
        return false;
    }
}