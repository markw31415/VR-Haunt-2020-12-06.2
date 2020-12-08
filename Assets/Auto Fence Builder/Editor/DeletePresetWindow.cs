using UnityEngine;
using UnityEditor;

public class DeletePresetWindow : EditorWindow {

    AutoFenceEditor editor = null;
	Color	darkCyan = new Color(0, .45f, .7f);
    string presetName;
    ScriptablePresetAFWB preset = null;
    bool deleted = false;

    public void Init(AutoFenceEditor inEditor, ScriptablePresetAFWB preset)
	{
        editor = inEditor;
        if (preset == null)
            return;
        presetName = preset.name;
        this.preset = preset;
    }
	void OnGUI() {

        if (preset == null)
        {
            Debug.Log("Skipping deletion, preset was null");
            Close();
            GUIUtility.ExitGUI();
        }

        GUIStyle headingStyle = new GUIStyle(EditorStyles.label);
		headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.fontSize = 13;
		headingStyle.normal.textColor = darkCyan;

		GUILayout.BeginVertical("Box");

        EditorGUI.LabelField(new Rect(130, 20, 225, 16), "Delete Preset", headingStyle);
		EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();

        if ( GUI.Button(new Rect(28, 100, 380, 16), "Delete Preset:  " + presetName))
        {
            deleted = false;
            int index = editor.FindPresetByName(presetName);
            if(index != -1) { 
                editor.scriptablePresetList.RemoveAt(index);
                string fileName = presetName;
                string categoryName = preset.categoryName;
                string path  = "Assets/Auto Fence Builder/PresetsAFWB/" + categoryName + "/" + fileName + ".asset";
                deleted = AssetDatabase.DeleteAsset(path);
                if(deleted)
                    Debug.Log(fileName + " was deleted.");

                editor.helper.LoadAllScriptablePresets(editor.af.allowContentFreeUse);
                editor.helper.SetupPreset(editor.af.currentScrPresetIndex);
            }
            Close();
            GUIUtility.ExitGUI();
        }
        if (GUI.Button(new Rect(28, 145, 380, 16), "Cancel"))
        {
            Close();
            GUIUtility.ExitGUI();
        }


        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        EditorGUILayout.Separator(); EditorGUILayout.Separator();
        GUILayout.EndVertical();
		EditorGUILayout.Separator();EditorGUILayout.Separator();
        
	}
}