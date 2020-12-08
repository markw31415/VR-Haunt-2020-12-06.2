using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;


[CreateAssetMenu(fileName = "autofence_permissions", menuName = "AutoFence/ConfigFile", order = 1)]
public class AutoFenceConfigurationManager : ScriptableObject
{
    public bool allowContentFreeTool = false;
    public string note1 = "Do NOT enable this unless you're exporting a content-free version.";
    public string note2 = "This will remove all prefabs!";
    //---------------------------
    public static AutoFenceConfigurationManager ReadPermissionFile(AutoFenceCreator af)
    {
        //List<ScriptablePresetAFWB> presetList = new List<ScriptablePresetAFWB>();
        //string presetFilePath = "Assets/Auto Fence Builder/Presets_AFWB";
        

        bool mainPresetFolderExists = AssetDatabase.IsValidFolder(af.currAutoFenceBuilderDirLocation);
        if (mainPresetFolderExists == false)
        {
            Debug.LogWarning("Main AFWB Folder Missing, Can't load config file.");
            return null;
        }

        string configFilePath = af.currAutoFenceBuilderDirLocation + "/Editor/AutoFenceConfig.asset";
        
        AutoFenceConfigurationManager configFile = AssetDatabase.LoadAssetAtPath(configFilePath, typeof(AutoFenceConfigurationManager)) as AutoFenceConfigurationManager;
        if (configFile == false)
        {
            Debug.LogWarning("Main AFWB configFile missing");
            return null;
        }
        //else 
            //Debug.Log("Found AutoFence Config File");

        bool allowContentFree = configFile.allowContentFreeTool;


        if (allowContentFree == true && af.usingMinimalVersion == false)
        {
            Debug.LogWarning("You are loading a content-free version of Auto Fence to create a prefab-free package, are you sure this is correct? \n");
            Debug.LogWarning("Possibly Auto Fence Builder/Editor/AutoFenceConfig has 'Allow Content Free Tool set to 'On' by mistake." +
                             "If so, please deselect that option in the AutoFenceConfig file and try again, otherwise you can ignore this warning. \n");
            
        }
        
        return configFile;
    }
}

