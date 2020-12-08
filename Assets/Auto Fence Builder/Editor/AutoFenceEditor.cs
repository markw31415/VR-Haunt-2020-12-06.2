/* Auto Fence & Wall Builder v3.0 twoclicktools@gmail.com Feb 2019 */
#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

//--------------------------
[CustomEditor(typeof(AutoFenceCreator))]
public class AutoFenceEditor : Editor
{
    AutoFenceCreator.LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet; // to save a lot of typing
    AutoFenceCreator.LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    AutoFenceCreator.LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    AutoFenceCreator.LayerSet kExtraLayer = AutoFenceCreator.LayerSet.extraLayerSet;
    AutoFenceCreator.LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;
    AutoFenceCreator.LayerSet kAllLayer = AutoFenceCreator.LayerSet.allLayerSet;
    
    
    public AutoFenceCreator af; // the main AutoFence script
    public SerializedProperty globalLiftLower, extraGameObject, userRailObject, userPostObject, userExtraObject;
    public SerializedProperty fenceHeight, fenceWidth, postSize, postRotation, postHeightOffset, mainPostSizeBoost;
    public SerializedProperty extraSize, extraPositionOffset, extraRotation;
    public SerializedProperty numStackedRailsA, numStackedRailsB, railASize, railBSize, railAPositionOffset, railARotation, railBPositionOffset, railBRotation;
    private SerializedProperty useMainRails, useSecondaryRails, railASpread, railBSpread;
    //private SerializedProperty     rotateY/*, rotateX, mirrorH*/ ; //v2.3
    public SerializedProperty subSpacing, useSubPosts, subpostSize, subpostPositionOffset, subpostRotation, useSubJoiners;
    private SerializedProperty roundingDistance;
    private SerializedProperty showControls, closeLoop, frequency, amplitude, wavePosition, useWave, switchControlsAlso;
    private SerializedProperty obj;
    private SerializedProperty gs, scaleInterpolationAlso; //global scale
    private SerializedProperty keepInterpolatedPostsGrounded;
    private SerializedProperty lerpPostRotationAtCorners, lerpPostRotationAtCornersInters, hideInterpolated, snapMainPosts, snapSize;
    private SerializedProperty jitterAmount;
    //===== Post Variaton Parameters ========
    private SerializedProperty postSpacingVariation, allowVertical180Invert_Post, allowMirroring_X_Post, allowMirroring_Z_Post;
    private SerializedProperty jitterPostVerts, mirrorXPostProbability, mirrorZPostProbability, verticalInvertPostProbability;
    private SerializedProperty postVariation2, postVariation3;
    //===== Rail Variaton Parameters ========
    private SerializedProperty allowMirroring_X_Rail, allowMirroring_Z_Rail, useRailASeq;
    private SerializedProperty jitterRailVerts, mirrorXRailProbability, mirrorZRailProbability, verticalInvertRailProbability;
    private SerializedProperty railVariation1, railVariation2, railVariation3, railADisplayVariationGOs;
    private SerializedProperty minRailHeightLimit, maxRailHeightLimit;
    private SerializedProperty minRailAHeightVar, maxRailAHeightVar, minRailBHeightVar, maxRailBHeightVar;
    public SerializedProperty railAProbArray, varRailAPositionOffset, varRailASize, varRailARotation;
    public SerializedProperty railBProbArray, varRailBPositionOffset, varRailBSize, varRailBRotation;
    private SerializedProperty varRailABackToFront, varRailAMirrorZ, varRailAInvert, varRailBBackToFront, varRailBMirrorZ, varRailBInvert;
    private SerializedProperty varRailABackToFrontBools, varRailAMirrorZBools, varRailAInvertBools;
    private SerializedProperty varRailBBackToFrontBools, varRailBMirrorZBools, varRailBInvertBools;
    public SerializedProperty railASinglesList, railBSinglesList;
    public SerializedProperty quantizeRotIndexPost, quantizeRotIndexSubpost;
    public SerializedProperty postQuantizeRotAmount, subPostQuantizeRotAmount;
    
    private bool oldCloseLoop = false;
    protected bool fencePrefabsFolderFound = true, userUnloadedAssets = false;
    public string presetName = "Fence Preset_001";
    public string scriptablePresetName = "scriptablePresetName";
    public bool undone = false;
    public bool addedPostNow = false, deletedPostNow = false;
    public Color darkCyan = new Color(0, .35f, .65f);
    Color darkMagenta = new Color(.35f, 0.05f, .95f);
    Color darkRed = new Color(0.85f, .0f, .0f), darkerRed = new Color(0.75f, .0f, .0f);
    Color transRed = new Color(0.999f, .0f, .0f, 0.5f);
    public Color switchGreen = new Color(0.6f, 0.99f, 0.6f), switchRed = new Color(0.999f, 0.8f, 0.8f);

    public GUIStyle warningStyle, mildWarningStyle, infoStyle, infoStyleSmall, cyanBoldStyle, cyanBoldStyleBigger, italicStyle, lightGrayStyle, darkGrayStyle;
    public GUIStyle mediumPopup, redWarningStyle, defaultButtonStyle, regularPopupStyle, grayHelpStyle;
    public GUIStyle smallLabel, smallStyle, smallButtonStyle, smallToolbarStyle, miniBold, smallBoldBlack, label11;
    bool showBatchingHelp = false, showRefreshAndUnloadHelp = false;
    public bool showRailAVariations = true, showRailBVariations = true, displayRailsA = true, displayRailsB = false;
    public bool showPostVariations = true;
    int selGridInt = 0;
    string[] variationModeToolbarStrings = { "Quick Optimal Variation", "Random Variation", "Sequenced" };
    string[] railSetToolbarStrings = { "Rails Main Layer A", "Rails Secondary Layer B" };
    static string railATooltipString = "Show settings for main Rails layer. \n" +
        "Layer A can be toggled on/off while using Layer B, by Control-Clicking the Toolbar Button";
    static string railBTooltipString = "Use this when you want to add a secondary Rail design in conjunction with Rail Layer A. \n" +
        "Layer B can be toggled on/off while using Layer A, by Control-Clicking the Toolbar Button";
    GUIContent[] railsetToolbarGUIContent = {new GUIContent("Rails Main Layer A", "This is the default Rail that is used where only one Rail design is required"),
        new GUIContent("Rails Secondary Layer B", railBTooltipString)};


    GUIContent[] globalsToolbarRow1_GUIContent = {  new GUIContent("Scale & Raise/Lower", "Scale everything. Raise or lower everything"), new GUIContent("Smoothing", "Round corners"),
                                                    new GUIContent("Snap, Close, Reverse", "Snap to Unity World Grid, Close loop of fence")
        };
    GUIContent[] globalsToolbarRow2_GUIContent = {  new GUIContent("Cloning", "Clone other fence"), new GUIContent("Combine", "Combine Finished Fence Meshes"),
                                                    new GUIContent("Refresh/Unload", "Reloal or offload prefabs"), 
                                                    new GUIContent("Settings", "Parenting, Colliders, Layer Numbers, Gaps, LOD")
        };
    GUIContent[] componentToobarContent = {  new GUIContent("Posts Controls", "Show Posts Controls"),
                                                new GUIContent("Rails A Controls", "Show Rails A Controls"),
                                                new GUIContent("Rails B Controls", "Show Rails B Controls"),
                                                new GUIContent("Subposts Controls", "Show Subposts Controls"),
                                                     new GUIContent("Extras Controls", "Show Extras Controls"),
    };

    public string[] seqStrings = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24" };
    public int currSeqAStepIndex = 1, currSeqBStepIndex = 1;
    public int currSeqPostStepIndex = 1, varPostSeqPopup = 0;
    int varASeqPopup = 0, varBSeqPopup = 0;

    public bool rotationsWindowIsOpen = false;
   public  BakeRotationsWindow rotWindow = null;
    FencePrefabLoader loader;
    Texture2D railSectionTexture, testTex;

    Color defaultBackgroundColor = Color.white;
    Color lightBlueBackgroundColor = new Color(0.94f, 0.95f, .96f, 1.0f);
    Color lightYellowBackgroundColor = new Color(.99f, 0.98f, 0.97f, 1.0f);
    Color lightRedBackgroundColor = new Color(.99f, 0.94f, 0.94f, 1.0f);
    Color midGrayInterfaceColor = new Color(.88f, 0.88f, 0.88f, 1.0f);
    int randomizeUserSeq = 0, optimaToUserSeq = 0;
    string randomizeUserSeqString = "Randomize All Steps", optimalToUserSeqString = "Replace All Steps With Optimal";
    string randomToUserSeqString = "Replace From Random Mode";
    string newNameForPrefab = "newNameForPrefab";
    public SeqVariant currSeqAStepVariant, currSeqBStepVariant, currSeqPostStepVariant;
    //public SeqVariant copySeqAStepVariant = null, copySeqBStepVariant = null, copySeqPostStepVariant = null;
    public SeqVariant copySeqStepVariant = null;
    int sceneViewMenuIndex = 0;
    public List<string> scriptablePresetNames = new List<string>();
    public List<ScriptablePresetAFWB> scriptablePresetList = new List<ScriptablePresetAFWB>();
    public string[] randomScopeStrings = { "Main Only", "Variations Only", "Main & Variations" };
    string[] variationDisplayAmountStrings = { "Show All Variations", "Show Active Only", "Show None" };
    int variationDisplayChoice = 0;
    public ScriptablePresetAFWB currScrPreset = null;
    public string[] quantizeRotStrings = { "15", "30", "45", "60", "90", "120", "180" };
    int launchPresetIndex = 22;
    public string presetSaveRename = "";
    public bool foundEnabledRailA = true, foundEnabledRailB = true, foundEnabledPost = true;

    RandomLookupAFWB randTableRailA = null, randTableRailB = null, randTablePost = null;
    int railARandIndex = 0, railBRandIndex = 0, postRandIndex = 0, randTableSize=0;
    RandomRecords railARandRecords = new RandomRecords();
    public bool showSinglesA = true, showSinglesB = true, railASinglesEnabled = true, railBSinglesEnabled = true;

    private Transform post0 = null;
    string[] slopeModeNames = { "Normal Slope", "Stepped", "Sheared" };
    bool showTriangleCounts = false;
    
    public EditorHelperAFWB helper;
    public ResourceUtilities res;
    public VariationsHelper varHelper;
    public SequenceEditor postSeqEd, railSeqEd, subpostSeqEd;
    public RandomizationEditor randEd;
    public SinglesEditor singlesEd;
    public int frameCount = 0;
    public bool showVarHelp = false, showSeqHelp = false;

    private bool openFinishAndDuplicateControls = false, openFinishControls = false;
    //---------------------------------------
    void Awake()
    {//Debug.Log("Editor Awake()\n");

        if (userUnloadedAssets == true || fencePrefabsFolderFound == false) // AFWB will not function until the user reloads it.
            return;

        af = (AutoFenceCreator)target;
        LinkHelperClasses();

        CheckDirectoryLocations(true);

        LoadedCheck();
        
        
        if (af.needsReloading == true)
        {
            LoadPrefabs(af.allowContentFreeUse, true);
        }
        af.CheckFolders();
        SetupStyles();
        helper.SetupRandomTables();

        af.AwakeAutoFence();
        if (af.initialReset == false && fencePrefabsFolderFound == true)
            af.ResetAutoFence();
    }
    //---------------------------------------
    public void CheckDirectoryLocations(bool rebuildIfChanged)
    {
        bool changed = false;
        
        // Prefabs
        string[] prefabsLocation = AssetDatabase.FindAssets("FencePrefabs_AFWB");
        if (prefabsLocation.Length == 0 || prefabsLocation[0] == "")
        {
            Debug.LogWarning("Couldn't find prefabsLocation in CheckDirectoryLocations()  \n");
        }
        else
        {
            af.currPrefabsDirLocation = AssetDatabase.GUIDToAssetPath(prefabsLocation[0]);
        }
        
        // Presets
        string[] presetsLocation = AssetDatabase.FindAssets("Presets_AFWB");
        if (presetsLocation.Length == 0 || presetsLocation[0] == "")
        {
            Debug.LogWarning("Couldn't find presetsLocation in CheckDirectoryLocations() \n");
        }
        else
        {
            af.currPresetsDirLocation = AssetDatabase.GUIDToAssetPath(presetsLocation[0]);
        }


        string oldCurrAFBLocation = af.currAutoFenceBuilderDirLocation;
        // Auto Fence Builder
        string[] afbLocation = AssetDatabase.FindAssets("Auto Fence Builder");
        if (afbLocation.Length == 0 || afbLocation[0] == "")
        {
            Debug.LogWarning("Couldn't find afbLocation   Length " + afbLocation[0].Length + "\n");
        }
        else
        {
            af.currAutoFenceBuilderDirLocation = AssetDatabase.GUIDToAssetPath(afbLocation[0]);
        }
        
        helper.SetupRandomTables();

        if (rebuildIfChanged && af.currAutoFenceBuilderDirLocation != oldCurrAFBLocation)
        {
            af.ForceRebuildFromClickPoints();
            //Debug.Log("Location changed in CheckDirectoryLocations().   Rebuilding");
        }
    }
    //---------------------------------------
    protected void LinkHelperClasses()
    {
        helper = new EditorHelperAFWB(af, this);
        res = new ResourceUtilities(af, this);
        varHelper = new VariationsHelper(af, this);
        postSeqEd = new SequenceEditor(af, this, AutoFenceCreator.LayerSet.postLayerSet);
        railSeqEd = new SequenceEditor(af, this, AutoFenceCreator.LayerSet.railALayerSet);
        subpostSeqEd = new SequenceEditor(af, this, AutoFenceCreator.LayerSet.subpostLayerSet);
        //railBSeqEd = new SequenceEditor(af, this, AutoFenceCreator.LayerSet.railBLayerSet);
        //railEd = new RailEditor(af, this);
        randEd = new RandomizationEditor(af, this);
        singlesEd = new SinglesEditor(af, this, AutoFenceCreator.LayerSet.railALayerSet);
    }
    //---------------------------------------
    // Check if the prefabs need reloading, if not backup their original meshes anyway. can't load editor resources from  main script so check the status here.
    protected void LoadedCheck()
    {
        AutoFenceConfigurationManager configFile = AutoFenceConfigurationManager.ReadPermissionFile(af);
        if (configFile != null && af.usingMinimalVersion == false)
        {
            af.allowContentFreeUse = configFile.allowContentFreeTool;
        }
        
        bool needLoadPrefabs = false;
        if (af.postPrefabs.Count == 0 || af.railPrefabs.Count == 0)
        {
            needLoadPrefabs = true;
        }
        if (af.origRailPrefabMeshes == null || af.origRailPrefabMeshes.Count == 0)
        {
            if (needLoadPrefabs == false)
                af.BackupPrefabMeshes(af.railPrefabs, af.origRailPrefabMeshes);
        }


        if (af.allowContentFreeUse == true)
        {
            if (needLoadPrefabs == true)
                LoadPrefabs(true, true);
            if (scriptablePresetList == null || scriptablePresetList.Count < 1)
                helper.LoadAllScriptablePresets(true);

            launchPresetIndex = 0;
            af.currentScrPresetIndex = 0;
            af.currentPostType = 0;
            af.currentRailAType = 0;
            af.currentRailBType = 0;
            af.currentSubpostType = 0;
            af.currentExtraType = 0;

            af.allowContentFreeUse = false; //the new directories have been created, so we can access them directly now 
            af.usingMinimalVersion = true;
        }
        else 
        {
            if (needLoadPrefabs == true)
                LoadPrefabs(false, true);
            
            if (scriptablePresetList == null || scriptablePresetList.Count < 1)
                helper.LoadAllScriptablePresets(false);
            
            if (af.currentScrPresetIndex >= scriptablePresetList.Count)
                af.currentScrPresetIndex = scriptablePresetList.Count - 1;
        
        
            af.scrPresetSaveName = scriptablePresetList[af.currentScrPresetIndex].name;
        }
    }
    //---------------------------------------
    public void LoadPrefabs(bool contentFreeUse, bool fixRailMeshes = true)
    { //Debug.Log("LoadPrefabs()\n");

        af.postPrefabs.Clear();
        af.railPrefabs.Clear();
        af.subPrefabs.Clear();
        af.subJoinerPrefabs.Clear();
        af.extraPrefabs.Clear();


        if (contentFreeUse == true)
        {
            loader = new FencePrefabLoader();
            if (fencePrefabsFolderFound != false)// we haven't already failed, or user pressed 'Retry'
            {
                fencePrefabsFolderFound = loader.LoadAllFencePrefabsMinimal(this, af.extraPrefabs, af.postPrefabs, af.subPrefabs,
                    af.railPrefabs, af.subJoinerPrefabs, ref af.clickMarkerObj);
            } 
        }
        else
        {
            loader = new FencePrefabLoader();
            if (fencePrefabsFolderFound != false)// we haven't already failed, or user pressed 'Retry'
            {
                fencePrefabsFolderFound = loader.LoadAllFencePrefabs(this, af.extraPrefabs, af.postPrefabs, af.subPrefabs,
                    af.railPrefabs, af.subJoinerPrefabs, ref af.clickMarkerObj);
            } 
        }
        

        af.needsReloading = false;
        af.prefabsLoaded = true;
        userUnloadedAssets = false;
        if (fencePrefabsFolderFound)
        {
            af.BackupPrefabMeshes(af.railPrefabs, af.origRailPrefabMeshes);
            af.BackupPrefabMeshes(af.postPrefabs, af.origPostPrefabMeshes);
            af.CreatePartStringsForMenus();
        }
    }
    //-----------------------------------------
    void UnloadUnusedAssets()
    {
        af.postPrefabs.Clear();
        af.railPrefabs.Clear();
        af.subPrefabs.Clear();
        af.subJoinerPrefabs.Clear();
        af.extraPrefabs.Clear();
        userUnloadedAssets = true;
        af.needsReloading = true;
    }
    //---------------------------------------
    /*void OnDisable()
    {
        //_editors.Remove(this);
        Debug.Log("OnDisable Editors count " + _editors.Count + "\n");
    }*/

