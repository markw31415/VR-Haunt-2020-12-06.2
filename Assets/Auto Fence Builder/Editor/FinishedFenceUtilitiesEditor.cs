#pragma warning disable 0219
#pragma warning disable 0414

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

[CustomEditor(typeof(FinishedFenceUtilities))]
public class FinishedFenceUtilitiesEditor : Editor
{

	public SerializedProperty presetID;
	public FinishedFenceUtilities 		 finishedUtils;
	public GameObject rootFolder = null;
	private Vector3 finishedPos, livePosition;
	private string editButtonText = "Edit                  [Replaces contents of current Auto Fence Builder session]", 
		editButtonTextSure = "Are you sure?  This will Replace the current contents of Auto Fence Builder", currEditButtonText;
	
	void OnEnable()    
	{

		finishedUtils = (FinishedFenceUtilities)target;
		rootFolder = finishedUtils.finishedFolderRoot.gameObject;
		
		presetID = serializedObject.FindProperty("presetID");
		currEditButtonText = editButtonText;
	}
	//------------------------------------------
	public void CreateCurrentFromFinished()
	{
		AutoFenceCreator af = GameObject.FindObjectOfType<AutoFenceCreator>();
		
		rootFolder.SetActive(false);
		af.CopyLayoutFromOtherFence(true, rootFolder);
				
		string[] presets = AssetDatabase.FindAssets(finishedUtils.presetID);
		string presetPath;
		if (presets.Length == 0 || presets[0] == "")
		{
			Debug.LogWarning("Couldn't find finished preset in CreateCurrentFromFinished(). It should be in PresetsForFinishedFences folder. " +
			                 "Fence will be rebuilt with current settings instead. \n");
		}
		else
		{
			presetPath = AssetDatabase.GUIDToAssetPath(presets[0]);
			ScriptablePresetAFWB preset = AssetDatabase.LoadMainAssetAtPath(presetPath) as ScriptablePresetAFWB;
			preset.BuildFromPreset(af);
		}
		af.ResetAllPools();
		af.ForceRebuildFromClickPoints();
	}
	//------------------------------------------
	public void CreateFinishedFromCurrent()
	{
		AutoFenceCreator af = GameObject.FindObjectOfType<AutoFenceCreator>();
		if (af == null)
			return;
		
		List<Transform> posts = af.posts;
		for (int p = 0; p < af.allPostsPositions.Count - 1; p++)
		{
			if (posts[p] != null)
				posts[p].gameObject.layer = 0;
		} 
		if (af.allPostsPositions.Count > 0)
		{//Reposition handle at base of first post
			Vector3 currPos = af.fencesFolder.transform.position;
			livePosition = currPos;
			Vector3 delta = af.allPostsPositions[0] - currPos;
			af.fencesFolder.transform.position = af.allPostsPositions[0];
			af.postsFolder.transform.position = af.allPostsPositions[0] - delta;
			af.railsFolder.transform.position = af.allPostsPositions[0] - delta;
			af.subpostsFolder.transform.position = af.allPostsPositions[0] - delta;
		}
		SaveRailMeshes.SaveProcRailMeshesAsAssets(finishedUtils.af);
		GameObject finishedFolder = af.FinishAndStartNew(af.finishedFoldersParent, "Finished By Editing Session"); // is called from the window now
            
		FinishedFenceUtilities newFinishedUtils = finishedFolder.AddComponent<FinishedFenceUtilities>();
		string dateStr = af.GetPartialTimeString(true);
		newFinishedUtils.presetID = dateStr;
		ScriptablePresetAFWB preset = EditorHelperAFWB.SaveFinishedPreset(dateStr + "_" + finishedFolder.name, finishedUtils.af);
	}
	//------------------------------------------
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		
		if(finishedUtils.af  == null)
			finishedUtils.af = GameObject.FindObjectOfType<AutoFenceCreator>();
		
		GUILayout.Label("Preset ID = " + presetID.stringValue);
		
		EditorGUILayout.Separator();EditorGUILayout.Separator();
		
		//======   Edit & Replace   ======
		if (GUILayout.Button(new GUIContent(currEditButtonText, "The current Auto Fence Builder will be overwritten with the settings to edit this fence." +
		                                                        "Use this if Auto Fence Builder is empty, or it's OK to discard contents." +
		                                                        "Use the option below: 'Edit   [First create Finished fence...]' if you would like to save" +
		                                                        "that session first as a Finished fence"), GUILayout.Width(500)))
		{
			if (currEditButtonText == editButtonText)
			{
				currEditButtonText = editButtonTextSure;
			}
			else if (currEditButtonText == editButtonTextSure)
			{
				if (finishedUtils != null)
				{
					
					CreateCurrentFromFinished();
					Selection.activeGameObject = finishedUtils.af.transform.gameObject;
					rootFolder.name += " [Pre Edit]";
					rootFolder.SetActive(false);
				}
				
				currEditButtonText = editButtonText;
			}
		}
		
		EditorGUILayout.Separator();
		//======   Edit & Finish Current   ======
		//if (currEditButtonText == editButtonText)
		{
			if (GUILayout.Button(new GUIContent("Edit        [Will first create Finished fence from the current Auto Fence Builder session]",
				"This will save your current Auto Fence Builder as a Finished fence, and replace the settings in order to edit this fence."), GUILayout.Width(500)))
			{
				finishedPos = finishedUtils.af.fencesFolder.transform.position;
				CreateFinishedFromCurrent();
				AutoFenceCreator af = GameObject.FindObjectOfType<AutoFenceCreator>();
				
				
				af.ClearAllFences();
				if (finishedUtils != null)
				{
					CreateCurrentFromFinished();
					Selection.activeGameObject = af.transform.gameObject;
					rootFolder.name += " [Pre Edit]";
					rootFolder.SetActive(false);
				}
				currEditButtonText = editButtonText;
			}
		}
		
		EditorGUILayout.Separator();
		GUILayout.Label("Editing a Finished fence will place its settings in to the current Auto Fence Builder session."); 
		GUILayout.Label("If the current session is not empty, you can Finish it manually, or choose the 2nd Edit button which will Finish it."); 
		GUILayout.Label("Alternatively, you can choose to overwrite it."); 
		EditorGUILayout.Separator();
		GUILayout.Label("This Finished version will be de-activated, and you may safely delete it afterwards."); 
		EditorGUILayout.Separator();
		GUILayout.Label("Note: This will work fully for any Finished fence created in v3.2+"); 
		GUILayout.Label("Only layout & build positions - not design - are available in earlier versions");
		EditorGUILayout.Separator();
		//======   Cancel  ======
		EditorGUILayout.Separator();
		if (currEditButtonText == editButtonTextSure)
		{
			if (GUILayout.Button("Cancel", GUILayout.Width(110)))
			{
				currEditButtonText = editButtonText;
			}
		}
		
	}
	



}
