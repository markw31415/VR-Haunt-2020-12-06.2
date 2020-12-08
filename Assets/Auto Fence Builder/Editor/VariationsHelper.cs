#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class VariationsHelper
{
    AutoFenceCreator af;
    AutoFenceEditor ed;
    bool showSourcePrefabs = true;

    AutoFenceCreator.LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet; // to save a lot of typing
    AutoFenceCreator.LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    AutoFenceCreator.LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    AutoFenceCreator.LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;
    AutoFenceCreator.LayerSet kExtraLayer = AutoFenceCreator.LayerSet.postLayerSet;

    public VariationsHelper(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
    }
    //---------------
    public void CheckPresetValidateVariants(ScriptablePresetAFWB preset)
    {
        if (preset.railAVariants.Count > AutoFenceCreator.kNumRailVariations || preset.railBVariants.Count > AutoFenceCreator.kNumRailVariations)
        {
            Debug.Log("Too many rail variants in " + preset.name + "   A: " + preset.railAVariants.Count + "   B: " + preset.railBVariants.Count);
            int endIndex = AutoFenceCreator.kNumRailVariations;
            preset.railAVariants.RemoveRange(endIndex, preset.railAVariants.Count - endIndex);
            preset.railBVariants.RemoveRange(endIndex, preset.railBVariants.Count - endIndex);
        }
    }
    //----------------------
    public void ShowVariationSourcesHelp()
    {
        if (GUILayout.Button("?", GUILayout.Width(25)))
        {
            ed.showVarHelp = !ed.showVarHelp;
        }
        if (ed.showVarHelp)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("• Use the 4 slots in 'Choose Source Prefabs' as possible variation prefabs", ed.grayHelpStyle);
            EditorGUILayout.LabelField("• Use the 'Assign All Sources' shortcut buttons to quickly fill all 4 slots", ed.grayHelpStyle);
            EditorGUILayout.LabelField("• Each of these 4 prefabs will now be available to assign to any step in the sequence", ed.grayHelpStyle);
            EditorGUILayout.LabelField("• 'Auto Update Sequence' will automatically assign these to seq steps when changed", ed.grayHelpStyle);
            EditorGUILayout.LabelField("• Note: If you have less than 5 steps in your sequence, you will only see the first 1, 2, or 3 variations",
                ed.grayHelpStyle);
            EditorGUILayout.Separator();
            if (GUILayout.Button("Close Help", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                ed.showVarHelp = false;
            }

            EditorGUILayout.Separator();
            GUILayout.EndVertical();
        }
    }
    //----------------------
    public void ShowSequencerHelp()
    {
        if (GUILayout.Button("?", GUILayout.Width(25)))
        {
            ed.showSeqHelp = !ed.showSeqHelp;
        }
        if (ed.showSeqHelp)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("  Here you can set up a sequence to modify each section of the fence", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  Each 'step' is a collection of settings that will modify a particular section of fence", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  The steps will Loop, according to how many steps you choose", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  For example: If you had 3 steps, and your fence was 8 sections long, then the sequence would be:", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  [Step1, Step2, Step3]    [Step1, Step2, Step3]    [Step1, Step2]", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  (See the example preset: Variation Templates/3 Step Sequence Example)", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  Each step can be assigned a prefab using the buttons; a transform, and a choice of orientation flips", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  In the example preset, Step 1 = VariationTest1      Step 2 = VariationTest2      Step 1 = ABasicConcrete", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  Additionally, Step2 is Inverted, and Step 3 is taller with  its Size.Y set to 1.5", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  ");
            EditorGUILayout.LabelField("  You can use the 'Optimise' and 'Randomise' Options to quickly fill all steps", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  ");
            EditorGUILayout.LabelField("  Prefab choices for each section of fence can also be set by Control-Right-Clicking on them in Scene View", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  Here you have a choice of:'Free': ", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  • These will be single independent changes and appear in the 'Singles' list below", ed.grayHelpStyle);
            EditorGUILayout.LabelField("    They will override the sequence at that position. This is useful if you want to modify only one section", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  Or 'Put in Sequence Step': ", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  • These will be placed at the correct sequence step, and looped with the sequence", ed.grayHelpStyle);
            EditorGUILayout.LabelField("  • In both cases, the ONLY available prefabs are the ones assigned in 'Choose Source Prefabs' ", ed.grayHelpStyle);
            EditorGUILayout.Separator();
            if (GUILayout.Button("Close Help", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                ed.showSeqHelp = false;
            }

            EditorGUILayout.Separator();
            GUILayout.EndVertical();
        }
    }
    //-------------------------
    public void SyncControlsDisplayFromVariant(FenceVariant variant, int variantIndex, AutoFenceCreator.LayerSet layerSet, bool fillNullsWithMain = true)
    {
        int i = variantIndex;

        if (i == 0)
        {
            af.railAMenuIndex = ed.ConvertRailPrefabIndexToMenuIndex(af.currentRailAType);
        }
        if (layerSet == kRailALayer)
        {
            if (i == 0)
            {
                af.currentRailAType = af.FindRailPrefabIndexByName(variant.go.name);
                af.railAMenuIndex = ed.ConvertRailPrefabIndexToMenuIndex(af.currentRailAType);
            }
            if (i > 0 && variant.go == null && fillNullsWithMain)
                variant.go = af.railAVariants[0].go;

            af.varPrefabIndexRailA[i] = af.FindRailPrefabIndexByName(variant.go.name);
            af.varMenuIndexRailA[i] = ed.ConvertRailPrefabIndexToMenuIndex(af.varPrefabIndexRailA[i]);

            af.varRailAPositionOffset[i] = variant.positionOffset;
            af.varRailARotation[i] = variant.rotation;
            af.varRailASize[i] = variant.size;
            af.useRailVarA[i] = variant.enabled;
            af.varRailABackToFront[i] = variant.backToFront;
            af.varRailABackToFrontBools[i] = System.Convert.ToBoolean(variant.backToFront);
            af.varRailAMirrorZ[i] = variant.mirrorZ;
            af.varRailAMirrorZBools[i] = System.Convert.ToBoolean(variant.mirrorZ);
            af.varRailAInvert[i] = variant.invert;
            af.varRailAInvertBools[i] = System.Convert.ToBoolean(variant.invert);
            af.varRailAProbs[i] = variant.probability;
        }
        else if (layerSet == kRailBLayer)
        {
            if (i == 0)
            {
                af.railBMenuIndex = ed.ConvertRailPrefabIndexToMenuIndex(af.currentRailBType);
            }
            if (i > 0 && variant.go == null && fillNullsWithMain)
                variant.go = af.railBVariants[0].go;

            af.varPrefabIndexRailB[i] = af.FindRailPrefabIndexByName(variant.go.name);
            af.varMenuIndexRailB[i] = ed.ConvertRailPrefabIndexToMenuIndex(af.varPrefabIndexRailB[i]);

            af.varRailBPositionOffset[i] = variant.positionOffset;
            af.varRailBRotation[i] = variant.rotation;
            af.varRailBSize[i] = variant.size;
            af.useRailVarB[i] = variant.enabled;
            af.varRailBBackToFront[i] = variant.backToFront;
            af.varRailBBackToFrontBools[i] = System.Convert.ToBoolean(variant.backToFront);
            af.varRailBMirrorZ[i] = variant.mirrorZ;
            af.varRailBMirrorZBools[i] = System.Convert.ToBoolean(variant.mirrorZ);
            af.varRailBInvert[i] = variant.invert;
            af.varRailBInvertBools[i] = System.Convert.ToBoolean(variant.invert);
            af.varRailBProbs[i] = variant.probability;
        }
        else if (layerSet == kPostLayer)
        {
            if (i == 0)
            {
                af.postMenuIndex = ed.ConvertPostPrefabIndexToMenuIndex(af.currentPostType);
            }
            if (i > 0 && variant.go == null && fillNullsWithMain)
                variant.go = af.postVariants[0].go;

            af.varPrefabIndexPost[i] = af.FindPostPrefabIndexByName(variant.go.name);
            af.varMenuIndexPost[i] = ed.ConvertPostPrefabIndexToMenuIndex(af.varPrefabIndexPost[i]);

            af.varPostPositionOffset[i] = variant.positionOffset;
            af.varPostRotation[i] = variant.rotation;
            af.varPostSize[i] = variant.size;
            af.usePostVar[i] = variant.enabled;
            af.varPostProbs[i] = variant.probability;
        }
        else if (layerSet == kSubpostLayer)
        {
            if (i == 0)
            {
                af.subpostMenuIndex = ed.ConvertPostPrefabIndexToMenuIndex(af.currentSubpostType);
            }
            if (i > 0 && variant.go == null && fillNullsWithMain)
                variant.go = af.subpostVariants[0].go;

            af.varPrefabIndexSubpost[i] = af.FindPostPrefabIndexByName(variant.go.name);
            af.varMenuIndexSubpost[i] = ed.ConvertPostPrefabIndexToMenuIndex(af.varPrefabIndexSubpost[i]);

            af.varSubpostPositionOffset[i] = variant.positionOffset;
            af.varSubpostRotation[i] = variant.rotation;
            af.varSubpostSize[i] = variant.size;
            af.useSubpostVar[i] = variant.enabled;
            //af.varSubpostProbs[i] = variant.probability;
        }
    }
    //-------------------------
    // The Global preset changed
    public void SyncControlsAfterPresetChange()
    {
        int numberOfActiveGOsA = 0, numberOfActiveGOsB = 0;

        for (int i = 0; i < AutoFenceCreator.kNumRailVariations; i++)
        {
            SyncControlsDisplayFromVariant(af.railAVariants[i], i, kRailALayer);
            if (af.railAVariants[i].enabled == true && af.railAVariants[i].go != null)
                numberOfActiveGOsA += 1;
            af.railADisplayVariationGOs[i] = af.railAVariants[i].go;

            SyncControlsDisplayFromVariant(af.railBVariants[i], i, kRailBLayer);
            if (af.railBVariants[i].enabled == true && af.railBVariants[i].go != null)
                numberOfActiveGOsB += 1;
            af.railBDisplayVariationGOs[i] = af.railBVariants[i].go;
        }

        for (int i = 0; i < AutoFenceCreator.kNumPostVariations; i++)
        {
            SyncControlsDisplayFromVariant(af.postVariants[i], i, kPostLayer);
            af.postDisplayVariationGOs[i] = af.postVariants[i].go;
        
            SyncControlsDisplayFromVariant(af.subpostVariants[i], i, kSubpostLayer);
            af.subpostDisplayVariationGOs[i] = af.subpostVariants[i].go;
        }
    }
    //-------------------------
    // a Main rail/post/extra prefab was changed from the selction menus
    public void SyncControlsAfterComponentChange()
    {
        af.railADisplayVariationGOs[0] = af.railAVariants[0].go = af.railPrefabs[af.currentRailAType];
        af.railBDisplayVariationGOs[0] = af.railBVariants[0].go = af.railPrefabs[af.currentRailBType];
        af.postDisplayVariationGOs[0] = af.postVariants[0].go = af.postPrefabs[af.currentPostType];
        af.subpostDisplayVariationGOs[0] = af.subpostVariants[0].go = af.postPrefabs[af.currentSubpostType];
    }
    //------------------------------------
    // Necessary after switching on/off variant objects to ensure the sequence is not referencing an obsolete go
    public void CheckSequencerHasValidObjects(AutoFenceCreator.LayerSet railsSet)
    {
        int numSeqSteps = af.railASeqInfo.numSteps, currSeqStep = ed.currSeqAStepIndex;
        List<SeqVariant> userSequence = af.userSequenceRailA;
        List<FenceVariant> variants = af.railAVariants;
        int nonNullCount = af.nonNullRailAVariants.Count;
        int[] seqVarIndex = af.seqRailAVarIndex;
        SeqVariant currSeqStepVariant = af.userSequenceRailA[currSeqStep];

        if (railsSet == kRailBLayer)
        {
            numSeqSteps = af.railBSeqInfo.numSteps ;
            currSeqStep = ed.currSeqBStepIndex;
            userSequence = af.userSequenceRailB;
            variants = af.railBVariants;
            nonNullCount = af.nonNullRailBVariants.Count;
            seqVarIndex = af.seqRailBVarIndex;
            currSeqStepVariant = af.userSequenceRailB[currSeqStep];
        }

        for (int i = 0; i < numSeqSteps; i++)
        {
            SeqVariant thisStep = userSequence[i];
            int seqStepObjIndex = thisStep.objIndex;

            if (seqStepObjIndex >= variants.Count || seqVarIndex[i] >= variants.Count)
            {
                thisStep.objIndex = 0;
                userSequence[i] = thisStep;
                seqVarIndex[i] = 0;
            }
        }

        if (railsSet == kRailALayer)
            af.seqRailAVarIndex = seqVarIndex;
        else if (railsSet == kRailBLayer)
            af.seqRailBVarIndex = seqVarIndex;


        currSeqStepVariant.objIndex = seqVarIndex[ed.currSeqAStepIndex];
        currSeqStepVariant.go = variants[seqVarIndex[currSeqStep]].go;
        userSequence[currSeqStep] = currSeqStepVariant;
    }
    //-----------
    public void InitializeUserSequence(AutoFenceCreator.LayerSet railsSet)
    {
        if (railsSet == kRailALayer)
        {
            //Initialize the first 1-5 with any available RailVariants
            for (int i = 0; i < af.railAVariants.Count; i++)
            {
                af.userSequenceRailA[i] = new SeqVariant(i, af.railAVariants[i]);
            }
            // then initialize the rest with the base
            for (int i = af.railAVariants.Count; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                af.userSequenceRailA[i] = new SeqVariant(0, af.railAVariants[0]);
            }
            SyncSequencerControlsDisplayFromSeqVariant(railsSet, af.userSequenceRailA[0], 0);

        }

        if (railsSet == kRailBLayer)
        {
            for (int i = 0; i < af.railBVariants.Count; i++)
            {
                af.userSequenceRailB[i] = new SeqVariant(i, af.railBVariants[i]);
            }
            for (int i = af.railBVariants.Count; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                af.userSequenceRailB[i] = new SeqVariant(0, af.railBVariants[0]);
            }
            SyncSequencerControlsDisplayFromSeqVariant(railsSet, af.userSequenceRailB[0], 0);
        }

    }
    //-------------------------
    // If the sequence step is changed programmatically, we need to update the interface contrlols
    public void SyncSequencerControlsDisplayAllSteps(AutoFenceCreator.LayerSet layerSet)
    {
        if (layerSet == kRailALayer)
        {
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                SyncSequencerControlsDisplayFromSeqVariant(layerSet, af.userSequenceRailA[i], i);
            }
        }
        if (layerSet == kRailBLayer)
        {
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                SyncSequencerControlsDisplayFromSeqVariant(layerSet, af.userSequenceRailB[i], i);
            }
        }
        if (layerSet == kPostLayer)
        {
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                SyncSequencerControlsDisplayFromSeqVariant(layerSet, af.userSequencePost[i], i);
            }
        }
        if (layerSet == kSubpostLayer)
        {
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                SyncSequencerControlsDisplayFromSeqVariant(layerSet, af.userSequenceSubpost[i], i);
            }
        }
    }
    //-------------------------
    // If the sequence step is changed programmatically, we need to update the interface contrlol
    public void SyncSequencerControlsDisplayFromSeqVariant(AutoFenceCreator.LayerSet layerSet, SeqVariant seqVariant, int stepIndex)
    {
        if (layerSet == kRailALayer)
        {
            af.seqRailAStepEnabled[stepIndex] = seqVariant.stepEnabled;
            /*af.seqAX[stepIndex] = seqVariant.backToFront;
            af.seqAZ[stepIndex] = seqVariant.mirrorZ;
            af.seqAInvert180[stepIndex] = seqVariant.invert;*/

            af.seqRailAVarIndex[stepIndex] = seqVariant.objIndex;

            af.seqRailASize[stepIndex] = seqVariant.size;
            af.seqRailAOffset[stepIndex] = seqVariant.pos;
            af.seqRailARotate[stepIndex] = seqVariant.rot;
        }
        else if (layerSet == kRailBLayer)
        {
            af.seqRailBStepEnabled[stepIndex] = seqVariant.stepEnabled;
            /*af.seqBX[stepIndex] = seqVariant.backToFront;
            af.seqBZ[stepIndex] = seqVariant.mirrorZ;
            af.seqBInvert180[stepIndex] = seqVariant.invert;*/

            af.seqRailBVarIndex[stepIndex] = seqVariant.objIndex;
            af.seqRailBSize[stepIndex] = seqVariant.size;
            af.seqRailBOffset[stepIndex] = seqVariant.pos;
            af.seqRailBRotate[stepIndex] = seqVariant.rot;
        }
        else if (layerSet == kPostLayer)
        {
            af.seqPostStepEnabled[stepIndex] = seqVariant.stepEnabled;
            af.seqPostX[stepIndex] = seqVariant.backToFront;
            af.seqPostZ[stepIndex] = seqVariant.mirrorZ;
            af.seqPostInvert180[stepIndex] = seqVariant.invert;

            af.seqPostVarIndex[stepIndex] = seqVariant.objIndex;
            af.seqPostSize[stepIndex] = seqVariant.size;
            af.seqPostOffset[stepIndex] = seqVariant.pos;
            af.seqPostRotate[stepIndex] = seqVariant.rot;
        }
        else if (layerSet == kSubpostLayer)
        {
            af.seqSubpostStepEnabled[stepIndex] = seqVariant.stepEnabled;
            af.seqSubpostX[stepIndex] = seqVariant.backToFront;
            af.seqSubpostZ[stepIndex] = seqVariant.mirrorZ;
            af.seqSubpostInvert180[stepIndex] = seqVariant.invert;

            af.seqSubpostVarIndex[stepIndex] = seqVariant.objIndex;
            af.seqSubpostSize[stepIndex] = seqVariant.size;
            af.seqSubpostOffset[stepIndex] = seqVariant.pos;
            af.seqSubpostRotate[stepIndex] = seqVariant.rot;
        }
    }
    //-----------
    public SeqVariant SetSequenceVariantFromDisplaySettings(AutoFenceCreator.LayerSet layerSet, ref SeqVariant seqStepVariant, int currSeqStep)
    {
        if (layerSet == kPostLayer)
        {
            seqStepVariant.pos = af.seqPostOffset[currSeqStep];
            seqStepVariant.size = af.seqPostSize[currSeqStep];
            seqStepVariant.rot = af.seqPostRotate[currSeqStep];
            seqStepVariant.go = af.postVariants[seqStepVariant.objIndex].go;
            af.userSequencePost[currSeqStep] = seqStepVariant;
        }
        else if (layerSet == kSubpostLayer)
        {
            seqStepVariant.pos = af.seqSubpostOffset[currSeqStep];
            seqStepVariant.size = af.seqSubpostSize[currSeqStep];
            seqStepVariant.rot = af.seqSubpostRotate[currSeqStep];
            seqStepVariant.go = af.subpostVariants[seqStepVariant.objIndex].go;
            af.userSequenceSubpost[currSeqStep] = seqStepVariant;
        }
        else if (layerSet == kRailALayer)
        {
            seqStepVariant.pos = af.seqRailAOffset[currSeqStep];
            seqStepVariant.size = af.seqRailASize[currSeqStep];
            seqStepVariant.rot = af.seqRailARotate[currSeqStep];

            seqStepVariant.objIndex = af.seqRailAVarIndex[currSeqStep];
            seqStepVariant.go = af.railAVariants[seqStepVariant.objIndex ].go;
            
            /*seqStepVariant.backToFront = af.seqAX[currSeqStep];
            seqStepVariant.mirrorZ = af.seqAZ[currSeqStep];
            seqStepVariant.invert = af.seqAInvert180[currSeqStep];*/
            
            af.userSequenceRailA[currSeqStep] = seqStepVariant;
        }
        else if (layerSet == kRailBLayer)
        {
            seqStepVariant.pos = af.seqRailBOffset[currSeqStep];
            seqStepVariant.size = af.seqRailBSize[currSeqStep];
            seqStepVariant.rot = af.seqRailBRotate[currSeqStep];
            
            seqStepVariant.objIndex = af.seqRailBVarIndex[currSeqStep];
            seqStepVariant.go = af.railBVariants[seqStepVariant.objIndex].go;
            
            /*seqStepVariant.backToFront = af.seqBX[currSeqStep];
            seqStepVariant.mirrorZ = af.seqBZ[currSeqStep];
            seqStepVariant.invert = af.seqBInvert180[currSeqStep];*/
            
            af.userSequenceRailB[currSeqStep] = seqStepVariant;
        }

        return seqStepVariant;
    }
    //-----------
    public SeqVariant SetPostSequenceData(ref SeqVariant currSeqStepVariant, int currSeqStep)
    {
        currSeqStepVariant.pos = af.seqPostOffset[currSeqStep];
        currSeqStepVariant.size = af.seqPostSize[currSeqStep];
        currSeqStepVariant.rot = af.seqPostRotate[currSeqStep];
        currSeqStepVariant.objIndex = af.seqPostVarIndex[currSeqStep];
        currSeqStepVariant.go = af.postVariants[currSeqStepVariant.objIndex].go;
        af.userSequenceRailA[currSeqStep] = currSeqStepVariant;
        return currSeqStepVariant;
    }
    //------------------
    public void FillEmptyVariantsWithMain()
    {
        int prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.railPrefab, af.railAVariants[0].go.name);
        int mainMenuIndexA = ed.ConvertRailPrefabIndexToMenuIndex(prefabIndex);

        prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.railPrefab, af.railBVariants[0].go.name);
        int mainMenuIndexB = ed.ConvertRailPrefabIndexToMenuIndex(prefabIndex);

        prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.postPrefab, af.postVariants[0].go.name);
        int mainMenuIndexPost = ed.ConvertPostPrefabIndexToMenuIndex(prefabIndex);
        
        prefabIndex = af.FindPrefabIndexByName(AutoFenceCreator.FencePrefabType.postPrefab, af.subpostVariants[0].go.name);
        int mainMenuIndexSubpost = ed.ConvertPostPrefabIndexToMenuIndex(prefabIndex);

        for (int i = 1; i < AutoFenceCreator.kNumRailVariations; i++)
        {
            if (af.railAVariants[i].go == null)
            {
                af.railAVariants[i].go = af.railADisplayVariationGOs[i] = af.railAVariants[0].go;
                af.varMenuIndexRailA[i] = mainMenuIndexA;
            }
            if (af.railBVariants[i].go == null)
            {
                af.railBVariants[i].go = af.railBDisplayVariationGOs[i] = af.railBVariants[0].go;
                af.varMenuIndexRailA[i] = mainMenuIndexB;
            }
        }
        for (int i = 1; i < AutoFenceCreator.kNumPostVariations; i++)
        {
            if (af.postVariants[i].go == null)
            {
                af.postVariants[i].go = af.postDisplayVariationGOs[i] = af.postVariants[0].go;
                af.varMenuIndexPost[i] = mainMenuIndexPost;
            }
            if (af.subpostVariants[i].go == null)
            {
                af.subpostVariants[i].go = af.subpostDisplayVariationGOs[i] = af.subpostVariants[0].go;
                af.varMenuIndexSubpost[i] = mainMenuIndexSubpost;
            }
        }
    }
    //-------------
    /*public void CopyOptimalToUserSequence(AutoFenceCreator.LayerSet railSet, bool createAlso = true)
    {
        int optCount = af.optimalSequenceRailA.Count;
        if (railSet == kRailALayer)
        {
            if(createAlso)
                CreateOptimalSequenceA();
            if (optCount > AutoFenceCreator.kMaxNumSeqSteps)
                optCount = AutoFenceCreator.kMaxNumSeqSteps;
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                int optIndex = i % optCount;
                af.userSequenceRailA[i] = new SeqVariant(af.optimalSequenceRailA[optIndex]);
            }
        }
        else if (railSet == kRailBLayer)
        {
            if(createAlso)
                CreateOptimalSequenceB();
            optCount = af.optimalSequenceRailB.Count;
            if (optCount > AutoFenceCreator.kMaxNumSeqSteps)
                optCount = AutoFenceCreator.kMaxNumSeqSteps;
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                int optIndex = i % optCount;
                af.userSequenceRailB[i] = new SeqVariant(af.optimalSequenceRailB[optIndex]);
            }
        }
    }*/
    //-------------
    // will loop source if fewer than dest
    public void CopySequence(List<SeqVariant> source, List<SeqVariant> dest)
    {
        int sourceCount = source.Count;

        int length = AutoFenceCreator.kMaxNumSeqSteps;
        
        if (dest.Count < AutoFenceCreator.kMaxNumSeqSteps)
        {
            Debug.LogWarning("dest.Count < kMaxNumSeqSteps in CopySequence()");
        }

        if (sourceCount > length)
            sourceCount = length;
        for (int i = 0; i < length; i++)
        {
            int loopIndex = i % sourceCount;
            dest[i] = new SeqVariant(source[loopIndex]);
        }
    }
    //-------------
    /*public List<SeqVariant> void CreateOptimalSequence(AutoFenceCreator.LayerSet layerSet,  List<FenceVariant> variantList, 
                                                    bool allowBackToFront, bool allowMirrorZ, bool allowInvert)
    {
        List<SeqVariant> optimalSeq = af.optimalSequenceRailA;
        if(layerSet == kRailBLayer)
            optimalSeq = af.optimalSequenceRailB;
        if(layerSet == kPostLayer)
            optimalSeq = af.optimalSequencePost;
        if(layerSet == kSubpostLayer)
            optimalSeq = af.optimalSequenceSubpost;
     

        optimalSeq = FenceVariations.CreateOptimalSequence(variantList, allowBackToFront, allowMirrorZ, allowInvert);

        return optimalSeq;
    }*/
    //-------------
    public List<SeqVariant> CreateOptimalSequenceA()
    {
        af.optimalSequenceRailA = FenceVariations.CreateOptimalSequence(af.nonNullRailAVariants,
        System.Convert.ToBoolean(af.varRailABackToFront[0]),
        System.Convert.ToBoolean(af.varRailAMirrorZ[0]),
        System.Convert.ToBoolean(af.varRailAInvert[0]));
        return af.optimalSequenceRailA;
    }
    //-------------
    public List<SeqVariant> CreateOptimalSequenceB()
    {
        af.optimalSequenceRailB = FenceVariations.CreateOptimalSequence(af.nonNullRailBVariants,
        System.Convert.ToBoolean(af.varRailBBackToFront[0]),
        System.Convert.ToBoolean(af.varRailBMirrorZ[0]),
        System.Convert.ToBoolean(af.varRailBInvert[0]));
        return af.optimalSequenceRailB;
    }
    /*public List<SeqVariant> CreateOptimalPost()
    {
        af.optimalSequencePost = FenceVariations.CreateOptimalSequence(af.nonNullPostVariants,
            System.Convert.ToBoolean(af.varRailBBackToFront[0]),
            System.Convert.ToBoolean(af.varRailBMirrorZ[0]),
            System.Convert.ToBoolean(af.varRailBInvert[0]));
        return af.optimalSequencePost;
    }
    public List<SeqVariant> CreateOptimalSubpost()
    {
        af.optimalSequenceSubpost = FenceVariations.CreateOptimalSequence(af.nonNullSubpostVariants,
            System.Convert.ToBoolean(af.varRailBBackToFront[0]),
            System.Convert.ToBoolean(af.varRailBMirrorZ[0]),
            System.Convert.ToBoolean(af.varRailBInvert[0]));
        return af.optimalSequenceSubpost;
    }*/
    //-------------
    // Fuill the user sequence with random values
    public void RandomizeSequence(AutoFenceCreator.LayerSet railSet, int startStep = 0, int endStep = AutoFenceCreator.kMaxNumSeqSteps)
    {
        SeqVariant thisSeqStep = new SeqVariant();
        List<FenceVariant> variantList = af.nonNullRailAVariants;
        //List<SeqVariant> seq = af.userSequenceRailA; 
        //int numGos = 0;
        /*if(railSet == kRailALayer) {
            seq = af.userSequenceRailA;
            variantList = af.nonNullRailAVariants;
         }*/
        if (railSet == kRailBLayer)
        {
            //seq = af.userSequenceRailB;
            variantList = af.nonNullRailBVariants;
        }

        for (int i = startStep; i < endStep; i++)
        {
            int goIndex = UnityEngine.Random.Range(0, variantList.Count);
            //thisSeqStep.go = variantList[goIndex].go;
            thisSeqStep.objIndex = goIndex;
            thisSeqStep.backToFront = (UnityEngine.Random.value > 0.5f);
            thisSeqStep.mirrorZ = (UnityEngine.Random.value > 0.5f);
            thisSeqStep.invert = (UnityEngine.Random.value > 0.5f);

            thisSeqStep.size = Vector3.one;
            //thisSeqStep.size.x = UnityEngine.Random.Range(0.5f, 1.5f);

            if (railSet == kRailALayer)
                af.userSequenceRailA[i] = new SeqVariant(thisSeqStep);
            else if (railSet == kRailBLayer)
                af.userSequenceRailB[i] = new SeqVariant(thisSeqStep);
        }

    }
    //-----------------------------------------
    public void CheckValidPrefabPresetIndices()
    {
        if (af.currentRailAType >= af.railPrefabs.Count)
            af.currentRailAType = 0;
        if (af.currentRailBType >= af.railPrefabs.Count)
            af.currentRailBType = 0;
        if (af.currentPostType >= af.postPrefabs.Count)
            af.currentPostType = 0;
        if (af.currentSubpostType >= af.postPrefabs.Count)
            af.currentSubpostType = 0;
        if (af.currentExtraType >= af.extraPrefabs.Count)
            af.currentExtraType = 0;
    }
    //-----------------------------------------
    // Whenever a base part changes, ensure that it's added in the variants list
    public void SetBaseVariantObjects()
    {

        //if (af.railAVariants.Count > AutoFenceCreator.kNumRailVariations || af.nonNullRailAVariants.Count > AutoFenceCreator.kNumRailVariations)
        //Debug.Log("Too Many Rail Variants in SetBaseVariantObjects() 1   " + af.railAVariants.Count + "   " + af.nonNullRailAVariants.Count);


        CheckValidPrefabPresetIndices();

        //if (af.railAVariants.Count > AutoFenceCreator.kNumRailVariations || af.nonNullRailAVariants.Count > AutoFenceCreator.kNumRailVariations)
        //Debug.Log("Too Many Rail Variants in SetBaseVariantObjects() 2   " + af.railAVariants.Count + "   " + af.nonNullRailAVariants.Count);

        //-- always set the [0] to be the main rail
        af.railADisplayVariationGOs[0] = af.railAVariants[0].go = af.railPrefabs[af.currentRailAType];

        if (af.nonNullRailAVariants.Count == 0)
            af.nonNullRailAVariants.Add(af.railAVariants[0]);
        else if (af.nonNullRailAVariants[0] == null)
            af.nonNullRailAVariants[0] = af.railAVariants[0];

        af.railBDisplayVariationGOs[0] = af.railBVariants[0].go = af.railPrefabs[af.currentRailBType];
        if (af.nonNullRailBVariants.Count == 0)
            af.nonNullRailBVariants.Add(af.railBVariants[0]);
        else if (af.nonNullRailBVariants[0] == null)
            af.nonNullRailBVariants[0] = af.railBVariants[0];


        af.postDisplayVariationGOs[0] = af.postVariants[0].go = af.postPrefabs[af.currentPostType];
        af.subpostDisplayVariationGOs[0] = af.subpostVariants[0].go = af.postPrefabs[af.currentSubpostType];
    }

    //------------------------
    //-- If Variations are not being used, still need to check the base[0] is populated
    public void CheckNonNull()
    {
        if (af.nonNullRailAVariants[0] == null)
            af.nonNullRailAVariants[0] = new FenceVariant(af.railPrefabs[af.currentRailAType]);
        if (af.nonNullRailBVariants[0] == null)
            af.nonNullRailBVariants[0] = new FenceVariant(af.railPrefabs[af.currentRailBType]);
    }
    //------------------------
    //-- Ensure that all the  railAVariants are populated
    public void CheckVariationGOs()
    {
        //---- Rails A ----
        if (af.railAVariants.Count < AutoFenceCreator.kNumRailVariations)
        {
            af.railAVariants.Clear();
            af.railAVariants.AddRange(new FenceVariant[AutoFenceCreator.kNumRailVariations]);
            af.railAVariants[0] = new FenceVariant(af.railPrefabs[af.currentRailAType]);
        }
        else
        {
            for (int i = 0; i < af.railAVariants.Count; i++)
            {
                if (af.railAVariants[i] == null)
                {
                    af.railAVariants[i] = new FenceVariant(af.railADisplayVariationGOs[i]);
                    af.railAVariants[i].go = af.railAVariants[0].go;
                    af.railAVariants[i].enabled = af.useRailVarA[i];
                    af.railAVariants[i].probability = af.varRailAProbs[i];
                    af.railAVariants[i].positionOffset = af.varRailAPositionOffset[i];
                    af.railAVariants[i].size = af.varRailASize[i];
                    af.railAVariants[i].rotation = af.varRailARotation[i];
                }
                else if (af.railAVariants[i].go == null)
                {
                    af.railAVariants[i].go = af.railAVariants[0].go;
                }
            }
        }
        af.nonNullRailAVariants = af.CreateUsedVariantsList(kRailALayer);
        for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
        {
            if (af.userSequenceRailA[i] == null)
            {
                SeqVariant seqVarA = new SeqVariant();
                seqVarA.pos = af.seqRailAOffset[i];
                seqVarA.size = af.seqRailASize[i];
                seqVarA.rot = af.seqRailARotate[i];
                /*seqVarA.mirrorZ = af.seqAZ[i];
                seqVarA.backToFront = af.seqAX[i];
                seqVarA.invert = af.seqAInvert180[i];*/
                seqVarA.objIndex = af.seqRailAVarIndex[i];
                af.userSequenceRailA[i] = seqVarA;
            }

        }

        //---- Rails B ----
        if (af.railBVariants.Count < AutoFenceCreator.kNumRailVariations)
        {
            af.railBVariants.Clear();
            af.railBVariants.AddRange(new FenceVariant[AutoFenceCreator.kNumRailVariations]);
            af.railBVariants[0] = new FenceVariant(af.railPrefabs[af.currentRailBType]);
        }
        else
        {
            for (int i = 0; i < af.railBVariants.Count; i++)
            {
                if (af.railBVariants[i] == null)
                {
                    af.railBVariants[i] = new FenceVariant(af.railBDisplayVariationGOs[i]);
                    af.railBVariants[i].go = af.railBVariants[0].go;
                    af.railBVariants[i].enabled = af.useRailVarB[i];
                    af.railBVariants[i].probability = af.varRailBProbs[i];
                    af.railBVariants[i].positionOffset = af.varRailBPositionOffset[i];
                    af.railBVariants[i].size = af.varRailBSize[i];
                    af.railBVariants[i].rotation = af.varRailBRotation[i];
                }
                else if (af.railBVariants[i].go == null)
                {
                    af.railBVariants[i].go = af.railBVariants[0].go;
                }
            }
        }
        af.nonNullRailBVariants = af.CreateUsedVariantsList(kRailBLayer);
        for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
        {
            if (af.userSequenceRailB[i] == null)
            {
                SeqVariant seqVarB = new SeqVariant();
                seqVarB.pos = af.seqRailBOffset[i];
                seqVarB.size = af.seqRailBSize[i];
                seqVarB.rot = af.seqRailBRotate[i];
                /*seqVarB.mirrorZ = af.seqBZ[i];
                seqVarB.backToFront = af.seqBX[i];
                seqVarB.invert = af.seqBInvert180[i];*/
                seqVarB.objIndex = af.seqRailBVarIndex[i];
                af.userSequenceRailB[i] = seqVarB;
            }
        }


        //---- Posts ----
        if (af.postVariants.Count < AutoFenceCreator.kNumPostVariations)
        {
            af.postVariants.Clear();
            af.postVariants.AddRange(new FenceVariant[AutoFenceCreator.kNumPostVariations]);
            af.postVariants[0] = new FenceVariant(af.postPrefabs[af.currentPostType]);
        }
        else
        {
            if (af.postVariants[0].go == null)
                af.postVariants[0].go = af.postPrefabs[af.currentPostType];
            for (int i = 0; i < af.postVariants.Count; i++)
            {
                if (af.postVariants[i] == null)
                {
                    af.postVariants[i] = new FenceVariant(af.postDisplayVariationGOs[i]);
                    af.postVariants[i].go = af.postVariants[0].go;
                    af.postVariants[i].enabled = af.usePostVar[i];
                    af.postVariants[i].probability = af.varPostProbs[i];
                    af.postVariants[i].positionOffset = af.varPostPositionOffset[i];
                    af.postVariants[i].size = af.varPostSize[i];
                    af.postVariants[i].rotation = af.varPostRotation[i];
                }
                else if (af.postVariants[i].go == null)
                {
                    af.postVariants[i].go = af.postVariants[0].go;
                }
            }
        }
        //af.nonNullPostVariants = af.CreateUsedVariantsList(kPostLayer);
        for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
        {
            if (af.userSequencePost[i] == null)
            {
                SeqVariant seqVarPost = new SeqVariant();
                seqVarPost.pos = af.seqPostOffset[i];
                seqVarPost.size = af.seqPostSize[i];
                seqVarPost.rot = af.seqPostRotate[i];
                seqVarPost.objIndex = af.seqPostVarIndex[i];
                af.userSequencePost[i] = seqVarPost;
            }
        }
        //---- Subosts ----
        if (af.subpostVariants.Count < AutoFenceCreator.kNumPostVariations)
        {
            af.subpostVariants.Clear();
            af.subpostVariants.AddRange(new FenceVariant[AutoFenceCreator.kNumPostVariations]);
            af.subpostVariants[0] = new FenceVariant(af.postPrefabs[af.currentSubpostType]);
        }
        else
        {
            if (af.subpostVariants[0].go == null)
                af.subpostVariants[0].go = af.postPrefabs[af.currentPostType];
            for (int i = 0; i < af.subpostVariants.Count; i++)
            {
                if (af.subpostVariants[i] == null)
                {
                    af.subpostVariants[i] = new FenceVariant(af.subpostDisplayVariationGOs[i]);
                    af.subpostVariants[i].go = af.subpostVariants[0].go;
                    af.subpostVariants[i].enabled = af.useSubpostVar[i];
                    //af.subpostVariants[i].probability = af.varSubpostProbs[i];
                    af.subpostVariants[i].positionOffset = af.varSubpostPositionOffset[i];
                    af.subpostVariants[i].size = af.varSubpostSize[i];
                    af.subpostVariants[i].rotation = af.varSubpostRotation[i];
                }
                else if (af.subpostVariants[i].go == null)
                {
                    af.subpostVariants[i].go = af.subpostVariants[0].go;
                }
            }
        }
        //af.nonNullPostVariants = af.CreateUsedVariantsList(kPostLayer);
        for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
        {
            if (af.userSequenceSubpost[i] == null)
            {
                SeqVariant seqVarSubpost = new SeqVariant();
                seqVarSubpost.pos = af.seqSubpostOffset[i];
                seqVarSubpost.size = af.seqSubpostSize[i];
                seqVarSubpost.rot = af.seqSubpostRotate[i];
                seqVarSubpost.objIndex = af.seqSubpostVarIndex[i];
                af.userSequenceSubpost[i] = seqVarSubpost;
            }
        }

    }

}
