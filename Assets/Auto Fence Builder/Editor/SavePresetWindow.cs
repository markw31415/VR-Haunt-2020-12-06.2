using UnityEngine;
using UnityEditor;

public class SavePresetWindow : EditorWindow {

    AutoFenceEditor editor = null;
	Color	darkCyan = new Color(0, .45f, .7f);
    string origName, incrementedName, category;
    ScriptablePresetAFWB preset;

    public void Init(AutoFenceEditor inEditor, string presetName, ScriptablePresetAFWB inPreset)
	{
        editor = inEditor;
        origName = presetName;
        preset = inPreset;

        if (presetName == "")
        { // if blank,  name it
            presetName = "Untitled Fence Preset";
            origName = presetName;
        }
        else
        {
            if (presetName.Length > 4)
            {
                // If the name is already numbered, e.g. myPreset_001, increment the number
                string endOfCurrName = presetName.Substring(presetName.Length - 4);
                if (endOfCurrName.StartsWith("_"))
                {
                    string endDigits = presetName.Substring(presetName.Length - 3);
                    int n;
                    bool isNumeric = int.TryParse(endDigits, out n);
                    if (isNumeric)
                    {
                        int newN = n + 1;
                        incrementedName = presetName.Substring(0, presetName.Length - 3);
                        if (newN < 10) incrementedName += "00";
                        else if (newN < 100) incrementedName += "0";
                        incrementedName += newN.ToString();
                    }
                }
                incrementedName = origName + "__0";
            }
            else // If it's not numbered, just add '+' to the end
                incrementedName = origName + "__0";
        }

    }
	void OnGUI() {

        GUIStyle headingStyle = new GUIStyle(EditorStyles.label);
		headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.fontSize = 13;
		headingStyle.normal.textColor = darkCyan;

		GUILayout.BeginVertical("Box");

        EditorGUI.LabelField(new Rect(130, 20, 175, 16), "Preset Name Exists", headingStyle);
		EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();


        // Rename: Save with a new or incremented name
        incrementedName = EditorGUI.TextField(new Rect(120, 55, 305, 16), incrementedName);
        if (GUI.Button(new Rect(28, 55, 80, 16), "Rename "))
        {
            string filePath = ScriptablePresetAFWB.CreateSaveString(editor.af, preset.name, preset.categoryName);
            if (filePath == "")
            {
                Debug.LogWarning("filePath was zero. Not saving");
            }
            else
            {
                ScriptablePresetAFWB.SaveScriptablePreset(editor.af, preset, filePath, true, false);
                editor.helper.LoadAllScriptablePresets(editor.af.allowContentFreeUse);
                string menuName = preset.categoryName + "/" + preset.name;
                int index = editor.scriptablePresetNames.IndexOf(menuName);
                if (index != -1)
                {
                    editor.af.currentScrPresetIndex = index;
                    editor.helper.SetupPreset(index);
                }
            }


            Close();
            GUIUtility.ExitGUI();
        }


        // Replace with same name
        if ( GUI.Button(new Rect(28, 100, 400, 16), "Replace Existing:  " + origName))
        {
            string filePath = ScriptablePresetAFWB.CreateSaveString(editor.af, preset.name, preset.categoryName);
            if (filePath == "")
            {
                Debug.LogWarning("filePath was zero. Not saving");
            }
            else
            {
                ScriptablePresetAFWB.SaveScriptablePreset(editor.af, preset, filePath, true, true);
                editor.helper.LoadAllScriptablePresets(editor.af.allowContentFreeUse);
                string menuName = preset.categoryName + "/" + preset.name;
                int index = editor.scriptablePresetNames.IndexOf(menuName);
                if (index != -1)
                {
                    editor.af.currentScrPresetIndex = index;
                    editor.helper.SetupPreset(index);
                }
            }

            Close();
            GUIUtility.ExitGUI();
        }
        if (GUI.Button(new Rect(28, 145, 400, 16), "Cancel"))
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