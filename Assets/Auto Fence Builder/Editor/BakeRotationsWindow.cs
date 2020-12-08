#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;



public class BakeRotationsWindow : EditorWindow
{

    //AutoFenceEditor ed = null;
    AutoFenceCreator afb = null;
    bool isDirty = false;
    Color darkGrey = new Color(.15f, .15f, .15f);
    Color darkCyan = new Color(0, .45f, .7f);
    GUIStyle infoStyle, headingStyle;
    bool x90 = false, y90 = false, z90 = false;
    bool x90minus = false, y90minus = false, z90minus = false;
    Vector3 tempRailUserMeshBakeRotations = Vector3.zero;
    Vector3 tempPostUserMeshBakeRotations = Vector3.zero;
    AutoFenceCreator.LayerSet layerSet;

    public int selctionMode = 0;// 0 = user custom settings, 1 = auto, 2 = don't rotate mesh
    public string[] selStrings = new string[] { "Use Above Rotations", "Auto", "Don't Rotate" };

    public BakeRotationsWindow(AutoFenceCreator inAFB, AutoFenceCreator.LayerSet inLayerSet)
    {
       //ed = inEd;
        afb = inAFB;
        tempRailUserMeshBakeRotations = afb.railUserMeshBakeRotations;
        tempPostUserMeshBakeRotations = afb.postUserMeshBakeRotations;
        layerSet = inLayerSet;
        x90 = y90 = z90 = x90minus = y90minus = z90minus = false;
        if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
        {
            selctionMode = afb.postBakeRotationMode;
            if (afb.postUserMeshBakeRotations.x == 90)
                x90 = true;
            if (afb.postUserMeshBakeRotations.y == 90)
                y90 = true;
            if (afb.postUserMeshBakeRotations.z == 90)
                z90 = true;

            if (afb.postUserMeshBakeRotations.x == -90)
                x90minus = true;
            if (afb.postUserMeshBakeRotations.y == -90)
                y90minus = true;
            if (afb.postUserMeshBakeRotations.z == -90)
                z90minus = true;
        }
        else if (layerSet == AutoFenceCreator.LayerSet.railALayerSet || layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
        {
            selctionMode = afb.railBakeRotationMode;
            if (afb.railUserMeshBakeRotations.x == 90)
                x90 = true;
            if (afb.railUserMeshBakeRotations.y == 90)
                y90 = true;
            if (afb.railUserMeshBakeRotations.z == 90)
                z90 = true;

            if (afb.railUserMeshBakeRotations.x == -90)
                x90minus = true;
            if (afb.railUserMeshBakeRotations.y == -90)
                y90minus = true;
            if (afb.railUserMeshBakeRotations.z == -90)
                z90minus = true;
        }
    }
    //--------------------------
    void SetValuesInAFB()
    {
        float x = 0, y = 0, z = 0;
        if (x90)
            x = 90;
        else if (x90minus)
            x = -90;
        if (y90)
            y = 90;
        else if (y90minus)
            y = -90;
        if (z90)
            z = 90;
        else if (z90minus)
            z = -90;
        
        if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
        {
            afb.postUserMeshBakeRotations = new Vector3(x, y, z);
            afb.postBakeRotationMode = selctionMode;
        }
        if (layerSet == AutoFenceCreator.LayerSet.railALayerSet || layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
        {
            afb.railUserMeshBakeRotations = new Vector3(x, y, z);
            afb.railBakeRotationMode = selctionMode;
        }
        
    }
    //--------------------------
    void SetSelectionMode(int inSelectionMode)
    {
        if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
            selctionMode = afb.postBakeRotationMode = inSelectionMode;
        if (layerSet == AutoFenceCreator.LayerSet.railALayerSet || layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
            selctionMode = afb.railBakeRotationMode = inSelectionMode;
        
    }
    //-------------------
    void OnGUI()
    {

        AutoFenceEditor editor = null;
        AutoFenceEditor[] editors = Resources.FindObjectsOfTypeAll<AutoFenceEditor>();
        if (editors != null && editors.Length > 0)
            editor = editors[0];
        if (editor != null)
            editor.rotationsWindowIsOpen = true;

        headingStyle = new GUIStyle(EditorStyles.label);
        headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.normal.textColor = darkCyan;
        infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.fontStyle = FontStyle.Normal;
        infoStyle.normal.textColor = darkGrey;

        if (afb.currentCustomRailObject == null)
        {
            infoStyle.normal.textColor = Color.red;
            EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
            EditorGUILayout.LabelField("You need to import a GameObject first. Drag & Drop in to the 'Custom Object Import' box. Then re-open this dialog", infoStyle);
            EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
            if (GUILayout.Button("OK"))
            {
                Close();
                if (editor != null)
                    editor.rotationsWindowIsOpen = false;
                if (isDirty)
                {
                    SetValuesInAFB();
                }
                GUIUtility.ExitGUI();
            }
            return;
        }

        EditorGUILayout.Separator();
        GUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Imported Mesh Rotation Baking", headingStyle);
        EditorGUILayout.Separator();

        //========== X ============
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotate X   90", GUILayout.Width(70));
        EditorGUI.BeginChangeCheck();
        x90 = EditorGUILayout.Toggle("", x90, GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (x90)
                x90minus = false;

        }
        EditorGUILayout.LabelField("-90", GUILayout.Width(25));
        EditorGUI.BeginChangeCheck();
        x90minus = EditorGUILayout.Toggle("", x90minus);
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (x90minus)
                x90 = false;
        }
        GUILayout.EndHorizontal();

        //========== Y ============
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotate Y   90", GUILayout.Width(70));
        EditorGUI.BeginChangeCheck();
        y90 = EditorGUILayout.Toggle("", y90, GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (y90)
                y90minus = false;
        }
        EditorGUILayout.LabelField("-90", GUILayout.Width(25));
        EditorGUI.BeginChangeCheck();
        y90minus = EditorGUILayout.Toggle("", y90minus);
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (y90minus)
                y90 = false;
        }
        GUILayout.EndHorizontal();

        //========== Z ============
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotate Z   90", GUILayout.Width(70));
        EditorGUI.BeginChangeCheck();
        z90 = EditorGUILayout.Toggle("", z90, GUILayout.Width(40));
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (z90)
                z90minus = false;
        }
        EditorGUILayout.LabelField("-90", GUILayout.Width(25));
        EditorGUI.BeginChangeCheck();
        z90minus = EditorGUILayout.Toggle("", z90minus);
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            SetSelectionMode(0);
            if (z90minus)
                z90 = false;
        }
        GUILayout.EndHorizontal();

