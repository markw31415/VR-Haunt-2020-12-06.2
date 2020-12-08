using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SettingsWindow : EditorWindow {

	
    AutoFenceCreator afb = null;
	bool	isDirty = false;
	Color	darkGrey = new Color(.2f, .2f, .3f);
	Color	darkCyan = new Color(0, .45f, .7f);
	GUIStyle infoStyle, headingStyle;
	int tempPostColliderMode = 2, tempRailColliderMode = 2, tempExtraColliderMode = 2;
	bool tempAllowGaps = true, tempShowDebugLines = true;
	Transform parent = null;

	public void Init(AutoFenceCreator inAFB)
	{
		afb = inAFB;
		tempPostColliderMode = afb.postColliderMode;
		tempRailColliderMode= afb.railAColliderMode;
		tempExtraColliderMode = afb.extraColliderMode;

		tempAllowGaps = afb.allowGaps;
		tempShowDebugLines = afb.showDebugGapLine;
		parent = inAFB.finishedFoldersParent;
	}
	void OnGUI() {

		headingStyle = new GUIStyle(EditorStyles.label);
		headingStyle.fontStyle = FontStyle.Bold;
		headingStyle.normal.textColor = darkCyan;

		infoStyle = new GUIStyle(EditorStyles.label);
		infoStyle.fontStyle = FontStyle.Normal;
		infoStyle.normal.textColor = darkGrey;

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();


		//=================================
		//	 Parent Folder for Finished
		//=================================
		GUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Optional Parent for Finished Folders", headingStyle);
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("If you want your Finished Fence folders to be parented to an object in your hierarchy", infoStyle);
		EditorGUILayout.LabelField("drag the parent object here\n", infoStyle);

		EditorGUI.BeginChangeCheck();
		parent = EditorGUILayout.ObjectField(parent, typeof(Transform), true) as Transform;
		if(EditorGUI.EndChangeCheck() ){
			afb.finishedFoldersParent = parent;
		}

		EditorGUILayout.Separator();
		GUILayout.EndVertical();
		EditorGUILayout.Separator();EditorGUILayout.Separator();

		//=================================
		//			Colliders
		//=================================

		GUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Colliders", headingStyle);
		EditorGUILayout.Separator();

		EditorGUILayout.LabelField("By default, a single BoxCollider will be placed on the rails/walls, set to the height of the posts.\n", infoStyle);
		EditorGUILayout.LabelField("For most purposes this gives the expected collision on the fence.\n", infoStyle);
		EditorGUILayout.LabelField("It's not usually necessary to have colliders on the posts.\n", infoStyle);
		EditorGUILayout.LabelField("You can change this if, for example, the posts & rails are radically different thicknesses,\n", infoStyle);
		EditorGUILayout.LabelField("or if you have posts but no rails.", infoStyle);
		EditorGUILayout.LabelField("For best performance, use Single or None where possible. Using 'Keep Original' on", infoStyle);
		EditorGUILayout.LabelField("custom objects which have MeshColliders, or multiple small colliders is not recommended.", infoStyle);

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();
		EditorGUILayout.Separator();
		//=========== Defaults ============
		if(GUILayout.Button("Set Defaults", GUILayout.Width(100))){ 
			afb.postColliderMode = 2;
			afb.railAColliderMode = 2;
			afb.extraColliderMode = 2;
			afb.railBoxColliderHeightScale = 1.0f;
			afb.railBoxColliderHeightOffset = 0.0f;
			isDirty = true;
		}
		EditorGUILayout.Separator();
		EditorGUILayout.Separator();
		EditorGUILayout.Separator();

		//Collider Modes: 0 = single box, 1 = keep original (user), 2 = no colliders
		string[] subModeNames = {"Use Single Box Collider", "Keep Original Colliders (Custom Objects Only)", "No Colliders", "Mesh Colliders"};
		int[] subModeNums = {0,1,2,3};
		EditorGUI.BeginChangeCheck();
		afb.railAColliderMode = EditorGUILayout.IntPopup("Rail A Colliders: ", afb.railAColliderMode, subModeNames, subModeNums);
		if(EditorGUI.EndChangeCheck() ){
			isDirty = true;
		}
		EditorGUI.BeginChangeCheck();
		afb.postColliderMode = EditorGUILayout.IntPopup("Post Colliders: ", afb.postColliderMode, subModeNames, subModeNums);
		if(EditorGUI.EndChangeCheck() ){
			isDirty = true;
		}
		EditorGUI.BeginChangeCheck();
		afb.extraColliderMode = EditorGUILayout.IntPopup("Extras Colliders: ", afb.extraColliderMode, subModeNames, subModeNums);
		if(EditorGUI.EndChangeCheck() ){
			isDirty = true;
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Use these to modify the height and vertical postion of the Rail/Wall's Box Collider:", infoStyle);
		EditorGUI.BeginChangeCheck();
		afb.railBoxColliderHeightScale = EditorGUILayout.FloatField("Rail BoxCollider Y Scale", afb.railBoxColliderHeightScale );
		if(afb.railBoxColliderHeightScale < 0.01f)
			afb.railBoxColliderHeightScale = 0.01f;
		if(afb.railBoxColliderHeightScale > 10f)
			afb.railBoxColliderHeightScale = 10.0f;
		EditorGUILayout.Separator();
		afb.railBoxColliderHeightOffset = EditorGUILayout.FloatField("Rail BoxCollider Y Offset", afb.railBoxColliderHeightOffset );
		if(afb.railBoxColliderHeightOffset < -10.0f)
			afb.railBoxColliderHeightOffset = -10.0f;
		if(afb.railBoxColliderHeightOffset > 10f)
			afb.railBoxColliderHeightOffset = 10.0f;
		if(EditorGUI.EndChangeCheck() ){
			isDirty = true;
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("(On long or complex fences, selecting 'No Colliders' will improve performance", infoStyle);
		EditorGUILayout.LabelField("while designing in the Editor. Add them when you're ready to finish.)", infoStyle);
		EditorGUILayout.Separator();
		EditorGUILayout.Separator();
        afb.addBoxCollidersToRailB = EditorGUILayout.Toggle("Add Box Colliders to Rail B", afb.addBoxCollidersToRailB);
        GUILayout.EndVertical();
		EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();



        //=================================
        //			Gaps
        //=================================
        GUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Gaps", headingStyle);
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Control-Right-Click to create gaps in the fence.", infoStyle);

		EditorGUILayout.Separator();
		afb.allowGaps = EditorGUILayout.Toggle("Allow Gaps", afb.allowGaps);
		afb.showDebugGapLine = EditorGUILayout.Toggle("Show Gap Lines", afb.showDebugGapLine);
		EditorGUILayout.Separator();
		GUILayout.EndVertical();

        EditorGUI.BeginChangeCheck();
        afb.ignoreControlNodesLayerNum = EditorGUILayout.IntField("ignoreControlsLayerNum", afb.ignoreControlNodesLayerNum);
        if (EditorGUI.EndChangeCheck() ) isDirty = true;

        EditorGUILayout.Separator();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("OK")) {
			Close();

            if (isDirty){
                List<Transform> posts = afb.posts;
                for (int p = 0; p < afb.allPostsPositions.Count - 1; p++){
                    if (posts[p] != null)
                        posts[p].gameObject.layer = 0;
                }
                afb.ForceRebuildFromClickPoints();
			}
			if(afb.railAColliderMode < 2 || afb.postColliderMode < 2 || afb.extraColliderMode < 2){
				Debug.Log("Colliders are being used. It's recommended to leave colliders off until ready to Finish the Fence. " +
					"(They have to be recalculated every time there's a change, this can slow down responsiveness in the Editor.)\n");
			}
           
            GUIUtility.ExitGUI();
		}
		if (GUILayout.Button("Cancel")) {
			Close();
			afb.postColliderMode = tempPostColliderMode;
			afb.railAColliderMode = tempRailColliderMode;
			afb.extraColliderMode = tempExtraColliderMode;

			afb.allowGaps = tempAllowGaps;
			afb.showDebugGapLine = tempShowDebugLines;

			GUIUtility.ExitGUI();
		}
		GUILayout.EndHorizontal();

	}
}