    //---------------------------------------
    void OnEnable()
    {//Debug.Log("Editor OnEnable()\n");
        af = (AutoFenceCreator)target;
        
        /*if(_editors.Contains(this) == false)
            _editors.Add(this);

        Debug.Log("OnEnable Editors count " + _editors.Count + "\n");
        
        
        foreach (AutoFenceEditor ed in _editors)
        {
            if (ed != null)
            {
                Debug.Log("ed is not null");
            }
            else
            {
                Debug.Log("ed IS null");
            }
							
        }*/
        
        
        
        
        

        LoadedCheck(); // make sure all the assets/resources are still present and correct

        extraPositionOffset = serializedObject.FindProperty("extraPositionOffset");
        extraSize = serializedObject.FindProperty("extraSize");
        extraRotation = serializedObject.FindProperty("extraRotation");
        userExtraObject = serializedObject.FindProperty("userExtraObject");

        gs = serializedObject.FindProperty("gs");
        scaleInterpolationAlso = serializedObject.FindProperty("scaleInterpolationAlso");
        switchControlsAlso = serializedObject.FindProperty("switchControlsAlso");
        

        railASpread = serializedObject.FindProperty("railASpread");
        railBSpread = serializedObject.FindProperty("railBSpread");
        numStackedRailsA = serializedObject.FindProperty("numStackedRailsA");
        numStackedRailsB = serializedObject.FindProperty("numStackedRailsB");
        railAPositionOffset = serializedObject.FindProperty("railAPositionOffset");
        railBPositionOffset = serializedObject.FindProperty("railBPositionOffset");
        railASize = serializedObject.FindProperty("railASize");
        railBSize = serializedObject.FindProperty("railBSize");
        railARotation = serializedObject.FindProperty("railARotation");
        railBRotation = serializedObject.FindProperty("railBRotation");
        userRailObject = serializedObject.FindProperty("userRailObject");
        useMainRails = serializedObject.FindProperty("useRailsA");
        useSecondaryRails = serializedObject.FindProperty("useRailsB");

        fenceHeight = serializedObject.FindProperty("fenceHeight");
        fenceWidth = serializedObject.FindProperty("fenceWidth");
        postHeightOffset = serializedObject.FindProperty("postHeightOffset");
        postSize = serializedObject.FindProperty("postSize");
        mainPostSizeBoost = serializedObject.FindProperty("mainPostSizeBoost");
        postRotation = serializedObject.FindProperty("postRotation");
        userPostObject = serializedObject.FindProperty("userPostObject");

        roundingDistance = serializedObject.FindProperty("roundingDistance");

        subSpacing = serializedObject.FindProperty("subSpacing");
        useSubPosts = serializedObject.FindProperty("useSubposts");
        subpostPositionOffset = serializedObject.FindProperty("subpostPositionOffset");
        subpostSize = serializedObject.FindProperty("subpostSize");
        subpostRotation = serializedObject.FindProperty("subpostRotation");
        showControls = serializedObject.FindProperty("showControls");
        closeLoop = serializedObject.FindProperty("closeLoop");
        frequency = serializedObject.FindProperty("frequency");
        amplitude = serializedObject.FindProperty("amplitude");
        wavePosition = serializedObject.FindProperty("wavePosition");
        useWave = serializedObject.FindProperty("useWave");
        useSubJoiners = serializedObject.FindProperty("useSubJoiners");

        gs.floatValue = 1.0f;

        keepInterpolatedPostsGrounded = serializedObject.FindProperty("keepInterpolatedPostsGrounded");
        snapMainPosts = serializedObject.FindProperty("snapMainPosts");
        snapSize = serializedObject.FindProperty("snapSize");
        lerpPostRotationAtCorners = serializedObject.FindProperty("lerpPostRotationAtCorners");
        lerpPostRotationAtCornersInters = serializedObject.FindProperty("lerpPostRotationAtCornersInters");
        hideInterpolated = serializedObject.FindProperty("hideInterpolated");

        globalLiftLower = serializedObject.FindProperty("globalLift");

        //==== Variation Parameters ========
        allowVertical180Invert_Post = serializedObject.FindProperty("allowVertical180Invert_Post");
        allowMirroring_X_Post = serializedObject.FindProperty("allowMirroring_X_Post");
        allowMirroring_Z_Post = serializedObject.FindProperty("allowMirroring_Z_Post");
        jitterPostVerts = serializedObject.FindProperty("jitterPostVerts");
        mirrorXPostProbability = serializedObject.FindProperty("mirrorXPostProbability");
        mirrorZPostProbability = serializedObject.FindProperty("mirrorZPostProbability");
        verticalInvertPostProbability = serializedObject.FindProperty("verticalInvertPostProbability");
        postSpacingVariation = serializedObject.FindProperty("postSpacingVariation");
        postVariation2 = serializedObject.FindProperty("postVariation2");
        postVariation3 = serializedObject.FindProperty("postVariation3");

        allowMirroring_X_Rail = serializedObject.FindProperty("allowMirroring_X_Rail");
        allowMirroring_Z_Rail = serializedObject.FindProperty("allowMirroring_Z_Rail");
        jitterRailVerts = serializedObject.FindProperty("jitterRailVerts");
        mirrorXRailProbability = serializedObject.FindProperty("mirrorXRailProbability");
        mirrorZRailProbability = serializedObject.FindProperty("mirrorZRailProbability");
        verticalInvertRailProbability = serializedObject.FindProperty("verticalInvertRailProbability");

        minRailHeightLimit = serializedObject.FindProperty("minRailHeightSlider");
        maxRailHeightLimit = serializedObject.FindProperty("maxRailHeightSlider");
        minRailAHeightVar = serializedObject.FindProperty("minRailAHeightVar");
        maxRailAHeightVar = serializedObject.FindProperty("maxRailAHeightVar");
        minRailBHeightVar = serializedObject.FindProperty("minRailBHeightVar");
        maxRailBHeightVar = serializedObject.FindProperty("maxRailBHeightVar");

        railVariation1 = serializedObject.FindProperty("railVariation1");
        railVariation2 = serializedObject.FindProperty("railVariation2");
        railVariation3 = serializedObject.FindProperty("railVariation3");
        railADisplayVariationGOs = serializedObject.FindProperty("railADisplayVariationGOs");
        useRailASeq = serializedObject.FindProperty("useRailASeq");

        railAProbArray = serializedObject.FindProperty("varRailAProbs");
        railBProbArray = serializedObject.FindProperty("varRailBProbs");
        varRailAPositionOffset = serializedObject.FindProperty("varRailAPositionOffset");
        varRailASize = serializedObject.FindProperty("varRailASize");
        varRailARotation = serializedObject.FindProperty("varRailARotation");
        varRailBPositionOffset = serializedObject.FindProperty("varRailBPositionOffset");
        varRailBSize = serializedObject.FindProperty("varRailBSize");
        varRailBRotation = serializedObject.FindProperty("varRailBRotation");

        varRailABackToFront = serializedObject.FindProperty("varRailABackToFront");
        varRailAMirrorZ = serializedObject.FindProperty("varRailAMirrorZ");
        varRailAInvert = serializedObject.FindProperty("varRailAInvert");
        varRailBBackToFront = serializedObject.FindProperty("varRailBBackToFront");
        varRailBMirrorZ = serializedObject.FindProperty("varRailBMirrorZ");
        varRailBInvert = serializedObject.FindProperty("varRailBInvert");

        varRailABackToFrontBools = serializedObject.FindProperty("varRailABackToFrontBools");
        varRailAMirrorZBools = serializedObject.FindProperty("varRailAMirrorZBools");
        varRailAInvertBools = serializedObject.FindProperty("varRailAInvertBools");
        varRailBBackToFrontBools = serializedObject.FindProperty("varRailBBackToFrontBools");
        varRailBMirrorZBools = serializedObject.FindProperty("varRailBMirrorZBools");
        varRailBInvertBools = serializedObject.FindProperty("varRailBInvertBools");

        railASinglesList = serializedObject.FindProperty("railASingleVariants");
        railBSinglesList = serializedObject.FindProperty("railBSingleVariants");
        
        postQuantizeRotAmount = serializedObject.FindProperty("postQuantizeRotAmount"); 
        subPostQuantizeRotAmount = serializedObject.FindProperty("subPostQuantizeRotAmount");
        quantizeRotIndexPost = serializedObject.FindProperty("quantizeRotIndexPost"); 
        quantizeRotIndexSubpost = serializedObject.FindProperty("quantizeRotIndexSubpost"); 
        
        if (varHelper == null)
            LinkHelperClasses();
        varHelper.CheckVariationGOs();
        helper.SetupRandomTables();
        InitTextures();

        int initialPresetNum = 1;
        if (af.currentScrPresetIndex == 0 && scriptablePresetList.Count > initialPresetNum)
        {
            /*af.currentScrPresetIndex = initialPresetNum;
            h.SetupPreset(af.currentScrPresetIndex);
            Debug.Log("Setting Preset to " + af.currentScrPresetIndex +"\n");*/
        }
        if (af.launchPresetAssigned == false)
        {
            int presetIndex = launchPresetIndex;
            if (af.allowContentFreeUse == true)
                presetIndex = 0;
            helper.SetupPreset(presetIndex);
            af.launchPresetAssigned = true;
        }
        if(af.clickPoints.Count > 0)
            af.Ground(af.clickPoints); 
    }
    //---------------------------------------
    public void ReloadPrefabsAndPresets(bool rebuild = true)
    {
        if (af.allowContentFreeUse == true)
        {
            LoadPrefabs(true, true);
            helper.LoadAllScriptablePresets(true);

            af.currentScrPresetIndex = 0;
            af.currentPostType = 0;
            af.currentRailAType = 0;
            af.currentRailBType = 0;
            af.currentSubpostType = 0;
            af.currentExtraType = 0;
        }
        else 
        {
            LoadPrefabs(false, true);
            helper.LoadAllScriptablePresets(af.allowContentFreeUse);
        }

        if (rebuild)
        {
            af.DestroyPools();
            af.ForceRebuildFromClickPoints();
        }
    }
    //--------------------------------
     void PrintSequencerInfo(List<SeqVariant> seqList, int start = 0, int end = AutoFenceCreator.kMaxNumSeqSteps)
    {
        for (int i = start; i < end; i++)
        {
            SeqVariant seqStep = seqList[i];
            //Debug.Log(i + " size = " + seqStep.size + "    offset = " + seqStep.pos +  "\n");
            //Debug.Log(i + "  seqList offset = " + seqStep.pos + "        seqRailAOffset" +  af.seqRailAOffset[i] + "\n");
           // Debug.Log(i + "   backToFront: " + seqStep.backToFront + "   mirrorZ: " + seqStep.mirrorZ + "   invert: " + seqStep.invert);
        }
    }
    //--------------------------------
    private void DisplaySaveField()
    {
        EditorGUILayout.LabelField("   ______________________________________________________________________________________", cyanBoldStyle);
            EditorGUILayout.LabelField("To make a new category add  'name/'  to the beginning of the preset name"
            + "                                   (shift+ctrl to overwrite)", infoStyleSmall);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Set Preset Name: ", GUILayout.Width(92));
        af.scrPresetSaveName = EditorGUILayout.TextField(af.scrPresetSaveName, GUILayout.Width(230));
        EditorGUILayout.LabelField(new GUIContent("   Category:", "To make a new category add 'xxx/' to the beginning of the preset name"), GUILayout.Width(65));
        regularPopupStyle.fontSize = 10;
        EditorGUI.BeginChangeCheck();
        af.categoryIndex = EditorGUILayout.Popup(af.categoryIndex, af.categoryNames.ToArray(), regularPopupStyle, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.LabelField("", GUILayout.Width(10));
        if (GUILayout.Button(" Save Preset", GUILayout.Width(100)))
        {
            helper.SavePreset(false);
        }
        GUILayout.EndHorizontal();
    }
    //----------
    public int FindPresetByName(string name)
    {
        for (int i = 0; i < scriptablePresetList.Count; i++)
        {
            if (scriptablePresetList[i].name == name)
                return i;
        }
        return -1;
    }
    //----------
    public void PrintVariantsList(List<FenceVariant> variantList)
    {
        Debug.Log("___________\n");
        for (int i = 0; i < variantList.Count; i++)
        {
            Debug.Log(variantList[i].go.name + "\n");
        }
    }
    //===============================================================================================================
    //
    //                                      OnInspectorGUI()
    //
    //===============================================================================================================
    
    public override void OnInspectorGUI()
    {
        // Completely block use, if user has chosen to unload assets to optimize build size, or FencePrefabs folder is missing
        if (OnInspectorAssetsCheck() == false)
            return;

        //-- Useful for testing things that only need checking periodically
        frameCount++;
        if (frameCount > 50)
        {
            frameCount = 0;
            CheckDirectoryLocations(true);
        }
        //Debug.Log("frameCount  " + frameCount);
        
        Timer t = new Timer("OnInspectorGUI");
        defaultButtonStyle = new GUIStyle(GUI.skin.button);
        Color defaultBackgroundColor = GUI.backgroundColor;
        GUIStyle biggerPopup = new GUIStyle(EditorStyles.popup);
        regularPopupStyle = new GUIStyle(EditorStyles.popup);
        mediumPopup = new GUIStyle(EditorStyles.popup);
        serializedObject.Update(); // updates serialized editor from the main script

        if (varHelper == null)
            LinkHelperClasses();


        if (userRailObject.objectReferenceValue == null)
        { //looks pointless, but removes the 'missing' label
            userRailObject.objectReferenceValue = null;
        }
            
        if (Event.current.keyCode == KeyCode.Escape)// cancels a ClearAll
            af.clearAllFencesWarning = 0;
        /*if(af.posts[0] == null)
            Debug.Log("General Start: posts are null \n");*/

        if (af.posts.Count > 0 && af.posts[0] == null)
        {
            if (af.postsFolder != null)
                    DestroyImmediate(af.postsFolder);
                af.postsFolder = new GameObject("Posts");
                af.postsFolder.transform.parent = af.fencesFolder.transform;
                Debug.Log("Post:  Killed Orphans \n");
        }
        if ((af.railsA.Count > 0 && af.railsA[0] == null) || (af.railsB.Count > 0 && af.railsA[0] == null))
        {
                if (af.railsFolder != null)
                    DestroyImmediate(af.railsFolder);
                af.railsFolder = new GameObject("Rails");
                af.railsFolder.transform.parent = af.fencesFolder.transform;
                Debug.Log("Rails:  Killed Orphans \n");
        }

        //========================
        //  Tidy up after Undo
        //========================
        if (Event.current.commandName == "UndoRedoPerformed")
        {
            //if(af.posts[0] == null)
                //Debug.Log("Undo: posts are null \n");
            af.ResetAllPools();
            af.ForceRebuildFromClickPoints();
            //Debug.Log("AFWB Undo Performed \n");
        }
        //===========================
        //   Finish And Start New
        //===========================
        GUILayout.BeginHorizontal("box");
        if ( (openFinishControls == true || GUILayout.Button("Finish & Start New"/*, GUILayout.Width(110)*/))  && af.clickPoints.Count > 0)
        {
            openFinishControls = true;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("             Finish & Start New", GUILayout.Width(235));
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("If you want the Finished Fence folder to be", GUILayout.Width(235));
            EditorGUILayout.LabelField("parented to an object in the Hierarchy,", GUILayout.Width(235));
            EditorGUILayout.LabelField("drag the parent object here, else it will be", GUILayout.Width(235));
            EditorGUILayout.LabelField("placed in the top level of the Hierarchy", GUILayout.Width(235));
            
            af.finishedFoldersParent = EditorGUILayout.ObjectField(af.finishedFoldersParent, typeof(Transform), true, GUILayout.Width(200)) as Transform;
            EditorGUILayout.Separator(); EditorGUILayout.Separator();EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Fence Name:", GUILayout.Width(75));
            string fenceName = "[Finished Fence] " + af.scrPresetSaveName;
            GUI.SetNextControlName("NameTextField");
            fenceName = EditorGUILayout.TextField(fenceName,  GUILayout.Width(240));
            EditorGUI.FocusTextInControl("NameTextField");
            EditorGUILayout.Separator();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(110))) {
                
                if (af.allPostsPositions.Count() > 0)
                {//Reposition handle at base of first post
                    Vector3 currPos = af.fencesFolder.transform.position;
                    Vector3 delta = af.allPostsPositions[0] - currPos;
                    af.fencesFolder.transform.position = af.allPostsPositions[0];
                    af.postsFolder.transform.position = af.allPostsPositions[0] - delta;
                    af.railsFolder.transform.position = af.allPostsPositions[0] - delta;
                    af.subpostsFolder.transform.position = af.allPostsPositions[0] - delta;
                }
                SaveRailMeshes.SaveProcRailMeshesAsAssets(af);
                
                GameObject finishedFolder = af.FinishAndStartNew(af.finishedFoldersParent, fenceName);
                openFinishControls = false;

                FinishedFenceUtilities finishedUtils = finishedFolder.AddComponent<FinishedFenceUtilities>();
                string dateStr = af.GetPartialTimeString(true);
                finishedUtils.presetID = dateStr;
                ScriptablePresetAFWB preset = EditorHelperAFWB.SaveFinishedPreset(dateStr + "_" + finishedFolder.name, af);
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(110)))
            {
                openFinishControls = false;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            GUILayout.EndVertical();
            EditorGUILayout.Separator();
        }
        //===========================
        //   Finish And Duplicate
        //===========================
        if ( (openFinishAndDuplicateControls == true || GUILayout.Button("Finish & Duplicate"/*, GUILayout.Width(110)*/))  && af.clickPoints.Count > 0)
        {
            openFinishAndDuplicateControls = true;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("             Finish & Duplicate", GUILayout.Width(235));
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("If you want the Finished Fence folder to be", GUILayout.Width(235));
            EditorGUILayout.LabelField("parented to an object in the Hierarchy,", GUILayout.Width(235));
            EditorGUILayout.LabelField("drag the parent object here, else it will be", GUILayout.Width(235));
            EditorGUILayout.LabelField("placed in the top level of the Hierarchy", GUILayout.Width(235));
            
            af.finishedFoldersParent = EditorGUILayout.ObjectField(af.finishedFoldersParent, typeof(Transform), true, GUILayout.Width(200)) as Transform;
            EditorGUILayout.Separator(); EditorGUILayout.Separator();EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Fence Name:", GUILayout.Width(75));
            string fenceName = "[Finished Fence] " + af.scrPresetSaveName;
            GUI.SetNextControlName("NameTextField");
            fenceName = EditorGUILayout.TextField(fenceName,  GUILayout.Width(240));
            EditorGUI.FocusTextInControl("NameTextField");
            EditorGUILayout.Separator();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(110))) {
                
                if (af.allPostsPositions.Count() > 0)
                {//Reposition handle at base of first post
                    Vector3 currPos = af.fencesFolder.transform.position;
                    Vector3 delta = af.allPostsPositions[0] - currPos;
                    af.fencesFolder.transform.position = af.allPostsPositions[0];
                    af.postsFolder.transform.position = af.allPostsPositions[0] - delta;
                    af.railsFolder.transform.position = af.allPostsPositions[0] - delta;
                    af.subpostsFolder.transform.position = af.allPostsPositions[0] - delta;
                }
                SaveRailMeshes.SaveProcRailMeshesAsAssets(af);
                
                GameObject finishedFolder = af.FinishAndDuplicate(af.finishedFoldersParent, fenceName);
                openFinishAndDuplicateControls = false;

                FinishedFenceUtilities finishedUtils = finishedFolder.AddComponent<FinishedFenceUtilities>();
                string dateStr = af.GetPartialTimeString(true);
                finishedUtils.presetID = dateStr;
                ScriptablePresetAFWB preset = EditorHelperAFWB.SaveFinishedPreset(dateStr + "_" + finishedFolder.name, af);
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(110)))
            {
                openFinishAndDuplicateControls = false;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            GUILayout.EndVertical();
            EditorGUILayout.Separator();
        }
        //============================
        //    Rebuild HardReset
        //============================
        if (GUILayout.Button(new GUIContent("Rebuild", "Rebuild if necessary") ))
        {
            DestroyImmediate(af.fencesFolder);
            af.SetupFolders();
            ReloadPrefabsAndPresets();
            af.ResetAllPools();
            af.ForceRebuildFromClickPoints();
            helper.SetupRandomTables();
        }
        /*//========================
        //      Settings
        //========================
        if (GUILayout.Button("Settings..."))
        {
            SettingsWindow settingsWindow = ScriptableObject.CreateInstance(typeof(SettingsWindow)) as SettingsWindow;
            settingsWindow.Init(af);
            settingsWindow.minSize = new Vector2(520, 700);
            settingsWindow.ShowUtility();
        }*/
        //========================
        //   Save Meshes
        //========================
        if (GUILayout.Button(new GUIContent( "Save Meshes", "The only occasion this is needed is if you're working with a 3rd-party asset that needs" +
             " constant access to saved mesh assets, otherwise you never need to use it. (As the rails in AFWB are created procedurally in realtime," +
              " they normally only become saved mesh assets after performing a 'Finish'.)")) && af.clickPoints.Count > 0)
        {
            //Timer timer = new Timer("CreateAssets timer");
            bool success = SaveRailMeshes.SaveProcRailMeshesAsAssets(af);
            if (success == false)
            {
                Debug.LogWarning(" SaveProcRailMeshesAsAssets() Failed \n");
            }
            //timer.End();
        }
        //========================
        //      Clear All
        //========================
        if (GUILayout.Button("Clear All", GUILayout.Width(65)) && af.clickPoints.Count > 0)
        {
            if (af.clearAllFencesWarning == 1)
            {
                af.ClearAllFences();
                af.clearAllFencesWarning = 0;
            }
            else
                af.clearAllFencesWarning = 1;
        }

        if (af.clearAllFencesWarning == 1)
        {
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("   ** This will clear all the fence parts currently being built. (Design parameters are preserved)", warningStyle);
            EditorGUILayout.LabelField("      Press [Clear All] again to continue or Escape Key to cancel **", warningStyle);
            af.clearAllFencesWarning = 1;
        }
        else
            GUILayout.EndHorizontal();

        //newNameForPrefab = EditorGUILayout.TextField(newNameForPrefab);

        EditorGUILayout.Separator();

        //============================
        //    Test
        //============================
        /*if (GUILayout.Button(new GUIContent("Test", "Put tests here"), GUILayout.Width(150) ))
        {
        }*/

        //=================================
        //      Show Controls
        //=================================
        GUILayout.BeginVertical("box");
        
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        
        
        EditorGUILayout.PropertyField(showControls, new GUIContent(""), GUILayout.Width(12));
        EditorGUILayout.LabelField("Show Controls", smallBoldBlack, GUILayout.Width(78));
        EditorGUILayout.LabelField("ADD Post: Shift-Click     INSERT: Ctrl-Shift-Click     GAP: Shift-Right-Click     DELETE:Ctrl-Left-Click node", infoStyleSmall);
        
        //showControls.boolValue = af.showControls = EditorGUILayout.BeginToggleGroup("Show  Controls", showControls.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            af.SetMarkersActiveStatus(showControls.boolValue);
        }
        //EditorGUILayout.EndToggleGroup();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        //==============================================================================================
        //
        //                           Choose Scriptable Main Fence/Wall Preset
        //
        //==============================================================================================
        if (scriptablePresetNames != null && scriptablePresetNames.Count > 0 && scriptablePresetList != null && scriptablePresetList.Count > 0)
        {
            GUI.backgroundColor = lightBlueBackgroundColor;
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("box");
            EditorGUILayout.Separator();
            //========================
            //      Choose Preset
            //========================
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            //GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Choose Preset:", cyanBoldStyle, GUILayout.Width(100));
            if (GUILayout.Button("<",  EditorStyles.miniButton, GUILayout.Width(17)) && af.currentScrPresetIndex > 0)
            {
                af.currentScrPresetIndex -= 1;
            }
            if (GUILayout.Button(">", EditorStyles.miniButton, GUILayout.Width(17)) && af.currentScrPresetIndex < scriptablePresetList.Count - 1)
            {
                af.currentScrPresetIndex += 1;
            }
            biggerPopup.fontSize = 12;
            biggerPopup.normal.textColor = Color.blue;
            af.currentScrPresetIndex = EditorGUILayout.Popup(af.currentScrPresetIndex, scriptablePresetNames.ToArray(), biggerPopup, GUILayout.Width(300));
            //GUILayout.EndHorizontal();

            if (af.currentScrPresetIndex < scriptablePresetNames.Count)
                scriptablePresetName = scriptablePresetNames[af.currentScrPresetIndex];
            EditorGUILayout.LabelField("", cyanBoldStyle, GUILayout.Width(10));
            if (GUILayout.Button("Fave", GUILayout.Width(40)))
            {
                if (scriptablePresetName.Contains("/"))
                    scriptablePresetName = scriptablePresetName.Replace("/", "-");
                af.scrPresetSaveName = "Favorite/" + af.scrPresetSaveName;
                //DisplaySaveField();
                helper.SavePreset(true);
            }
            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                DeletePresetWindow deleteWindow = ScriptableObject.CreateInstance(typeof(DeletePresetWindow)) as DeletePresetWindow;
                deleteWindow.Init(this, currScrPreset);
                deleteWindow.minSize = new Vector2(430, 190); deleteWindow.maxSize = new Vector2(430, 190);
                deleteWindow.ShowUtility();
                //presetName = af.presets[af.currentPresetIndex].name;
               
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                helper.SetupPreset(af.currentScrPresetIndex);
                //PrintSequencerInfo(af.optimalSequenceRailA, 0, af.optimalSequenceRailA.Count);
            }
            //========================
            //      Save Preset
            //========================

            DisplaySaveField();
            EditorGUILayout.Separator();
            GUILayout.EndVertical();
        }
        GUI.backgroundColor = defaultBackgroundColor;
        
        //==========================================================
        //
        //          Interpolate Post Spacing
        //
        //==========================================================
        GUILayout.BeginVertical("box");
        EditorGUILayout.Separator();
        EditorGUI.BeginChangeCheck();
        
        GUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(new GUIContent("Inter-post distance:", "The distance between each interpolated post. " +
            "This is a target size. E.g. if your fence is 10m long and you request a distance of 3m, " +
            "the nearest value to give a whole number of sections would be 3.333 (x 3 sections = 10m).   The default target value is = 3m"), 
            cyanBoldStyle, GUILayout.Width(118));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("interPostDist"), new GUIContent(""), GUILayout.Width(125));
        
        string actualDistStr = "(" + af.actualInterPostDistance.ToString("F1") + ")";
        EditorGUILayout.LabelField(new GUIContent(actualDistStr, actualDistStr + " is the closest useable value to " + af.interPostDist 
                                     + " needed to create a whole number of sections"), lightGrayStyle, GUILayout.Width(35));
        
        //======   Set Default   ======
        
        if (GUILayout.Button(new GUIContent("Defaults", "Reset Interpost distance to 3.0"), EditorStyles.miniButton, GUILayout.Width(60)))
        {
            af.baseInterPostDistance = 3.0f;
            af.interPostDist = af.baseInterPostDistance * af.fenceHeight;
            af.interpolate = true;
            af.keepInterpolatedPostsGrounded = true;
            af.postSpacingVariation = postSpacingVariation.floatValue = 0;
            af.ForceRebuildFromClickPoints();
        }
        
        
        //====== Randomize Spacing =========
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(new GUIContent("  Random Spacing: ",
                "Randomizes the length of each inter-post section, does not affect your clicked points. Default 0"),
            cyanBoldStyle, GUILayout.Width(115));
        EditorGUILayout.PropertyField(postSpacingVariation, new GUIContent(""), GUILayout.Width(120));
        if (GUILayout.Button(new GUIContent("R", "Reset Random Spacing Variation to 0"), EditorStyles.miniButton, GUILayout.Width(20)))
        {
            af.postSpacingVariation = postSpacingVariation.floatValue = 0;
            af.ForceRebuildFromClickPoints();
        }
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }

        EditorGUILayout.EndHorizontal();
        
        
        EditorGUILayout.BeginHorizontal();
        //======   Interpolate Switch   ======
        EditorGUILayout.LabelField(new GUIContent("In-Between Posts", "Use interpolated posts between the main click points. Default is On"), GUILayout.Width(120));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interpolate"), new GUIContent(""), GUILayout.Width(25));
        
        //======   Keep Grounded Switch   ======
        EditorGUILayout.LabelField(new GUIContent(" Keep Grounded", "Ensure in-between posts are forced to the ground. Default On"), GUILayout.Width(99));
        EditorGUILayout.PropertyField(keepInterpolatedPostsGrounded, new GUIContent(""), GUILayout.Width(19));
        
        //========= Smooth ===========
        EditorGUILayout.LabelField(new GUIContent("       Smooth", "Smooths/Rounds the path of the fence"), GUILayout.Width(85));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("smooth"), new GUIContent(""), GUILayout.Width(19));
        EditorGUILayout.LabelField(new GUIContent("(Adjust Smoothing settings in 'Globals')"), darkGrayStyle, GUILayout.Width(200));
        
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
        af.baseInterPostDistance = af.interPostDist / af.fenceHeight;
        EditorGUILayout.Separator();
        GUILayout.EndVertical();

        //====================================================================================================
        //
        //                                          Component Part Switches
        //
        //====================================================================================================
        EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("                                                 Component Part Active Switches", cyanBoldStyle);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Switch Controls Also", infoStyle, GUILayout.Width(130));
        EditorGUILayout.PropertyField(switchControlsAlso, new GUIContent("", "Automatically show controls when enabling a component"), GUILayout.Width(20));
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        int buttonWidth = 120;
        EditorGUILayout.LabelField("",GUILayout.Width(2));
        
        if (af.usePosts)
            GUI.backgroundColor = switchGreen;
        else
            GUI.backgroundColor = switchRed;
        if (GUILayout.Button(new GUIContent("Posts", ""), EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
        {
            af.usePosts = !af.usePosts;
            af.ForceRebuildFromClickPoints();
            if (af.switchControlsAlso && af.usePosts)
                af.componentToolbar = AutoFenceCreator.ComponentToolbar.posts;
        }
        
        if (af.useRailsA)
            GUI.backgroundColor = switchGreen;
        else
            GUI.backgroundColor = switchRed;
        if (GUILayout.Button(new GUIContent("Rail A", ""), EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
        {
            af.useRailsA = !af.useRailsA;
            af.ForceRebuildFromClickPoints();
            if (af.switchControlsAlso && af.useRailsA)
                af.componentToolbar = AutoFenceCreator.ComponentToolbar.railsA;
        }
        if (af.useRailsB)
            GUI.backgroundColor = switchGreen;
        else
            GUI.backgroundColor = switchRed;
        if (GUILayout.Button(new GUIContent("Rail B", ""), EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
        {
            af.useRailsB = !af.useRailsB;
            af.ForceRebuildFromClickPoints();
            if (af.switchControlsAlso && af.useRailsB)
                af.componentToolbar = AutoFenceCreator.ComponentToolbar.railsB;
        }
        if (af.useSubposts)
            GUI.backgroundColor = switchGreen;
        else
            GUI.backgroundColor = switchRed;
        if (GUILayout.Button(new GUIContent("Subs", ""), EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
        {
            af.useSubposts = !af.useSubposts;
            af.ForceRebuildFromClickPoints();
            if (af.switchControlsAlso)
                af.componentToolbar = AutoFenceCreator.ComponentToolbar.subposts;
        }
        if (af.useExtraGameObject)
            GUI.backgroundColor = switchGreen;
        else
            GUI.backgroundColor = switchRed;
        if (GUILayout.Button(new GUIContent("Extra", ""), EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
        {
            af.useExtraGameObject = !af.useExtraGameObject;
            af.ForceRebuildFromClickPoints();
            if (af.switchControlsAlso && af.useExtraGameObject)
                af.componentToolbar = AutoFenceCreator.ComponentToolbar.extras;
        }
        GUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Separator();
        
        
        //===================================================================================================
        //                                         Component Toolbar
        //===================================================================================================
        
        af.componentToolbar = (AutoFenceCreator.ComponentToolbar)GUILayout.Toolbar((int)af.componentToolbar, componentToobarContent);

        //===================================================================================================
        //
        //                                          Post Options
        //
        //===================================================================================================
        if (af.componentToolbar == AutoFenceCreator.ComponentToolbar.posts)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(" Posts"), cyanBoldStyleBigger, GUILayout.Width(50));
            bool mainPostChanged = false;

            //========= All Posts Disabled Warning =======
            foundEnabledPost = false;
            for (int i = 0; i < af.postSeqInfo.numSteps; i++)
            {
                if (af.userSequencePost[i].stepEnabled == true)
                {
                    foundEnabledPost = true;
                    break;
                }
            }
            if (foundEnabledPost == false && af.usePostVariations == true)
                EditorGUILayout.LabelField(" [All steps are disabled!  Re-enable in Variations]", warningStyle, GUILayout.Width(160));
            
            //====== Reset  ======
            EditorGUILayout.Separator();
            if (GUILayout.Button(new GUIContent("Reset", "Reset all Post Scaling/Offsets/Rotations"), GUILayout.Width(120)))
            {
                af.ResetPostTransforms(true);
            }

            GUILayout.EndHorizontal();
            if (af.usePosts == false)
                EditorGUILayout.LabelField(new GUIContent("[Posts are off] "), mildWarningStyle, GUILayout.Width(250));
            else
                EditorGUILayout.Separator();
           
            //==================================================================
            //           Choose Post Prefab 
            //==================================================================
            helper.SetMainPrefab(kPostLayer);

            
            //===================================================================
            //     Posts Main Parameters - Raise, Size, Position, Rotation  
            //===================================================================
            helper.SetPostMainParameters();

            EditorGUI.BeginChangeCheck();
            //======  Hide Interpolated Posts  ======
            EditorGUILayout.PropertyField(hideInterpolated);
            //======  Rotate Corner Posts to Match Direction ======
            EditorGUILayout.PropertyField(lerpPostRotationAtCorners);
            EditorGUILayout.PropertyField(lerpPostRotationAtCornersInters);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ResetPostPool();
                af.ForceRebuildFromClickPoints(kPostLayer);
                Debug.Log("hideInterpolated    " + af.hideInterpolated + "\n");
                Debug.Log("Lerp    " + af.lerpPostRotationAtCorners + "\n");
            }
            
            //===================================================================
            //                          Posts Randomization  
            //===================================================================
            randEd.SetupRandomization(kPostLayer);

            //===================================================================
            //                          Posts Variation  
            //====================================================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("box"); // begin VERTICAL Post  Variations       

            //====  Show/Enable Variations  ====
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("usePostVariations"));

            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ResetPostPool();
                af.ForceRebuildFromClickPoints();
            }

            if (af.usePostVariations == true)
            {
                postSeqEd.SetupSourcePrefabs(kPostLayer);
                postSeqEd.SetupVariations();
            }
            else
                EditorGUILayout.Space();
            GUILayout.EndVertical();

            EditorGUILayout.Separator();
            GUILayout.EndVertical();
            /*if (af.posts[0] == null)
                Debug.Log("After Change Num Steps 2: posts are null \n");*/
        }//end of posts

        //===================================================================================================
        //
        //                                Subpost Options
        //
        //===================================================================================================
        EditorGUILayout.Separator();
        
        if (af.componentToolbar == AutoFenceCreator.ComponentToolbar.subposts)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Reset", "Reset all Extra Scaling/Offsets/Rotations"), GUILayout.Width(44)))
            {
                af.ResetSubpostTransforms(true);
            }
            GUILayout.EndHorizontal();

            //======  Choose Post Preset or Add Custom Prefab  ======
            helper.SetMainPrefab(kSubpostLayer);
            
            //====== SubPost Spacing Mode ======
            string[] subModeNames = { "Fixed Number Between Posts", "Depends on Section Length", "Duplicate Main Post Positions Only" };
            int[] subModeNums = { 0, 1, 2 };
            EditorGUI.BeginChangeCheck();
            af.subsFixedOrProportionalSpacing = EditorGUILayout.IntPopup("SubPosts Spacing Mode", af.subsFixedOrProportionalSpacing, subModeNames, subModeNums);
            EditorGUILayout.PropertyField(subSpacing);
            if (af.subsFixedOrProportionalSpacing == 0)
            {// Mode 0 = Fixed, so round the number.
                af.subSpacing = Mathf.Round(af.subSpacing);
                if (af.subSpacing < 1) { af.subSpacing = 1; }
            }
            //====== SubPost Spread/Bunch =======
            if (af.subsFixedOrProportionalSpacing == 0 && af.subSpacing > 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("subPostSpread"), new GUIContent("SubPost Spread/Bunch", "Push subpost either in toward the center, or out toward the posts"));
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }
            
            EditorGUILayout.Separator();
            //======  Subpost Position - Size - Rotation  ========
            helper.SetSubpostMainParameters();
            
            EditorGUI.BeginChangeCheck();
            //======  Force Ground,Bury, Add at post point  ========
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forceSubsToGroundContour"), new GUIContent("Stretch To Ground Contour.", " Useful to have off if section spans 2 high points."));
            if (af.forceSubsToGroundContour == true)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("subsGroundBurial"), new GUIContent("Bury in Ground, useful to appear sunk in,"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("addSubpostAtPostPointAlso"), 
                        new GUIContent("Add Subpost at Post position also", 
                            "Duplicates subpost at incoming post position. Useful when setting up a pattern of subposts and you don't need posts. Default = off"));
            

            //======= Sub Wave ==========
            EditorGUILayout.PropertyField(useWave);
            af.useWave = useWave.boolValue;
            if (af.useWave)
            {
                EditorGUILayout.PropertyField(frequency);
                EditorGUILayout.PropertyField(amplitude);
                EditorGUILayout.PropertyField(wavePosition);
                EditorGUILayout.PropertyField(useSubJoiners);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                subpostSize.vector3Value = EnforceVectorMinimums(subpostSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
                af.ForceRebuildFromClickPoints();
            }
            //==============================================================================
            //                          Subposts Randomization  
            //==============================================================================
            randEd.SetupRandomization(kSubpostLayer);
            
            //===================================================================
            //                          Subposts Variation  
            //====================================================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("box"); // begin VERTICAL Post  Variations       

            //====  Enable Variations  ====
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useSubpostVariations"), GUILayout.Width(400));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ResetSubpostPool();
                af.ForceRebuildFromClickPoints();
            }

            if (af.useSubpostVariations == true)
            {
                subpostSeqEd.SetupSourcePrefabs(kSubpostLayer);
                subpostSeqEd.SetupVariations();
            }
            else
                EditorGUILayout.Space();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        //=======================================================
        //                                                      
        //                        Rails A                     
        //                                                             
        //=======================================================
        if (af.componentToolbar == AutoFenceCreator.ComponentToolbar.railsA)
        { 
            GUILayout.BeginVertical("box");
            EditorGUILayout.Separator();
            bool rebuildAfterRailAPrefabChange = false, rebuild = false;
            GUI.backgroundColor = defaultBackgroundColor;
            GUI.backgroundColor = switchRed;
            if (af.useRailsA == false)
            {
                EditorGUILayout.LabelField(new GUIContent("[Rails A are off]"), mildWarningStyle, GUILayout.Width(250));
            }
            GUI.backgroundColor = defaultBackgroundColor;

            GUILayout.BeginHorizontal();
            //====  Centralise Y  ====
            if (GUILayout.Button(new GUIContent("Central Y", "Centralise the A Rails"), EditorStyles.miniButton,
                GUILayout.Width(63)))
                af.CentralizeRails(kRailALayer);
            //====  Ground  ====
            if (GUILayout.Button(new GUIContent("Ground", "Place lowest A rail/wall flush with ground"),
                EditorStyles.miniButton, GUILayout.Width(54)))
                af.GroundRails(kRailALayer);
            //====  Reset  ====
            if (GUILayout.Button(new GUIContent("Reset", "Reset all A Rail Scaling/Offsets/Rotations"),
                EditorStyles.miniButton, GUILayout.Width(44)))
                af.ResetRailATransforms(true);
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            bool rebuildRailA = false;

            //=========================== 
            // Choose Rail Preset or Add Custom Prefab
            //===========================
            helper.SetMainPrefab(kRailALayer);

            //----- Rail Options -------------
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(numStackedRailsA);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.CheckResizePools();
                af.ForceRebuildFromClickPoints(kRailALayer);
            }

            EditorGUI.BeginChangeCheck();
            //==  Rail A Spread ==
            EditorGUILayout.PropertyField(railASpread,
                new GUIContent("Rail A Spread Distance",
                    "The total vertical distance from the top rail to the bottom rail"));
            //==  Rail A Keep Grounded ==
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(370));
            EditorGUILayout.LabelField(
                new GUIContent("Lock Y Grounded",
                    "Keep the base of the fence on the ground  This will overide the PositionOffset.y"),
                GUILayout.Width(100));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("railAKeepGrounded"), new GUIContent(""),
                GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }

            GUILayout.EndHorizontal();

            //==============================================================================
            //           Rails A Main Parameters - Raise, Size, Position, Rotation  
            //==============================================================================
            helper.SetRailMainParameters(kRailALayer);

            //======================================================================
            //                     Rail A Randomization 
            //======================================================================   
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            randEd.SetupRandomization(kRailALayer);

            //===============================================================
            //                    Rail A Overlap & Hide & Slope 
            //===============================================================  
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Overlap At Corners", "Close the gap between adjoining rails"),
                GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overlapAtCorners"), new GUIContent(""),
                GUILayout.Width(27));

            EditorGUILayout.LabelField(
                new GUIContent(" AutoHide Buried Rails", "Hide if rail through ground/other objects"),
                GUILayout.Width(132));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoHideBuriedRails"), new GUIContent(""),
                GUILayout.Width(27));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }

            //=============== Slope Mode ================
            AutoFenceCreator.FenceSlopeMode oldSlopeMode = af.slopeModeRailA;
            string[] slopeModeNames = {"Normal Slope", "Stepped", "Sheared"};
            EditorGUILayout.LabelField(new GUIContent("Slope Mode", "Default is shear"), GUILayout.Width(72));
            af.slopeModeRailA = (AutoFenceCreator.FenceSlopeMode) EditorGUILayout.Popup("", (int) af.slopeModeRailA,
                slopeModeNames, GUILayout.Width(100));
            if (af.slopeModeRailA != oldSlopeMode)
            {
                af.ResetRailAPool();
                af.ForceRebuildFromClickPoints();
            }
            GUILayout.EndHorizontal();
            
            /*GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("doThatThing"));
            if (af.doThatThing == true)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stretchUVs"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (af.doThatThing)
                    af.overlapAtCorners = false;
                else
                    af.overlapAtCorners = true;
                af.ForceRebuildFromClickPoints();
            }

            if (af.doThatThing == true)
            {
                EditorGUI.BeginChangeCheck();
                
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    af.ForceRebuildFromClickPoints();
                } 
            }
            GUILayout.EndHorizontal();*/

            //==================================================================
            //                   Rails A Variation  
            //==================================================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("box"); // begin VERTICAL Rails A Variations       
            GameObject currRailAPrefab = af.railPrefabs[af.currentRailAType];

            //====  Enable Variations  ====
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useRailAVariations"), GUILayout.Width(410));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (af.nonNullRailAVariants.Count == 1)
                {
                    af.railAVariants[1].go = af.railADisplayVariationGOs[1] = af.railPrefabs[af.currentRailAType];
                    af.varMenuIndexRailA[1] = ConvertRailPrefabIndexToMenuIndex(af.varPrefabIndexRailA[1]);
                }
                af.ResetRailAPool();
                af.ForceRebuildFromClickPoints();
            }
            af.autoHideRailAVar = EditorGUILayout.ToggleLeft("Hide when disabled", af.autoHideRailAVar, GUILayout.Width(145));

            if (af.useRailAVariations == true)
            {
                varHelper.ShowVariationSourcesHelp();
                if (showVarHelp == false)
                    GUILayout.EndHorizontal();
            }
            else
                GUILayout.EndHorizontal();


            if (af.useRailAVariations == false)
                EditorGUI.BeginDisabledGroup(true);
            
            if(af.useRailAVariations == true || af.autoHideRailAVar == false)
            {
                railSeqEd.SetupSourcePrefabs(kRailALayer);
                railSeqEd.SetupVariations();
            }
            EditorGUILayout.EndVertical(); // End  Show Variation
            
            singlesEd.SetupSinglesRailA();
            if (af.useRailAVariations == false)
                EditorGUI.EndDisabledGroup();

            //---- End Of Rails A -----
            GUILayout.EndVertical();
        }
        //==================================================
        //                                                //
        //                     Rails B                    //
        //                                                //
        //==================================================
        if (af.nonNullRailBVariants.Count == 0)
            af.nonNullRailBVariants.Add(new FenceVariant(af.railPrefabs[af.currentRailBType]));

        bool rebuildAfterRailBPrefabChange = false; //rebuild = false;
        if(af.componentToolbar == AutoFenceCreator.ComponentToolbar.railsB)
        {
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = defaultBackgroundColor;
            //GUI.backgroundColor = switchRed;
            if (af.useRailsB == false)
            {
                EditorGUILayout.LabelField(new GUIContent("[Rails B are off]"), mildWarningStyle, GUILayout.Width(250));
            }
            GUI.backgroundColor = defaultBackgroundColor;

            GUILayout.BeginHorizontal();
            //railsetToolbarGUIContent[0] = new GUIContent("Rails Main Layer B", railATooltipString);
            
            //====  Centralise Y  ====
            if (GUILayout.Button(new GUIContent("Central Y", "Centralise the B Rails"), EditorStyles.miniButton, GUILayout.Width(63)))
                af.CentralizeRails(kRailBLayer);
            
            //====  Ground  ====
            if (GUILayout.Button(new GUIContent("Ground", "Place lowest B rail/wall flush with ground"), EditorStyles.miniButton, GUILayout.Width(54)))
                af.GroundRails(kRailBLayer);
            
            //====  Reset  ====
            if (GUILayout.Button(new GUIContent("Reset", "Reset all B Rail Scaling/Offsets/Rotations"), EditorStyles.miniButton, GUILayout.Width(44)))
                af.ResetRailBTransforms(true);
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            bool rebuildRailB = false;

            //=========================== 
            // Choose Rail Preset or Add Custom Prefab
            //===========================
            helper.SetMainPrefab(kRailBLayer);

            //===== Stacked Rails =====
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(numStackedRailsB);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints(kRailBLayer);
            }
            EditorGUI.BeginChangeCheck();
             //==  Rail B Spread ==
            EditorGUILayout.PropertyField(railBSpread, new GUIContent("Rail B Spread Distance", "The total vertical distance from the top rail to the bottom rail"));
            
            //==  Rail B Keep Grounded ==
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(370));
            EditorGUILayout.LabelField(new GUIContent("Lock Y Grounded", "Keep the base of the fence on the ground  This will overide the PositionOffset.y"),
            GUILayout.Width(100));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("railBKeepGrounded"), new GUIContent(""), GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            } 
            GUILayout.EndHorizontal();
            
            //==============================================================================
            //           Rails B Main Parameters - Raise, Size, Position, Rotation  
            //==============================================================================
            helper.SetRailMainParameters(kRailBLayer);

            //======================================================================
            //                     Rail B Randomization 
            //======================================================================   
            EditorGUILayout.Space(); EditorGUILayout.Space();
            randEd.SetupRandomization(kRailBLayer);

            //===============================================================
            //                    Rail B Overlap & Hide & Slope 
            //===============================================================  
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Overlap At Corners", "Close the gap between adjoining rails"), GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overlapAtCorners"), new GUIContent(""), GUILayout.Width(27));

            EditorGUILayout.LabelField(new GUIContent(" AutoHide Buried Rails", "Hide if rail through ground/other objects"), GUILayout.Width(132));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoHideBuriedRails"), new GUIContent(""), GUILayout.Width(27));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }    
            //=============== Slope Mode ================
            AutoFenceCreator.FenceSlopeMode oldSlopeModeB = af.slopeModeRailB;
            EditorGUILayout.LabelField(new GUIContent("Slope Mode", "Default is shear"), GUILayout.Width(72));
            af.slopeModeRailB = (AutoFenceCreator.FenceSlopeMode)EditorGUILayout.Popup("", (int)af.slopeModeRailB, slopeModeNames, GUILayout.Width(100));
            if (af.slopeModeRailB != oldSlopeModeB)
            {
                af.ResetRailBPool();
                af.ForceRebuildFromClickPoints();
            }
            GUILayout.EndHorizontal();

            //==================================================================
            //                   Rails B Variation  
            //==================================================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("box"); // begin VERTICAL Rails A Variations       
            
            //====  Show/Enable Variations  ====
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useRailBVariations"), GUILayout.Width(400));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (af.nonNullRailBVariants.Count == 1)
                {
                    af.railBVariants[1].go = af.railBDisplayVariationGOs[1] = af.railPrefabs[af.currentRailBType];
                    af.varMenuIndexRailB[1] = ConvertRailPrefabIndexToMenuIndex(af.varPrefabIndexRailB[1]);
                }
                af.ResetRailBPool();
                af.ForceRebuildFromClickPoints();
            }
            af.autoHideRailBVar = EditorGUILayout.ToggleLeft("Hide when disabled", af.autoHideRailBVar, GUILayout.Width(150));
            
            if (af.useRailBVariations == true)
            {
                varHelper.ShowVariationSourcesHelp();
                if (showVarHelp == false)
                    GUILayout.EndHorizontal();
            }
            else
                GUILayout.EndHorizontal();

            if (af.useRailBVariations == false)
                EditorGUI.BeginDisabledGroup(true);
            
            if(af.useRailBVariations == true || af.autoHideRailBVar == false)
            {
                railSeqEd.SetupSourcePrefabs(kRailBLayer);
                railSeqEd.SetupVariations();
            }
            EditorGUILayout.EndVertical(); 
            
            singlesEd.SetupSinglesRailB();
            
            if(af.useRailBVariations == false)
                EditorGUI.EndDisabledGroup();
            
            GUILayout.EndVertical();
        }     

        //================================================================================
        //                          Extra Game Object Options
        //================================================================================

        if (af.componentToolbar == AutoFenceCreator.ComponentToolbar.extras)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(" Extras"), cyanBoldStyleBigger, GUILayout.Width(50));
            bool mainPostChanged = false;
            
            if (af.useExtraGameObject == false)
                EditorGUILayout.LabelField(new GUIContent("[Extras are off] "), mildWarningStyle, GUILayout.Width(200));

            //====== Reset  ======
            EditorGUILayout.Separator();
            if (GUILayout.Button(new GUIContent("Reset", "Reset all Extra Scaling/Offsets/Rotations"),
                GUILayout.Width(120)))
            {
                af.ResetExtraTransforms();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            //==================================================================
            //           Choose Extra Prefab 
            //==================================================================
            helper.SetMainPrefab(kExtraLayer);

            
            //===================================================================
            //     Posts Main Parameters - Raise, Size, Position, Rotation  
            //===================================================================
            helper.SetExtraMainParameters();
            
            
            //==================================================================
            //           Extra Specific 
            //==================================================================
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeMovement"), new GUIContent("Move Relative to Distance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeScaling"), new GUIContent("Scale Relative to Distance"));

            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRotateExtra"), new GUIContent("Auto Rotate At Corners"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("raiseExtraByPostHeight"), new GUIContent("Raise by post-height"));
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extrasFollowIncline"), new GUIContent("Incline with slopes"));

            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Freq (0:Main, 1:All, 20:Ends, 21:Not-main)", GUILayout.Width(235));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extraFrequency"), new GUIContent(""));
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("makeMultiArray"), new GUIContent("Make Stack"));
            if (af.makeMultiArray)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("numExtras"), new GUIContent("Num Extras"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("extrasGap"), new GUIContent("Extras Gap"));
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EnforceVectorMinimums(extraSize.vector3Value, new Vector3(0.01f, 0.01f, 0.01f));
                af.multiArraySize.y = af.numExtras;//temp
                af.ForceRebuildFromClickPoints();
            }

            //===================================================================
            //                          Extras Randomization  
            //===================================================================
            randEd.SetupRandomization(kExtraLayer);

            GUILayout.EndVertical();
        }
        //---------------------------------
        //====================================================================================================
        //
        //                                          Globals
        //
        //====================================================================================================
        EditorGUILayout.Separator();
        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("                                                                 Globals", cyanBoldStyle);

        af.currGlobalsToolbarRow1 = GUILayout.Toolbar(af.currGlobalsToolbarRow1, globalsToolbarRow1_GUIContent);
        if (af.currGlobalsToolbarRow1 >= 0)
            af.currGlobalsToolbarRow2 = -1;
        af.currGlobalsToolbarRow2 = GUILayout.Toolbar(af.currGlobalsToolbarRow2, globalsToolbarRow2_GUIContent);
        if (af.currGlobalsToolbarRow2 >= 0)
            af.currGlobalsToolbarRow1 = -1;


        EditorGUILayout.Separator();
       
         //==  Global Scale  ==
        if (af.currGlobalsToolbarRow1 == 0) 
        {
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Global Scale", "Scales all components of the Fence/Wall, including the Inter-Post spacing." +
                "(You can re-adjust the Inter-Post spaacing if needed after scaling)"), cyanBoldStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField(new GUIContent("( Keep Linked", "Scales Both Height & Width Equally"), GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keepGlobalScaleHeightWidthLinked"), new GUIContent(""), GUILayout.Width(12));
            EditorGUILayout.LabelField(")", GUILayout.Width(14));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.LabelField("Height:", GUILayout.Width(43));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fenceHeight, new GUIContent(""), GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (af.keepGlobalScaleHeightWidthLinked)
                    af.fenceWidth = af.fenceHeight;
            
                af.interPostDist = af.baseInterPostDistance * af.fenceHeight;
                af.globalScale = new Vector3(af.fenceWidth, af.fenceHeight, af.fenceWidth);
                af.ForceRebuildFromClickPoints();
            }
            
            EditorGUILayout.LabelField("   Width:", GUILayout.Width(48));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fenceWidth, new GUIContent(""), GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (af.keepGlobalScaleHeightWidthLinked)
                {
                    af.fenceHeight = af.fenceWidth;
                    af.interPostDist = af.baseInterPostDistance * af.fenceHeight;
                }
            
                af.globalScale = new Vector3(af.fenceWidth, af.fenceHeight, af.fenceWidth);
                af.ForceRebuildFromClickPoints();
            }
            if (GUILayout.Button(new GUIContent("R", "Reset Global Scaling to (1,1,1)"), EditorStyles.miniButton, GUILayout.Width(17)))
            {
                af.baseInterPostDistance = af.interPostDist / af.fenceHeight;
                af.fenceWidth = af.fenceHeight = 1;
                af.interPostDist = af.baseInterPostDistance * af.fenceHeight;
                af.globalScale = new Vector3(1, 1, 1);
                af.ForceRebuildFromClickPoints();
            }
            GUILayout.EndHorizontal();


            //== Global Lift
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(globalLiftLower); //this should be 0.0 unless you're layering a fence above another one
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }
            EditorGUILayout.Separator();
        }

        //============================
        //      Smoothing 
        //============================
        if (af.currGlobalsToolbarRow1 == 1)
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Smooth", cyanBoldStyle, GUILayout.Width(99));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smooth"), new GUIContent(""));
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(roundingDistance);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tension"), new GUIContent("   Corner Tightness"));
            GUILayout.Label("Use these to reduce the number of Smoothing posts for performance:", infoStyle);
            GUILayout.Label("(It helps to temporarily disable 'Interpolate' to see the effect of these)", infoStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("removeIfLessThanAngle"), new GUIContent("Remove Where Straight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stripTooClose"), new GUIContent("Remove Vey Close Posts"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                af.ForceRebuildFromClickPoints();
            }
            EditorGUILayout.Separator();
        }

        EditorGUI.BeginChangeCheck();
        if (af.currGlobalsToolbarRow1 == 2) {
            //============================
            //      Close Loop 
            //============================
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(closeLoop);
            if (af.closeLoop != oldCloseLoop)
            {
                Undo.RecordObject(af, "Change Loop Mode");
                af.ManageLoop(af.closeLoop);
                SceneView.RepaintAll();
            }
            oldCloseLoop = af.closeLoop;
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            
            //============================
            //      Snapping
            //============================
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(snapMainPosts);
            EditorGUILayout.PropertyField(snapSize);
            GUILayout.EndHorizontal();
            
            //============================
            //      Reverse
            //============================
            EditorGUILayout.Separator();
            if (GUILayout.Button(new GUIContent("Reverse Fence", "Reverses the order of your click-points. This will also make all objects face 180 the other way."), GUILayout.Width(110)))
            {
                ReverseClickPoints();
            }
            EditorGUILayout.Separator();
        }
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            af.ForceRebuildFromClickPoints();
        }
        
        //======================
        //        ROW 2
        //======================
        //EditorGUI.BeginChangeCheck();
        //==========================================
        //              Cloning & Layout
        //==========================================  
        if (af.currGlobalsToolbarRow2 == 0)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Cloning Options: ", cyanBoldStyle);
           
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Layout", GUILayout.Width(100)) && af.fenceToCopyFrom != null)
            {
                af.CopyLayoutFromOtherFence();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fenceToCopyFrom"), new GUIContent("Drag finished fence here:"));
           
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                string sourceName = af.fenceToCopyFrom.name;

                Transform mainRailsFolder = af.fenceToCopyFrom.transform.Find("Rails");
                Transform firstRailsFolder = mainRailsFolder.transform.Find("RailsAGroupedFolder0");

                int railAClonePrefabIndex = -1;
                if (firstRailsFolder != null)
                {
                    Transform firstChild = firstRailsFolder.GetChild(0);
                    string firstChildName = firstChild.name;

                    int split = firstChildName.IndexOf("_Panel_Rail");
                    if(split == -1)
                        split = firstChildName.IndexOf("_Rail");
                    
                    string shortName = firstChildName.Substring(0, split);
                    
                    Debug.Log("shortName = " + shortName + "\n");
                    
                    for (int i = 0; i < af.railNames.Count; i++)
                    {
                        if (af.railNames[i] == null)
                            continue;
                        string name = af.railNames[i];
                        if (name.Contains(shortName))
                        {
                            Debug.Log("shortName = " + shortName + "\n");
                            railAClonePrefabIndex = i;
                            break;
                        }
                    }

                    if (railAClonePrefabIndex != -1)
                    {
                        int index = ConvertRailMenuIndexToPrefabIndex(railAClonePrefabIndex);
                        
                        af.currentRailAType = index;
                        af.SetRailAType(af.currentRailAType, false, false);
                        af.railAVariants[0].go = af.railPrefabs[af.currentRailAType ];
                        af.ResetRailAPool();
                        
                        af.fenceToCopyFrom.SetActive(false);
                        af.CopyLayoutFromOtherFence(false);
                        af.ForceRebuildFromClickPoints();
                        Debug.Log("Rebuild from " + sourceName + "\n");
                    }
                }
                //af.ForceRebuildFromClickPoints();
            }
        }

        //===========================================
        //                Combining & Batching
        //===========================================
        if (af.currGlobalsToolbarRow2 == 1)
        {
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Batching & Combining: Performance Options: ", cyanBoldStyle);
            showBatchingHelp = EditorGUILayout.Foldout(showBatchingHelp, "Show Batching Help");
            if (showBatchingHelp)
            {
                italicStyle.fontStyle = FontStyle.Italic; italicStyle.normal.textColor = new Color(0.6f, 0.4f, 0.2f);
                GUILayout.Label("• If using Unity Static Batching, select 'Static Batching'.", italicStyle);
                GUILayout.Label("   All parts will be marked as 'Static'.", italicStyle);
                GUILayout.Label("  (You MUST ensure Unity's Static Batching is on [Edit->Project Settings->Player]).", italicStyle);
                GUILayout.Label("•If not using Unity's Static Batching,", italicStyle);
                GUILayout.Label("  select 'Add Combine Scripts' to combnine groups of meshes at runtime", italicStyle);
                GUILayout.Label("•'None' lacks the performance benefits of batching/combining,", italicStyle);
                GUILayout.Label("  but enables moving/deleting single parts at runtime", italicStyle);
                GUILayout.Label("  (avoid this on long complex fences as the cost could affect frame rate.", italicStyle);
            }
            string[] batchingMenuNames = { "Static Batching", "Add Combine Scripts", "None" };
            int[] batchingMenuNums = { 0, 1, 2 };
            af.batchingMode = EditorGUILayout.IntPopup("Batching Mode", af.batchingMode, batchingMenuNames, batchingMenuNums);

            if (af.batchingMode == 0)
            {
                af.addCombineScripts = false;
                af.usingStaticBatching = true;
            }
            else if (af.batchingMode == 1)
            {
                af.addCombineScripts = true;
                af.usingStaticBatching = false;
            }
            else
            {
                af.addCombineScripts = false;
                af.usingStaticBatching = false;
            }

            if (EditorGUI.EndChangeCheck())
            {
                af.ForceRebuildFromClickPoints();
            }
        }
        //===============================================
        //          Refreshing & Unloading Prefabs
        //===============================================
        if (af.currGlobalsToolbarRow2 == 2) {

            if (GUILayout.Button("Refresh Prefabs & Presets", GUILayout.Width(170)))
            {
                ReloadPrefabsAndPresets();
            }
            italicStyle.fontStyle = FontStyle.Italic; italicStyle.normal.textColor = new Color(0.6f, 0.4f, 0.2f);
            GUILayout.Label("'Refresh Prefabs' will reload all prefabs, including your custom ones.", italicStyle);
            GUILayout.Label("Use this if your custom prefabs are not appearing in the preset parts dropdown menus.", italicStyle);


            EditorGUILayout.Separator();
            if (GUILayout.Button("Unload Unused Assets [Optimize Build Size]", GUILayout.Width(260)))
            {
                UnloadUnusedAssets();
            }
            GUILayout.Label("'Unload Unused Assets' will remove all unused models and textures from Auto Fence & Wall Builder.", italicStyle);
            GUILayout.Label("It's important to do this befoe you perform a final Unity 'Build' to make the built application as small as possible.", italicStyle);
        
            EditorGUILayout.Separator();
        }

        if (af.currGlobalsToolbarRow2 == 3)
        {
            Transform parent = af.finishedFoldersParent;
            bool isDirty = false;
            
            GUIStyle headingStyle = new GUIStyle(EditorStyles.label);
            headingStyle.fontStyle = FontStyle.Bold;
            headingStyle.normal.textColor = darkCyan;

            EditorGUILayout.Separator();


            //=================================
            //	 Parent Folder for Finished
            //=================================
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Optional Parent for Finished Folders", headingStyle);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(
                "If you want your Finished Fence folders to be parented to an object in your hierarchy", infoStyle);
            EditorGUILayout.LabelField("drag the parent object here\n", infoStyle);

            EditorGUI.BeginChangeCheck();
            parent = EditorGUILayout.ObjectField(parent, typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                af.finishedFoldersParent = parent;
            }

            EditorGUILayout.Separator();
            GUILayout.EndVertical();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            //=================================
            //			Colliders
            //=================================

            GUILayout.BeginVertical("Box");
            
            //EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Colliders", headingStyle);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(
                "By default, a single BoxCollider will be placed on the rails/walls, set to the height of the posts.\n",
                infoStyle);
            EditorGUILayout.LabelField("For most purposes this gives the expected collision on the fence.\n",
                infoStyle);
            EditorGUILayout.LabelField("It's not usually necessary to have colliders on the posts.\n", infoStyle);
            EditorGUILayout.LabelField(
                "You can change this if, for example, the posts & rails are radically different thicknesses,\n",
                infoStyle);
            EditorGUILayout.LabelField("or if you have posts but no rails.", infoStyle);
            EditorGUILayout.LabelField(
                "For best performance, use Single or None where possible. Using 'Keep Original' on", infoStyle);
            EditorGUILayout.LabelField(
                "custom objects which have MeshColliders, or multiple small colliders is not recommended.", infoStyle);
            
            EditorGUILayout.LabelField(
                "Note: The correct collider will be added when you 'Finish' a fence. During editing a box collider is used");

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            //=========== Defaults ============
            if (GUILayout.Button("Set Defaults", GUILayout.Width(100)))
            {
                af.postColliderMode = 2;
                af.railAColliderMode = 2;
                af.extraColliderMode = 2;
                af.railBoxColliderHeightScale = 1.0f;
                af.railBoxColliderHeightOffset = 0.0f;
                isDirty = true;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            //Collider Modes: 0 = single box, 1 = keep original (user), 2 = no colliders
            string[] subModeNames =
            {
                "Use Single Box Collider", "Keep Original Colliders (Custom Objects Only)", "No Colliders",
                "Mesh Colliders"
            };
            int[] subModeNums = {0, 1, 2, 3};
            EditorGUI.BeginChangeCheck();
            af.railAColliderMode =
                EditorGUILayout.IntPopup("Rail A Colliders: ", af.railAColliderMode, subModeNames, subModeNums);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            EditorGUI.BeginChangeCheck();
            af.postColliderMode =
                EditorGUILayout.IntPopup("Post Colliders: ", af.postColliderMode, subModeNames, subModeNums);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            EditorGUI.BeginChangeCheck();
            af.extraColliderMode =
                EditorGUILayout.IntPopup("Extras Colliders: ", af.extraColliderMode, subModeNames, subModeNums);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(
                "Use these to modify the height and vertical postion of the Rail/Wall's Box Collider:", infoStyle);
            EditorGUI.BeginChangeCheck();
            af.railBoxColliderHeightScale =
                EditorGUILayout.FloatField("Rail BoxCollider Y Scale", af.railBoxColliderHeightScale);
            if (af.railBoxColliderHeightScale < 0.01f)
                af.railBoxColliderHeightScale = 0.01f;
            if (af.railBoxColliderHeightScale > 10f)
                af.railBoxColliderHeightScale = 10.0f;
            EditorGUILayout.Separator();
            af.railBoxColliderHeightOffset =
                EditorGUILayout.FloatField("Rail BoxCollider Y Offset", af.railBoxColliderHeightOffset);
            if (af.railBoxColliderHeightOffset < -10.0f)
                af.railBoxColliderHeightOffset = -10.0f;
            if (af.railBoxColliderHeightOffset > 10f)
                af.railBoxColliderHeightOffset = 10.0f;
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("(On long or complex fences, selecting 'No Colliders' will improve performance",
                infoStyle);
            EditorGUILayout.LabelField("while designing in the Editor. Add them when you're ready to finish.)",
                infoStyle);
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            af.addBoxCollidersToRailB =
                EditorGUILayout.Toggle("Add Box Colliders to Rail B", af.addBoxCollidersToRailB);
            GUILayout.EndVertical();
            EditorGUILayout.Separator();
            
            
            //=================================
            //			LOD
            //=================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("LOD", headingStyle);
            EditorGUILayout.LabelField("With this option selected, a basic LOD group with cutoff distance set to about 8%.", infoStyle);
            EditorGUILayout.LabelField("This will perform simple culling and provide the LOD group ready to add other levels that you prepare", infoStyle);
            af.addLODGroup = EditorGUILayout.Toggle("Add LOD Group when Finishing fence", af.addLODGroup);
            
            EditorGUILayout.EndVertical();
            //=================================
            //			User Object Import Scaling
            //=================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Custom Object Scaling", headingStyle);
            EditorGUILayout.LabelField("With this option selected, the Size setting will be changed to try to match the custom object size", infoStyle);
            af.addScalingToSizeYAfterUserObjectImport = EditorGUILayout.Toggle("Rescale Custom Objects", af.addScalingToSizeYAfterUserObjectImport);
            
            EditorGUILayout.EndVertical();
            //=================================
            //			Gaps
            //=================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Gaps", headingStyle);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Control-Right-Click to create gaps in the fence.", infoStyle);

            EditorGUILayout.Separator();
            af.allowGaps = EditorGUILayout.Toggle("Allow Gaps", af.allowGaps);
            af.showDebugGapLine = EditorGUILayout.Toggle("Show Gap Lines", af.showDebugGapLine);
            EditorGUILayout.Separator();
            GUILayout.EndVertical();
            //=================================
            //			Layer number
            //=================================
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Layer Number", headingStyle);
            EditorGUI.BeginChangeCheck();
            af.ignoreControlNodesLayerNum = EditorGUILayout.IntField("ignoreControlsLayerNum", af.ignoreControlNodesLayerNum);
            if (EditorGUI.EndChangeCheck() ) 
                isDirty = true;
            GUILayout.EndVertical();
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();


            if (isDirty){
                List<Transform> posts = af.posts;
                for (int p = 0; p < af.allPostsPositions.Count - 1; p++){
                    if (posts[p] != null)
                        posts[p].gameObject.layer = 0;
                }
                af.ForceRebuildFromClickPoints();
            }
            if(af.railAColliderMode < 2 || af.postColliderMode < 2 || af.extraColliderMode < 2){
                EditorGUILayout.LabelField("Colliders are being used. It may improve editor performance to leave them  off until ready to Finish the Fence.\n");
            }
            
            EditorGUILayout.EndVertical();
            isDirty = false;

        }
        /*if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }*/

        GUILayout.EndVertical(); //End Globals Box
        EditorGUILayout.Separator();
    }
    //====================================================================================
    //                              End of OnInspectorGUI()
    //====================================================================================
    public void CreateScriptablePresetStringsForMenus(List<ScriptablePresetAFWB> presetList)
    {
        scriptablePresetNames.Clear();
        for (int i = 0; i < presetList.Count; i++)
        {
            string menuName = presetList[i].categoryName + "/" + presetList[i].name;
            scriptablePresetNames.Add(menuName);
        }
    }
    //---------------
    public void GetCategoryNamesFromLoadedPresetList()
    {
        ScriptablePresetAFWB preset;
        string categoryName = "";
        for (int i = 0; i < scriptablePresetList.Count; i++)
        {
            preset = scriptablePresetList[i];
            categoryName = preset.categoryName;

            if (af.categoryNames.Contains(categoryName) == false)
            {
                af.categoryNames.Add(categoryName);
            }
        }
    }

    //--------------
    void ConvertAllPresets()
    {
        /*int numPresets = af.presets.Count;
        for (int i = 0; i < numPresets; i++)
        {
            Debug.Log("Converting " + i + "/" + af.presets.Count + "   " + af.presets[i].name);
            ConvertOldPresetToNew(i, false, false);
        }
        AssetDatabase.SaveAssets();
        h.LoadAllScriptablePresets();
        af.ForceRebuildFromClickPoints();*/
    }
        //----------
    void HandleImportRotation()
    {
        if (rotationsWindowIsOpen == false)
        {
            rotWindow = new BakeRotationsWindow(af, kRailALayer);
            rotWindow.position = new Rect(300, 300, 690, 500);
            rotWindow.ShowPopup();
        }
        else
        {
            rotationsWindowIsOpen = false;
            if (rotWindow != null)
                rotWindow.Close();
        }
    }
    //------------------------------------------
    void UpdatePresetsWithNewPrefabName(string name, string newName, bool saveAll = true)
    {
        ScriptablePresetAFWB thisPreset;
        int numPresets = scriptablePresetList.Count;
        int updatedCount = 0, checkedCount = 0; ;

        for(int i=0; i<numPresets; i++)
        {
            thisPreset = scriptablePresetList[i];
            bool save = false;

            //-- Check RailA --
            if (thisPreset.railAName == name)
            {
                Debug.Log("---Found RailA" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.railAName = newName;
                save = true;
            }
            for (int j = 0; j < AutoFenceCreator.kNumRailVariations; j++)
            {
                if (thisPreset.railAVariants[j].goName == name)
                {
                    Debug.Log("---Found railAVariants" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                    thisPreset.railAVariants[j].goName = newName;
                    save = true;
                }
            }
           
            //-- Check RailB --
            if (thisPreset.railBName == name)
            {
                Debug.Log("---Found RailB" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.railBName = newName;
                save = true;
            }
            for (int j = 0; j < AutoFenceCreator.kNumRailVariations; j++)
            {
                if (thisPreset.railBVariants[j].goName == name)
                {
                    Debug.Log("---Found railBVariants" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                    thisPreset.railBVariants[j].goName = newName; 
                    save = true; 
                }
            }

            //-- Check Post --
            if (thisPreset.postName == name)
            {
                Debug.Log("---Found Post" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.postName = newName;
                save = true;
            }
            for (int j = 0; j < AutoFenceCreator.kNumPostVariations; j++)
            {
                if (thisPreset.postVariants == null)
                    Debug.Log("Null\n");
                else if (thisPreset.postVariants.Count < AutoFenceCreator.kNumPostVariations)
                    Debug.Log("Not enough\n");
                else if (thisPreset.postVariants[j] == null)
                    Debug.Log("[j] Null\n");
                else if (thisPreset.postVariants[j].goName == null)
                    Debug.Log("goName Null\n");

                else if (thisPreset.postVariants[j].goName == name)
                {
                    Debug.Log("---Found Variant" + thisPreset.name + "  with " + name + " to " + newName + "\n");
                    thisPreset.postVariants[j].goName = newName;
                    save = true;
                }
            }
            //-- Check SubPost --
            if (thisPreset.subPostName == name)
            {
                Debug.Log("---Found SubPost in " + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.subPostName = newName;
                save = true;
            }


            //-- Check Extra --
            if (thisPreset.extraName == name)
            {
                Debug.Log("---Found Extra in " + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.extraName = newName;
                save = true;
            }

            if (save == true && saveAll == true)
            {
                Debug.Log("    *Resaving " + thisPreset.name + "  with " + name + " to " + newName + "\n");
                thisPreset.BuildFromPreset(af); // apply preset to current settings

                if (thisPreset.categoryName == "")
                {
                    thisPreset.categoryName = ScriptablePresetAFWB.FindCategoryForPreset(thisPreset, thisPreset.name, "Auto", af);
                    Debug.Log("Found empty categoryName for " + thisPreset.name + "    Changed to " + thisPreset.categoryName + "\n");
                }
                ScriptablePresetAFWB preset = ScriptablePresetAFWB.CreatePresetFromCurrentSettings(thisPreset.name, thisPreset.categoryName, af);

                string filePath = ScriptablePresetAFWB.CreateSaveString(af, preset.name, thisPreset.categoryName);
                if (filePath == "")
                    continue;

                if (AssetDatabase.Contains(preset))
                {
                    AssetDatabase.CreateAsset(preset, filePath);
                }
                else
                    AssetDatabase.CreateAsset(preset, filePath);

                Debug.Log("      Resaved " + preset.name + "  with " + name + " to "+ newName + "\n");
                Debug.Log("...___\n");

                updatedCount++;
            }
            checkedCount++;

        }
        AssetDatabase.SaveAssets();
        Debug.Log("Checked " + checkedCount + "  presets\n");
        Debug.Log("Resaved " + updatedCount + "  presets\n");
    }
    //------------------------------------------
    List<int> FindPresetsContainingPrefab(AutoFenceCreator.FencePrefabType prefabType, int prefabIndex)
    {
        List<int> matchingPresetList = new List<int>();
        ScriptablePresetAFWB preset;
        List<GameObject> prefabList = GetPrefabListOfType(prefabType);

        string prefabNameToMatch = prefabList[prefabIndex].name;

        int numPresets = scriptablePresetList.Count;



        for (int i = 0; i < numPresets; i++)
        {
            preset = scriptablePresetList[i];
            if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
            {
                if (preset.railAName == prefabNameToMatch)
                    matchingPresetList.Add(i);
                if (preset.railBName == prefabNameToMatch)
                    matchingPresetList.Add(i);
            }
            else if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
            {
                if (preset.postName == prefabNameToMatch)
                    matchingPresetList.Add(i);

            }
            else if (prefabType == AutoFenceCreator.FencePrefabType.extraPrefab)
            {
                if (preset.extraName == prefabNameToMatch)
                    matchingPresetList.Add(i);
            }
        }
        return matchingPresetList;
    }
        //------------
    public void ReSeed()
    {
        af.SeedRandom(false);
        /*if (af.railSetToolbarChoice == 0)//A
        {
            af.shuffledRailAIndices = FenceVariations.CreateShuffledIndices(af.nonNullRailAVariants, af.allPostsPositions.Count - 1);
            af.ResetRailAPool();
        }
        else if (af.railSetToolbarChoice == 1)//B
        {
            af.shuffledRailBIndices = FenceVariations.CreateShuffledIndices(af.nonNullRailBVariants, af.allPostsPositions.Count - 1);
            af.ResetRailBPool();
        }*/
        af.ForceRebuildFromClickPoints();
    }

    AutoFenceCreator.FencePrefabType GetPrefabTypeByName(string name)
    {
        if (name.EndsWith("_Post"))
            return AutoFenceCreator.FencePrefabType.postPrefab;
        else if (name.EndsWith("_Rail"))
            return AutoFenceCreator.FencePrefabType.railPrefab;
        else if (name.EndsWith("_Extra"))
            return AutoFenceCreator.FencePrefabType.extraPrefab;

        return AutoFenceCreator.FencePrefabType.postPrefab;
    }
    //------------------------------------------
    GameObject GetPrefabByName(string name)
    {
        AutoFenceCreator.FencePrefabType prefabType = GetPrefabTypeByName(name);
        int prefabIndex = af.FindPrefabIndexByName(prefabType, name);

        if (prefabIndex == -1)
            return null;

        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
            return af.postPrefabs[prefabIndex];
        else if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
            return af.railPrefabs[prefabIndex];
        else if (prefabType == AutoFenceCreator.FencePrefabType.extraPrefab)
            return af.extraPrefabs[prefabIndex];

        return null;
    }
    //------------------------------------------
    // If renaming a prefab, make sure all the presets are updated to reflect the change
    public void RenamePrefab(string originalName, string newName)
    {
        AutoFenceCreator.FencePrefabType prefabType = GetPrefabTypeByName(newName);
        int prefabIndex = af.FindPrefabIndexByName(prefabType, originalName);
        GameObject prefab = GetPrefabByName(originalName);

        if (prefabIndex != -1)
        {
            //-- First resave the prefab with the new name
            ResavePrefabWithNewName(prefab, prefabType, newName);


            UpdatePresetsWithNewPrefabName(originalName, newName, true);

            LoadPrefabs(false, true);
            helper.LoadAllScriptablePresets(af.allowContentFreeUse);
        }
    }
    //------------------------------------------
    List<GameObject> GetPrefabListOfType(AutoFenceCreator.FencePrefabType prefabType)
    {
        List<GameObject> prefabList = af.railPrefabs;

        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
            prefabList = af.postPrefabs;
        else if (prefabType == AutoFenceCreator.FencePrefabType.extraPrefab)
            prefabList = af.extraPrefabs;

        return prefabList;

    }
    //-------------
    public string GetCurrentRailPrefabNameFromMenuNames()
    {
        string prefabName = af.railNames[af.railAMenuIndex]; // name including category
        prefabName = prefabName.Remove(0, prefabName.IndexOf("/") + 1); // name without category
        return prefabName;
    }
    //-------------
    public int ConvertMenuIndexToPrefabIndex(int menuIndex, AutoFenceCreator.FencePrefabType prefabType)
    {
        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
            return ConvertPostMenuIndexToPrefabIndex(menuIndex);
        if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
            return ConvertRailMenuIndexToPrefabIndex(menuIndex);
        if (prefabType == AutoFenceCreator.FencePrefabType.extraPrefab)
            return ConvertExtraMenuIndexToPrefabIndex(menuIndex);
        return 0;
    }
    //-------------
    public int ConvertPrefabIndexToMenuIndex(int prefabIndex, AutoFenceCreator.FencePrefabType prefabType)
    {
        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
            return ConvertPostPrefabIndexToMenuIndex(prefabIndex);
        if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
            return ConvertRailPrefabIndexToMenuIndex(prefabIndex);
        return 0;
    }
    //-------------
    public int ConvertRailMenuIndexToPrefabIndex(int railmenuIndex)
    {
        string prefabName = af.railNames[railmenuIndex]; // name including category
        prefabName = prefabName.Remove(0, prefabName.IndexOf("/") + 1); // remove category name
        int prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.railPrefab, prefabName);
        return prefabIndex;
    }
    //-------------
    public int ConvertRailPrefabIndexToMenuIndex(int railprefabIndex)
    {
        string prefabName = af.railPrefabs[railprefabIndex].name; // name including category
        int menuIndex = af.FindPrefabIndexInNamesList(AutoFenceCreator.FencePrefabType.railPrefab, prefabName);
        return menuIndex;
    }
    //-------------
    public int ConvertPostMenuIndexToPrefabIndex(int postMenuIndex)
    {
        string prefabName = af.postNames[postMenuIndex]; // name including category
        prefabName = prefabName.Remove(0, prefabName.IndexOf("/") + 1); // remove category name
        int prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.postPrefab, prefabName);
        return prefabIndex;
    }
    //-------------
    public int ConvertPostPrefabIndexToMenuIndex(int postPrefabIndex)
    {
        string prefabName = af.postPrefabs[postPrefabIndex].name; // name including category
        int menuIndex = af.FindPrefabIndexInNamesList(AutoFenceCreator.FencePrefabType.postPrefab, prefabName);
        return menuIndex;
    }
    //-------------
    public int ConvertExtraMenuIndexToPrefabIndex(int extraMenuIndex)
    {
        string prefabName = af.extraNames[extraMenuIndex]; // name including category
        prefabName = prefabName.Remove(0, prefabName.IndexOf("/") + 1); // remove category name
        int prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.extraPrefab, prefabName);
        return prefabIndex;
    }
    //-------------
    public int ConvertExtraPrefabIndexToMenuIndex(int extraPrefabIndex)
    {
        string prefabName = af.extraPrefabs[extraPrefabIndex].name; // name including category
        int menuIndex = af.FindPrefabIndexInNamesList(AutoFenceCreator.FencePrefabType.extraPrefab, prefabName);
        return menuIndex;
    }
    void InitTextures()
    {
        railSectionTexture = new Texture2D(1, 1);
        railSectionTexture.SetPixel(0, 0, transRed);
        railSectionTexture.Apply();
        /*sectionRect.x = 0;
        sectionRect.y = 0;
        sectionRect.width = 400;
        sectionRect.height = 400;*/
    }
    //---------------------------------------
    public void DestroyNow(GameObject go)
    {
        DestroyImmediate(go);
    }
    //---------------
    protected void SetupStyles()
    {
        cyanBoldStyle = new GUIStyle(EditorStyles.label);
        cyanBoldStyle.fontStyle = FontStyle.Bold;
        cyanBoldStyle.normal.textColor = darkCyan;

        cyanBoldStyleBigger = new GUIStyle(EditorStyles.label);
        cyanBoldStyleBigger.fontStyle = FontStyle.Bold;
        cyanBoldStyleBigger.normal.textColor = darkCyan;
        cyanBoldStyleBigger.fontSize = 13;

        warningStyle = new GUIStyle(EditorStyles.label);
        warningStyle.fontStyle = FontStyle.Bold;
        warningStyle.normal.textColor = darkRed;

        mildWarningStyle = new GUIStyle(EditorStyles.label);
        mildWarningStyle.normal.textColor = new Color(0.7f, 0.2f, 0.2f);

        infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.fontStyle = FontStyle.Italic;
        infoStyle.normal.textColor = darkCyan;

        infoStyleSmall = new GUIStyle(EditorStyles.label);
        infoStyleSmall.fontStyle = FontStyle.Italic;
        infoStyleSmall.fontSize = 10;
        infoStyleSmall.normal.textColor = darkCyan;
        
        smallBoldBlack = new GUIStyle(EditorStyles.label);
        smallBoldBlack.fontStyle = FontStyle.Bold;
        smallBoldBlack.fontSize = 10;
        smallBoldBlack.normal.textColor = Color.black;

        italicStyle = new GUIStyle(EditorStyles.label);
        italicStyle.fontStyle = FontStyle.Italic;
        italicStyle.normal.textColor = new Color(0.6f, 0.4f, 0.2f);

        lightGrayStyle = new GUIStyle(EditorStyles.label);
        lightGrayStyle.fontStyle = FontStyle.Italic;
        lightGrayStyle.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
        lightGrayStyle.fontSize = 10;
        
        darkGrayStyle = new GUIStyle(EditorStyles.label);
        darkGrayStyle.fontStyle = FontStyle.Italic;
        darkGrayStyle.normal.textColor = new Color(0.35f, 0.35f, 0.4f);
        darkGrayStyle.fontSize = 10;
        
        grayHelpStyle = new GUIStyle(EditorStyles.label);
        grayHelpStyle.fontStyle = FontStyle.Italic;
        grayHelpStyle.normal.textColor = new Color(0.15f, 0.2f, 0.3f);
        grayHelpStyle.fontSize = 11;

        smallStyle = new GUIStyle(EditorStyles.label);
        smallStyle.fontSize = 10;

        smallButtonStyle = new GUIStyle(EditorStyles.miniButton);
        smallButtonStyle.fontSize = 7;

        smallToolbarStyle = new GUIStyle(EditorStyles.miniButton);
        smallToolbarStyle.fontSize = 12;
        
        smallLabel = new GUIStyle(EditorStyles.miniLabel);
        
        label11 = new GUIStyle(EditorStyles.label);
        label11.fontSize = 11;

        miniBold = new GUIStyle(EditorStyles.miniButton);
        miniBold.fontStyle = FontStyle.Bold;
    }
    //------------------------------------------
    public bool OnInspectorAssetsCheck()
    {
        if (userUnloadedAssets == true || fencePrefabsFolderFound == false)
        {
            EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
            if (fencePrefabsFolderFound == false)
            {
                EditorGUILayout.LabelField("Missing FencePrefabs Folder. It must be at Assets/Auto Fence Builder/FencePrefabs/");
                EditorGUILayout.LabelField("Please relocate this folder or re-import Auto Fence & Wall Builder");
                if (GUILayout.Button("Retry", GUILayout.Width(200)))
                {
                    fencePrefabsFolderFound = true; // assume it's true before retrying
                    LoadPrefabs(af.allowContentFreeUse, true);
                }
            }
            else
            {
                EditorGUILayout.LabelField("You have Unloaded all AFWB Assets to optimize Build size.", warningStyle);
                EditorGUILayout.LabelField("To continue using AFWB, press Reload below.", warningStyle);
                if (GUILayout.Button("Reload Auto Fence & Wall Builder", GUILayout.Width(200)))
                {
                    ReloadPrefabsAndPresets();
                    userUnloadedAssets = false;
                }
            }
            EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
            return false;
        }
        return true;
    }
    //---------------------------------------
    // Reversing the order also has the effect of making all objects face 180 the other way.
    void ReverseClickPoints()
    {
        af.clickPoints.Reverse();
        af.ForceRebuildFromClickPoints();
    }
   
    //--------------------------
    public struct EditorSceneInfo
    {
        public AutoFenceEditor editor;
        public int menuIndex;
        public int flag;
        public int sectionIndex;
        public bool isLayerA, isLayerB, isPost;
        public bool isFree;
        public bool resetSection;
    }
    //--------------------------
    //- Gets the world position with an x, y offset
    public Vector3 GetWorldPos(float x, float y, ref Vector3 currWorldPos, Camera cam)
    {

        Vector3 screenPoint = cam.WorldToScreenPoint(currWorldPos);
        screenPoint.x += x;
        screenPoint.y += y;
        currWorldPos = cam.ScreenToWorldPoint(screenPoint);
        return currWorldPos;
    }
    //--------------------
    private void ShowBuildInfo()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 11;

        int lineHeight = 16;

        Camera cam = SceneView.lastActiveSceneView.camera;


        //- working from bottom upwards
        int totalTrianglesCount = af.railsATotalTriCount + af.railsBTotalTriCount + af.postsTotalTriCount
        + af.extrasTotalTriCount + af.subPostsTotalTriCount;
        int numRailA = af.railACounter, numRailB = af.railACounter;
        int numPosts = af.postCounter, numExtras = af.extraCounter, numSubs = af.subCounter;
        int railATriCount = 0, railBTriCount = 0, postTriCount = 0, extraTriCount = 0, subTriCount = 0, avgTrisPerSection = 0;

        if (af.usePosts == false)
            numPosts = 0;

        if (af.railACounter > 0 && af.railsATotalTriCount > 0 && numRailA > 0)
            railATriCount = af.railsATotalTriCount / numRailA;
        if (af.railBCounter > 0 && af.railsBTotalTriCount > 0 && numRailB > 0)
            railBTriCount = af.railsBTotalTriCount / numRailB;
        if (af.postCounter > 0 && af.usePosts == true && af.postsTotalTriCount > 0)
            postTriCount = af.postsTotalTriCount / numPosts;
        if (af.extraCounter > 0 && af.extrasTotalTriCount > 0)
            extraTriCount = af.extrasTotalTriCount / numExtras;
        if (af.subCounter > 0 && af.subPostsTotalTriCount > 0)
            subTriCount = af.subPostsTotalTriCount / numSubs;


        int numSects = (af.allPostsPositions.Count - 1);
        if (numSects > 0)
        {
            avgTrisPerSection = totalTrianglesCount / numSects;
        }
        else
        {
            numSects = 0;
        }


        Vector3 baseScreenPos = new Vector3(10, 85, 20);
        Vector3 screenPos = baseScreenPos;
        Vector3 wPos = cam.ScreenToWorldPoint(screenPos);//world position

        Handles.Label(wPos, "Number of Rails A = " + (af.railACounter) + "    Rails B = " + (af.railBCounter) +
        "    Posts = " + af.allPostsPositions.Count + "    SubPosts = " + af.subCounter + "    Extras = " + af.extraCounter);
        //Handles.Label(GetWorldPos(0, lineHeight, ref wPos, cam), "Pool Sizes:  Posts   " + af.posts.Count + "      Rails " + af.railsA.Count + "      Extras " + af.extras.Count);
        Handles.Label(GetWorldPos(0, lineHeight, ref wPos, cam), "num variations  A: " + af.nonNullRailAVariants.Count + "     B: " + af.nonNullRailBVariants.Count);
        Handles.Label(GetWorldPos(0, lineHeight, ref wPos, cam),
                                        "Total Triangle Count = " + totalTrianglesCount + " :" +
                                        "     Rails-A: " + af.railsATotalTriCount + " (" + numRailA + " x " + railATriCount + ")" +
                                        "     Rails-B: " + af.railsBTotalTriCount + " (" + numRailB + " x " + railBTriCount + ")" +
                                        ",    Posts: " + af.postsTotalTriCount + " (" + numPosts + " x " + postTriCount + ")" +
                                        ",    Extras: " + af.extrasTotalTriCount + " (" + numExtras + " x " + extraTriCount + ")" +
                                        ",    SubPosts: " + af.subPostsTotalTriCount + " (" + numSubs + " x " + subTriCount + ")" +
                                        "");
        Handles.Label(GetWorldPos(0, lineHeight, ref wPos, cam), "Average Triangles Per Section =  " + avgTrisPerSection + "  (" + numSects + " sections)");
    }
    //=============================================================================================================

    void OnSceneGUI()
    {
        Handles.BeginGUI();
        
        af.showPreviewLines = GUI.Toggle(new Rect(5, (Screen.height - 90), 220, 20), af.showPreviewLines, "Show Preview Lines");
        showTriangleCounts = GUI.Toggle(new Rect(5, (Screen.height - 75), 220, 20), showTriangleCounts, "Show AutoFence Triangle Counts");
        EditorGUI.BeginChangeCheck();
        af.showControls = GUI.Toggle(new Rect(5, (Screen.height - 60), 220, 20), af.showControls , "Show Control Nodes");
        if(af.showControls)
            GUI.Label( new Rect(140, (Screen.height - 60), 560, 20), 
                "[ ADD Post: Shift-Click     INSERT: Ctrl-Shift-Click     GAP: Shift-Right-Click     DELETE:Ctrl-Left-Click node ]", darkGrayStyle);
        if (EditorGUI.EndChangeCheck())
        {
            af.ForceRebuildFromClickPoints();
        }       
        Handles.EndGUI();

        if (showTriangleCounts == true)
            ShowBuildInfo();
        if(af.allPostsPositions.Count == 0) // show as empty if nothing built yet
        {
            Vector3 wPos = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(new Vector3(10, 70, 20));
            Handles.Label(wPos, "[Empty Fence - Shift-click to start building]");
        }
        

        // Completely block use, if user has chosen to unload assets to optimize build size
        if (userUnloadedAssets == true)
            return;

        af.CheckFolders();
        Event currentEvent = Event.current;
        if (currentEvent.alt)
            return;     // It's not for us!
        Vector3 clickPoint = Vector3.zero;
        int shiftRightClickAddGap = 0; // use 0 instead of a boolean so we can store int flags in clickPointFlags

        //============= Delete Post==============
        if (af.showControls && currentEvent.control && currentEvent.type == EventType.MouseDown && currentEvent.button == 0) // showControls + control-left-click
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                string name = hit.collider.gameObject.name;
                if (name.StartsWith("FenceManagerMarker_"))
                {
                    Undo.RecordObject(af, "Delete Post");
                    string indexStr = name.Remove(0, 19);
                    int index = Convert.ToInt32(indexStr);
                    af.DeletePost(index);
                    //deletedPostNow = true;
                }
            }
        }
        //============= Toggle Gap Status of Post==============
        bool togglingGaps = false;
        if (af.showControls && currentEvent.control && currentEvent.type == EventType.MouseDown && currentEvent.button == 1)// showControls + control-right-click
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                string name = hit.collider.gameObject.name;
                if (name.StartsWith("FenceManagerMarker_"))
                {
                    Undo.RecordObject(af, "Toggle Gap Status Of Post");
                    string indexStr = name.Remove(0, 19);
                    int index = Convert.ToInt32(indexStr);
                    int oldStatus = af.clickPointFlags[index];
                    af.clickPointFlags[index] = 1 - oldStatus; // invert 0/1
                    af.ForceRebuildFromClickPoints();
                    togglingGaps = true;
                }
            }
        }
        //======== Add Gap =====
        //some redundant checking, but need to make this extra visible for maintainence, as control-click has two very different effects. 
        if (togglingGaps == false && currentEvent.button == 1 && currentEvent.shift && !currentEvent.control && currentEvent.type == EventType.MouseDown)
        {
            shiftRightClickAddGap = 1;// we're inserting a new clickPoint, but as a break/gap
        }
        //============== Add Post ============
        if ((!currentEvent.control && currentEvent.shift && currentEvent.type == EventType.MouseDown && Event.current.button != 1) || shiftRightClickAddGap == 1)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                if (currentEvent.button == 0 || (shiftRightClickAddGap == 1 && af.allowGaps))
                {
                    Undo.RecordObject(af, "Add Post");
                    af.endPoint = Handles.PositionHandle(af.endPoint, Quaternion.identity);
                    af.endPoint = hit.point;
                    clickPoint = hit.point - new Vector3(0, 0.00f, 0); //bury it in ground as little
                    if (af.snapMainPosts)
                        clickPoint = SnapHandles(clickPoint, af.snapSize);
                    oldCloseLoop = af.closeLoop = false;
                    RepositionFolderHandles(clickPoint);
                    af.clickPoints.Add(clickPoint);
                    af.clickPointFlags.Add(shiftRightClickAddGap); // 0 if normal, 1 if break
                    af.keyPoints.Add(clickPoint);
                    //Timer t = new Timer("ForceRebuild");
                    af.ForceRebuildFromClickPoints();
                    if (af.rotateY)
                        af.ForceRebuildFromClickPoints();
                    //t.End();
                    // copy click points to handle points
                    af.handles.Clear();
                    for (int i = 0; i < af.clickPoints.Count; i++)
                    {
                        af.handles.Add(af.clickPoints[i]);
                    }

                }
            }
        }
        Selection.activeGameObject = af.gameObject;
        
        if(Event.current.type == EventType.MouseMove) SceneView.RepaintAll();
        //============== Create Vectors for Preview Lines ============
        if (currentEvent.shift && af.clickPoints.Count > 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                if (currentEvent.button == 0 || (shiftRightClickAddGap == 1 && af.allowGaps))
                {
                    clickPoint = hit.point; 
                    if (af.snapMainPosts)
                        clickPoint = SnapHandles(clickPoint, af.snapSize);
                    Vector3 lastPt = af.clickPoints[af.clickPoints.Count - 1];
                    af.previewPoints[0] = lastPt;
                    af.previewPoints[1] = clickPoint;
                }
            }
        }

        //============= Insert Post ===============
        if (currentEvent.shift && currentEvent.control && currentEvent.type == EventType.MouseDown)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                Undo.RecordObject(af, "Insert Post");
                af.InsertPost(hit.point);
            }
        }

        //======== Handle dragging & controls ============
        if (af.showControls && af.clickPoints.Count > 0)
        {
            bool wasDragged = false;
            // Create handles at every click point
            if (currentEvent.type == EventType.MouseDrag)
            {
                af.handles.Clear();
                af.handles.AddRange(af.clickPoints); // copy them to the handles
                wasDragged = true;
                Undo.RecordObject(af, "Move Post");
            }
            for (int i = 0; i < af.handles.Count; i++)
            {
                if (af.closeLoop && i == af.handles.Count - 1)// don't make a handle for the last point if it's a closed loop
                    continue;
                af.handles[i] = Handles.PositionHandle(af.handles[i], Quaternion.identity); //allows movement of the handles
                if (af.snapMainPosts)
                    af.handles[i] = SnapHandles(af.handles[i], af.snapSize);
                af.clickPoints[i] = af.handles[i];// set new clickPoint position
                af.Ground(af.clickPoints); // re-ground the clickpoints
                af.handles[i] = new Vector3(af.handles[i].x, af.clickPoints[i].y, af.handles[i].z); // set the y position back to the clickpoint (grounded)
            }
            if (wasDragged)
            {
                //Undo.RecordObject(script, "Move Post");
                af.ForceRebuildFromClickPoints();
            }
        }

        //=======================================================
        //        Game Object menu Selection   
        //=======================================================
        if (currentEvent.control)
        {
            bool overRail = false, overPost = false, isLayerA = false, isLayerB = false;
            int railASectionIndex = -1, railBSectionIndex = -1;
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                GameObject go = hit.transform.gameObject;
                if (go != null && go.name != "Terrain" && go.name.Contains("_Rail"))
                {
                    //Get the variation index by parsing the string (which ends with ...[x v1]
                    char penultimateChar = go.name[go.name.Length - 2];
                    //int variationIndex = (int)char.GetNumericValue(penultimateChar);
                    //Get the fence section index by parsing the string 
                    //Debug.Log(go.name + "\n");
                    isLayerA = IsLayerA(go);
                    if (isLayerA == false)
                        isLayerB = true;
                    if (isLayerA == true/*&& af.variationModeRailA == VariationMode.sequenced*/)
                    {
                        railASectionIndex = GetSectionIndex(go);
                        string seqString = go.name.Substring(go.name.Length - 2);
                        if (af.variationModeRailA == VariationMode.sequenced && seqString != "-1") //
                            af.railASeqInfo.currStepIndex = int.Parse(seqString);
                       //Debug.Log(go.name + " " + sectionIndex + "   " + currSeqAStepIndex);
                    }
                    else if (isLayerB == true )
                    {
                        railBSectionIndex = GetSectionIndex(go);
                        string seqString = go.name.Substring(go.name.Length - 2);
                        if (af.variationModeRailB == VariationMode.sequenced)
                            af.railBSeqInfo.currStepIndex = int.Parse(seqString);
                        //Debug.Log(go.name + " " + sectionIndex + "   " + currSeqAStepIndex);
                    }
                    //Debug.Log(go.name + " " + sectionIndex + "   " + currSeqAStepIndex);
                    Repaint();
                    overRail = true;
                }
               else if (go != null && go.name.Contains("_Post"))
                {
                    char penultimateChar = go.name[go.name.Length - 2];
                    //int variationIndex = (int)char.GetNumericValue(penultimateChar);
                    //postSectionIndex = GetSectionIndex(go);
                    string seqString = go.name.Substring(go.name.Length - 2);
                    currSeqPostStepIndex = int.Parse(seqString);

                    Repaint();
                    overPost = true;
                }
            }
            if (Event.current.button == 1 && Event.current.type == EventType.MouseDown)
            {
                EditorSceneInfo editorStruct = new EditorSceneInfo();
                editorStruct.editor = this;
                editorStruct.flag = 0;
                editorStruct.isLayerA = isLayerA;
                editorStruct.sectionIndex = railASectionIndex;
                editorStruct.resetSection = false;
                if (isLayerA == false)
                {
                    editorStruct.isLayerB = true;
                    editorStruct.sectionIndex = railBSectionIndex;
                }

                if (overRail == true)
                {
                    List<string> names = new List<string>();
                    GenericMenu menu = new GenericMenu();
                    VariationMode variationMode = VariationMode.optimal;
                    if (isLayerA == true)
                    {
                        variationMode = af.variationModeRailA;
                        names = GetVariantNames(kRailALayer);
                        if (variationMode != VariationMode.sequenced)
                            editorStruct.flag = 2;

                        menu.AddItem(new GUIContent("---- Layer A: Assign Variation -  Free Single  ----"), false, null, null);
                    }
                    else if (isLayerB == true)
                    { 
                        variationMode = af.variationModeRailB;
                        names = GetVariantNames(kRailBLayer);
                        if (variationMode != VariationMode.sequenced)
                            editorStruct.flag = 2;
                       
                       menu.AddItem(new GUIContent("---- Layer B: Assign Variation - Free Single  ----"), false, null, null);
                    }

                    //menu.allowDuplicateNames = true;
                    for (int v = 0; v < names.Count; v++)
                    {
                        this.sceneViewMenuIndex = v;
                        editorStruct.menuIndex = v;
                        editorStruct.isFree = true;
                        string prefixStr = "(Main) "; // To get around Popup not showing duplicates pre Unity 2018.2
                        if (v > 0)
                            prefixStr = v.ToString() + " ";
                        menu.AddItem(new GUIContent(prefixStr + names[v]), false, Callback, editorStruct);
                    }
                    if(variationMode == VariationMode.sequenced)
                    {
                        if (isLayerA)
                            menu.AddItem(new GUIContent("---- Layer A: Put In Sequencer Step " + (af.railASeqInfo.currStepIndex+1) + " ----"), false, null, null);
                        if (isLayerB)
                            menu.AddItem(new GUIContent("---- Layer A: Put In Sequencer Step " + (af.railBSeqInfo.currStepIndex+1) + " ----"), false, null, null);

                        for (int v = names.Count; v < (names.Count+names.Count); v++)
                        {
                            this.sceneViewMenuIndex = v;
                            editorStruct.menuIndex = v - names.Count;
                            editorStruct.isFree = false; // it's for the sequencer
                            menu.AddItem(new GUIContent(v.ToString() + " " + names[v- names.Count]), false, Callback, editorStruct);
                            //menu.AddItem(new GUIContent(names[v - names.Count]), false, Callback, editorStruct);
                        }
                    }

                    menu.AddItem(new GUIContent(""), false, Callback, editorStruct);
                    editorStruct.resetSection = true;
                    editorStruct.menuIndex = 100;
                    menu.AddItem(new GUIContent("Reset Section"), false, Callback, editorStruct);

                    menu.ShowAsContext();
                    Repaint();

                }
                else if (overPost == true)
                {
                    List<string> names = new List<string>();
                    names = GetVariantNames(kPostLayer);
                    GenericMenu menu = new GenericMenu();
                    
                    for (int v = 0; v < names.Count; v++)
                    {
                        this.sceneViewMenuIndex = v;
                        editorStruct.menuIndex = v;
                        string prefixStr = "(Main) "; // To get around Popup not showing duplicates
                        if (v > 0)
                            prefixStr = v.ToString() + " ";

                        menu.AddItem(new GUIContent(prefixStr + names[v]), false, Callback, editorStruct);
                    }
                    menu.ShowAsContext();
                }
            }
        }
    }
   
    //---------------
    static void Callback(object obj)
    {
        EditorSceneInfo info = (EditorSceneInfo)obj;
        AutoFenceEditor editor = info.editor;
        int menuIndex = info.menuIndex;

        if (menuIndex == -1)
            return;

        AutoFenceCreator.LayerSet layerA = AutoFenceCreator.LayerSet.railALayerSet;
        AutoFenceCreator.LayerSet layerB = AutoFenceCreator.LayerSet.railBLayerSet;

        //==== Reset and clear the assigned single ====
        if(menuIndex == 100 && info.resetSection == true)
        {
            if (info.isLayerA)
            {
                if (info.sectionIndex != -1)
                {
                    FenceVariant singleVariant = editor.af.FindSingleVariantWithSectionIndex(layerA, info.sectionIndex);
                    if (singleVariant != null)
                        editor.af.railASingleVariants.Remove(singleVariant);
                }
                if (info.flag == 2) // Not in Sequencer Mode
                {
                    editor.af.railASingles[info.sectionIndex] = -1; // -1 means ignore as single
                }
                else // In Sequencer Mode
                {
                    if (info.sectionIndex != -1)
                        editor.af.railASingles[info.sectionIndex] = -1; // -1 means ignore as single
                    editor.af.seqRailAVarIndex[editor.currSeqAStepIndex] = 0;
                    editor.varHelper.SetSequenceVariantFromDisplaySettings(layerA, ref editor.currSeqAStepVariant, editor.currSeqAStepIndex);
                    Debug.Log("Resetting Section");
                }
                editor.af.ResetRailAPool();
            }
            if (info.isLayerB)
            {
                if (info.sectionIndex != -1)
                {
                    FenceVariant singleVariant = editor.af.FindSingleVariantWithSectionIndex(layerB, info.sectionIndex);
                    if (singleVariant != null)
                        editor.af.railBSingleVariants.Remove(singleVariant);
                }
                if (info.flag == 2) // Not in Sequencer Mode
                {
                    if (info.sectionIndex != -1)
                        editor.af.railBSingles[info.sectionIndex] = -1; // -1 means ignore as single
                }
                else // In Sequencer Mode
                {
                    if (info.sectionIndex != -1)
                        editor.af.railBSingles[info.sectionIndex] = -1; // -1 means ignore as single
                    editor.af.seqRailBVarIndex[editor.currSeqBStepIndex] = 0;
                    editor.varHelper.SetSequenceVariantFromDisplaySettings(layerB, ref editor.currSeqBStepVariant, editor.currSeqBStepIndex);
                    Debug.Log("Resetting Section");
                }
                editor.af.ResetRailBPool();
            }
            editor.af.ForceRebuildFromClickPoints();
            editor.Repaint();
            return;
        }

        if (info.isLayerA)
        { 
            if (info.flag == 2) // Not in Sequencer Mode
            {
                if (info.sectionIndex != -1)
                {
                    editor.af.railASingles[info.sectionIndex] = menuIndex;
                    FenceVariant.AddVariantToSingles(layerA, info.sectionIndex, menuIndex, editor.af);
                }
            }
            else // In Sequencer Mode
            {

                // it's for the sequencer
                if (info.isFree == false) 
                {
                    //First reset any free singles
                    if (info.sectionIndex != -1)
                        editor.af.railASingles[info.sectionIndex] = -1; // -1 means ignore as single
                    //-----
                    editor.af.seqRailAVarIndex[editor.af.railASeqInfo.currStepIndex] = menuIndex;
                    editor.varHelper.SetSequenceVariantFromDisplaySettings(layerA, ref editor.currSeqAStepVariant, editor.af.railASeqInfo.currStepIndex);
                }
                else // it's free
                {
                    //int nonNullIndex = editor.af.FindFirstInNonNullRailVariants(layerA, editor.af.railAVariants[menuIndex].go);
                    if (info.sectionIndex != -1)
                    {
                        editor.af.railASingles[info.sectionIndex] = menuIndex;
                        FenceVariant.AddVariantToSingles(layerA, info.sectionIndex, menuIndex, editor.af);
                    }
                }
            }
            editor.af.ResetRailAPool();
            editor.af.ForceRebuildFromClickPoints();
        }
        if (info.isLayerB)
        {
            if (info.flag == 2) // Not in Sequencer Mode
            {
                if (info.sectionIndex != -1)
                {
                    editor.af.railBSingles[info.sectionIndex] = menuIndex;
                    FenceVariant.AddVariantToSingles(layerB, info.sectionIndex, menuIndex, editor.af);
                }

            }
            else // In Sequencer Mode
            {
                if (info.isFree == false) // it's for the sequencer
                {
                    //First reset any free singles
                    if (info.sectionIndex != -1)
                        editor.af.railBSingles[info.sectionIndex] = -1; // -1 means ignore as single
                    //-----
                    editor.af.seqRailBVarIndex[editor.af.railASeqInfo.currStepIndex] = menuIndex;
                    editor.varHelper.SetSequenceVariantFromDisplaySettings(layerB, ref editor.currSeqBStepVariant, editor.af.railBSeqInfo.currStepIndex);
                }
                else// it's free
                {
                    if (info.sectionIndex != -1)
                    {
                        editor.af.railBSingles[info.sectionIndex] = menuIndex;
                        FenceVariant.AddVariantToSingles(layerB, info.sectionIndex, menuIndex, editor.af);
                    }
                }
            }
            editor.af.ResetRailBPool();
            editor.af.ForceRebuildFromClickPoints();
        }
        editor.Repaint();
    }
    //---------------
    bool IsLayerA(GameObject go)
    {
        if (go.transform.parent.name.Contains("RailsAGroupedFolder"))
            return true;
        return false;
    }
    //---------------
    // Section Index is the sequential position of the part along the whole fence length
    private static int GetSectionIndex(GameObject go)
    {
        int sectStart = go.name.IndexOf("_Rail[");
        AutoFenceCreator.LayerSet layerSet = FindLayerFromParentName(go);
        if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
            sectStart = go.name.IndexOf("_Post[");

        if (sectStart == -1)
            return -1;

        string sectionStr = go.name.Substring(sectStart + 6);
        int sectEnd = sectionStr.IndexOf("v");
        sectionStr = sectionStr.Remove(sectEnd - 1);
        int sectionIndex = int.Parse(sectionStr);
        return sectionIndex;
    }

    private static AutoFenceCreator.LayerSet FindLayerFromParentName(GameObject go)
    {
        GameObject parent = go.transform.parent.gameObject;
        AutoFenceCreator.LayerSet layerSet = AutoFenceCreator.LayerSet.railALayerSet;

        if (parent.name.Contains("RailsAGrouped"))
            layerSet = AutoFenceCreator.LayerSet.railALayerSet;
        else if (parent.name.Contains("RailsBGrouped"))
            layerSet = AutoFenceCreator.LayerSet.railBLayerSet;
        if (parent.name.Contains("PostsGrouped"))
            layerSet = AutoFenceCreator.LayerSet.postLayerSet;

        return layerSet;
    }
    //---------------
    List<string> GetVariantNames(AutoFenceCreator.LayerSet layerSet)
    {
        List<string> names = new List<string>();

        if (layerSet == kRailALayer)
        {
            int count = af.railAVariants.Count;
            for (int i = 0; i < count; i++)
            {
                FenceVariant thisVariant = af.railAVariants[i];
                if (thisVariant != null && thisVariant.go != null)
                    names.Add(thisVariant.go.name);
            }
        }
        else if (layerSet == kRailBLayer)
        {
            int count = af.railBVariants.Count;
            for (int i = 0; i < count; i++)
            {
                FenceVariant thisVariant = af.railBVariants[i];
                if (thisVariant != null && thisVariant.go != null)
                    names.Add(thisVariant.go.name);
            }
        }
        else if (layerSet == kPostLayer)
        {
            int count = af.postVariants.Count;
            for (int i = 0; i < count; i++)
            {
                FenceVariant thisVariant = af.postVariants[i];
                if (thisVariant != null && thisVariant.go != null)
                    names.Add(thisVariant.go.name);
            }
        }
        return names;
    }
    //----------------------------------------------------------------------------------------
    //Save the user-added object in to the FencePrefabs and Meshes folders
    void ResavePrefabWithNewName(GameObject go, AutoFenceCreator.FencePrefabType prefabType, string newName)
    {
        string prefabPath = "";
        string prefabName = go.name;

        if (prefabType == AutoFenceCreator.FencePrefabType.postPrefab)
        {
            prefabPath = "Assets/Auto Fence Builder/FencePrefabs/_Posts/" + prefabName + ".prefab";

        }
        if (prefabType == AutoFenceCreator.FencePrefabType.railPrefab)
        {
            prefabPath = "Assets/Auto Fence Builder/FencePrefabs/_Rails/" + prefabName + ".prefab";
        }
        if (prefabType == AutoFenceCreator.FencePrefabType.extraPrefab)
        {
            prefabPath = "Assets/Auto Fence Builder/FencePrefabs/_Extras/" + prefabName + ".prefab";
        }
        AssetDatabase.RenameAsset(prefabPath, newName);
        AssetDatabase.Refresh();
    }

    //----------------------------------------------------------------------------------------  
    //Saves the procedurally generated Rail meshes produced when using Sheared mode as prefabs, in order to create a working prefab from the Finished AutoFence
    /*bool SaveProcRailMeshesAsAssets()
    {//Debug.Log("SaveProcRailMeshesAsAssets()\n");

        if(af.railACounter == 0 && af.railBCounter == 0)
            Debug.Log("No rail meshes needed saving \n");
        
        List<Transform> rails = af.railsA;
        int numRails = 0;
        string dateStr = af.GetPartialTimeString(true);
        string hourMinSec = dateStr.Substring(11, 8);
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
            if (meshAExists == false && meshAExists == false)
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
    private  void ReportSavedMeshes(int numUpdatedA, int numUpdatedB, int numCreatedA, int numCreatedB, int railsCountA, int railsCountB)
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
    }*/

    //---------------------------------------------------------------------
    Vector3 SnapHandles(Vector3 inVec, float val)
    {

        Vector3 snapVec = Vector3.zero;
        snapVec.y = inVec.y;

        snapVec.x = Mathf.Round(inVec.x / val) * val;
        snapVec.z = Mathf.Round(inVec.z / val) * val;

        return snapVec;
    }
    //---------------------------------------------------------------------
    // move the folder handles out of the way of the real moveable handles
    void RepositionFolderHandles(Vector3 clickPoint)
    {
        Vector3 pos = clickPoint;
        if (af.clickPoints.Count > 0)
        {
            //pos = (af.clickPoints[0] + af.clickPoints[af.clickPoints.Count-1])/2;
            pos = af.clickPoints[0];
        }
        af.gameObject.transform.position = pos + new Vector3(0, 4, 0);
        /*af.fencesFolder.transform.position = pos + new Vector3(0,4,0);
        af.postsFolder.transform.position = pos + new Vector3(0,4,0);
        af.railsFolder.transform.position = pos + new Vector3(0,4,0);
        af.subpostsFolder.transform.position = pos + new Vector3(0,4,0);*/
    }
    //------------------------------------------
    public Vector3 EnforceVectorNonZero(Vector3 inVec, float nonZeroValue)
    {
        if (inVec.x == 0) inVec.x = nonZeroValue;
        if (inVec.y == 0) inVec.y = nonZeroValue;
        if (inVec.z == 0) inVec.z = nonZeroValue;
        return inVec;
    }
    public Vector3 EnforceVectorMinimums(Vector3 inVec, Vector3 mins)
    {
        if (inVec.x < mins.x) inVec.x = mins.x;
        if (inVec.y < mins.y) inVec.y = mins.y;
        if (inVec.z < mins.z) inVec.z = mins.z;
        return inVec;
    }
    public Vector3 EnforceVectorMaximums(Vector3 inVec, Vector3 maxs)
    {
        if (inVec.x > maxs.x) inVec.x = maxs.x;
        if (inVec.y > maxs.y) inVec.y = maxs.y;
        if (inVec.z > maxs.z) inVec.z = maxs.z;
        return inVec;
    }
    public Vector3 EnforceVectorMinMax(Vector3 inVec, float globalMin, float globalMax)
    {
        if (inVec.x < globalMin) inVec.x = globalMin;
        if (inVec.y < globalMin) inVec.y = globalMin;
        if (inVec.z < globalMin) inVec.z = globalMin;

        if (inVec.x > globalMax) inVec.x = globalMax;
        if (inVec.y > globalMax) inVec.y = globalMax;
        if (inVec.z > globalMax) inVec.z = globalMax;
        return inVec;
    }
    public Vector3 EnforceVectorMinMax(Vector3 inVec, Vector3 mins, Vector3 maxs)
    {
        if (inVec.x < mins.x) inVec.x = mins.x;
        if (inVec.y < mins.y) inVec.y = mins.y;
        if (inVec.z < mins.z) inVec.z = mins.z;

        if (inVec.x > maxs.x) inVec.x = maxs.x;
        if (inVec.y > maxs.y) inVec.y = maxs.y;
        if (inVec.z > maxs.z) inVec.z = maxs.z;
        return inVec;
    }
}








