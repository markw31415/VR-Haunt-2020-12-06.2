using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;


[CreateAssetMenu(fileName = "preset", menuName = "AutoFence/Preset", order = 1)]
public class ScriptablePresetAFWB : ScriptableObject
{
    //public string objectName = "New ScriptablePresetAFWB";
    public string categoryName = "";


    //=========  Posts  =========
    public string postName = "ConcreteOldSquare_Post";
    public bool usePosts = true;
    public Vector3 postSize = Vector3.one, postRotation = Vector3.zero;
    public Vector3 mainPostSizeBoost = Vector3.one;
    //-- Posts Randomization
    public bool allowPostHeightVariation = false;
    public float minPostHeightVar = 0.95f, maxPostHeightVar = 1.05f;
    public bool allowRandPostRotationVariation = false;
    public Vector3 postRandRotationAmount = Vector3.zero;
    public float postQuantizeRotAmount = 90;
    public bool allowQuantizedRandomPostRotation = false;
    public float chanceOfMissingPost = 0;
    //-- Posts Variation
    public bool usePostVariations = false;
    public List<FenceVariant> postVariants = new List<FenceVariant>();
    public List<SeqVariant> userSequencePost = SeqVariant.CreateInitialisedSeqVariantList(AutoFenceCreator.kMaxNumSeqSteps);
    public int numUserSeqStepsPost = 5;
    public RandomRecords postRandRec;

    //=========  Rails A  =========
    public bool useMainRails = true;
    public string railAName = "Wall-High_Brick_Panel_Rail";
    public int numStackedRailsA = 1;
    public float railASpread = 0.6f;
    public Vector3 railAPositionOffset = Vector3.zero, railASize = Vector3.one, railARotation = Vector3.zero;
    public bool railAKeepGrounded = false;
    public bool rotateFromBaseRailA = false;
    public bool overlapAtCorners;
    public bool autoHideBuriedRails;
    public AutoFenceCreator.FenceSlopeMode slopeModeRailA;
    //-- Rails A Randomization
    public int railARandomScope = 2; // 2 = Main & Vars
    public bool allowRailAHeightVariation = false;
    public float minRailAHeightVar = 0.57f, maxRailAHeightVar = 1.47f;
    public bool allowRandRailARotationVariation = false;
    public Vector3 railARandRotationAmount = Vector3.zero;
    public float chanceOfMissingRailA = 0;
    //-- Rails A Variation
    public bool useRailAVariations = false;
    public bool scaleVariationHeightToMainHeightA = false;
    public bool allowIndependentSubmeshVariationA = false;
    public VariationMode variationModeRailA = VariationMode.sequenced;
    public List<FenceVariant> railAVariants = new List<FenceVariant>();
    public List<SeqVariant> userSequenceRailA = SeqVariant.CreateInitialisedSeqVariantList(AutoFenceCreator.kMaxNumSeqSteps);
    public int numUserSeqStepsRailA = 5;
    public List<SeqVariant> optimalSequenceRailA = SeqVariant.CreateInitialisedSeqVariantList(16);
    //-- Rail A Random Seeds --
    public RandomRecords railARandRec;



    //========  Rails B  =========
    public string railBName = "ABasicConcrete_Panel_Rail";
    public bool useSecondaryRails = false;
    public int numStackedRailsB = 1;
    public float railBSpread = 0.6f;
    public Vector3 railBPositionOffset = Vector3.zero, railBSize = Vector3.one, railBRotation = Vector3.zero;
    public bool railBKeepGrounded = false;
    public bool rotateFromBaseRailB = false;
    public AutoFenceCreator.FenceSlopeMode slopeModeRailB;
    //-- Rails B Randomization
    public int railBRandomScope = 2;
    public bool allowRailBHeightVariation = false;
    public float minRailBHeightVar = 0.95f, maxRailBHeightVar = 1.05f;
    public bool allowRandRailBRotationVariation = false;
    public Vector3 railBRandRotationAmount = Vector3.zero;
    public float chanceOfMissingRailB = 0;
    //-- Rails B Variation
    public bool useRailBVariations = false;
    public bool scaleVariationHeightToMainHeightB = false;
    public bool allowIndependentSubmeshVariationB = false;
    public VariationMode variationModeRailB = VariationMode.sequenced;
    public List<FenceVariant> railBVariants = new List<FenceVariant>();
    public List<SeqVariant> userSequenceRailB = SeqVariant.CreateInitialisedSeqVariantList(AutoFenceCreator.kMaxNumSeqSteps);
    public int numUserSeqStepsRailB = 5;
    public List<SeqVariant> optimalSequenceRailB = SeqVariant.CreateInitialisedSeqVariantList(16);
    public RandomRecords railBRandRec;

