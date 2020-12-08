using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

//== For shuffling variations, high quality randomness is not needed, and much easier to save a lookup entry with a preset.
/* This is a work in progress... most is not implemented yet */

[CreateAssetMenu(fileName = "preset", menuName = "AutoFence/Random", order = 1)]
public class RandomLookupAFWB : ScriptableObject
{
    public const int kRandLookupTableSize = 1000;
    public static RandomLookupAFWB randForRailA = null, randForRailB = null, randForPost = null;
    public static int railARandLookupIndex = 0, railBRandLookupIndex = 123, postRandLookupIndex = 234;
    public float[] randFloats = new float[kRandLookupTableSize];
    public int currIndex = 0;

    public void FillRand()
    {
        for (int i = 0; i < kRandLookupTableSize; i++)
        {
            randFloats[i] = UnityEngine.Random.value;
        }
    }
    //--------------
    // return an int in range inclusive of both min and max
    public int RandomRange(int min, int max)
    {
        float val = randFloats[currIndex];
        int rand = (int)Mathf.Floor(val * (max - min)) + min;
        //Debug.Log("lookupIndex " + lookupIndex + "   val  " + val + "   rand  " + rand + "\n");
        // Every time we access the table we increment the lookup index
        currIndex++;
        if (currIndex >= kRandLookupTableSize)
            currIndex = 0;
        return rand;
    }
    //--------------
    public int RandomRangeIndexed(int min, int max, ref int lookupIndex)
    {
        float val = randFloats[lookupIndex];
        int rand = (int)Mathf.Floor(val * (max - min)) + min;
        //Debug.Log("lookupIndex " + lookupIndex + "   val  " + val + "   rand  " + rand + "\n");
        // Every time we access the table we increment the lookup index
        lookupIndex++;
        if (lookupIndex >= kRandLookupTableSize)
            lookupIndex = 0;
        return rand;
    }
    //--------------
    public float RandomRange(float min, float max, ref int lookupIndex)
    {
        float val = randFloats[lookupIndex];
        float rand = (val * (max - min)) + min;
        //Debug.Log("lookupIndex " + lookupIndex + "   val  " + val + "   rand  " + rand + "\n");
        lookupIndex++;
        if (lookupIndex >= kRandLookupTableSize)
            lookupIndex = 0;
        return rand;
    }
    //----------------
    // returns random float in range 0.0 to 1.0
    public float Random()
    {
        float randVal = randFloats[currIndex];
        // Every time we access the table we increment the lookup index
        currIndex++;
        if (currIndex >= kRandLookupTableSize)
            currIndex = 0;
        return randVal;
    }
    //----------------
    // returns random float in range 0.0 to 1.0
    public float Random(ref int lookupIndex)
    {
        float randVal = randFloats[lookupIndex];
        // Every time we access the table we increment the lookup index
        lookupIndex++;
        if (lookupIndex >= kRandLookupTableSize)
            lookupIndex = 0;
        return randVal;
    }
    //-------------------------------------
    // Doing it like this ready for v3.1 adding more random init stuff
    public static RandomLookupAFWB CreateRandomTable(int startIndex)
    {
        RandomLookupAFWB newTable = RandomLookupAFWB.CreateInstance<RandomLookupAFWB>();
        newTable.FillRand();
        newTable.currIndex = startIndex;
        return newTable;
    }
    //--------------
    /*public static bool SaveRandomLookup(RandomLookupAFWB randLookup, string savePath)
    {

        AssetDatabase.CreateAsset(randLookup, savePath);

        AssetDatabase.SaveAssets();
        return true;
    }*/

}

