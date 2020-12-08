#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection.Emit;

// Called from AutoFenceEditor, builds controls for the Variations Sequence Blocks
public class SinglesEditor
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
    bool showSourcePrefabs = true;
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
    string randomizeUserSeqString = "Randomize All Steps", optimalToUserSeqString = "Quick-Fill Optimal";
    string randomToUserSeqString = "Replace From Random Mode";
    private List<string> prefabNames = null;

    private string[] autoAssignStrings = new[] {"Consecutive", "Same category", "Any", "All Main"};
    private int autoAssignMenuIndex = 0;
    private bool updateSequenceAlso = false;
    int autoAssignToolbarValue = 0;

    
    Color lineColor = new Color(0.94f, 0.94f, 0.94f);
    
    public SinglesEditor(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor, AutoFenceCreator.LayerSet inLayerSet)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
        currLayerSet = inLayerSet;
        //GrabVariablesForSet();
    }
    //------------------
    public void SetupSinglesRailA()
    {
        int numSingles = ed.railASinglesList.arraySize;

        using (new EditorGUI.IndentLevelScope())
        {
            GUILayout.BeginHorizontal();
            ed.showSinglesA = EditorGUILayout.Foldout(ed.showSinglesA, new GUIContent(
                "Show Single Rail Modifications",
                "If you've assigned any individual sections" +
                " (by control-right-clicking them in the Scene View) they will appear here so you can modify their position/scale/rotation"));

            GUILayout.EndHorizontal();
        }

        if (numSingles == 0)
        {
            GUIStyle greyStyle = new GUIStyle(EditorStyles.label);
            greyStyle.fontStyle = FontStyle.Italic;
            greyStyle.normal.textColor = Color.gray;
            greyStyle.fontSize = 11;
            EditorGUILayout.LabelField("No singles have been set. To assign single variations, ctrl-right-click on any panel in Scene View", greyStyle);
        }

        if (ed.showSinglesA && numSingles > 0)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                new GUIContent("Clear All Singles", "Sets all sections assigned with a unique variation back to their default."), GUILayout.Width(120)))
            {
                af.ClearAllSinglesA();
                ed.railASinglesEnabled = true;
                af.ToggleAllSingleVariants(kRailALayer, ed.railASinglesEnabled);
                af.ResetRailAPool();
                af.ForceRebuildFromClickPoints();
                return;
            }

            string disableString = "Disable All Singles";
            if (ed.railASinglesEnabled == false)
                disableString = "Enable All Singles";
            if (GUILayout.Button(
                new GUIContent(disableString, "Toggle disabling all single section modifications"),
                GUILayout.Width(130)))
            {
                ed.railASinglesEnabled = !ed.railASinglesEnabled;
                af.ToggleAllSingleVariants(kRailALayer, ed.railASinglesEnabled);
                af.ResetRailAPool();
                af.ForceRebuildFromClickPoints();
            }

            if (ed.railASinglesEnabled == false && ed.railASinglesList.arraySize > 0)
                EditorGUILayout.LabelField(
                    new GUIContent("All Singles are disabled, 'Enable All Singles' to show"), ed.warningStyle,
                    GUILayout.Width(300));

            GUILayout.EndHorizontal();

            if (ed.railASinglesEnabled && af.railASingleVariants.Count > 0)
            {
                GUIStyle smallGreyStyle = new GUIStyle(EditorStyles.label);
                smallGreyStyle.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
                smallGreyStyle.fontSize = 9;
                EditorGUILayout.Space();
                for (int i = 0; i < ed.railASinglesList.arraySize; i++)
                {
                    SerializedProperty thisSingleVariant = ed.railASinglesList.GetArrayElementAtIndex(i);
                    SerializedProperty thisPos = thisSingleVariant.FindPropertyRelative("positionOffset");
                    SerializedProperty thisSize = thisSingleVariant.FindPropertyRelative("size");
                    SerializedProperty thisRotation = thisSingleVariant.FindPropertyRelative("rotation");

                    
                    GUILayout.BeginHorizontal();
                    FenceVariant variant = af.railASingleVariants[i];
                    string indexStr = i + ":  ";
                    string shortprefabName = variant.go.name;
                    shortprefabName = shortprefabName.Substring(0, shortprefabName.Length - 5);
                    indexStr += shortprefabName;
                    EditorGUILayout.LabelField(indexStr, smallGreyStyle, GUILayout.Width(180));
                    
                    indexStr = "    Section ";
                    indexStr += variant.singleIndex;
                    EditorGUILayout.LabelField(indexStr, smallGreyStyle);
                    GUILayout.EndHorizontal();
                    
                    
                    
                    GUILayout.BeginHorizontal();
                    //===================================
                    //      Position Offset
                    //===================================
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.LabelField(new GUIContent("Pos:", "Offset the position of this variation. Default=0."), ed.smallStyle, GUILayout.Width(38));
                    EditorGUILayout.PropertyField(thisPos, new GUIContent(""), GUILayout.Width(127));
                    if (GUILayout.Button(new GUIContent("X", "Set Position Offset values to default 0"), GUILayout.Width(8)))
                    {
                        thisPos.vector3Value = Vector3.zero;
                    }
                    //===================================
                    //      Size
                    //===================================
                    EditorGUILayout.LabelField(new GUIContent("  Size:", "Multiply Size of this variation. Default=1/1/1."), ed.smallStyle, GUILayout.Width(48));
                    EditorGUILayout.PropertyField(thisSize, new GUIContent(""), GUILayout.Width(127));
                    if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), GUILayout.Width(8)))
                    {
                        thisSize.vector3Value = Vector3.one;
                    }
                    //===================================
                    //      Rotation
                    //===================================
                    EditorGUILayout.LabelField(new GUIContent("   Rot:", "Add Rotation to this variation . Default=0."), ed.smallStyle, GUILayout.Width(48));
                    EditorGUILayout.PropertyField(thisRotation, new GUIContent(""), GUILayout.Width(127));
                    if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), GUILayout.Width(8)))
                    {
                        thisRotation.vector3Value = Vector3.zero;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        ed.serializedObject.ApplyModifiedProperties();
                        af.ResetRailAPool();
                        af.ForceRebuildFromClickPoints();
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
    //------------------------------------------
    public void SetupSinglesRailB()
    {
        int numSingles = ed.railBSinglesList.arraySize;
        using (new EditorGUI.IndentLevelScope())
        {
            GUILayout.BeginHorizontal();
            ed.showSinglesB = EditorGUILayout.Foldout(ed.showSinglesB, new GUIContent("Show Single Rail A Modifications",
                "If you've assigned any individual sections" +
                " (by control-right-clicking them in the Scene View) they will appear here so you can modify their position/scale/rotation"));
            GUILayout.EndHorizontal();
            
            if (numSingles == 0)
            {
                GUIStyle greyStyle = new GUIStyle(EditorStyles.label);
                greyStyle.fontStyle = FontStyle.Italic;
                greyStyle.normal.textColor = Color.gray;
                greyStyle.fontSize = 11;
                EditorGUILayout.LabelField("No singles have been set. To assign single variations, ctrl-right-click on any panel in Scene View", greyStyle);
            }
            
            if (ed.showSinglesB && numSingles > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(
                    new GUIContent("Clear All Singles", "Sets all sections assigned with a unique variation back to their default."), GUILayout.Width(120)))
                {
                    af.ClearAllSinglesB();
                    ed.railBSinglesEnabled = true;
                    af.ToggleAllSingleVariants(kRailBLayer, ed.railBSinglesEnabled);
                    af.ResetRailBPool();
                    af.ForceRebuildFromClickPoints();
                }

                string disableString = "Disable All Singles";
                if (ed.railBSinglesEnabled == false)
                    disableString = "Enable All Singles";
                if (GUILayout.Button(
                    new GUIContent(disableString, "Toggle disabling all single section modifications"),
                    GUILayout.Width(130)))
                {
                    ed.railBSinglesEnabled = !ed.railBSinglesEnabled;
                    af.ToggleAllSingleVariants(kRailBLayer, ed.railBSinglesEnabled);
                    af.ResetRailBPool();
                    af.ForceRebuildFromClickPoints();
                }

                if (ed.railBSinglesEnabled == false && ed.railBSinglesList.arraySize > 0)
                    EditorGUILayout.LabelField(
                        new GUIContent("All Singles are disabled, 'Enable All Singles' to show"), ed.warningStyle,
                        GUILayout.Width(300));

                GUILayout.EndHorizontal();

                if (ed.railBSinglesEnabled && af.railBSingleVariants.Count > 0)
                {
                    for (int i = 0; i < ed.railBSinglesList.arraySize; i++)
                    {
                        GUIStyle smallGreyStyle = new GUIStyle(EditorStyles.label);
                        //smallGreyStyle.fontStyle = FontStyle.Italic;
                        smallGreyStyle.normal.textColor = Color.gray;
                        smallGreyStyle.fontSize = 9;
                        
                        SerializedProperty thisSingleVariant = ed.railBSinglesList.GetArrayElementAtIndex(i);
                        SerializedProperty thisPos = thisSingleVariant.FindPropertyRelative("positionOffset");
                        SerializedProperty thisSize = thisSingleVariant.FindPropertyRelative("size");
                        SerializedProperty thisRotation = thisSingleVariant.FindPropertyRelative("rotation");
                        
                        GUILayout.BeginHorizontal();
                        FenceVariant variant = af.railBSingleVariants[i];
                        string indexStr = i + ":  ";
                        string shortprefabName = variant.go.name;
                        shortprefabName = shortprefabName.Substring(0, shortprefabName.Length - 5);
                        indexStr += shortprefabName;
                        EditorGUILayout.LabelField(indexStr, smallGreyStyle, GUILayout.Width(180));
                    
                        indexStr = "    Section ";
                        indexStr += variant.singleIndex;
                        EditorGUILayout.LabelField(indexStr, smallGreyStyle);
                        GUILayout.EndHorizontal();
                        
                        GUILayout.BeginHorizontal();
                        
                        //====  Position Offset  ====
                        EditorGUILayout.LabelField(new GUIContent(i.ToString() + ":"), GUILayout.Width(18));
                        
                        //===================================
                        //      Position Offset
                        //===================================
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.LabelField(new GUIContent("Pos:", "Offset the position of this variation. Default=0."), ed.smallStyle, GUILayout.Width(30));
                        EditorGUILayout.PropertyField(thisPos, new GUIContent(""), GUILayout.Width(127));
                        if (GUILayout.Button(new GUIContent("X", "Set Position Offset values to default 0"), GUILayout.Width(8)))
                        {
                            thisPos.vector3Value = Vector3.zero;
                        }
                        //===================================
                        //      Size
                        //===================================
                        EditorGUILayout.LabelField(new GUIContent("  Size:", "Multiply Size of this variation. Default=1/1/1."), ed.smallStyle, GUILayout.Width(46));
                        EditorGUILayout.PropertyField(thisSize, new GUIContent(""), GUILayout.Width(127));
                        if (GUILayout.Button(new GUIContent("X", "Set Size values to default 1"), GUILayout.Width(8)))
                        {
                            thisSize.vector3Value = Vector3.one;
                        }
                        //===================================
                        //      Rotation
                        //===================================
                        EditorGUILayout.LabelField(new GUIContent("   Rot:", "Add Rotation to this variation . Default=0."), ed.smallStyle, GUILayout.Width(35));
                        EditorGUILayout.PropertyField(thisRotation, new GUIContent(""), GUILayout.Width(127));
                        if (GUILayout.Button(new GUIContent("X", "Set Rotation values to default 0"), GUILayout.Width(8)))
                        {
                            thisRotation.vector3Value = Vector3.zero;
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            ed.serializedObject.ApplyModifiedProperties();
                            af.ResetRailBPool();
                            af.ForceRebuildFromClickPoints();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }
    }
}
