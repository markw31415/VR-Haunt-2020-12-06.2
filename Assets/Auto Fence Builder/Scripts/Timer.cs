using UnityEngine;
using System.Collections;
using System;
// Two Click Tools Timer Utility
/* Usage:

Timer t = new Timer("Timing a complex loop");

...

t.Lap("test") // time elapsed so far, will print:  "test 50ms"

at the end:  t.End(); This will print:  "Timing a complex loop: 123ms"
*/

public class Timer {

	static public bool globalTimerDisplayEnabled = true;

	DateTime startTime;
	float timeDelta = 0, timeAtLastLap = 0;
	float[] lapTimes = new float[100];
	string[] lapStrings = new string[100];
	int numLapTimes = 0;
	string description;
	bool enabled = true;

	public Timer(string str = "", bool enabled = true)
	{
		Start ();
		description = str;
		this.enabled = enabled;
	}

	public void Start(){
		startTime = System.DateTime.Now;
	}
	public float End(){
		timeDelta = (float)System.DateTime.Now.Subtract(startTime).TotalMilliseconds;
		if(enabled && globalTimerDisplayEnabled){
			description += " : ";
			for(int i=0; i<numLapTimes; i++){
				description += " (" + lapStrings[i] + " " + lapTimes[i].ToString("F1") + ") ";
			}
			Debug.Log(description + timeDelta.ToString("F1") + "ms\n");
		}
		return timeDelta;
	}
	public float Lap(string lapStr = ""){
		timeDelta = (float)System.DateTime.Now.Subtract(startTime).TotalMilliseconds;
		float lapTime = timeDelta - timeAtLastLap;
		timeAtLastLap = timeDelta;
		lapStrings[numLapTimes] = lapStr + ": ";
		lapTimes[numLapTimes++] = lapTime;
		return lapTime;
	}
	public void Reset(){
		Start ();
	}
	public float GetTime(){
		timeDelta = (float)System.DateTime.Now.Subtract(startTime).TotalMilliseconds;
		return timeDelta;
	}
}
