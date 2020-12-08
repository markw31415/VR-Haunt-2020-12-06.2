using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AFWBCustomDemo))]
class AFWBCustomDemoEditor : Editor {

	public AFWBCustomDemo demoScript;

	public override void OnInspectorGUI() {
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("(If error - click on Auto Fence Builder first to ensure it's loaded and initialized.)");
		EditorGUILayout.Separator();
		if(GUILayout.Button("Test")){
			Debug.Log("Testing " + target.name + "\n");
			demoScript = (AFWBCustomDemo)target;
			demoScript.TestDemo();
		}
		EditorGUILayout.Separator();
	}
}