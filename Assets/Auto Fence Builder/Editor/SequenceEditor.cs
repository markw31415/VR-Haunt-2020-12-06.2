#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

// Called from AutoFenceEditor, builds controls for the Variations Sequence Blocks
public class SequenceEditor
{
    AutoFenceCreator.LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet;
    AutoFenceCreator.LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    AutoFenceCreator.LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    AutoFenceCreator.LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;

    AutoFenceCreator af;
    AutoFenceEditor ed;
    
    AutoFenceCreator.LayerSet currLayerSet;

    SerializedProperty numUserSeqStepsProperty;
    SerializedProperty seqOffset, seqSize, seqRot;
    SerializedProperty seqInfoProperty;
    private SerializedProperty seqVariantListProperty;
    

    SeqVariant currSeqStepVariant = null;
    List<SeqVariant> seqVariantList = null; // the list of SeqVariant for all seq steps
    //private List<SeqVariant> optimalSeq = null; 
    private List<FenceVariant> sourceVariants = null; // the 5 prefabs that have been assigned as possible variants
    List<GameObject> displayVariationGOs = null;
    public List<GameObject> mainPrefabs = null;
    
        
    bool foundEnabledObject = false;
    bool showSourcePrefabs = true, showOptimiseRandomise = false;
    bool needsRebuild = false;
    bool[] prefabButtonSwitch = new bool[5];
    private SeqInfo seqInfo = null;
    AutoFenceCreator.FencePrefabType prefabType;
    private AutoFenceCreator.LayerSet layerSet;
    private int maxNumVariations = 0;
    private int currentMainPrefabType = 0;
    int mainMenuIndex = 0;
    List<int> varPrefabIndex = null;
    List<int> varMenuIndex = null;
    string displayVariationGOsStr = "";
    string layerWord = "post";
    string randomToUserSeqString = "Quick-Fill Random", optimalToUserSeqString = "Quick-Fill Optimal";
    private List<string> prefabNames = null;

    private string[] autoAssignStrings = new[] {"All Main", "Consecutive", "Same category", "Any"};
    private int autoAssignMenuIndex = 0, autoAssignToolbarValue = 0;
    private bool updateSequenceAlso = false;
    private string[] optimiseRandomiseToolbarStrings= new[] {"Optimise Sequence...", "Randomise Sequence..."};
    private int testdel = 0;

    
    Color lineColor = new Color(0.94f, 0.94f, 0.94f);
    