        //============= Rotation Mode Selection ================
        EditorGUILayout.Separator();
        GUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        selctionMode = GUILayout.SelectionGrid(selctionMode, selStrings, 3, GUILayout.Width(400));
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
        }
        GUILayout.EndHorizontal();

        //============== Preview ==============
        if (editor != null)
        {
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Mode above, then check in Scene with Preview: ", infoStyle, GUILayout.Width(290));
            if (GUILayout.Button("*** Preview ***", GUILayout.Width(150)))
            {// 0 = user custom settings, 1 = auto, 2 = don't rotate mesh
                SetValuesInAFB();
                if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
                {
                    editor.res.ImportCustomPost(afb.currentCustomPostObject);
                }
                if (layerSet == AutoFenceCreator.LayerSet.railALayerSet)
                {
                    editor.res.ImportCustomRail(afb.currentCustomRailObject, AutoFenceCreator.LayerSet.railALayerSet);
                }
                else if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
                {
                    editor.res.ImportCustomRail(afb.currentCustomRailObject, AutoFenceCreator.LayerSet.railBLayerSet);
                }
                

                if (((layerSet == AutoFenceCreator.LayerSet.railALayerSet || layerSet == AutoFenceCreator.LayerSet.railBLayerSet) && afb.railBakeRotationMode == 1) || 
                    (layerSet == AutoFenceCreator.LayerSet.postLayerSet&& afb.postBakeRotationMode == 1))
                { // Auto
                    x90 = y90 = z90 = x90minus = y90minus = z90minus = false;
                    if (afb.autoRotationResults.x == 90)
                    {
                        x90 = true;
                        x90minus = false;
                    }
                    else if (afb.autoRotationResults.x == -90)
                    {
                        x90 = false;
                        x90minus = true;
                    }
                    if (afb.autoRotationResults.y == 90)
                    {
                        y90 = true;
                        y90minus = false;
                    }
                    else if (afb.autoRotationResults.y == -90)
                    {
                        y90 = false;
                        y90minus = true;
                    }
                    if (afb.autoRotationResults.z == 90)
                    {
                        z90 = true;
                        z90minus = false;
                    }
                    else if (afb.autoRotationResults.z == -90)
                    {
                        z90 = false;
                        z90minus = true;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        // Force-close in the event of a Unity glitch (control-right-click)
        Event currentEvent = Event.current;
        if (currentEvent.control && currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
        {
            Close();
            editor.rotationsWindowIsOpen = false;
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Separator(); EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("OK"))
        {
            Close();
            if (editor != null)
                editor.rotationsWindowIsOpen = false;
            if (isDirty)
            {
                SetValuesInAFB();
            }
            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();


        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Imported Models need to be in the appropriate orientation for use as as a wall or post. For example, a cylinder\n", infoStyle);
        EditorGUILayout.LabelField("on its side would need to be rotated upright to be a useful post. Something used as a rail/wall is usually longer \n", infoStyle);
        EditorGUILayout.LabelField("than it is wide/tall. In this case, you would rotate something that looks like a post on to its side to become a rail.\n", infoStyle);
        EditorGUILayout.LabelField("If the mesh is not rotated correctly there will be stretching or flattening, e.g. if a post was stretched to a 3m wide wall.\n", infoStyle);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Auto", headingStyle);
        EditorGUILayout.LabelField("Most of the time 'Auto' will correctly guess based on the relative dimensions, so try this first.\n", infoStyle);
        EditorGUILayout.LabelField("However, Game Objects with unusual shapes, or complex parent/child transforms might give unexpected results.\n", infoStyle);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Try the opposite +/- value if Auto is correct but invertped horizontally/vertically.\n", infoStyle);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Custom", headingStyle);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Press preview, or re-import to apply these changes\n", infoStyle);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("(Note: These are not Unity rotations, instead the mesh vertices are being rotated, in the order X, Y, Z)\n", infoStyle);

    }
}
