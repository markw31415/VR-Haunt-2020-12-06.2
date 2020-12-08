using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishedFenceUtilities : MonoBehaviour
{
	public string presetID;
	public AutoFenceCreator af = null;
	public Transform finishedFolderRoot;

	void Awake()
	{
		af = GameObject.FindObjectOfType<AutoFenceCreator>();
		finishedFolderRoot = transform.root;
	}

	void Reset()
	{
		af = GameObject.FindObjectOfType<AutoFenceCreator>();
		finishedFolderRoot = transform.root;
	}
}