    //=========  Subs  =========
    public string subPostName = "ConcreteOldSquare_Post";
    public bool useSubPosts = false;
    public int subsFixedOrProportionalSpacing = 0;
    public float subSpacing;
    public Vector3 subPositionOffset = Vector3.zero;
    public Vector3 subSize = Vector3.one;
    public Vector3 subRotation = Vector3.zero;
    public bool forceSubsToGroundContour;
    public bool useWave, useSubJoiners;
    public float frequency;
    public float amplitude;
    public float wavePosition;
    public float subPostSpread = 0;
    //-- SubPosts Randomization
    public bool allowSubPostHeightVariation = false;
    public float minSubPostHeightVar = 0.95f, maxSubPostHeightVar = 1.05f;
    public bool allowRandSubPostRotationVariation = false;
    public Vector3 subPostRandRotationAmount = Vector3.zero;
    public float subPostQuantizeRotAmount = 90;
    public bool allowQuantizedRandomSubPostRotation = false;
    public float chanceOfMissingSubPost = 0;
    //-- Posts Variation
    public bool useSubpostVariations = false;
    public List<FenceVariant> subpostVariants = new List<FenceVariant>();
    public List<SeqVariant> userSequenceSubpost = SeqVariant.CreateInitialisedSeqVariantList(AutoFenceCreator.kMaxNumSeqSteps);
    public int numUserSeqStepsSubpost = 5;
    public RandomRecords subpostRandRec;

    //=========  Extras  =========
    public bool useExtraGameObject = false;
    public string extraName = "ConcreteOldSquare_Post";
    public bool relativeMovement;
    public bool relativeScaling;
    public Vector3 extraPositionOffset, extraSize = Vector3.one, extraRotation;
    public int extraFrequency; //****
    public bool makeMultiArray;
    public int numExtras;
    public float extrasGap;
    public bool raiseExtraByPostHeight;
    public bool extrasFollowIncline = false;

    //=========  Globals  ========
    //public float fenceHeight = 1;
    public float postHeightOffset = 0;
    public bool interpolate = true;
    public float interPostDist = 3;
    public bool smooth= false;
    public float tension = 0.1f;
    public int roundingDistance = 1;
    public float removeIfLessThanAngle = 1, stripTooClose = 1;

    public Vector3 globalScale = Vector3.one;
    public bool scaleInterpolationAlso = true;
    public bool snapMainPosts = false;
    public float snapSize = 1;
    public bool hideInterpolated = false;
    public bool lerpPostRotationAtCorners = true;
    public float postSpacingVariation = 0;

    //== Random Seeds ==
   //public int rsPostSpacing = 0;
   //public int rsRailARand = 1;