    public SequenceEditor(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor, AutoFenceCreator.LayerSet inLayerSet)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
        currLayerSet = inLayerSet;
        GrabVariablesForSet();
        SetPrefabButtonSwitchesState(0);
    }
    //------------------
    void GrabVariablesForSet()
    {
        if (currLayerSet == kPostLayer)
        {
            seqInfoProperty = ed.serializedObject.FindProperty("postSeqInfo");
            seqVariantList = af.userSequencePost;
            seqVariantListProperty = ed.serializedObject.FindProperty("userSequencePost");
            layerWord = "post";
            seqOffset = ed.serializedObject.FindProperty("seqPostOffset");
            seqSize = ed.serializedObject.FindProperty("seqPostSize");
            seqRot = ed.serializedObject.FindProperty("seqPostRotate");
            seqInfo = af.postSeqInfo;
            maxNumVariations = AutoFenceCreator.kNumPostVariations;
            prefabType = AutoFenceCreator.FencePrefabType.postPrefab;
            sourceVariants = af.postVariants;
            displayVariationGOsStr = "postDisplayVariationGOs";
            displayVariationGOs = af.postDisplayVariationGOs;
            varPrefabIndex = af.varPrefabIndexPost;
            varMenuIndex = af.varMenuIndexPost;
            prefabNames = af.postNames;
            mainPrefabs = af.postPrefabs;
            currentMainPrefabType = af.currentPostType;
            mainMenuIndex = af.postMenuIndex;

        }
        else if (currLayerSet == kRailALayer)
        {
            seqInfoProperty = ed.serializedObject.FindProperty("railASeqInfo");
            seqVariantList = af.userSequenceRailA;
            seqVariantListProperty = ed.serializedObject.FindProperty("userSequenceRailA");
            layerWord = "railA";
            seqOffset = ed.serializedObject.FindProperty("seqRailAOffset");
            seqSize = ed.serializedObject.FindProperty("seqRailASize");
            seqRot = ed.serializedObject.FindProperty("seqRailARotate");
            seqInfo = af.railASeqInfo;
            maxNumVariations = AutoFenceCreator.kNumRailVariations;
            prefabType = AutoFenceCreator.FencePrefabType.railPrefab;
            sourceVariants = af.railAVariants;
            displayVariationGOsStr = "railADisplayVariationGOs";
            displayVariationGOs = af.railADisplayVariationGOs;
            varPrefabIndex = af.varPrefabIndexRailA;
            varMenuIndex = af.varMenuIndexRailA;
            prefabNames = af.railNames;
            mainPrefabs = af.railPrefabs;
            currentMainPrefabType = af.currentRailAType;
            mainMenuIndex = af.railAMenuIndex;

        }
        else if (currLayerSet == kRailBLayer)
        {
            seqInfoProperty = ed.serializedObject.FindProperty("railBSeqInfo");
            seqVariantList = af.userSequenceRailB;
            seqVariantListProperty = ed.serializedObject.FindProperty("userSequenceRailB");
            layerWord = "railB";
            seqOffset = ed.serializedObject.FindProperty("seqRailBOffset");
            seqSize = ed.serializedObject.FindProperty("seqRailBSize");
            seqRot = ed.serializedObject.FindProperty("seqRailBRotate");
            seqInfo = af.railBSeqInfo;
            maxNumVariations = AutoFenceCreator.kNumRailVariations;
            prefabType = AutoFenceCreator.FencePrefabType.railPrefab;
            sourceVariants = af.railBVariants;
            displayVariationGOsStr = "railBDisplayVariationGOs";
            displayVariationGOs = af.railBDisplayVariationGOs;
            varPrefabIndex = af.varPrefabIndexRailB;
            varMenuIndex = af.varMenuIndexRailB;
            prefabNames = af.railNames;
            mainPrefabs = af.railPrefabs;
            currentMainPrefabType = af.currentRailBType;
            mainMenuIndex = af.railBMenuIndex;
        }
        else if (currLayerSet == kSubpostLayer)
        {
            seqInfoProperty = ed.serializedObject.FindProperty("subpostSeqInfo");
            seqVariantList = af.userSequenceSubpost;
            seqVariantListProperty = ed.serializedObject.FindProperty("userSequenceSubpost");
            layerWord = "subpost";
            seqInfo = af.subpostSeqInfo;
            maxNumVariations = AutoFenceCreator.kNumPostVariations;
            prefabType = AutoFenceCreator.FencePrefabType.postPrefab;
            sourceVariants = af.subpostVariants;
            displayVariationGOsStr = "subpostDisplayVariationGOs";
            displayVariationGOs = af.subpostDisplayVariationGOs;
            prefabNames = af.postNames;
            mainPrefabs = af.postPrefabs;
            currentMainPrefabType = af.currentSubpostType;
            mainMenuIndex = af.subpostMenuIndex;
            
            varPrefabIndex = af.varPrefabIndexSubpost;
            varMenuIndex = af.varMenuIndexSubpost;
            
            seqOffset = ed.serializedObject.FindProperty("seqSubpostOffset");
            seqSize = ed.serializedObject.FindProperty("seqSubpostSize");
            seqRot = ed.serializedObject.FindProperty("seqSubpostRotate");
        }
        
        numUserSeqStepsProperty = seqInfoProperty.FindPropertyRelative("numSteps");
    }
    //--------------------------------
    public void SetupVariations()
    {
        GrabVariablesForSet();
        needsRebuild = false;
        DrawUILine(lineColor, 2);
        GUIStyle greyStyle = new GUIStyle(EditorStyles.label);
        greyStyle.fontStyle = FontStyle.Italic;
        greyStyle.normal.textColor = Color.gray;
        greyStyle.fontSize = 11;
     
        GUILayout.BeginVertical();
        
    
        //===================================
        //      Number Of Steps
        //===================================
        EditorGUILayout.Space();
        EditorGUILayout.Space();
  
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Set Number Of Sequence Steps: ", ed.cyanBoldStyle, GUILayout.Width(180));

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(numUserSeqStepsProperty, new GUIContent(""), GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck())
        {
            
            ed.serializedObject.ApplyModifiedProperties();
            if (seqInfo.numSteps < 2)
                seqInfo.numSteps = 2;
            else if (seqInfo.numSteps > AutoFenceCreator.kMaxNumSeqSteps)
                seqInfo.numSteps = AutoFenceCreator.kMaxNumSeqSteps;

            if(seqInfo.currStepIndex >= seqInfo.numSteps)
            {
                seqInfo.currStepIndex = seqInfo.numSteps - 1;
            }
            ed.seqStrings = new string[seqInfo.numSteps];
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints();
        }

        if (seqInfo.currStepIndex >= seqInfo.numSteps || seqInfo.currStepIndex < 0)
            seqInfo.currStepIndex = 0;
            
        
        EditorGUILayout.LabelField("", GUILayout.Width(2));
        
        EditorGUILayout.LabelField("     Hold 'Control' and hover over section will select step", greyStyle);
        
        ed.varHelper.ShowSequencerHelp();
        if(ed.showSeqHelp == false)
            GUILayout.EndHorizontal();
        
        
        
        //GUILayout.EndHorizontal();

        //===================================
        //      Steps Toolbar
        //===================================
        if (ed.seqStrings.Length != seqInfo.numSteps)
            ed.seqStrings = new string[seqInfo.numSteps];
        for (int i = 0; i < seqInfo.numSteps; i++)
        {
            ed.seqStrings[i] = (i + 1).ToString();
        }
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        int currStepIndex = 0;
        currStepIndex = seqInfo.currStepIndex = GUILayout.Toolbar((int)seqInfo.currStepIndex, ed.seqStrings);
        if (EditorGUI.EndChangeCheck())
        {
            int varIndexForThisStep = seqVariantList[currStepIndex].objIndex;
            SetPrefabButtonSwitchesState(varIndexForThisStep);
        }
        //===================================
        //      Enable / Disable
        //===================================
        EditorGUILayout.Space();
        currSeqStepVariant = seqVariantList[currStepIndex];
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Step " + (currStepIndex + 1).ToString() + ":", ed.cyanBoldStyle, GUILayout.Width(100));

        
        //==  Enable/Disable Step  ==
        EditorGUI.BeginChangeCheck();
        var thisStepEnabled = seqVariantListProperty.GetArrayElementAtIndex(currStepIndex).FindPropertyRelative("stepEnabled");

        if(thisStepEnabled.boolValue == false)
            EditorGUILayout.LabelField(new GUIContent("Enabled", "Show or remove this " + layerWord), ed.warningStyle, GUILayout.Width(48));
        else
            EditorGUILayout.LabelField(new GUIContent("Enabled", "Show or remove this " + layerWord), GUILayout.Width(48));
        EditorGUILayout.PropertyField(thisStepEnabled, new GUIContent(""), GUILayout.Width(55));
        if (EditorGUI.EndChangeCheck())
        {
            needsRebuild = true;
        }
        //== Enable/disable all ==
        bool allEnableStateChanged = false, allEnableState = true;
        if (GUILayout.Button(new GUIContent("Enable All", "Enable All Sequencer Steps"), EditorStyles.miniButton, GUILayout.Width(70)))
        {
            allEnableState = true; allEnableStateChanged = true;
        }
        if (GUILayout.Button(new GUIContent("Disable All", "Disable All Sequencer Steps (Hides all objects on this layer)"), EditorStyles.miniButton, GUILayout.Width(78)))
        {
            allEnableState = false; allEnableStateChanged = true;
        }
        if (allEnableStateChanged)
        {
            for (int i = 0; i < AutoFenceCreator.kMaxNumSeqSteps; i++)
            {
                seqVariantList[i].stepEnabled = allEnableState;
            }
            foundEnabledObject = allEnableState;
            needsRebuild = true;
        }
        GUILayout.EndHorizontal();

        //===================================
        //      Prefab Choice Switches
        //===================================
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        for(int i=0; i<5; i++)
        {
            if (prefabButtonSwitch[i] == true)
                GUI.backgroundColor = ed.switchGreen;
            else
                GUI.backgroundColor = Color.white;
            if (sourceVariants[i].go == null)
                sourceVariants[i].go = sourceVariants[0].go;
            string shortprefabName = sourceVariants[i].go.name;
            shortprefabName = shortprefabName.Substring(0, shortprefabName.Length - 5);
            if(shortprefabName.Length > 20)
                shortprefabName = shortprefabName.Substring(0, 20);

            GUIContent buttonTitleAndTooltip = new GUIContent(shortprefabName,
                "Set the currently selected step [ " + (currStepIndex+1) + " ] to use this Source Prefab:\n" + shortprefabName);

            if (GUILayout.Button(buttonTitleAndTooltip, EditorStyles.miniButton, GUILayout.Width(110)))
            {
                if (prefabButtonSwitch[i] == true)
                    continue; // Ignore if a switch that is on is presses

                SetPrefabButtonSwitchesState(i);
                if (prefabButtonSwitch[i] == true)
                {
                    seqVariantList[currStepIndex].objIndex = i;
                    needsRebuild = true;
                }
            }
        }
        GUILayout.EndHorizontal();
        SetPrefabButtonSwitchesState(seqVariantList[currStepIndex].objIndex); // needed to update when hovering over section
        //---------------------------
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();
        
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        //===================================
        //      Sequence Position Offset
        //===================================
        EditorGUILayout.LabelField(new GUIContent("Pos", "Offset the position of this variation step. Default=0."), ed.smallStyle, GUILayout.Width(30));
        var thisOffset = seqOffset.GetArrayElementAtIndex(currStepIndex);
        EditorGUILayout.PropertyField(thisOffset, new GUIContent(""), GUILayout.Width(127));
        if (GUILayout.Button(new GUIContent("X", "Set Position Offset values to default 0"), GUILayout.Width(8)))
        {
            thisOffset.vector3Value = Vector3.zero;

        }
        //===================================
        //      Sequence Size
        //===================================
        EditorGUILayout.LabelField(new GUIContent("  Size", "Multiply Size of this variation step. Default=1/1/1."), ed.smallStyle, GUILayout.Width(42));
        var thisSize = seqSize.GetArrayElementAtIndex(currStepIndex);
        EditorGUILayout.PropertyField(thisSize, new GUIContent(""), GUILayout.Width(127));
        if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), GUILayout.Width(8)))
        {
            thisSize.vector3Value = Vector3.one;
        }
        //===================================
        //      Sequence Rotation
        //===================================
        EditorGUILayout.LabelField(new GUIContent("   Rot:", "Add Rotation to this variation step. Default=0."), ed.smallStyle, GUILayout.Width(30));
        var thisRotate = seqRot.GetArrayElementAtIndex(currStepIndex);
        EditorGUILayout.PropertyField(thisRotate, new GUIContent(""), GUILayout.Width(127));
        if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), GUILayout.Width(8)))
        {
            thisRotate.vector3Value = Vector3.zero;
        }
        
        GUILayout.EndHorizontal();
        //------------------------
        
        //=======================================
        //     Create the Sequence Step Variant 
        //=======================================
        bool foundEnabledStep = true;
        if (EditorGUI.EndChangeCheck() || needsRebuild == true)
        {
            ed.serializedObject.ApplyModifiedProperties();
            if (currLayerSet == kPostLayer)
            {
                af.seqPostVarIndex[currStepIndex] = seqVariantList[currStepIndex].objIndex;
                af.seqPostSize[currStepIndex] = ed.EnforceVectorMinMax(af.seqPostSize[currStepIndex], -9.99f, 9.99f);
            }
            else if (currLayerSet == kRailALayer)
            {
                af.seqRailAVarIndex[currStepIndex] = seqVariantList[currStepIndex].objIndex;
                af.seqRailASize[currStepIndex] = ed.EnforceVectorMinMax(af.seqRailASize[currStepIndex], -9.99f, 9.99f);
            }
            else if (currLayerSet == kRailBLayer)
            {
                af.seqRailBVarIndex[currStepIndex] = seqVariantList[currStepIndex].objIndex;
                af.seqRailBSize[currStepIndex] = ed.EnforceVectorMinMax(af.seqRailBSize[currStepIndex], -9.99f, 9.99f);
            }
            else if (currLayerSet == kSubpostLayer)
            {
                af.seqSubpostVarIndex[currStepIndex] = seqVariantList[currStepIndex].objIndex;
                af.seqSubpostSize[currStepIndex] = ed.EnforceVectorMinMax(af.seqSubpostSize[currStepIndex], -9.99f, 9.99f);
            }
            
            ed.varHelper.SetSequenceVariantFromDisplaySettings(currLayerSet, ref currSeqStepVariant, currStepIndex);
            //FenceVariations.PrintSeqVariantList(seqVariantList, seqInfo.numSteps);

            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints(currLayerSet);
            foundEnabledStep = false;
            for (int i = 0; i < seqInfo.numSteps; i++)
            {
                if(seqVariantList[i].stepEnabled  == true)
                {
                    foundEnabledStep = true;
                    break;
                }
            }
        }
        EditorGUILayout.Space();
        if (foundEnabledStep == false)
            EditorGUILayout.LabelField("All steps are disabled!", ed.warningStyle, GUILayout.Width(150));

        if (af.userSequenceRailA[1].backToFront)
            testdel++;
        //=================================
        //        Orientation Switches
        //=================================
        HandleOrientationSwitches(currLayerSet, currStepIndex);
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", ed.infoStyle, GUILayout.Width(390));
        EditorGUILayout.LabelField("(Ctrl-Click Shuffle settings also)", ed.infoStyle, GUILayout.Width(170));
        GUILayout.EndHorizontal();
        
        
        
        //=================================
        //        Buttons
        //=================================
        GUILayout.BeginHorizontal();
        // Copy Step
        if (GUILayout.Button(new GUIContent("Copy Step", "This will copy the current seq step for pasting in to another step. "),
        EditorStyles.miniButton, GUILayout.Width(72)))
        {
            ed.copySeqStepVariant = currSeqStepVariant;
        }
        EditorGUI.BeginDisabledGroup(ed.currSeqPostStepVariant == null);
        // Paste Step
        if (GUILayout.Button(new GUIContent("Paste Step", "This will paste the copied step in to the current step. "),
        EditorStyles.miniButton, GUILayout.Width(75)))
        {
            currSeqStepVariant = new SeqVariant(ed.copySeqStepVariant);
            seqVariantList[currStepIndex] = currSeqStepVariant;
            ed.varHelper.SyncSequencerControlsDisplayFromSeqVariant(currLayerSet, currSeqStepVariant, currStepIndex);
            SetPrefabButtonSwitchesState(seqVariantList[currStepIndex].objIndex);
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints();
        }
        EditorGUI.EndDisabledGroup();
        
        // Reset Step
        if (GUILayout.Button(new GUIContent("Reset Step", "This will replace the step with the main Game Object and settings. "),
        EditorStyles.miniButton, GUILayout.Width(75)))
        {
            currSeqStepVariant.InitWithBaseVariant(sourceVariants);
            ed.varHelper.SyncSequencerControlsDisplayFromSeqVariant(currLayerSet, currSeqStepVariant, currStepIndex);
            SetPrefabButtonSwitchesState(0);
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints();
        }

        // Reset All Steps
        if (GUILayout.Button(new GUIContent("Reset All Steps", "This will replace the all steps with the main Game Object and settings."),
        EditorStyles.miniButton, GUILayout.Width(95)))
        {
            for (int s = 0; s < seqInfo.numSteps; s++)
            {
                seqVariantList[s].InitWithBaseVariant(sourceVariants);
                ed.varHelper.SyncSequencerControlsDisplayFromSeqVariant(currLayerSet, seqVariantList[s], s);
            }
            SetPrefabButtonSwitchesState(0);
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints();
        }
        // Assign All
        if (GUILayout.Button(new GUIContent("Assign All", "This will fill all steps with one of the prefabs from the 4 variants + main." +
        "Will not affect position/scale/rotation or orientation settings."), EditorStyles.miniButton, GUILayout.Width(70)))
        {
            ed.ReSeed();
            SeqVariant.AssignObjectIndicesInSequence(seqVariantList, seqInfo.numSteps, sourceVariants);
            SetPrefabButtonSwitchesState(seqVariantList[currStepIndex].objIndex);
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints(currLayerSet);
        }
        // Shuffle
        if (GUILayout.Button(new GUIContent("Shuffle All Variants", "This will replace the all steps with one of the 4 variants. " +
         "It will keep the position/size settings. Press 'Reset All' first if you want these reset before randomizing"),
            EditorStyles.miniButton, GUILayout.Width(165)))
        {
            bool randStepParameterAlso = false;
            Event currentEvent = Event.current;
            if (currentEvent.control)
                randStepParameterAlso = true;
            ed.ReSeed();
            SeqVariant.ShuffleObjectIndicesInSequence(seqVariantList, seqInfo.numSteps, sourceVariants, randStepParameterAlso, currLayerSet);
            SetPrefabButtonSwitchesState(seqVariantList[currStepIndex].objIndex);
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints();
        }
        GUILayout.EndHorizontal();
        
        //==============================
        //      Optimise Randomise
        //==============================
        EditorGUILayout.Space();
        DrawUILine(lineColor, 2, 3);
        EditorGUILayout.Space();
        using (new EditorGUI.IndentLevelScope())
        {
            GUILayout.BeginHorizontal();
            showOptimiseRandomise = EditorGUILayout.Foldout(showOptimiseRandomise,
                new GUIContent("Show Optimise/Randomise Options ", "Options to Optimise or Randomise the sequence"));
            GUILayout.EndHorizontal();

            if(showOptimiseRandomise)
            {
                EditorGUILayout.Space();
                if (currLayerSet == kRailALayer)
                {
                    af.optimiseRandomiseToolbarValueA = GUILayout.Toolbar(af.optimiseRandomiseToolbarValueA,
                        optimiseRandomiseToolbarStrings, ed.smallButtonStyle);
                    if (af.optimiseRandomiseToolbarValueA == 0)
                        SetOptimise(currLayerSet, currStepIndex);
                    else if (af.optimiseRandomiseToolbarValueA == 1)
                        SetRandomise(currLayerSet, currStepIndex);
                }

                if (currLayerSet == kRailBLayer)
                {
                    af.optimiseRandomiseToolbarValueB = GUILayout.Toolbar(af.optimiseRandomiseToolbarValueB,
                        optimiseRandomiseToolbarStrings, ed.smallButtonStyle);
                    if (af.optimiseRandomiseToolbarValueB == 0)
                        SetOptimise(currLayerSet, currStepIndex);
                    else if (af.optimiseRandomiseToolbarValueB == 1)
                        SetRandomise(currLayerSet, currStepIndex);
                }
            }

            DrawUILine(lineColor, 2, 3);
        }


        ReApplyModifiedEditorParameters();
        
        
        
        GUILayout.EndVertical();
    }
    //----------------
    public static void DrawUILine(Color color, int reduceWidth, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
        r.height = thickness;
        r.y+=padding/2;
        r.x-=2;
        //r.width = 500;
        r.width -= reduceWidth;
        EditorGUI.DrawRect(r, color);
        
    }
    //-------------------
    public void SetRandomise(AutoFenceCreator.LayerSet layerSet, int currStepIndex)
    {
        EditorGUILayout.Space();
        GUIStyle redWarningStyle = new GUIStyle(GUI.skin.button);
        redWarningStyle.normal.textColor = new Color(0.75f, .0f, .0f); //dark red
        GUIStyle buttonStyle = ed.defaultButtonStyle;
        if (optimalToUserSeqString == "Sure? This will replace all steps")
            buttonStyle = redWarningStyle;

        GUILayout.BeginVertical();

        
        if (GUILayout.Button(new GUIContent("Randomize", "Randomize the order of the variation prefabs, and their orientation flips"), EditorStyles.miniButton, GUILayout.Width(100)))
        {
            af.SeedRandom(false);
            af.shuffledRailAIndices = FenceVariations.CreateShuffledIndices(sourceVariants, af.allPostsPositions.Count - 1);

            //Debug.Log(af.shuffledRailAIndices.Length);
            //Debug.Log(seqInfo.numSteps);
            for (int i = 0; i < seqInfo.numSteps; i++)
            {
                int a = i % af.shuffledRailAIndices.Length ;
                //Debug.Log(af.shuffledRailAIndices[a]);
                seqVariantList[i].objIndex = af.shuffledRailAIndices[a];
                seqVariantList[i].go = sourceVariants[seqVariantList[i].objIndex].go;

                if (af.allowBackToFrontRailA)
                {
                    seqVariantList[i].backToFront = System.Convert.ToBoolean( UnityEngine.Random.Range(0, 2) );
                }
                if (af.allowMirrorZRailA)
                {
                    seqVariantList[i].mirrorZ = System.Convert.ToBoolean( UnityEngine.Random.Range(0, 2) );
                }
                if (af.allowInvertRailA)
                {
                    seqVariantList[i].invert = System.Convert.ToBoolean( UnityEngine.Random.Range(0, 2) );
                }
            }
            af.ResetPool(layerSet);
            af.ForceRebuildFromClickPoints();
        }
        
        //====  Main Rail Probability ( can not be 0 )  ====
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField(new GUIContent("Probabilities:",
            "The amount of times this variation will appear, relative to the others. If all set to the same value, they will appear equally."));
        
            GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("[Main]  " + sourceVariants[0].go.name +  "    ",
             "The amount of times this variation will appear, relative to the others. If all set to the same value, they will appear equally."),
                 GUILayout.Width(260));

        EditorGUI.BeginChangeCheck();
        var mainProb = ed.railAProbArray.GetArrayElementAtIndex(0);
        EditorGUILayout.PropertyField(mainProb, new GUIContent(""), GUILayout.Width(150));
        if (EditorGUI.EndChangeCheck())
        {
            sourceVariants[0].probability = mainProb.floatValue;
            
            ed.serializedObject.ApplyModifiedProperties();
            if (af.varRailAProbs[0] < 0.01f)
                af.varRailAProbs[0] = 0.01f;
            af.railAVariants[0].probability = mainProb.floatValue;
            //af.shuffledRailAIndices = FenceVariations.CreateShuffledIndices(af.nonNullRailAVariants, af.allPostsPositions.Count - 1);
            //changedGameObjects = true;
        }
        EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(40));
        
        GUILayout.EndHorizontal();
        
        //====  Probability  ====
        
        
        for(int i=1; i<5; i++)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("[" + i + "]       " + sourceVariants[i].go.name +  "    ",
                    "The amount of times this variation will appear, relative to the others. If all set to the same value, they will appear equally."),
                GUILayout.Width(260));
            EditorGUI.BeginChangeCheck();
            var varProb = ed.railAProbArray.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(varProb, new GUIContent(""), GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                ed.serializedObject.ApplyModifiedProperties();
                sourceVariants[i].probability = varProb.floatValue;
                //af.shuffledRailAIndices = FenceVariations.CreateShuffledIndices(af.nonNullRailAVariants, af.allPostsPositions.Count - 1);
            }
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Allow Randomise to use:    ", ed.label11, GUILayout.Width(150));
        if (layerSet == kRailALayer)
        {
            EditorGUILayout.LabelField("Back to Front", ed.smallLabel, GUILayout.Width(80));
            af.allowBackToFrontRailA = EditorGUILayout.Toggle(af.allowBackToFrontRailA,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Mirror Z", ed.smallLabel,GUILayout.Width(60));
            af.allowMirrorZRailA = EditorGUILayout.Toggle(af.allowMirrorZRailA,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Invert Vertical", ed.smallLabel,GUILayout.Width(90));
            af.allowInvertRailA = EditorGUILayout.Toggle(af.allowInvertRailA,GUILayout.Width(35));
        }
        else if (layerSet == kRailBLayer)
        {
            EditorGUILayout.LabelField("Back to Front", ed.smallLabel,GUILayout.Width(80));
            af.allowBackToFrontRailB = EditorGUILayout.Toggle(af.allowBackToFrontRailB,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Mirror Z", ed.smallLabel,GUILayout.Width(60));
            af.allowMirrorZRailB = EditorGUILayout.Toggle(af.allowMirrorZRailB,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Invert Vertical", ed.smallLabel,GUILayout.Width(90));
            af.allowInvertRailB = EditorGUILayout.Toggle(af.allowInvertRailB,GUILayout.Width(35));
        }

        if (EditorGUI.EndChangeCheck())
        {
            for (int s = 0; s < seqInfo.numSteps; s++)
            {
                if(layerSet == kRailALayer)
                {
                    if (seqVariantList[s].backToFront == true && af.allowBackToFrontRailA == false)
                        seqVariantList[s].backToFront = false;
                    if (seqVariantList[s].mirrorZ == true && af.allowMirrorZRailA == false)
                        seqVariantList[s].mirrorZ = false;
                    if (seqVariantList[s].invert == true && af.allowInvertRailA == false)
                        seqVariantList[s].invert = false;
                }
                if(layerSet == kRailBLayer)
                {
                    if (seqVariantList[s].backToFront == true && af.allowBackToFrontRailB == false)
                        seqVariantList[s].backToFront = false;
                    if (seqVariantList[s].mirrorZ == true && af.allowMirrorZRailB == false)
                        seqVariantList[s].mirrorZ = false;
                    if (seqVariantList[s].invert == true && af.allowInvertRailB == false)
                        seqVariantList[s].invert = false;
                }
            }
            af.ForceRebuildFromClickPoints(layerSet);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    //-------------------
    // Put the variantList indices in to the seq list
    void GetMasterVariantListIndices(List<FenceVariant> mainVariants, List<SeqVariant> seqList)
    {
        int index = -1;
        for (int i = 0; i < seqList.Count; i++)
        {
            index = af.FindFirstInVariants(mainVariants, seqList[i].go);
            seqList[i].objIndex = index;
        }
    }
    //-------------------
    public void SetOptimise(AutoFenceCreator.LayerSet layerSet, int currStepIndex)
    {
        EditorGUILayout.Space();
        
        GUIStyle redWarningStyle = new GUIStyle(GUI.skin.button);
        redWarningStyle.normal.textColor = new Color(0.75f, .0f, .0f); //dark red
        GUIStyle buttonStyle = ed.defaultButtonStyle;
        if (optimalToUserSeqString == "Sure? This will replace all steps")
            buttonStyle = redWarningStyle;

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent(optimalToUserSeqString, "Using this, you can quickly fill all steps and then modify specific steps.\n\n" +
                "It will use the best combination of the different (if any) source prefabs you have assigned, along with the orientation changes you allow.\n\n" +
                "To use only the Main prefab, first choose 'All Main' in the 'Choose Source Prefabs' options."),  EditorStyles.miniButton, GUILayout.Width(110)))
        {
            List<FenceVariant> uniqueList = CreateUniqueVariantList(sourceVariants);
            //FenceVariations.PrintFenceVariantList(uniqueList);

            List<SeqVariant> optimalSeq = null;
            if(layerSet == kRailALayer)
                optimalSeq = FenceVariations.CreateOptimalSequence(uniqueList, af.allowBackToFrontRailA, af.allowMirrorZRailA, af.allowInvertRailA);
            else if(layerSet == kRailBLayer)
                optimalSeq = FenceVariations.CreateOptimalSequence(uniqueList, af.allowBackToFrontRailB, af.allowMirrorZRailB, af.allowInvertRailB);

            GetMasterVariantListIndices(sourceVariants, optimalSeq);
            //FenceVariations.PrintSeqVariantList(optimalSeq);
            
            ed.varHelper.CopySequence(optimalSeq, seqVariantList);
            seqInfo.numSteps = optimalSeq.Count;
            if (seqInfo.numSteps> AutoFenceCreator.kMaxNumSeqSteps)
                seqInfo.numSteps = AutoFenceCreator.kMaxNumSeqSteps;
            //FenceVariations.PrintSeqVariantList(seqVariantList);
            
            ed.varHelper.SyncSequencerControlsDisplayAllSteps(layerSet);

            af.ResetPool(layerSet);
            af.ForceRebuildFromClickPoints();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Allow Optimal to use:    ", ed.label11, GUILayout.Width(150));
        if (layerSet == kRailALayer)
        {
            EditorGUILayout.LabelField("Back to Front", ed.smallLabel, GUILayout.Width(80));
            af.allowBackToFrontRailA = EditorGUILayout.Toggle(af.allowBackToFrontRailA,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Mirror Z", ed.smallLabel,GUILayout.Width(60));
            af.allowMirrorZRailA = EditorGUILayout.Toggle(af.allowMirrorZRailA,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Invert Vertical", ed.smallLabel,GUILayout.Width(90));
            af.allowInvertRailA = EditorGUILayout.Toggle(af.allowInvertRailA,GUILayout.Width(35));
        }
        else if (layerSet == kRailBLayer)
        {
            EditorGUILayout.LabelField("Back to Front", ed.smallLabel,GUILayout.Width(80));
            af.allowBackToFrontRailB = EditorGUILayout.Toggle(af.allowBackToFrontRailB,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Mirror Z", ed.smallLabel,GUILayout.Width(60));
            af.allowMirrorZRailB = EditorGUILayout.Toggle(af.allowMirrorZRailB,GUILayout.Width(35));
        
            EditorGUILayout.LabelField("Invert Vertical", ed.smallLabel,GUILayout.Width(90));
            af.allowInvertRailB = EditorGUILayout.Toggle(af.allowInvertRailB,GUILayout.Width(35));
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    //--------------------
    public List<FenceVariant> CreateUniqueVariantList(List<FenceVariant> sourceVariantList)
    {
        List<FenceVariant> uniqueList = new List<FenceVariant>();

        GameObject mainGO = sourceVariantList[0].go;

        foreach (var source in sourceVariantList)
        {
            bool found = false;
            foreach (var dest in uniqueList)
            {
                if (source.go == dest.go)
                {
                    found = true;
                    break;
                }
            }
            if(found == false)
            {
                uniqueList.Add(source);
            }
        }
        return uniqueList;
    }
    //-------------------
    public void HandleOrientationSwitches(AutoFenceCreator.LayerSet layerSet, int currStepIndex)
    {
        var backToFrontProperty = seqVariantListProperty.GetArrayElementAtIndex(currStepIndex).FindPropertyRelative("backToFront");
        var mirrorZProperty = seqVariantListProperty.GetArrayElementAtIndex(currStepIndex).FindPropertyRelative("mirrorZ");
        var invertProperty = seqVariantListProperty.GetArrayElementAtIndex(currStepIndex).FindPropertyRelative("invert");
        
        GUILayout.BeginHorizontal();
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Back to Front", GUILayout.Width(82));
        EditorGUILayout.PropertyField(backToFrontProperty, new GUIContent(""), GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck() )
        {
            ed.serializedObject.ApplyModifiedProperties();
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints(layerSet);
        }
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Mirror Z", GUILayout.Width(50));
        EditorGUILayout.PropertyField(mirrorZProperty, new GUIContent(""), GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck() )
        {
            ed.serializedObject.ApplyModifiedProperties();
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints(layerSet);
        }
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Invert Vertical", GUILayout.Width(80));
        EditorGUILayout.PropertyField(invertProperty, new GUIContent(""), GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck() )
        {
            ed.serializedObject.ApplyModifiedProperties();
            af.ResetPool(currLayerSet);
            af.ForceRebuildFromClickPoints(layerSet);
        }
        
        GUILayout.EndHorizontal();
    }
    //-------------------
    public void SetupSourcePrefabs(AutoFenceCreator.LayerSet layerSet)
    {
        currLayerSet = layerSet;
        GrabVariablesForSet();
        
        EditorGUILayout.Space();
        using (new EditorGUI.IndentLevelScope())
        {
            GUILayout.BeginHorizontal();
            showSourcePrefabs = EditorGUILayout.Foldout(showSourcePrefabs, new GUIContent("Choose Source Prefabs ", "Show a list of  4 alternative Post prefabs"));
            GUILayout.EndHorizontal();
        }

        if (showSourcePrefabs == false)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(" [Main:           " + sourceVariants[0].go.name  + "]", ed.cyanBoldStyle, GUILayout.Width(250));
        for (int i = 1; i < maxNumVariations; i++)
        {
            string varName = "";
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(" Variation " + i.ToString(), ed.cyanBoldStyle, GUILayout.Width(75));

            //====== Choose  Drag & Drop GameObjects =======
            EditorGUI.BeginChangeCheck();
            SerializedProperty varObjList = ed.serializedObject.FindProperty(displayVariationGOsStr);
            var thisVar = varObjList.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(thisVar, new GUIContent(""), GUILayout.Width(175));
            if (EditorGUI.EndChangeCheck())
            {
                ed.serializedObject.ApplyModifiedProperties();
                if (displayVariationGOs[i] != null)
                {
                    if(layerSet == kPostLayer)
                    {
                        varName = ed.res.ImportCustomPost(displayVariationGOs[i], false);
                    }
                    else if (layerSet == kRailALayer || layerSet == kRailBLayer)
                    {
                        varName = ed.res.ImportCustomRail(displayVariationGOs[i], layerSet, false);
                    }
                    ed.LoadPrefabs(ed.af.allowContentFreeUse, true);
                }
                
                varPrefabIndex[i] = af.FindPrefabIndexByName(prefabType, varName);
                varMenuIndex[i] = ed.ConvertPrefabIndexToMenuIndex(varPrefabIndex[i], prefabType);

                sourceVariants[i].go = displayVariationGOs[i];
            }

            if (displayVariationGOs[i] == null)
            {
                varPrefabIndex[i] = 0;
            }
                

            //====  GameObject: Choose From Menu  ====
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("<",  EditorStyles.miniButton, GUILayout.Width(17)) && varMenuIndex[i] > 0)
            {
                varMenuIndex[i] -= 1;
            }
            if (GUILayout.Button(">",  EditorStyles.miniButton, GUILayout.Width(17)) && varMenuIndex[i] < prefabNames.Count - 1)
            {
                varMenuIndex[i] += 1;
            }
            varMenuIndex[i] = EditorGUILayout.Popup("", varMenuIndex[i], prefabNames.ToArray(), GUILayout.Width(240));
            
            //== Set Variation to use Base  Object
            if (GUILayout.Button(new GUIContent("R", "Reset to the default base Object: " +
             mainPrefabs[currentMainPrefabType].name), ed.smallButtonStyle, GUILayout.Width(15)))
            {
                varMenuIndex[i] = mainMenuIndex;
                varPrefabIndex[i] = ed.ConvertMenuIndexToPrefabIndex(varMenuIndex[i], prefabType);
                if (varPrefabIndex[i] == -1)
                    displayVariationGOs[i] = null;
                else
                    displayVariationGOs[i] = mainPrefabs[varPrefabIndex[i]];
                sourceVariants[i].go = displayVariationGOs[i];
            }
            if (EditorGUI.EndChangeCheck())
            {
                varPrefabIndex[i] = ed.ConvertMenuIndexToPrefabIndex(varMenuIndex[i], prefabType);
                if (varPrefabIndex[i] == -1)
                    displayVariationGOs[i] = null;
                else
                    displayVariationGOs[i] = mainPrefabs[varPrefabIndex[i]];
                sourceVariants[i].go = displayVariationGOs[i];

                autoAssignToolbarValue = -1; //deselect toolbar choice as we've overridden it
                
                af.ResetPool(layerSet);
                af.ForceRebuildFromClickPoints();
            }
            GUILayout.EndHorizontal();
        }
        
        //===============================
        //        Auto Assign
        //===============================
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Assign All Sources: ", GUILayout.Width(112));
        
        autoAssignToolbarValue = GUILayout.Toolbar(autoAssignToolbarValue, autoAssignStrings, ed.smallButtonStyle, GUILayout.Width(358));

        EditorGUILayout.LabelField("   ", GUILayout.Width(15));
        updateSequenceAlso = EditorGUILayout.ToggleLeft(new GUIContent("Auto Update Sequence", 
            "With this selected the variations will be assigned to the sequence steps. With it off, set a variation within each step, or press 'Assign All' "),
            updateSequenceAlso, GUILayout.Width(154));
        
        if (EditorGUI.EndChangeCheck())
        {
            string catName = GetCategoryNameFromMenuName(prefabNames[mainMenuIndex]);
            List<int> categoryList =  ed.helper.GetListOfPrefabMenuIndicesWithCategory(prefabType, catName);
            for (int i = 1; i < maxNumVariations; i++)
            {
                if (autoAssignToolbarValue == 0) //All the same as main
                {
                    varMenuIndex[i] = mainMenuIndex;
                }
                if(autoAssignToolbarValue == 1)//consecutive
                    varMenuIndex[i] = mainMenuIndex+i;

                if (autoAssignToolbarValue == 2 && categoryList.Count > 0) //same category
                {
                    af.SeedRandom(false);
                    int r = UnityEngine.Random.Range(0, categoryList.Count - 1);
                    varMenuIndex[i] = categoryList[r];
                }
                if (autoAssignToolbarValue == 3) //any
                {
                    af.SeedRandom(false);
                    varMenuIndex[i] = UnityEngine.Random.Range(0, mainPrefabs.Count - 1);
                }
                

                varPrefabIndex[i] = ed.ConvertMenuIndexToPrefabIndex(varMenuIndex[i], prefabType);
                if (varPrefabIndex[i] == -1)
                    displayVariationGOs[i] = null;
                else
                    displayVariationGOs[i] = mainPrefabs[varPrefabIndex[i]];
                sourceVariants[i].go = displayVariationGOs[i];
            }
            if (updateSequenceAlso)
            {
                SeqVariant.AssignObjectIndicesInSequence(seqVariantList, seqInfo.numSteps, sourceVariants);
                //SetPrefabButtonSwitchesState(seqVariantList[currStepIndex].objIndex);
                af.ResetPool(currLayerSet);
                af.ForceRebuildFromClickPoints(currLayerSet);
            }
        }
        

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();EditorGUILayout.Space();
    }
    //------------------
    // switch onIndex on, the rest off
    void SetPrefabButtonSwitchesState(int onIndex)
    {
        if (onIndex == -1)
            onIndex = 0;
        
        for (int i = 0; i < AutoFenceCreator.kNumberOfPostVariations; i++)
        {
            prefabButtonSwitch[i] = false;
        }
        prefabButtonSwitch[onIndex] = true;
    }
    //--------------------
    string GetCategoryNameFromMenuName(string menuName)
    {
        string catName = "";
        int dirPositionInString = menuName.IndexOf('/');

        // If there's a '/', just take name part and strip the rest
        if (dirPositionInString != -1)
            catName= menuName.Substring(0, dirPositionInString);

        return catName;
    }
    //------------------
    void ReApplyModifiedEditorParameters()
    {
        if (currLayerSet == kPostLayer)
        {
            //currStepIndex = currSeqStepIndex;
        }
        else if (currLayerSet == kRailALayer)
        {
            //ed.currSeqAStepIndex = currSeqStepIndex;
        }
        else if (currLayerSet == kRailBLayer)
        {
            //ed.currSeqAStepIndex = currSeqStepIndex;
        }
    }
    
}

