#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ResourceUtilities
{
    AutoFenceCreator af;
    AutoFenceEditor ed;

    public ResourceUtilities(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
    }
    //----------------------------------------------------------------------------------------
    //Save the user-added object in to the FencePrefabs and Meshes folders
    public static GameObject SaveUserObject(GameObject userObj, AutoFenceCreator.FencePrefabType objType, AutoFenceCreator af)
    {
        if (userObj == null)
            return null;
        if (af.currAutoFenceBuilderDirLocation == null)
        {
            Debug.LogWarning("af.currAutoFenceBuilderDirLocation is null in SaveUserObject()");
            return null;
        }

        List<Mesh> userMeshes = new List<Mesh>();
        // If it's a simple 1 object 1 material model
        Mesh userMesh = null;
        MeshFilter mf = (MeshFilter)userObj.GetComponent<MeshFilter>(); // see if the top level object has a mesh
        if (mf == null)
        {
            userMeshes = MeshUtilitiesAFB.GetAllMeshesFromGameObject(userObj);
            if (userMeshes.Count == 0)
            {
                Debug.Log("No meshes could be found for " + userObj.name);
                return null;
            }
        }
        else
        { // it's a single mesh object
            userMeshes = MeshUtilitiesAFB.GetAllMeshesFromGameObject(userObj);
        }

        GameObject result = userObj; // just in case replace fails
        string meshPath = "", prefabPath = "";
        string objName = "";
        if (userObj.name.StartsWith("[User]") == false)
            objName += "[User]";
        objName += userObj.name;


        for (int i = 0; i < userMeshes.Count; i++)
        {
            userMesh = userMeshes[i];
            userMesh.name = "";
            if (userMesh.name.StartsWith("_user_") == false)
                userMesh.name += "_user_";
            userMesh.name += userObj.name + "_" + i;


            if (objType == AutoFenceCreator.FencePrefabType.postPrefab)
            {
                if (objName.EndsWith("_Post") == false)
                    objName += "_Post";
                userMesh.name += "_Post";
                meshPath = af.currAutoFenceBuilderDirLocation + "/Meshes/" + userMesh.name + ".asset";
                prefabPath = af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Posts_AFWB/" + objName + ".prefab";
            }
            if (objType == AutoFenceCreator.FencePrefabType.railPrefab)
            {
                if (objName.EndsWith("_Rail") == false)
                    objName += "_Rail";
                userMesh.name += "_Rail";
                meshPath = af.currAutoFenceBuilderDirLocation + "/Meshes/" + userMesh.name + ".asset";
                /*bool meshExists = Directory.Exists(meshPath);
                if (meshExists)
                {
                    meshPath = af.currAutoFenceBuilderDirLocation + "/Meshes/" + userMesh.name + "+.asset";  
                }*/
                prefabPath = af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Rails_AFWB/" + objName + ".prefab";
            }
            if (objType == AutoFenceCreator.FencePrefabType.extraPrefab)
            {
                if (objName.EndsWith("_Extra") == false)
                    objName += "_Extra";
                userMesh.name += "_Extra";
                meshPath = af.currAutoFenceBuilderDirLocation + "/Meshes/" + userMesh.name + ".asset";
                prefabPath = af.currAutoFenceBuilderDirLocation + "/FencePrefabs_AFWB/_Extras_AFWB/" + objName + ".prefab";
            }

            bool isAsset = AssetDatabase.Contains(userMesh);
            if (isAsset == false)
            { // if true, already exists, don't save it
                AssetDatabase.CreateAsset(userMesh, meshPath);
                AssetDatabase.Refresh();
            }
        }
        /*bool isPrefab = AssetDatabase.Contains(userObj);
        bool prefabExists = Directory.Exists(prefabPath);
        if (prefabExists)
        {
            PrefabUtility.SavePrefabAsset(userObj);
        }*/

        //GameObject prefab = PrefabUtility.CreatePrefab(prefabPath, userObj);//** remove commented after v3.5 to check all ok in 2019.3+
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(userObj, prefabPath);
        Debug.Log("CreatePrefab update");


        //result = PrefabUtility.ReplacePrefab(userObj, prefab, ReplacePrefabOptions.ConnectToPrefab);//**
        AssetDatabase.Refresh();
        return result;
    }
    //-------------
    public static bool SaveRandomLookup(RandomLookupAFWB randLookup, string savePath)
    {
        AssetDatabase.CreateAsset(randLookup, savePath);
        AssetDatabase.SaveAssets();
        return true;
    }

    //---------------------------------------
    public string ImportCustomRail(GameObject userAddedRail, AutoFenceCreator.LayerSet layerSet, bool refreshAll = true, bool useAsCurrentRail = true)
    {
        if (userAddedRail == null)
            return "";
        GameObject newRail = af.HandleUserRailChange(userAddedRail); //create the cloned GameObject & meshes
        //Save the user-added object in to the FencePrefabs and Meshes folders
        GameObject savedUserRailPrefab = SaveUserObject(newRail, AutoFenceCreator.FencePrefabType.railPrefab, af);
        if (refreshAll)
            ed.ReloadPrefabsAndPresets(false);
        if (savedUserRailPrefab != null)
        {

            if (useAsCurrentRail)
                af.RebuildWithNewUserPrefab(savedUserRailPrefab, layerSet);
            ed.DestroyNow(newRail);
            Debug.Log("ImportCustomRail: " + savedUserRailPrefab.name);
            if (layerSet == AutoFenceCreator.LayerSet.railALayerSet)
                af.useCustomRailA = true;
            if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
                af.useCustomRailB = true;
            return savedUserRailPrefab.name;
        }
        else
            Debug.LogWarning("savedUserRailPrefab was null");

        return "";
    }
    //---------------------------------------
    public string ImportCustomPost(GameObject userAddedPost, bool useAsCurrentPost = true)
    {
        if (userAddedPost == null)
            return "";
        GameObject newPost = af.HandleUserPostChange(userAddedPost); //create the cloned GameObject & meshes
        //Save the user-added object in to the FencePrefabs and Meshes folders
        GameObject savedUserPostPrefab = SaveUserObject(newPost, AutoFenceCreator.FencePrefabType.postPrefab, af);
        ed.ReloadPrefabsAndPresets(false);
        if (savedUserPostPrefab != null)
        {
            if (useAsCurrentPost)
            {
                af.RebuildWithNewUserPrefab(savedUserPostPrefab, AutoFenceCreator.LayerSet.postLayerSet);
                af.useCustomPost = true;
            }
            ed.DestroyNow(newPost);
            af.useCustomPost = true;
            return savedUserPostPrefab.name;
        }
        else
            Debug.LogWarning("savedUserPostPrefab was null");
        return "";
    }
    //---------------------------------------
    public string ImportCustomExtra(GameObject userAddedExtra, bool useAsCurrentExtra = true)
    {
        if (userAddedExtra == null)
            return "";
        GameObject newExtra = af.HandleUserPostChange(userAddedExtra); //create the cloned GameObject & meshes
        //Save the user-added object in to the FencePrefabs and Meshes folders
        GameObject savedUserExtraPrefab = SaveUserObject(newExtra, AutoFenceCreator.FencePrefabType.extraPrefab, af);
        ed.ReloadPrefabsAndPresets(false);
        if (savedUserExtraPrefab != null)
        {
            if (userAddedExtra)
            {
                af.RebuildWithNewUserPrefab(savedUserExtraPrefab, AutoFenceCreator.LayerSet.extraLayerSet);
                af.useCustomExtra = true;
            }
            ed.DestroyNow(newExtra);
            af.useCustomExtra = true;
            return savedUserExtraPrefab.name;
        }
        else
            Debug.LogWarning("savedUserExtraPrefab was null");
        return "";
    }


    //---------------------------
    public void BuildListOfAllTextures()
    {
        List<Texture> allTextures = new List<Texture>();
        for (int i = 0; i < af.railPrefabs.Count; i++)
        {
            GameObject go = af.railPrefabs[i];
            Renderer rend = go.GetComponent<Renderer>();

            if (rend != null)
            {
                Material mat = rend.sharedMaterial;
                if (mat == null)
                    continue;
                Shader shader = mat.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                //Debug.Log(i.ToString() + "    ------------\n");
                for (int j = 0; j < propertyCount; j++)
                {
                    if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture texture = rend.sharedMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                        if (texture != null)
                        {
                            if (allTextures.Contains(texture) == false)
                            {
                                allTextures.Add(texture);
                                //Debug.Log(allTextures.Count + "   " + texture.name + "\n");
                            }
                        }

                    }
                }
            }
        }
        for (int i = 0; i < af.postPrefabs.Count; i++)
        {
            GameObject go = af.postPrefabs[i];
            Renderer rend = go.GetComponent<Renderer>();

            if (rend != null)
            {
                Material mat = rend.sharedMaterial;
                if (mat == null)
                    continue;
                Shader shader = mat.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                //Debug.Log(i.ToString() + "    ------------\n");
                for (int j = 0; j < propertyCount; j++)
                {
                    if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture texture = rend.sharedMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                        if (texture != null)
                        {
                            if (allTextures.Contains(texture) == false)
                            {
                                allTextures.Add(texture);
                                //Debug.Log(allTextures.Count + "   " + texture.name + "\n");
                            }
                        }

                    }
                }
            }
        }
        for (int i = 0; i < af.extraPrefabs.Count; i++)
        {
            GameObject go = af.extraPrefabs[i];
            Renderer rend = go.GetComponent<Renderer>();

            if (rend != null)
            {
                Material mat = rend.sharedMaterial;
                if (mat == null)
                    continue;
                Shader shader = mat.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                //Debug.Log(i.ToString() + "    ------------\n");
                for (int j = 0; j < propertyCount; j++)
                {
                    if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture texture = rend.sharedMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                        if (texture != null)
                        {
                            if (allTextures.Contains(texture) == false)
                            {
                                allTextures.Add(texture);
                                //Debug.Log(allTextures.Count + "   " + texture.name + "\n");
                            }
                        }

                    }
                }
            }
        }

        string path = "Auto Fence Builder/Materials & Textures U5";
        Texture[] assetsTextureArray = GetAtPath<Texture>(path);
        for (int i = 0; i < assetsTextureArray.Length; i++)
        {
            //Debug.Log(i + "   " + assetsTextureArray[i].name + "\n");
        }
        IsAssetTextureInPrefabs(assetsTextureArray, allTextures);
    }
    //------------------------------------
    public void IsAssetTextureInPrefabs(Texture[] assetsTextures, List<Texture> prefabsTextures)
    {
        for (int i = 0; i < assetsTextures.Length; i++)
        {
            //Debug.Log(i + "   " + assetsTextures[i].name + "\n");
            string assetTexName = assetsTextures[i].name;

            bool foundInPrefab = false;
            for (int j = 0; j < prefabsTextures.Count; j++)
            {
                Texture prefabTex = prefabsTextures[j];
                string prefabTexName = prefabTex.name;

                if (prefabTexName == assetTexName)
                {
                    foundInPrefab = true;
                    break;
                }
            }
            if (foundInPrefab == false)
            {
                Debug.Log(assetTexName + " not found in Prefabs \n");
            }
        }
    }
    //------------------------------------
    public static T[] GetAtPath<T>(string path)
    {

        ArrayList al = new ArrayList();
        string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
        foreach (string fileName in fileEntries)
        {
            int index = fileName.LastIndexOf("/");
            string localPath = "Assets/" + path;

            if (index > 0)
                localPath += fileName.Substring(index);

            //UnityEngine.Object t = Resources.LoadAssetAtPath();
            UnityEngine.Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
            if (t != null)
                al.Add(t);
        }
        T[] result = new T[al.Count];
        for (int i = 0; i < al.Count; i++)
            result[i] = (T)al[i];

        return result;
    }


}
