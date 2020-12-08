using UnityEngine;
using System.Collections;
using UnityEditor;
public class AutoFenceManagerMenu : MonoBehaviour {

	AutoFenceCreator creator;

	//static public int globalNumFences = 0;

	[MenuItem ("GameObject/Create Auto Fence Builder #&f")]

	static void CreateFenceManager() {
        //globalNumFences += 1;
        
        AutoFenceCreator af = GameObject.FindObjectOfType<AutoFenceCreator>();

        if (af != null)
        {
	        Debug.LogWarning("Only 1 instance of AutoFenceBuilder can be used in the hierarchy. Use 'Finish & Start New' with Auto Fence Builder to make multiple fences");
	        Selection.activeGameObject = af.transform.gameObject; 
        }
        else
        {
	        string autoFenceName = "Auto Fence Builder";
	        GameObject go = new GameObject(autoFenceName);
	        go.transform.position = Vector3.zero;
	        go.AddComponent(typeof(AutoFenceCreator));
	        Selection.activeGameObject = go; 
        }
        
	}

}
