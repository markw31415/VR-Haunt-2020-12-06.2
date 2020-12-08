using UnityEngine;
using UnityEditor;

public class FinishWindow : EditorWindow {

	string fenceName = "Finished Fence";
	string modeString;
	AutoFenceCreator afb = null;
	Transform parentFolder = null;


	public void Init(AutoFenceCreator inAFB, string inModeString, Transform inParentFolder)
	{
		afb = inAFB;
		modeString = inModeString;
		parentFolder = inParentFolder;

		if(modeString == "FinishAndStartNew")
			fenceName = "Finished Fence";
		else if(modeString == "FinishAndDuplicate")
			fenceName = "Finished Duplicated Fence";
	}


	void OnGUI() {


		//=================================
		//	 Parent Folder for Finished
		//=================================
		GUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Optional Parent for Finished Folders");
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("If you want your Finished Fence folders to be parented to an object in your hierarchy");
		EditorGUILayout.LabelField("drag the parent object here\n");

		EditorGUI.BeginChangeCheck();
		parentFolder = EditorGUILayout.ObjectField(parentFolder, typeof(Transform), true) as Transform;
		if(EditorGUI.EndChangeCheck() ){
			afb.finishedFoldersParent = parentFolder;
		}

		EditorGUILayout.Separator();
		GUILayout.EndVertical();
		EditorGUILayout.Separator();EditorGUILayout.Separator();



		fenceName = EditorGUILayout.TextField("Fence Name", fenceName);

		if (GUILayout.Button("OK")) {
			Close();
			if(modeString == "FinishAndStartNew")
				afb.FinishAndStartNew(parentFolder, fenceName);
			else if(modeString == "FinishAndDuplicate")
				afb.FinishAndDuplicate(parentFolder, fenceName);
			GUIUtility.ExitGUI();
		}
	}
}