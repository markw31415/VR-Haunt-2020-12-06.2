using UnityEngine;
using UnityEditor;

public class RenamePrefabWindow : EditorWindow {

    AutoFenceEditor editor = null;
	Color	darkCyan = new Color(0, .45f, .7f);
    string  railAName = "", railBName = "", postName = "", extraName = "";
    string  newRailAName = "", newRailBName = "", newPostName = "", newExtraName = "";
    ScriptablePresetAFWB preset;

    public void Init(AutoFenceEditor inEditor)
	{
        editor = inEditor;
        AutoFenceCreator af = editor.af;

        newRailAName = railAName = af.railPrefabs[af.currentRailAType].name;
        newRailBName = railBName = af.railPrefabs[af.currentRailBType].name;
        newPostName = postName = af.GetCurrentPost().name;
        newExtraName = extraName = af.extraPrefabs[af.currentExtraType].name;


    }
	void OnGUI() {

        GUIStyle headingStyle = new GUIStyle(EditorStyles.label);
		headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.fontSize = 11;
		headingStyle.normal.textColor = darkCyan;
        EditorGUILayout.Separator();
        GUILayout.BeginVertical("Box");
        EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( "Rail A: " + railAName, headingStyle);
        newRailAName = EditorGUILayout.TextField( newRailAName);
        if (GUILayout.Button("Rename Rail Prefab"))
        {
            editor.RenamePrefab(railAName, newRailAName);
            Close();
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rail B: " + railBName, headingStyle);
        newRailBName = EditorGUILayout.TextField(newRailBName);
        if (GUILayout.Button("Rename Rail Prefab"))
        {
            editor.RenamePrefab(railBName, newRailBName);
            Close();
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Post: " + postName, headingStyle);
        newPostName = EditorGUILayout.TextField(newPostName);
        if (GUILayout.Button("Rename Post Prefab"))
        {
            editor.RenamePrefab(postName, newPostName);
            Close();
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator(); EditorGUILayout.Separator(); EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Extra: " + extraName, headingStyle);
        newExtraName = EditorGUILayout.TextField(newExtraName);
        if (GUILayout.Button("Rename Extra Prefab"))
        {
            editor.RenamePrefab(extraName, newExtraName);
            Close();
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();




        EditorGUILayout.Separator(); EditorGUILayout.Separator();
        if (GUILayout.Button( "Cancel"))
        {
            Close();
            GUIUtility.ExitGUI();
        }


        EditorGUILayout.Separator(); EditorGUILayout.Separator();

        GUILayout.EndVertical();

	}
}