#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class RandomizationEditor
{
    AutoFenceCreator.LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet;
    AutoFenceCreator.LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    AutoFenceCreator.LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    AutoFenceCreator.LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;

    AutoFenceCreator af;
    AutoFenceEditor ed;
    AutoFenceCreator.LayerSet currLayerSet;
    string layerWord = "";

    public RandomizationEditor(AutoFenceCreator autoFenceCreator, AutoFenceEditor autoFenceEditor)
    {
        af = autoFenceCreator;
        ed = autoFenceEditor;
        //GrabVariablesForSet();
    }
    //------------------
    void GrabVariablesForSet()
    {
        if (currLayerSet == kPostLayer)
        {
            layerWord = "Post";
        }
        else if (currLayerSet == kRailALayer)
        {
            layerWord = "Rail A";
        }
        else if (currLayerSet == kRailBLayer)
        {
            layerWord = "Rail B";
        }
        else if (currLayerSet == kSubpostLayer)
        {
            layerWord = "Subpost";
        }
    }
    //-----------------
    public void SetupRandomization(AutoFenceCreator.LayerSet inLayerSet)
    {
        currLayerSet = inLayerSet;
        GrabVariablesForSet();
        
        bool rebuild = false;
        EditorGUILayout.Space(); EditorGUILayout.Space();EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        //== Toolbar Scope ==
        if (currLayerSet == kPostLayer || currLayerSet == kRailALayer || currLayerSet == kRailBLayer || currLayerSet == kSubpostLayer)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(layerWord + " Randomization", ed.cyanBoldStyle, GUILayout.Width(219));
            if(currLayerSet != kSubpostLayer)
                EditorGUILayout.LabelField(" Apply To: ", GUILayout.Width(60));
            if (currLayerSet == kPostLayer)
                af.postRandomScope = GUILayout.Toolbar(af.postRandomScope, ed.randomScopeStrings, ed.smallButtonStyle, GUILayout.Width(324));
            if (currLayerSet == kRailALayer)
                af.railARandomScope = GUILayout.Toolbar(af.railARandomScope, ed.randomScopeStrings, ed.smallButtonStyle, GUILayout.Width(324));
            if (currLayerSet == kRailBLayer)
                af.railBRandomScope = GUILayout.Toolbar(af.railBRandomScope, ed.randomScopeStrings, ed.smallButtonStyle, GUILayout.Width(324));

            GUILayout.EndHorizontal(); 
        }
        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            rebuild = true;
        }

        //==============================
        //   HEIGHT Randomization 
        //=============================            
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        if (currLayerSet == kPostLayer )
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowPostHeightVariation"),
                new GUIContent("Post Height Variation"));
            EditorGUILayout.LabelField("", af.minPostHeightVar.ToString("F2"), GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref af.minPostHeightVar, ref af.maxPostHeightVar, af.minPostHeightLimit,
                af.maxPostHeightLimit, GUILayout.Width(180));
            EditorGUILayout.LabelField("", af.maxPostHeightVar.ToString("F2"), GUILayout.Width(36));
        }
        else if (currLayerSet == kRailALayer )
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRailAHeightVariation"),
                new GUIContent("Rail A Height Variation"));
            EditorGUILayout.LabelField("", af.minRailAHeightVar.ToString("F2"), GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref af.minRailAHeightVar, ref af.maxRailAHeightVar, af.minRailHeightLimit,
                af.maxRailHeightLimit, GUILayout.Width(180));
            EditorGUILayout.LabelField("", af.maxRailAHeightVar.ToString("F2"), GUILayout.Width(36));
        }
        else if (currLayerSet == kRailBLayer )
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRailBHeightVariation"),
                new GUIContent("Rail B Height Variation"));
            EditorGUILayout.LabelField("", af.minRailBHeightVar.ToString("F2"), GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref af.minRailBHeightVar, ref af.maxRailBHeightVar, af.minRailHeightLimit,
                af.maxRailHeightLimit, GUILayout.Width(180));
            EditorGUILayout.LabelField("", af.maxRailBHeightVar.ToString("F2"), GUILayout.Width(36));
        }
        else if (currLayerSet == kSubpostLayer )
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowSubpostHeightVariation"),
                new GUIContent("Subpost Height Variation"));
            EditorGUILayout.LabelField("", af.minSubpostHeightVar.ToString("F2"), GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref af.minSubpostHeightVar, ref af.maxSubpostHeightVar, af.minPostHeightLimit,
                af.maxPostHeightLimit, GUILayout.Width(180));
            EditorGUILayout.LabelField("", af.maxSubpostHeightVar.ToString("F2"), GUILayout.Width(36));
        }

        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            rebuild = true;
        }
        
        GUILayout.EndHorizontal();

        //===============================
        //   ROTATION Randomization  
        //===============================
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        if (currLayerSet == kPostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRandPostRotationVariation"), new GUIContent("Small Random Rotations"));
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("postRandRotationAmount"), new GUIContent(""));  
        }
        if (currLayerSet == kRailALayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRandRailARotationVariation"), new GUIContent("Small Random Rotations"));
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("railARandRotationAmount"), new GUIContent(""));  
        }
        if (currLayerSet == kRailBLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRandRailBRotationVariation"), new GUIContent("Small Random Rotations"));
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("railBRandRotationAmount"), new GUIContent(""));  
        }
        if (currLayerSet == kSubpostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowRandSubpostRotationVariation"), new GUIContent("Small Random Rotations"));
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("subpostRandRotationAmount"), new GUIContent(""));  
        }

        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            
            if (currLayerSet == kPostLayer)
                af.postRandRotationAmount = ed.EnforceVectorMinMax(af.postRandRotationAmount, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f));
            if (currLayerSet == kRailALayer)
                af.railARandRotationAmount = ed.EnforceVectorMinMax(af.railARandRotationAmount, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f));
            if (currLayerSet == kRailBLayer)
                af.railBRandRotationAmount = ed.EnforceVectorMinMax(af.railBRandRotationAmount, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f));
            if (currLayerSet == kSubpostLayer)
                af.subpostRandRotationAmount = ed.EnforceVectorMinMax(af.subpostRandRotationAmount, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f));
            
            rebuild = true;
        }
        GUILayout.EndHorizontal();

        //===============================
        //   QUANTIZED Rotation 
        //===============================
        
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        if (currLayerSet == kPostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowQuantizedRandomPostRotation"), new GUIContent("Quantized Random Rotations",
                "Rotates the post by a random multiple of these values. E.g. It can be useful to lock a square post to 90 degree rotations only." +
                " You can then add minor random fluctuations with 'Small Random Rotations'. The start and end posts are not rotated"));
            
            
            ed.quantizeRotIndexPost.intValue = EditorGUILayout.Popup("", ed.quantizeRotIndexPost.intValue, ed.quantizeRotStrings, ed.mediumPopup, GUILayout.Width(120)); 
            af.postQuantizeRotAmount = float.Parse(ed.quantizeRotStrings[ed.quantizeRotIndexPost.intValue]);
        }

        if (currLayerSet == kSubpostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("allowQuantizedRandomSubpostRotation"), new GUIContent("Quantized Random Rotations",
                "Rotates the post by a random multiple of these values"));
            ed.quantizeRotIndexSubpost.intValue = EditorGUILayout.Popup("", ed.quantizeRotIndexSubpost.intValue, ed.quantizeRotStrings, ed.mediumPopup, GUILayout.Width(120)); 
            af.subpostQuantizeRotAmount = float.Parse(ed.quantizeRotStrings[ed.quantizeRotIndexSubpost.intValue]);
        }

        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            rebuild = true;
        }
        GUILayout.EndHorizontal();

        //===========================
        //     Chance Of Missing 
        //===========================
        EditorGUI.BeginChangeCheck();

        if (currLayerSet == kPostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("chanceOfMissingPost"),
                new GUIContent("Chance of Missing Post", "Posts will be randomly omitted (except first/last post)")); 
        }
        if (currLayerSet == kRailALayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("chanceOfMissingRailA"),
                new GUIContent("Chance of Missing Rail A", "Rails will be randomly omitted (except first/last post)")); 
        }
        if (currLayerSet == kRailBLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("chanceOfMissingRailB"),
                new GUIContent("Chance of Missing Rail B", "Rails will be randomly omitted (except first/last post)")); 
        }
        if (currLayerSet == kSubpostLayer)
        {
            EditorGUILayout.PropertyField(ed.serializedObject.FindProperty("chanceOfMissingSubpost"),
                new GUIContent("Chance of Missing Subpost", "Subposts will be randomly omitted (except first/last post)")); 
        }
        if (EditorGUI.EndChangeCheck())
        {
            ed.serializedObject.ApplyModifiedProperties();
            rebuild = true;
        }

        
        if (rebuild)
        {
            af.ForceRebuildFromClickPoints(currLayerSet);
        }
        
        EditorGUILayout.Space();
    }

}