    static List<FenceVariant> CopyVariantList(List<FenceVariant> sourceList, bool copyGo, AutoFenceCreator.FencePrefabType prefabType, AutoFenceCreator af)
    {
        if (sourceList == null)
            return null;
        //Debug.Log(sourceList.Count + "  sourceList.Count\n");
        List<FenceVariant> copyList = FenceVariant.CreateInitialisedFenceVariantList(AutoFenceCreator.kNumRailVariations);
        for (int i = 0; i < sourceList.Count; i++)
        {
            FenceVariant thisVariant = sourceList[i];
            if (thisVariant == null)
            {
                continue;
            }
            if (thisVariant.go != null)
                thisVariant.goName = thisVariant.go.name; // we save the go as a name, rather than a reference to the object
            FenceVariant copy = new FenceVariant(thisVariant, copyGo, prefabType, af);// false means don't copy go, copy goName only
            copyList[i] = copy;
        }
        return copyList;
    }
    //-----------------------
    static List<SeqVariant> CopySequenceList(List<SeqVariant> sourceList, bool copyGo, int enforceMinimumCount)
    {
        if (sourceList == null)
            return null;
        int count = sourceList.Count;
        List<SeqVariant> copyList = SeqVariant.CreateInitialisedSeqVariantList(count);
        for (int i = 0; i < sourceList.Count; i++)
        {
            SeqVariant thisSeqStep = sourceList[i];
            if (thisSeqStep == null)
            {
                continue;
            }
            //if (thisSeqStep.go != null)
                //thisSeqStep.goName = thisSeqStep.go.name; // we save the go as a name, rather than a reference to the object
            SeqVariant copy = new SeqVariant(thisSeqStep);

            if (copyGo == false)
                copy.go = null;
            copyList[i] = copy;
        }
        // Pad with initialized if nedded
        if(count < enforceMinimumCount)
        {
            int padCount = enforceMinimumCount - count;
            for(int i=0; i<padCount; i++)
            {
                copyList.Add(new SeqVariant());
            }
        }
        return copyList;
    }
    //--------------
    public static ScriptablePresetAFWB CreatePresetFromCurrentSettings(string name, string categoryName, AutoFenceCreator af)
    {
        // (used 'preset.' for some easy copy/replace/paste job during preset sysyem conversion)
        //==  Globals  ==
        //preset.fenceHeight = af.fenceHeight;

        ScriptablePresetAFWB preset = ScriptableObject.CreateInstance<ScriptablePresetAFWB>();
        preset.name = name;
        preset.categoryName = categoryName;

        preset.globalScale = af.globalScale;
        preset.interpolate = af.interpolate;
        preset.interPostDist = af.interPostDist;
        preset.smooth = af.smooth;
        preset.tension = af.tension;
        preset.roundingDistance = af.roundingDistance;
        preset.removeIfLessThanAngle = af.removeIfLessThanAngle;
        preset.stripTooClose = af.stripTooClose;
        preset.overlapAtCorners = af.overlapAtCorners;
        preset.autoHideBuriedRails = af.autoHideBuriedRails;
        preset.slopeModeRailA = af.slopeModeRailA;
        preset.slopeModeRailB = af.slopeModeRailB;
        preset.scaleInterpolationAlso = af.scaleInterpolationAlso;
        preset.snapMainPosts = af.snapMainPosts;
        preset.snapSize = af.snapSize;
        preset.hideInterpolated = af.hideInterpolated;
        preset.lerpPostRotationAtCorners = af.lerpPostRotationAtCorners;
        preset.postSpacingVariation = af.postSpacingVariation;

        //==  Posts  ==
        preset.usePosts = af.usePosts;
        preset.postHeightOffset = af.postHeightOffset;
        preset.postName = af.postPrefabs[af.currentPostType].name;
        preset.postSize = af.postSize;
        preset.postRotation = af.postRotation;
        preset.mainPostSizeBoost = af.mainPostSizeBoost;
        //-- Posts Randomization
        preset.allowPostHeightVariation = af.allowPostHeightVariation;
        preset.minPostHeightVar = af.minPostHeightVar;
        preset.maxPostHeightVar = af.maxPostHeightVar;
        preset.allowRandPostRotationVariation = af.allowRandPostRotationVariation;
        preset.railARandRotationAmount = af.railARandRotationAmount;
        preset.postQuantizeRotAmount = af.postQuantizeRotAmount;
        preset.allowQuantizedRandomPostRotation = af.allowQuantizedRandomPostRotation;
        preset.chanceOfMissingPost = af.chanceOfMissingPost;
        //-- Posts Variation
        preset.usePostVariations = af.usePostVariations;
        preset.postVariants = CopyVariantList(af.postVariants, false, AutoFenceCreator.FencePrefabType.postPrefab, af);// false means don't copy go, copy goName only
        preset.userSequencePost = CopySequenceList(af.userSequencePost, false, AutoFenceCreator.kMaxNumSeqSteps);
        preset.numUserSeqStepsPost = af.postSeqInfo.numSteps;
        preset.postRandRec = af.postRandRec;

        //==  Rails A  ==
        preset.useMainRails = af.useRailsA;
        preset.railAName = af.railPrefabs[af.currentRailAType].name;
        preset.numStackedRailsA = af.numStackedRailsA;
        preset.railASpread = af.railASpread;
        preset.railAPositionOffset = af.railAPositionOffset;
        preset.railASize = af.railASize;
        preset.railARotation = af.railARotation;
        preset.railAKeepGrounded = af.railAKeepGrounded;
        preset.rotateFromBaseRailA = af.rotateFromBaseRailA;
        //-- Rail A Randomization
        preset.railARandomScope = af.railARandomScope;
        preset.allowRailAHeightVariation = af.allowRailAHeightVariation;
        preset.minRailAHeightVar = af.minRailAHeightVar;
        preset.maxRailAHeightVar = af.maxRailAHeightVar;
        preset.railARandRotationAmount = af.railARandRotationAmount;
        preset.chanceOfMissingRailA = af.chanceOfMissingRailA;
        //-- Rail A Variation
        preset.scaleVariationHeightToMainHeightA = af.scaleVariationHeightToMainHeightA;
        preset.allowIndependentSubmeshVariationA = af.allowIndependentSubmeshVariationA;
        preset.useRailAVariations = af.useRailAVariations;
        preset.variationModeRailA = af.variationModeRailA;
        preset.railAVariants = CopyVariantList(af.railAVariants, false, AutoFenceCreator.FencePrefabType.railPrefab, af);// false means don't copy go, copy goName only
        preset.userSequenceRailA = CopySequenceList(af.userSequenceRailA, false, AutoFenceCreator.kMaxNumSeqSteps);
        preset.numUserSeqStepsRailA = af.railASeqInfo.numSteps;
        preset.optimalSequenceRailA = CopySequenceList(af.optimalSequenceRailA, false, 1);
        preset.railARandRec = af.railARandRec;

        //==  Rails B  ==
        preset.useSecondaryRails = af.useRailsB;
        preset.railBName = af.railPrefabs[af.currentRailBType].name;
        preset.numStackedRailsB = af.numStackedRailsB;
        preset.railBSpread = af.railBSpread;
        preset.railBPositionOffset = af.railBPositionOffset;
        preset.railBSize = af.railBSize;
        preset.railBRotation = af.railBRotation;
        preset.railBKeepGrounded = af.railBKeepGrounded;
        preset.rotateFromBaseRailB = af.rotateFromBaseRailB;
        //-- Rail B Randomization
        preset.railBRandomScope = af.railBRandomScope;
        preset.allowRailBHeightVariation = af.allowRailBHeightVariation;
        preset.minRailBHeightVar = af.minRailBHeightVar;
        preset.maxRailBHeightVar = af.maxRailBHeightVar;
        preset.railBRandRotationAmount = af.railBRandRotationAmount;
        preset.chanceOfMissingRailB = af.chanceOfMissingRailB;
        //-- Rail B Variation
        preset.scaleVariationHeightToMainHeightB = af.scaleVariationHeightToMainHeightB;
        preset.allowIndependentSubmeshVariationB = af.allowIndependentSubmeshVariationB;
        preset.useRailBVariations = af.useRailBVariations;
        preset.variationModeRailB = af.variationModeRailB;
        preset.railBVariants = CopyVariantList(af.railBVariants, false, AutoFenceCreator.FencePrefabType.railPrefab, af);
        preset.userSequenceRailB = CopySequenceList(af.userSequenceRailB, false, AutoFenceCreator.kMaxNumSeqSteps);
        preset.numUserSeqStepsRailB = af.railBSeqInfo.numSteps;
        preset.optimalSequenceRailB = CopySequenceList(af.optimalSequenceRailB, false, 1);
        preset.railBRandRec = af.railBRandRec;

        //==  Extras  ==
        preset.useExtraGameObject = af.useExtraGameObject;
        preset.extraName = af.extraPrefabs[af.currentExtraType].name;
        preset.relativeMovement = af.relativeMovement;
        preset.relativeScaling = af.relativeScaling;
        preset.extraPositionOffset = af.extraPositionOffset;
        preset.extraSize = af.extraSize;
        preset.extraRotation = af.extraRotation;
        preset.extraFrequency = af.extraFrequency; //****
        preset.makeMultiArray = af.makeMultiArray;
        preset.numExtras = af.numExtras;
        preset.extrasGap = af.extrasGap;
        preset.raiseExtraByPostHeight = af.raiseExtraByPostHeight;
        preset.extrasFollowIncline = af.extrasFollowIncline;

        //==  SubPosts  ==
        preset.useSubPosts = af.useSubposts;
        preset.subPostName = af.postPrefabs[af.currentSubpostType].name;
        preset.subsFixedOrProportionalSpacing = af.subsFixedOrProportionalSpacing;
        preset.subSpacing = af.subSpacing;
        preset.subPositionOffset = af.subpostPositionOffset;
        preset.subSize = af.subpostSize;
        preset.subRotation = af.subpostRotation;
        preset.forceSubsToGroundContour = af.forceSubsToGroundContour;
        preset.useWave = af.useWave;
        preset.useSubJoiners = af.useSubJoiners;
        preset.frequency = af.frequency;
        preset.amplitude = af.amplitude;
        preset.wavePosition = af.wavePosition;
        preset.subPostSpread = af.subPostSpread;
        //-- Subposts Variation
        preset.useSubpostVariations = af.useSubpostVariations;
        preset.subpostVariants = CopyVariantList(af.subpostVariants, false, AutoFenceCreator.FencePrefabType.postPrefab, af);// false means don't copy go, copy goName only
        preset.userSequenceSubpost = CopySequenceList(af.userSequenceSubpost, false, AutoFenceCreator.kMaxNumSeqSteps);
        preset.numUserSeqStepsSubpost = af.subpostSeqInfo.numSteps;
        //preset.subpostRandRec = af.subpostRandRec;
        
        
        //-- SubPosts Randomization
        preset.allowSubPostHeightVariation = af.allowSubpostHeightVariation;
        preset.minSubPostHeightVar = af.minSubpostHeightVar;
        preset.allowRandSubPostRotationVariation = af.allowRandSubpostRotationVariation;
        preset.subPostRandRotationAmount = af.subpostRandRotationAmount;
        preset.subPostQuantizeRotAmount = af.subpostQuantizeRotAmount;
        preset.allowQuantizedRandomSubPostRotation = af.allowQuantizedRandomSubpostRotation;
        preset.chanceOfMissingSubPost = af.chanceOfMissingSubpost;

        return preset;
    }
    //---------------------
    public static string FindCategoryForPreset(ScriptablePresetAFWB preset, string name, string menuCategorySetting, AutoFenceCreator af)
    {

        string categoryFolderName = "";
        int dirPositionInString = name.IndexOf('/');

        // If the preset has a category assigned 
        if (preset.categoryName != "")
        {
            // If there's a '/', just take name part and strip the rest
            if (dirPositionInString != -1)
                preset.name = name.Substring(dirPositionInString + 1);
            //else
                //presetName = name;
        }
        // If there' no category assigned
        else if (preset.categoryName == "")
        {
            //Does the name have a preset prefix eg. "Brick/MyWall"
            if (dirPositionInString != -1)
            {
                categoryFolderName = name.Remove(dirPositionInString);
                preset.categoryName = categoryFolderName = categoryFolderName.Trim();
                preset.name  = name.Substring(dirPositionInString + 1);

            }
            else if (menuCategorySetting == "Auto")
            //If not try to auto-assign
            {
                categoryFolderName = af.GetPresetCategoryByName(name);
                preset.categoryName = categoryFolderName = categoryFolderName.Trim();
               //presetName = name;
            }
            else if (menuCategorySetting != "Auto")
            {
                if (menuCategorySetting != "")
                    preset.categoryName = categoryFolderName = menuCategorySetting;
                else 
                    preset.categoryName = categoryFolderName = af.GetPresetCategoryByName(name);
                //-- If somehow it's still failed, assign to 'Other'
                if(preset.categoryName == "")
                    preset.categoryName = "Other";
                preset.categoryName = categoryFolderName = categoryFolderName.Trim();
            }
        }
        return preset.categoryName;

    }
    //--------------
    public static bool SaveScriptablePreset(AutoFenceCreator af, ScriptablePresetAFWB preset, string savePath, bool saveBackupAlso, bool overwrite)
    {
        if (File.Exists(savePath))
        {
            if (overwrite == true)
            {
                AssetDatabase.CreateAsset(preset, savePath);
            }
            else
                return false;
        }
        else
        {
            try {
                AssetDatabase.CreateAsset(preset, savePath);
            }
            catch (Exception e) {
                Debug.LogWarning("Problem in  SaveScriptablePreset() " + e.ToString() + " \n");
                return false;
            }
        }
        Debug.Log("AFWB Saved " + preset.categoryName + "/" + preset.name + " to " + savePath + "\n");

        // Save to backup also
        if (saveBackupAlso)
        {
            var clone = Instantiate(preset);
            string folderPath = af.currAutoFenceBuilderDirLocation + "/Editor/PresetsAFWB_Backups" + "/" + preset.categoryName;
            bool folderExists = AssetDatabase.IsValidFolder(folderPath);
            if (folderExists == false)
            {
                string guid = AssetDatabase.CreateFolder(af.currAutoFenceBuilderDirLocation + "/Editor", "PresetsAFWB_Backups");
                folderPath = AssetDatabase.GUIDToAssetPath(guid);
            }
            AssetDatabase.CreateAsset(clone, folderPath + "/" + preset.name + "-backup" + ".asset");
        }
        AssetDatabase.SaveAssets();
        return true;
        
    }
    //-----------
    public static string CreateSaveString(AutoFenceCreator af, string name, string categoryFolderName)
    {
        if(categoryFolderName == "")
        {
            Debug.LogWarning("Empty Folder Name for " + name + "   Not saving");
            return "";
        }
        string presetsFilePath = af.currAutoFenceBuilderDirLocation + "/Presets_AFWB";

        string presetName = name;

        if (presetName.StartsWith("_"))
            presetName = presetName.Substring(1);

        name = presetName;
        bool folderExists = false;
        //Check if a folder exists for this category
        string guid, folderPath = presetsFilePath + "/" + categoryFolderName;
        folderExists = AssetDatabase.IsValidFolder(folderPath);
        if (folderExists == false)
        {
            guid = AssetDatabase.CreateFolder(presetsFilePath, categoryFolderName);
            folderPath = AssetDatabase.GUIDToAssetPath(guid);
        }
        string savePath = folderPath + "/" + presetName + ".asset";

        return savePath;
    }
    //-----------
    public static string CreateSaveStringForFinished(AutoFenceCreator af, string name, string categoryFolderName)
    {
        if(categoryFolderName == "")
        {
            Debug.LogWarning("Empty Folder Name for " + name + "   Not saving");
            return "";
        }
        string presetsFilePath = af.currAutoFenceBuilderDirLocation + "/Editor/PresetsForFinishedFences";

        string presetName = name;

        if (presetName.StartsWith("_"))
            presetName = presetName.Substring(1);

        name = presetName;
        bool folderExists = false;
        //Check if a folder exists for this category
        string guid, folderPath = presetsFilePath + "/" + categoryFolderName;
        folderExists = AssetDatabase.IsValidFolder(folderPath);
        if (folderExists == false)
        {
            guid = AssetDatabase.CreateFolder(presetsFilePath, categoryFolderName);
            folderPath = AssetDatabase.GUIDToAssetPath(guid);
        }
        string savePath = folderPath + "/" + presetName + ".asset";

        return savePath;
    }
    //===========================================
    public void BuildFromPreset(AutoFenceCreator af)
    {
        //==  Globals  ==
        af.globalScale = globalScale;
        af.postHeightOffset = postHeightOffset;
        af.interpolate = interpolate;
        af.interPostDist = interPostDist;
        af.smooth = smooth;
        af.tension = tension;
        af.roundingDistance = roundingDistance;
        af.removeIfLessThanAngle = removeIfLessThanAngle;
        af.stripTooClose = stripTooClose;
        af.overlapAtCorners = overlapAtCorners;
        af.autoHideBuriedRails = autoHideBuriedRails;
        af.slopeModeRailA = slopeModeRailA;
        af.slopeModeRailB = slopeModeRailB;
        af.scaleInterpolationAlso = scaleInterpolationAlso;
        af.snapMainPosts = snapMainPosts;
        af.snapSize = snapSize;
        af.hideInterpolated = hideInterpolated;
        af.lerpPostRotationAtCorners = lerpPostRotationAtCorners;
        af.postSpacingVariation = postSpacingVariation;

        //==  Posts  ==
        af.usePosts = usePosts;
        af.postHeightOffset = postHeightOffset;
        af.currentPostType =  af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.postPrefab, postName);
        af.postSize = postSize;
        af.postRotation = postRotation;
        af.mainPostSizeBoost = mainPostSizeBoost;
        //-- Posts Randomization
        af.allowPostHeightVariation = allowPostHeightVariation;
        af.minPostHeightVar = minPostHeightVar;
        af.maxPostHeightVar = maxPostHeightVar;
        af.allowRandPostRotationVariation = allowRandPostRotationVariation;
        af.railARandRotationAmount = railARandRotationAmount;
        af.postQuantizeRotAmount = postQuantizeRotAmount;
        af.allowQuantizedRandomPostRotation = allowQuantizedRandomPostRotation;
        af.chanceOfMissingPost = chanceOfMissingPost;
        //-- Posts Variation
        af.usePostVariations = usePostVariations;
        af.postVariants = CopyVariantList(postVariants, true, AutoFenceCreator.FencePrefabType.postPrefab, af);// true means copy go, or reinstate from name
        af.userSequencePost = CopySequenceList(userSequencePost, false, AutoFenceCreator.kMaxNumSeqSteps); ;
        af.postSeqInfo.numSteps = numUserSeqStepsPost;
        af.postRandRec = postRandRec;

