#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class EditorHelperAFWB
{

    AutoFenceCreator af;
    AutoFenceEditor ed;

    AutoFenceCreator.LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet; // to save a lot of typing
    AutoFenceCreator.LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    AutoFenceCreator.LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    AutoFenceCreator.LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;
    AutoFenceCreator.LayerSet kExtraLayer = AutoFenceCreator.LayerSet.extraLayerSet;

    RandomLookupAFWB randTableRailA = null, randTableRailB = null, randTablePost = null;
    int randTableSize = 0;
    RandomRecords railARandRecords = new RandomRecords();

    public EditorHelperAFWB(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
    }
    //===================================
    public List<GameObject>  GetListOfPrefabsWithCategory(AutoFenceCreator.FencePrefabType prefabType, string categoryString)
    {
        List<GameObject> categoryList = new List<GameObject>();
        
        
        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
        {
            for (int i= 0; i < af.postPrefabs.Count; i++)
            {
                if (af.postNames[i].Contains(categoryString))
                {
                    int prefabIndex = ed.ConvertMenuIndexToPrefabIndex(i, prefabType);
                    categoryList.Add(af.postPrefabs[prefabIndex]); 
                    Debug.Log(af.postNames[i] + "     " + af.postPrefabs[prefabIndex].name  + "\n");
                }
            }
        }

        return categoryList;
    }
    public List<int>  GetListOfPrefabMenuIndicesWithCategory(AutoFenceCreator.FencePrefabType prefabType, string categoryString)
    {
        List<int> categoryList = new List<int>();
        
        
        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
        {
            for (int i= 0; i < af.postPrefabs.Count; i++)
            {
                if (af.postNames[i].Contains(categoryString))
                {
                    categoryList.Add(i); 
                }
            }
        }
        else if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
        {
            for (int i= 0; i < af.railPrefabs.Count; i++)
            {
                if (af.railNames[i].Contains(categoryString))
                {
                    categoryList.Add(i); 
                }
            }
        }
        return categoryList;
    }

    //===================================
    public void SetMainPrefab(AutoFenceCreator.LayerSet layerSet)
    {
        bool mainPrefabChanged = false;


        int currMenuIndex = 0;
        int numMenuNames = 0;
        
        
        if(layerSet == kPostLayer)
        {
            currMenuIndex = af.postMenuIndex;
            numMenuNames = af.postNames.Count;
        }
        else if(layerSet == kRailALayer)
        {
            currMenuIndex = af.railAMenuIndex;
            numMenuNames = af.railNames.Count;
        }
        else if(layerSet == kRailBLayer)
        {
            currMenuIndex = af.railBMenuIndex;
            numMenuNames = af.railNames.Count;
        }
        else if(layerSet == kSubpostLayer)
        {
            currMenuIndex = af.subpostMenuIndex;
            numMenuNames = af.postNames.Count;
        }
        else if(layerSet == kExtraLayer)
        {
            currMenuIndex = af.extraMenuIndex;
            numMenuNames = af.extraNames.Count;
        }
        
        EditorGUILayout.Separator();
        //: The menu names are different to the prefab names, as they have a "category/" added, so we have to do some conversion between the two
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Choose Prefab Type:", ed.cyanBoldStyle, GUILayout.Width(175));

        if (GUILayout.Button("<", EditorStyles.miniButton, GUILayout.Width(17)) && currMenuIndex > 0)
        {
            if(layerSet == kPostLayer)
                af.postMenuIndex -= 1;
            else if(layerSet == kRailALayer)
                af.railAMenuIndex -= 1;
            else if(layerSet == kRailBLayer)
                af.railBMenuIndex -= 1;
            else if(layerSet == kSubpostLayer)
                af.subpostMenuIndex -= 1;
            else if(layerSet == kExtraLayer)
                af.extraMenuIndex -= 1;
            
            mainPrefabChanged = true;
        }
        if (GUILayout.Button(">", EditorStyles.miniButton, GUILayout.Width(17)) && currMenuIndex < numMenuNames - 1)
        {
            if(layerSet == kPostLayer)
                af.postMenuIndex += 1;
            else if(layerSet == kRailALayer)
                af.railAMenuIndex += 1;
            else if(layerSet == kRailBLayer)
                af.railBMenuIndex += 1;
            else if(layerSet == kSubpostLayer)
                af.subpostMenuIndex += 1;
            else if(layerSet == kExtraLayer)
                af.extraMenuIndex += 1;
            
            mainPrefabChanged = true;
        }
        EditorGUILayout.LabelField("", GUILayout.Width(2));//way to make layout cooperate
        
        //========  Main Popup Menu =========
        ed.mediumPopup.fontSize = 11;
        ed.mediumPopup.normal.textColor = new Color(.7f, .38f, 0.0f);
        EditorGUI.BeginChangeCheck();
        if(layerSet == kPostLayer)
            af.postMenuIndex = EditorGUILayout.Popup("", af.postMenuIndex, af.postNames.ToArray(), ed.mediumPopup);
        else if(layerSet == kRailALayer)
            af.railAMenuIndex = EditorGUILayout.Popup("", af.railAMenuIndex, af.railNames.ToArray(), ed.mediumPopup);
        else if(layerSet == kRailBLayer)
            af.railBMenuIndex = EditorGUILayout.Popup("", af.railBMenuIndex, af.railNames.ToArray(), ed.mediumPopup);
        else if(layerSet == kSubpostLayer)
            af.subpostMenuIndex = EditorGUILayout.Popup("", af.subpostMenuIndex, af.postNames.ToArray(), ed.mediumPopup);
        else if(layerSet == kExtraLayer)
            af.extraMenuIndex = EditorGUILayout.Popup("", af.extraMenuIndex, af.extraNames.ToArray(), ed.mediumPopup);
        
        if (EditorGUI.EndChangeCheck())
        {
            mainPrefabChanged = true;
        }
        GUILayout.EndHorizontal();

        //=============== User-Added Prefab ================
        GameObject userAddedPrefab = af.userPostObject;
        if(layerSet != kPostLayer)
            userAddedPrefab = af.userRailObject;

        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        if (layerSet == kPostLayer)
            EditorGUILayout.PropertyField(ed.userPostObject, new GUIContent("Custom Object Import - Drag Here: ",
        "Drag a GameObject from the Hierarchy in to this slot to use as a Post"));
        else if(layerSet == kRailALayer || layerSet == kRailBLayer)
            EditorGUILayout.PropertyField(ed.userRailObject, new GUIContent("Custom Object Import - Drag Here: ",
                "Drag a GameObject from the Hierarchy in to this slot to use as a Rail"));
        else if (layerSet == kExtraLayer)
        {
            EditorGUILayout.PropertyField(ed.userExtraObject, new GUIContent("Custom Object Import - Drag Here: ",
                "Drag a GameObject from the Hierarchy in to this slot to use as an Extra"));
        }
        if (EditorGUI.EndChangeCheck())
        {
            if (layerSet == kPostLayer)
            {
                af.postBakeRotationMode = 1; //Auto rotate
                userAddedPrefab = (GameObject)ed.userPostObject.objectReferenceValue;
                af.currentCustomPostObject = userAddedPrefab;
                ed.res.ImportCustomPost(userAddedPrefab); 
                //af.ResetPostTransforms();
            }
            else if(layerSet == kRailALayer || layerSet == kRailBLayer)
            {
                af.railBakeRotationMode = 1; //Auto rotate
                userAddedPrefab = (GameObject)ed.userRailObject.objectReferenceValue;
                //af.currentCustomRailObject = userAddedPrefab;
                if(layerSet == kRailALayer)
                {
                    ed.res.ImportCustomRail(userAddedPrefab, kRailALayer);
                    af.railAKeepGrounded = false;
                    af.slopeModeRailA = AutoFenceCreator.FenceSlopeMode.shear;
                    af.GroundRails(AutoFenceCreator.LayerSet.railALayerSet);
                    //af.ResetRailATransforms();
                }
                if(layerSet == kRailBLayer)
                {
                    ed.res.ImportCustomRail(userAddedPrefab, kRailBLayer); 
                    af.railBKeepGrounded = false;
                    af.slopeModeRailB = AutoFenceCreator.FenceSlopeMode.shear;
                    af.GroundRails(AutoFenceCreator.LayerSet.railBLayerSet);
                    //af.ResetRailBTransforms();
                }
            }
            else if(layerSet == kExtraLayer)
            {
                //=============== User-Added Custom Extra ================
                af.postBakeRotationMode = 1; //Auto rotate
                userAddedPrefab = (GameObject)ed.userExtraObject.objectReferenceValue;
                af.currentCustomExtraObject = userAddedPrefab;
                ed.res.ImportCustomExtra(userAddedPrefab); 
                //af.ResetExtraTransforms();
                
            }

            mainPrefabChanged = true;
            return; // important to return here. the old object gets set to null, so we have toi avoid ApplyModifiedProperties trying to re-attach it
        }
        //=========== prefab XYZ Settings ==================
        if (GUILayout.Button(new GUIContent("Fix XYZ",
        "If the custom object is rotated incorreectly, you can try to fix it here by applying baked rotations"),
        EditorStyles.miniButton, GUILayout.Width(56)))
        {
            if (ed.rotationsWindowIsOpen == false)
            {
                ed.rotWindow = new BakeRotationsWindow(af, kPostLayer);
                ed.rotWindow.position = new Rect(300, 300, 690, 500);
                ed.rotWindow.ShowPopup();
            }
            else
            {
                ed.rotationsWindowIsOpen = false;
                if (ed.rotWindow != null)
                    ed.rotWindow.Close();
            }
        }
        GUILayout.EndHorizontal();

        //--------------------------
        if (mainPrefabChanged)
        {
            if (layerSet == kPostLayer)
            {
                af.currentPostType = ed.ConvertMenuIndexToPrefabIndex(af.postMenuIndex, AutoFenceCreator.FencePrefabType.postPrefab);
                af.SetPostType(af.currentPostType, true);
                af.postVariants[0].go = af.postDisplayVariationGOs[0] = af.postPrefabs[af.currentPostType];
            }
            else if (layerSet == kRailALayer)
            {
                af.currentRailAType = ed.ConvertMenuIndexToPrefabIndex(af.railAMenuIndex, AutoFenceCreator.FencePrefabType.railPrefab);
                af.SetRailAType(af.currentRailAType, true);
                af.railAVariants[0].go = af.railADisplayVariationGOs[0] = af.railPrefabs[af.currentRailAType];
            }
            else if (layerSet == kRailBLayer)
            {
                af.currentRailBType = ed.ConvertMenuIndexToPrefabIndex(af.railBMenuIndex, AutoFenceCreator.FencePrefabType.railPrefab);
                af.SetRailBType(af.currentRailBType, true);
                af.railBVariants[0].go = af.railBDisplayVariationGOs[0] = af.railPrefabs[af.currentRailBType];
            }
            else if (layerSet == kSubpostLayer)
            {
                af.currentSubpostType = ed.ConvertMenuIndexToPrefabIndex(af.subpostMenuIndex, AutoFenceCreator.FencePrefabType.postPrefab);
                af.SetSubType(af.currentSubpostType, true);
                af.subpostVariants[0].go = af.subpostDisplayVariationGOs[0] = af.postPrefabs[af.currentSubpostType];
            }
            else if (layerSet == kExtraLayer)
            {
                af.currentExtraType = ed.ConvertMenuIndexToPrefabIndex(af.extraMenuIndex, AutoFenceCreator.FencePrefabType.extraPrefab);
                af.SetExtraType(af.currentExtraType, true);
            }

            af.ResetPool(layerSet);
            af.ForceRebuildFromClickPoints(layerSet);
        }
        EditorGUILayout.Separator();
    }
    //------------------------------------
    public void SetPostMainParameters()
    {
        EditorGUILayout.Separator();
        //===  Post Height Offset  ===
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Post Raise/Lower",
                "Use this to move the post up and down. There isn't a Position Offset x/y/z/ as the post's x&z position are determined by the click-points"),
            GUILayout.Width(225));
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ed.postHeightOffset, new GUIContent(""), GUILayout.Width(295));
        if (GUILayout.Button(new GUIContent("R", "Reset Post height offset to zero"), EditorStyles.miniButton, GUILayout.Width(20)))
        {

            ed.postHeightOffset.floatValue = af.postHeightOffset = 0;
        }
        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
        GUILayout.EndHorizontal();

        // Post Size
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.postSize); // Size
        if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.postSize.vector3Value = Vector3.one;
        }
        EditorGUILayout.EndHorizontal();
        // PostBoost Size
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.mainPostSizeBoost); // Size
        if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.mainPostSizeBoost.vector3Value = Vector3.one;
        }
        EditorGUILayout.EndHorizontal();
        // Post Rotation
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.postRotation); // Size
        if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.postRotation.vector3Value = Vector3.zero;
        }
        EditorGUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
        {
            ed.postSize.vector3Value = ed.EnforceVectorMinimums(ed.postSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.mainPostSizeBoost.vector3Value = ed.EnforceVectorMinimums(ed.mainPostSizeBoost.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
    }
    public void SetExtraMainParameters()
    {
        EditorGUILayout.Separator();
        //===  Extra Height Offset  ===
        /*GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Post Raise/Lower",
                "Use this to move the post up and down. There isn't a Position Offset x/y/z/ as the post's x&z position are determined by the click-points"),
            GUILayout.Width(225));
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ed.postHeightOffset, new GUIContent(""), GUILayout.Width(295));
        if (GUILayout.Button(new GUIContent("R", "Reset Post height offset to zero"), EditorStyles.miniButton, GUILayout.Width(20)))
        {

            ed.postHeightOffset.floatValue = af.postHeightOffset = 0;
        }
        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
        GUILayout.EndHorizontal();*/

        EditorGUI.BeginChangeCheck();
        
        // Extra position offset
        EditorGUILayout.PropertyField(ed.extraPositionOffset);
        
        // Extra Size
        EditorGUILayout.PropertyField(ed.extraSize);
        
        // Extra Rotation
        EditorGUILayout.PropertyField(ed.extraRotation);
        
        if (EditorGUI.EndChangeCheck())
        {
            ed.extraSize.vector3Value = ed.EnforceVectorMinimums(ed.extraSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
    }
    //------------------------------------
    public void SetSubpostMainParameters()
    {
        EditorGUILayout.Separator();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.subpostPositionOffset); // Position
        if (GUILayout.Button(new GUIContent("X", "Set Position values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.subpostPositionOffset.vector3Value = Vector3.zero;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.subpostSize); // Size
        if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.subpostSize.vector3Value = Vector3.one;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(ed.subpostRotation); // Rotation
        if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
        {
            ed.subpostRotation.vector3Value = Vector3.zero;
        }
        EditorGUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
        {
            ed.subpostSize.vector3Value = ed.EnforceVectorMinimums(ed.subpostSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints(); 
        }
    }
    //------------------------------------
    public void SetRailMainParameters(AutoFenceCreator.LayerSet railLayer)
    {
        EditorGUILayout.Separator();
        bool rebuild = false;

        if (railLayer == AutoFenceCreator.LayerSet.railALayerSet)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railAPositionOffset); // Position
            if (GUILayout.Button(new GUIContent("X", "Set Position values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railAPositionOffset.vector3Value = Vector3.zero;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railASize); // Size
            if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railASize.vector3Value = Vector3.one;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railARotation); // Rotation
            if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railARotation.vector3Value = Vector3.zero;
            }
            EditorGUILayout.EndHorizontal();
            
            af.rotateFromBaseRailA = EditorGUILayout.ToggleLeft(new GUIContent("Rotate From Base", "Default off, will rotate from centre"), af.rotateFromBaseRailA);
            
            if (EditorGUI.EndChangeCheck())
                rebuild = true;
        }
        else if (railLayer == AutoFenceCreator.LayerSet.railBLayerSet)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railBPositionOffset); // Position
            if (GUILayout.Button(new GUIContent("X", "Set Position values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railBPositionOffset.vector3Value = Vector3.zero;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railBSize); // Size
            if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railBSize.vector3Value = Vector3.one;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ed.railBRotation); // Rotation
            if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), EditorStyles.miniButton, GUILayout.Width(8)))
            {
                ed.railBRotation.vector3Value = Vector3.zero;
            }
            EditorGUILayout.EndHorizontal();
            
            af.rotateFromBaseRailB = EditorGUILayout.ToggleLeft(new GUIContent("Rotate From Base", "Default off, will rotate from centre"), af.rotateFromBaseRailB);
            
            if (EditorGUI.EndChangeCheck())
                rebuild = true;
        }

        if (rebuild)
        {
            ed.railASize.vector3Value = ed.EnforceVectorMinimums(ed.railASize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.railBSize.vector3Value = ed.EnforceVectorMinimums(ed.railBSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
            ed.serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints(); 
        }
    }
    //--------------------------------
    // This is th main SavePreset which creates a preset from current settings
    public bool SavePreset(bool forcedSave = false, bool reloadAll = true)
    {
        Event currentEvent = Event.current;
        if (currentEvent.control && currentEvent.shift)
            forcedSave = true; // will overwrite without asking

        ScriptablePresetAFWB preset = ScriptablePresetAFWB.CreatePresetFromCurrentSettings(ed.af.scrPresetSaveName, "", af);
        preset.categoryName = ScriptablePresetAFWB.FindCategoryForPreset(preset, ed.af.scrPresetSaveName, af.categoryNames[af.categoryIndex], af);
        string filePath = ScriptablePresetAFWB.CreateSaveString(af, preset.name, preset.categoryName);
        if (filePath == "")
        {
            Debug.LogWarning("filePath was zero. Not saving");
            return false;
        }

        bool fileExists = File.Exists(filePath);

        if (fileExists && forcedSave == false)
        {
            ed.presetSaveRename = "";
            SavePresetWindow saveWindow = ScriptableObject.CreateInstance(typeof(SavePresetWindow)) as SavePresetWindow;
            saveWindow.Init(ed, ed.af.scrPresetSaveName, preset);
            saveWindow.minSize = new Vector2(475, 190); saveWindow.maxSize = new Vector2(475, 190);
            saveWindow.ShowUtility();
        }
        else
        {
            ScriptablePresetAFWB.SaveScriptablePreset(af, preset, filePath, true, forcedSave);
            
            if(reloadAll == true)
            {
                LoadAllScriptablePresets(af.allowContentFreeUse);
                string menuName = preset.categoryName + "/" + preset.name;
                int index = ed.scriptablePresetNames.IndexOf(menuName);
                if (index != -1)
                {
                    af.currentScrPresetIndex = index;
                    SetupPreset(index);
                }
            }
        }
        return true;
    }
    //--------------------------------
    //This saves a safety preset every time a fence is finished
    public static ScriptablePresetAFWB SaveFinishedPreset(string finishedFolderName, AutoFenceCreator afc, bool forcedSave = true, bool reloadAll = false)
    {

        ScriptablePresetAFWB preset = ScriptablePresetAFWB.CreatePresetFromCurrentSettings(finishedFolderName + "_" + afc.scrPresetSaveName, "", afc);
        preset.categoryName = ScriptablePresetAFWB.FindCategoryForPreset(preset, afc.scrPresetSaveName, afc.categoryNames[afc.categoryIndex], afc);
        string filePath = ScriptablePresetAFWB.CreateSaveStringForFinished(afc, preset.name, preset.categoryName);
        if (filePath == "")
        {
            Debug.LogWarning("filePath was zero. Not saving");
            return null;
        }
        ScriptablePresetAFWB.SaveScriptablePreset(afc, preset, filePath, true, forcedSave);
        return preset;
    }
    //--------------------------------
    //this saves from an existing ScriptablePresetAFWB
    public bool SavePreset(ScriptablePresetAFWB preset, AutoFenceCreator afc, bool forcedSave, bool reloadAll)
    {
        Event currentEvent = Event.current;
        if (currentEvent.control && currentEvent.shift)
            forcedSave = true; // will overwrite without asking
        preset.categoryName = ScriptablePresetAFWB.FindCategoryForPreset(preset, ed.af.scrPresetSaveName, afc.categoryNames[afc.categoryIndex], af);
        string filePath = ScriptablePresetAFWB.CreateSaveString(af, preset.name, preset.categoryName);
        if (filePath == "")
        {
            Debug.LogWarning("filePath was zero. Not saving");
            return false;
        }
        bool fileExists = File.Exists(filePath);

        if (fileExists && forcedSave == false)
        {
            ed.presetSaveRename = "";
            SavePresetWindow saveWindow = ScriptableObject.CreateInstance(typeof(SavePresetWindow)) as SavePresetWindow;
            saveWindow.Init(ed, ed.af.scrPresetSaveName, preset);
            saveWindow.minSize = new Vector2(475, 190); saveWindow.maxSize = new Vector2(475, 190);
            saveWindow.ShowUtility();
        }
        else
        {
            ScriptablePresetAFWB.SaveScriptablePreset(af, preset, filePath, true, forcedSave);

            if(reloadAll == true)
            {
                LoadAllScriptablePresets(af.allowContentFreeUse);
                string menuName = preset.categoryName + "/" + preset.name;
                int index = ed.scriptablePresetNames.IndexOf(menuName);
                if (index != -1)
                {
                    afc.currentScrPresetIndex = index;
                    SetupPreset(index);
                }
            }
        }
        return true;
    }
    //--------------------------------
    //this saves from an existing ScriptablePresetAFWB
    public bool SaveZeroContentPreset(ScriptablePresetAFWB preset, AutoFenceCreator afc)
    {
        
        ScriptablePresetAFWB presetCopy = ScriptablePresetAFWB.CreatePresetFromCurrentSettings(preset.name, preset.categoryName, afc);
        string filePath = ScriptablePresetAFWB.CreateSaveString(af, presetCopy.name, presetCopy.categoryName);
        if (filePath == "")
        {
            Debug.LogWarning("filePath was zero. Not saving");
            return false;
        }
        ScriptablePresetAFWB.SaveScriptablePreset(af, presetCopy, filePath, true, true);
        return true;
    }
    //---------------
    public void LoadAllScriptablePresets(bool zeroContentVersion)
    {
        if(zeroContentVersion == false)
        {
            ed.scriptablePresetList = ScriptablePresetAFWB.ReadPresetFiles(af);
        }
        else if(zeroContentVersion == true)
        {
            HandleZeroContentPreset();
        }
        
        if (ed.scriptablePresetList == null || ed.scriptablePresetList.Count == 0)
        {
            Debug.LogWarning("Presets missing from Main AFWB Presets. No presets available\n");
            Debug.LogWarning("Presets should be in Auto Fence Builder/PresetsAFWB\n");
            ed.scriptablePresetList = new List<ScriptablePresetAFWB>();
            for (int i = 0; i < 2; i++)
            {
                ScriptablePresetAFWB defaultPreset = ScriptableObject.CreateInstance<ScriptablePresetAFWB>();
                defaultPreset.name = "default preset (it looks like your preset folder is empty " + i;
                defaultPreset.categoryName = "default";

                ed.scriptablePresetList.Add(defaultPreset);
               ed.scriptablePresetNames.Add(defaultPreset.name);
            }
        }

        // Check for old bad presets
        for (int i = 0; i < ed.scriptablePresetList.Count; i++)
        {
            ScriptablePresetAFWB preset = ed.scriptablePresetList[i];

            //-- Check for missing Category name
            if (preset.categoryName == "")
            {
                preset.categoryName = ScriptablePresetAFWB.FindCategoryForPreset(preset, ed.af.scrPresetSaveName, "", af);
                Debug.Log("categoryName missing for " + preset.name + ".  Assigned to: " + preset.categoryName);
            }


            if (af.FindPostPrefabIndexByName(preset.postName) == -1)
            {
                Debug.LogWarning("Missing Post [" + preset.postName + "] is -1 in " + preset.categoryName + "/" + preset.name + "\n");
                //return; 
            }

            //Check there's correct number of post variants
            if (preset.postVariants.Count < AutoFenceCreator.kNumPostVariations)
            {
                for (int j = preset.postVariants.Count; j < AutoFenceCreator.kNumPostVariations; j++)
                {
                    if (af.FindPostPrefabIndexByName(preset.postName) != -1)
                        preset.postVariants.Add(new FenceVariant(preset.postName));
                    else
                        preset.postVariants.Add(new FenceVariant("ABasicConcrete_Post"));

                }
            }
        }
        ed.CreateScriptablePresetStringsForMenus(ed.scriptablePresetList);
        ed.GetCategoryNamesFromLoadedPresetList();
        //Debug.Log("Read " + ed.scriptablePresetList.Count + " AFWB presets");
    }
    //-------------------------
    private void HandleZeroContentPreset()
    {
        ed.scriptablePresetList = ScriptablePresetAFWB.ReadZeroContentPresetFiles(af);
        // Create new presets folders if necessary
        string mainPresetsFolderPath = ed.af.currAutoFenceBuilderDirLocation + "/Presets_AFWB";
        bool folderExists = AssetDatabase.IsValidFolder(mainPresetsFolderPath);
        if (folderExists == false)
        {
            string guid = AssetDatabase.CreateFolder(ed.af.currAutoFenceBuilderDirLocation, "Presets_AFWB");
            mainPresetsFolderPath = AssetDatabase.GUIDToAssetPath(guid);
            if (mainPresetsFolderPath == "")
            {
                Debug.LogWarning("Couldn't create Presets_AFWB folder \n");
            }
            else
            {
                folderExists = true;
            }
        }

        if (folderExists && ed.scriptablePresetList != null && ed.scriptablePresetList.Count > 0)
        {
            bool saved = SaveZeroContentPreset(ed.scriptablePresetList[0], ed.af);
            if (saved == false)
                Debug.LogWarning("Problem saving Zero Content Preset \n");
        }
        else
        {
            if (folderExists == false)
                Debug.LogWarning("Couldn't create Presets_AFWB folder \n");
            if (ed.scriptablePresetList == null)
                Debug.LogWarning("ed.scriptablePresetList was null \n");
            if (ed.scriptablePresetList.Count == 0)
                Debug.LogWarning("ed.scriptablePresetList.Count was 0 \n");
        }
    }
    //------------------
    public void ResaveAllScriptablePresets()
    {
        
        for (int i = 0; i < ed.scriptablePresetList.Count; i++)
        {
            ScriptablePresetAFWB preset = ed.scriptablePresetList[i];

            ed.af.scrPresetSaveName = preset.name;

            af.currentScrPresetIndex = i;
            
            SetupPreset(af.currentScrPresetIndex);

            bool resave = false;
            if (preset.railAName.Contains("_Panel_Rail") && preset.slopeModeRailA == AutoFenceCreator.FenceSlopeMode.slope)
            {
                af.slopeModeRailA = AutoFenceCreator.FenceSlopeMode.shear;
                resave = true;
            }
            if (preset.railBName.Contains("_Panel_Rail") && preset.slopeModeRailB == AutoFenceCreator.FenceSlopeMode.slope)
            {
                af.slopeModeRailB = AutoFenceCreator.FenceSlopeMode.shear;
                resave = true;
            }

            if (resave)
            {
                Debug.Log(preset.name + "\n");
                SavePreset(true, false); 
            }
        }
    }
    //------------------
    /*public void ResaveScriptablePreset()
    {
        SavePreset(ScriptablePresetAFWB preset, AutoFenceCreator afc, bool forcedSave, bool reloadAll)
    }*/

    //--------------------
    bool CheckCurrentSettingsPrefabsAssignment()
    {
        if (af.currentPostType == -1)
        {
            Debug.LogWarning("currentPostType is -1  " + ed.currScrPreset.name + "\n");
            return false;
        }
        if (af.currentRailAType == -1)
        {
            Debug.LogWarning("currentRailAType is -1 " + ed.currScrPreset.name + "\n");
            return false;
        }
        if (af.currentRailBType == -1)
        {
            Debug.LogWarning("currentRailBType is -1 " + ed.currScrPreset.name + "\n");
            return false;
        }
        if (af.currentSubpostType == -1)
        {
            Debug.LogWarning("currentSubpostType is -1  " + ed.currScrPreset.name + "\n");
            return false;
        }
        if (af.currentExtraType == -1)
        {
            Debug.LogWarning("currentExtraType is -1  in " + ed.currScrPreset.name + "\n");
            return false;
        }

        //Check there's correct number of post variants
        if (af.postVariants.Count < AutoFenceCreator.kNumPostVariations)
        {
            for (int j = af.postVariants.Count; j < AutoFenceCreator.kNumPostVariations; j++)
            {
                if (af.currentPostType != -1)
                {
                    af.postVariants.Add(new FenceVariant(af.postPrefabs[af.currentPostType].name));
                    Debug.Log("Fixed current postVariants");
                }
                else
                    af.postVariants.Add(new FenceVariant("ABasicConcrete_Post"));
            }
        }
        return true;
    }

//---------------
    public void SetupPreset(int presetIndex)
    {
        if (presetIndex >= ed.scriptablePresetList.Count)
        {
            Debug.LogWarning("Trying to access presetIndex beyond count. " + presetIndex + " Aborting");
            return;
        }
        af.currentScrPresetIndex = presetIndex; //in case it's called from code
        ed.currScrPreset = ed.scriptablePresetList[presetIndex];

        //ed.varHelper.CheckPresetValidateVariants(ed.currScrPreset);
        //ed.currScrPreset.BuildFromPreset(af);

        SetupPreset(ed.currScrPreset);
    }

    //---------------
    public void SetupPreset(ScriptablePresetAFWB preset)
    {
        /*if (presetIndex >= ed.scriptablePresetList.Count)
        {
            Debug.LogWarning("Trying to access presetIndex beyond count. Aborting");
            return;
        }
        af.currentScrPresetIndex = presetIndex; //in case it's called from code*/
        
        ed.currScrPreset = preset;

        ed.varHelper.CheckPresetValidateVariants(ed.currScrPreset);
        ed.currScrPreset.BuildFromPreset(af);

        //-- Check Valid Main prefabs--
        if (af.currentPostType == -1)
        {
            Debug.LogWarning("Post [" + ed.currScrPreset.postName + "] is -1 in " + ed.currScrPreset.name + ".  Using default Post[0] instead.\n");
            af.currentPostType = 0;
        }
        if (af.currentRailAType == -1)
        {
            Debug.LogWarning("RailA [" + ed.currScrPreset.railAName + "] is -1 in " + ed.currScrPreset.name + ".  Using default Rail[0] instead.\n");
            af.currentRailAType = 0;
        }
        if (af.currentRailBType == -1)
        {
            Debug.LogWarning("RailB [" + ed.currScrPreset.railBName + "] is -1 in " + ed.currScrPreset.name + ".  Using default Rail[0] instead.\n");
            af.currentRailBType = 0;
        }
        if (af.currentSubpostType == -1)
        {
            Debug.LogWarning("SubPost [" + ed.currScrPreset.subPostName + "] is -1 in " + ed.currScrPreset.name + ".  Using default Post[0] instead.\n");
            af.currentSubpostType = 0;
        }
        if (af.currentExtraType == -1)
        {
            Debug.LogWarning("Extra [" + ed.currScrPreset.extraName + "] is -1 in " + ed.currScrPreset.name + ".  Using default Extra[0] instead.\n");
            af.currentExtraType = 0;
        }

        ed.af.scrPresetSaveName = ed.currScrPreset.name;
        ed.varHelper.SetBaseVariantObjects();
        ed.varHelper.FillEmptyVariantsWithMain();

        af.extraMenuIndex = ed.ConvertExtraPrefabIndexToMenuIndex(af.currentExtraType);
        af.subpostMenuIndex = ed.ConvertPostPrefabIndexToMenuIndex(af.currentSubpostType);

        af.CreateUsedVariantsList(kRailALayer);
        af.CreateUsedVariantsList(kRailBLayer);
        af.CreateUsedVariantsList(kPostLayer);
        af.CreateUsedVariantsList(kSubpostLayer);

        for (int i = 0; i < AutoFenceCreator.kNumRailVariations; i++)
        {
            if (af.railAVariants[i].go == null)
                af.railAVariants[i].go = af.railADisplayVariationGOs[i] = af.railPrefabs[af.currentRailAType];
            if (af.railBVariants[i].go == null)
                af.railBVariants[i].go = af.railBDisplayVariationGOs[i] = af.railPrefabs[af.currentRailBType];
            if (af.postVariants[i].go == null)
                af.postVariants[i].go = af.postDisplayVariationGOs[i] = af.postPrefabs[af.currentPostType];

            //Fix bad variants with all zero size
            if (af.railAVariants[i].size.x == 0 && af.railAVariants[i].size.y == 0 && af.railAVariants[i].size.z == 0)
                af.railAVariants[i].size = Vector3.one;
            if (af.railBVariants[i].size.x == 0 && af.railBVariants[i].size.y == 0 && af.railBVariants[i].size.z == 0)
                af.railBVariants[i].size = Vector3.one;
            if (af.postVariants[i].size.x == 0 && af.postVariants[i].size.y == 0 && af.postVariants[i].size.z == 0)
                af.postVariants[i].size = Vector3.one;
        }

        for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
        {
            //Fix bad sequence with all zero size
            if (af.userSequenceRailA[i].size.x == 0 && af.userSequenceRailA[i].size.y == 0 && af.userSequenceRailA[i].size.z == 0)
                af.userSequenceRailA[i].size = Vector3.one;
            if (af.userSequenceRailB[i].size.x == 0 && af.userSequenceRailB[i].size.y == 0 && af.userSequenceRailB[i].size.z == 0)
                af.userSequenceRailB[i].size = Vector3.one;
            if (af.userSequencePost[i].size.x == 0 && af.userSequencePost[i].size.y == 0 && af.userSequencePost[i].size.z == 0)
                af.userSequencePost[i].size = Vector3.one;
            if (af.userSequenceSubpost[i].size.x == 0 && af.userSequenceSubpost[i].size.y == 0 && af.userSequenceSubpost[i].size.z == 0)
                af.userSequenceSubpost[i].size = Vector3.one;

            af.seqRailAStepEnabled[i] = af.userSequenceRailA[i].stepEnabled;
            af.seqRailBStepEnabled[i] = af.userSequenceRailB[i].stepEnabled;
            af.seqPostStepEnabled[i] = af.userSequencePost[i].stepEnabled;
            af.seqSubpostStepEnabled[i] = af.userSequenceSubpost[i].stepEnabled;

            af.seqRailASize[i] = af.userSequenceRailA[i].size;
            af.seqRailAOffset[i] = af.userSequenceRailA[i].pos;
            af.seqRailARotate[i] = af.userSequenceRailA[i].rot;

            af.seqRailBSize[i] = af.userSequenceRailB[i].size;
            af.seqRailBOffset[i] = af.userSequenceRailB[i].pos;
            af.seqRailBRotate[i] = af.userSequenceRailB[i].rot;

            af.seqPostSize[i] = af.userSequencePost[i].size;
            af.seqPostOffset[i] = af.userSequencePost[i].pos;
            af.seqPostRotate[i] = af.userSequencePost[i].rot;
            
            af.seqSubpostSize[i] = af.userSequenceSubpost[i].size;
            af.seqSubpostOffset[i] = af.userSequenceSubpost[i].pos;
            af.seqSubpostRotate[i] = af.userSequenceSubpost[i].rot;


            af.seqPostX[i] = af.userSequencePost[i].backToFront;
            af.seqPostZ[i] = af.userSequencePost[i].mirrorZ;
            af.seqPostInvert180[i] = af.userSequencePost[i].invert;
            
            af.seqSubpostX[i] = af.userSequenceSubpost[i].backToFront;
            af.seqSubpostZ[i] = af.userSequenceSubpost[i].mirrorZ;
            af.seqSubpostInvert180[i] = af.userSequenceSubpost[i].invert;

            af.seqRailAVarIndex[i] = af.userSequenceRailA[i].objIndex;
            af.seqRailBVarIndex[i] = af.userSequenceRailB[i].objIndex;
            af.seqPostVarIndex[i] = af.userSequencePost[i].objIndex;
            af.seqSubpostVarIndex[i] = af.userSequenceSubpost[i].objIndex;
        }

        ed.varHelper.SyncControlsAfterPresetChange();
        ed.copySeqStepVariant  = null;
        //This control is unique to Posts, so set it here
        af.postQuantizeRotAmount = ed.currScrPreset.postQuantizeRotAmount;
        for (int i = 0; i < ed.quantizeRotStrings.Length; i++)
        {
            if (float.Parse(ed.quantizeRotStrings[i]) == af.postQuantizeRotAmount)
            {
                ed.quantizeRotIndexPost.intValue = i;
                break;
            }
        }
        ed.varHelper.CreateOptimalSequenceA();
        ed.varHelper.CreateOptimalSequenceB();

        //PrintSequencerInfo(af.userSequenceRailA, 0, af.numUserSeqStepsRailA);
        af.ClearAllSingles();
        af.extraMenuIndex = ed.ConvertExtraPrefabIndexToMenuIndex(af.currentExtraType);
        af.categoryIndex = af.categoryNames.IndexOf(ed.currScrPreset.categoryName);
        af.ResetAllPools();
        af.ForceRebuildFromClickPoints();
    }

    public void SetupRandomTables()
    {
        randTableSize = RandomLookupAFWB.kRandLookupTableSize;
        string tablesPath = af.currAutoFenceBuilderDirLocation + "/Editor/";

        //-- Keep this here in case user wants to re-generate tables
        /*railARandIndex = 123;
        railBRandIndex = 234;
        postRandIndex = 345;
        randTableRailA = RandomLookupAFWB.randForRailA = RandomLookupAFWB.CreateRandomTable(railARandIndex);
        RandomLookupAFWB.SaveRandomLookup(randTableRailA, tablesPath + "RandomLookupTableRailA.asset");
        randTableRailB = RandomLookupAFWB.randForRailB = RandomLookupAFWB.CreateRandomTable(railBRandIndex);
        RandomLookupAFWB.SaveRandomLookup(randTableRailB, tablesPath + "RandomLookupTableRailB.asset");
        randTablePost = RandomLookupAFWB.randForPost = RandomLookupAFWB.CreateRandomTable(postRandIndex);*/

        if (RandomLookupAFWB.randForRailA == null)
            randTableRailA = RandomLookupAFWB.randForRailA = AssetDatabase.LoadAssetAtPath(tablesPath + "RandomLookupTableRailA.asset", typeof(RandomLookupAFWB)) as RandomLookupAFWB;
        if (RandomLookupAFWB.randForRailB == null)
            randTableRailB = RandomLookupAFWB.randForRailB = AssetDatabase.LoadAssetAtPath(tablesPath + "RandomLookupTableRailB.asset", typeof(RandomLookupAFWB)) as RandomLookupAFWB;
        if (RandomLookupAFWB.randForPost == null)
            randTablePost = RandomLookupAFWB.randForPost = AssetDatabase.LoadAssetAtPath(tablesPath + "RandomLookupTablePost.asset", typeof(RandomLookupAFWB)) as RandomLookupAFWB;
    }




}