        //==  Rails A  ==
        af.useRailsA = useMainRails;
        af.currentRailAType = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.railPrefab, railAName);
        af.SetRailAType(af.currentRailAType, false, false);
        af.numStackedRailsA = numStackedRailsA;
        af.railASpread = railASpread;
        af.railAPositionOffset = railAPositionOffset;
        af.railASize = railASize;
        af.railARotation = railARotation;
        af.railAKeepGrounded = railAKeepGrounded;
        //-- Rail A Randomization
        af.railARandomScope = railARandomScope;
        af.allowRailAHeightVariation = allowRailAHeightVariation;
        af.minRailAHeightVar = minRailAHeightVar;
        if (af.minRailAHeightVar < af.minRailHeightLimit)
            af.minRailAHeightVar =0.97f;
        af.maxRailAHeightVar = maxRailAHeightVar;
        if (af.maxRailAHeightVar <= af.minRailHeightLimit)
            af.maxRailAHeightVar = 1.03f;
        af.railARandRotationAmount = railARandRotationAmount;
        af.chanceOfMissingRailA = chanceOfMissingRailA;
        //-- Rail A Variation
        af.useRailAVariations = useRailAVariations;
        af.scaleVariationHeightToMainHeightA = scaleVariationHeightToMainHeightA;
        af.allowIndependentSubmeshVariationA = allowIndependentSubmeshVariationA;
        af.variationModeRailA = variationModeRailA;
        af.railAVariants = CopyVariantList(railAVariants, true, AutoFenceCreator.FencePrefabType.railPrefab, af);// true means copy go, or reinstate from name
        af.userSequenceRailA = CopySequenceList(userSequenceRailA, false, AutoFenceCreator.kMaxNumSeqSteps);
        af.railASeqInfo.numSteps = numUserSeqStepsRailA;
        af.optimalSequenceRailA = CopySequenceList(optimalSequenceRailA, false,1);
        af.railARandRec = railARandRec;

        //==  Rails B  ==
        af.useRailsB = useSecondaryRails;
        af.currentRailBType = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.railPrefab, railBName);
        af.numStackedRailsB = numStackedRailsB;
        af.railBSpread = railBSpread;
        af.railBPositionOffset = railBPositionOffset;
        af.railBSize = railBSize;
        af.railBRotation = railBRotation;
        af.railBKeepGrounded = railBKeepGrounded;
        //-- Rail B Randomization
        af.railBRandomScope = railBRandomScope;
        af.allowRailBHeightVariation = allowRailBHeightVariation;
        af.minRailBHeightVar = minRailBHeightVar;
        if (af.minRailBHeightVar < af.minRailHeightLimit)
            af.minRailBHeightVar = 0.97f;
        af.maxRailBHeightVar = maxRailBHeightVar;
        if (af.maxRailBHeightVar <= af.minRailHeightLimit)
            af.maxRailBHeightVar = 1.03f;
        //-- Rail B Variation
        af.useRailBVariations = useRailBVariations;
        af.scaleVariationHeightToMainHeightB = scaleVariationHeightToMainHeightB;
        af.allowIndependentSubmeshVariationB = allowIndependentSubmeshVariationB;
        af.variationModeRailB = variationModeRailB;
        af.railBVariants = CopyVariantList(railBVariants, true, AutoFenceCreator.FencePrefabType.railPrefab, af);
        af.userSequenceRailB = CopySequenceList(userSequenceRailB, false, AutoFenceCreator.kMaxNumSeqSteps);
        af.railBSeqInfo.numSteps = numUserSeqStepsRailB;
        af.optimalSequenceRailB = CopySequenceList(optimalSequenceRailB, false,1);
        af.railBRandRec = railBRandRec;

        //==  Extras  ==
        af.useExtraGameObject = useExtraGameObject;
        af.currentExtraType = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.extraPrefab, extraName);
        af.relativeMovement = relativeMovement;
        af.relativeScaling = relativeScaling;
        af.extraPositionOffset = extraPositionOffset;
        af.extraSize = extraSize;
        af.extraRotation = extraRotation;
        af.extraFrequency = extraFrequency; //****
        af.makeMultiArray = makeMultiArray;
        af.numExtras = numExtras;
        af.extrasGap = extrasGap;
        af.raiseExtraByPostHeight = raiseExtraByPostHeight;
        af.extrasFollowIncline = extrasFollowIncline;

        //==  SubPosts  ==
        af.useSubposts = useSubPosts;
        af.currentSubpostType = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.postPrefab, subPostName);
        af.subsFixedOrProportionalSpacing = subsFixedOrProportionalSpacing;
        af.subSpacing = subSpacing;
        af.subpostPositionOffset = subPositionOffset;
        af.subpostSize = subSize;
        af.subpostRotation = subRotation;
        af.forceSubsToGroundContour = forceSubsToGroundContour;
        af.useWave = useWave;
        af.useSubJoiners = useSubJoiners;
        af.frequency = frequency;
        af.amplitude = amplitude;
        af.wavePosition = wavePosition;
        af.subPostSpread = subPostSpread;
        //-- Subposts Variation
        af.useSubpostVariations = useSubpostVariations;
        af.subpostVariants = CopyVariantList(subpostVariants, true, AutoFenceCreator.FencePrefabType.postPrefab, af);// true means copy go, or reinstate from name
        af.userSequenceSubpost = CopySequenceList(userSequenceSubpost, false, AutoFenceCreator.kMaxNumSeqSteps); ;
        af.subpostSeqInfo.numSteps = numUserSeqStepsSubpost;
        //af.subpostRandRec = subpostRandRec;
        //-- SubPosts Randomization
        af.allowSubpostHeightVariation = allowSubPostHeightVariation;
        af.minSubpostHeightVar = minSubPostHeightVar;
        af.allowRandSubpostRotationVariation = allowRandSubPostRotationVariation;
        af.subpostRandRotationAmount = subPostRandRotationAmount;
        af.subpostQuantizeRotAmount = subPostQuantizeRotAmount;
        af.allowQuantizedRandomSubpostRotation = allowQuantizedRandomSubPostRotation;
        af.chanceOfMissingSubpost = chanceOfMissingSubPost;

        //== Random Seeds ==
        //af.rsPostSpacing = rsPostSpacing;
        //af.rsRailARand = rsRailARand;
    }
    //---------------------------
    public static List<ScriptablePresetAFWB> ReadPresetFiles(AutoFenceCreator af, bool clearOld = true)
    {
        List<ScriptablePresetAFWB> presetList = new List<ScriptablePresetAFWB>();
        //string presetFilePath = "Assets/Auto Fence Builder/Presets_AFWB";
        string presetFilePath = af.currPresetsDirLocation;

        bool mainPresetFolderExists = AssetDatabase.IsValidFolder(presetFilePath);
        if (mainPresetFolderExists == false)
        {
            Debug.LogWarning("Main AFWB Presets Folder Missing, Can't load Presets.");
            return null;
        }

        string[] directoryPaths = null, filePaths = null;

        // First look for any loose, uncategorized presets in the top level folder
        filePaths = Directory.GetDirectories(presetFilePath);
        foreach (string filePath in filePaths)
        {
            if (filePath.Contains("Preset") && filePath.EndsWith(".asset"))
            {
                ScriptablePresetAFWB preset = AssetDatabase.LoadAssetAtPath(filePath, typeof(ScriptablePresetAFWB)) as ScriptablePresetAFWB;
                presetList.Add(preset);
            }
        }

        // Now loop through the category subdirectories
        try
        {
            directoryPaths = Directory.GetDirectories(presetFilePath);
        }
        catch (System.Exception e)
        {
            Debug.Log("Missing Preset Category Folder." + e.ToString());
            return null;
        }
        foreach (string dirPath in directoryPaths)
        {

            filePaths = Directory.GetFiles(dirPath);
            foreach (string filePath in filePaths)
            {
                if (filePath.Contains("Preset") && filePath.EndsWith(".asset"))
                {
                    ScriptablePresetAFWB preset = AssetDatabase.LoadAssetAtPath(filePath, typeof(ScriptablePresetAFWB)) as ScriptablePresetAFWB;

                    //-- check for bad size vectors from ols presets
                    for (int i=0; i<AutoFenceCreator.kMaxNumSeqSteps; i++)
                    {
                        if (preset.userSequenceRailA[i].size == Vector3.zero)
                            preset.userSequenceRailA[i].size = Vector3.one;
                        if (preset.userSequenceRailB[i].size == Vector3.zero)
                            preset.userSequenceRailB[i].size = Vector3.one;
                    }

                    presetList.Add(preset);
                }
            }
        }
        return presetList;
    }
    //---------------------------
    public static List<ScriptablePresetAFWB> ReadZeroContentPresetFiles(AutoFenceCreator af)
    {
        List<ScriptablePresetAFWB> presetList = new List<ScriptablePresetAFWB>();
        string presetFolderPath = af.currAutoFenceBuilderDirLocation + "/ZeroPrefabContentVersion/Test";
        

        bool presetFolderExists = AssetDatabase.IsValidFolder(presetFolderPath);
        if (presetFolderExists == false)
        {
            Debug.LogWarning("AFWB ZeroContent Presets Folder Missing, Can't load Presets.");
            return null;
        }
        
        string presetFilePath = presetFolderPath + "/ZeroContentTest.asset";
        
        ScriptablePresetAFWB preset = AssetDatabase.LoadAssetAtPath(presetFilePath, typeof(ScriptablePresetAFWB)) as ScriptablePresetAFWB;
        presetList.Add(preset);

        return presetList;
    }
}
