/* Auto Fence & Wall Builder v3.0 twoclicktools@gmail.com Feb 2019 */
#pragma warning disable 0219 // disbale unused variables warnings. Most of them needed ready for updates
#pragma warning disable 0414

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine.UI;

public enum VariationMode { optimal = 0, random, sequenced };

[System.Serializable]
public class RandomRecords
{
    public int sequenceShuffle;
    public int heightVariation, smallRotVariation, quantRotVariation;
}

[System.Serializable]
public class SeqInfo
{
    public int numSteps = 5; 
    public int currStepIndex=0;
    //public int[] stepVarIndex = new int[AutoFenceCreator.kMaxNumSeqSteps]; //which of the variants do each step use
}

[ExecuteInEditMode]
//------------------------------------
[System.Serializable]
public class AutoFenceCreator : MonoBehaviour
{
    LayerSet kRailALayer = AutoFenceCreator.LayerSet.railALayerSet; // to save a lot of typing
    LayerSet kRailBLayer = AutoFenceCreator.LayerSet.railBLayerSet;
    LayerSet kPostLayer = AutoFenceCreator.LayerSet.postLayerSet;
    LayerSet kExtraLayer = AutoFenceCreator.LayerSet.extraLayerSet;
    LayerSet kSubpostLayer = AutoFenceCreator.LayerSet.subpostLayerSet;
    LayerSet kAllLayer = AutoFenceCreator.LayerSet.allLayerSet;
    
    public enum ComponentToolbar { posts = 0, railsA, railsB, subposts, extras };
    public ComponentToolbar componentToolbar = 0;
    
    public const int kNumRailVariations = 5; //the number of GameObject variations INCLUDING the main
    public const int kNumPostVariations = 5; //the number of GameObject variations INCLUDING the main
    public const int kMaxNumSeqSteps = 20;
    public const int kMaxNumSingles = 100;
    public const int kSkipIndex = 9999; // denotes a 'single' replacement should be skipped (similar to a gap)
    public List<string> categoryNames = new List<string>(new string[]
    { "All New Variation Examples", "Auto", "Basic Templates", "Brick", "Brick + Wood", "Castle", "Demo Usage", "Concrete", "Concrete + Wood", "Favorite", 
    "Industrial", "Metal", "Military", "Natural", "Objects", "Other", "Railings", "Residential", "Stone",  "Test", "Urban", "Variation Templates", "Walls", "Wire", "Wood" }); //Don't use '&', it confuses submenu names

    public enum SplineFillMode { fixedNumPerSpan = 0, equiDistant, angleDependent };
    public enum FencePrefabType { postPrefab = 0, railPrefab, extraPrefab, allPrefab, nonePrefab }; 
    public enum FenceSlopeMode { slope = 0, step, shear };
    public enum LayerSet { railALayerSet = 0, railBLayerSet, postLayerSet, extraLayerSet, subpostLayerSet, allLayerSet, noneLayerSet };
    public enum VariationSwapOutMode { linearCycle = 0, randomProb, sequenced }; // we're referring to either the Main rails, or the Secondary rails
    public enum ItemVariationMode { optimalVariation = 0, random, randomNoRepeat, shuffled, sequenced };

    SeqVariant baseIV = new SeqVariant(null, false, false, false, 0, Vector3.zero, Vector3.one, Vector3.zero, 1, true);

    //List<SeqVariant> testSequence = new List<SeqVariant>(new SeqVariant[] { new SeqVariant(0), new SeqVariant(0), new SeqVariant(0), new SeqVariant(0) });
   

    public int autoFenceInstanceNum = 0; // 1-based indexing
    int objectsPerFolder = 100; // lower this number if using high-poly meshes. Only 65k can be combined, so objectsPerFolder * [number of verts/tris in mesh] must be less than 65,000
    public const float DEFAULT_RAIL_LENGTH = 3.0f;

    [Range(0.1f, 10.0f)]
    public float gs = 1.0f; //global scale, avoided long name as it occurs so often and takes up space!
    public Vector3 globalScale = Vector3.one;
    public bool scaleInterpolationAlso = true; // this can be annoying if you want your posts to stay where they are.

    public Vector3 startPoint = Vector3.zero;
    public Vector3 endPoint = Vector3.zero;
    List<Vector3> gaps = new List<Vector3>(); // stores the location of gap start & ends: {start0, end0, start1, end1} etc.
    [Tooltip(AFBTooltipsText.allowGaps)]
    public bool allowGaps = true, showDebugGapLine = true; // draws a blue line to fill gaps, only in Editor

    int defaultPoolSize = 30;

    public GameObject fencesFolder, postsFolder, railsFolder, subpostsFolder, extrasFolder;
    public List<GameObject> folderList = new List<GameObject>();

    public List<Transform> subposts = new List<Transform>();
    private List<Transform> subJoiners = new List<Transform>();
    private List<Transform> markers = new List<Transform>();
    public List<Transform> extras = new List<Transform>();

    //[NonSerialized]
    /*public List<Mesh> uniqueRailMeshesA = new List<Mesh>();// We need new meshes to make modified versions if they become modified (e.g. when sheared)
                                                        // we can't just modify the shared mesh, else all of them would be changed identically
    public List<Mesh> uniqueRailMeshesB = new List<Mesh>();*/
    private List<Vector3> interPostPositions = new List<Vector3>(); // temp for calculating linear interps
    public List<Vector3> clickPoints = new List<Vector3>(); // the points the user clicked, pure.
    public List<int> clickPointFlags = new List<int>(); //to hold potential extra info about the click points. Not used in v1.1 and below
    public List<Vector3> keyPoints = new List<Vector3>(); // the clickPoints + some added primary curve-fitting points
    public List<Vector3> allPostsPositions = new List<Vector3>(); // all
    public List<Vector3> handles = new List<Vector3>(); // the positions of the transform handles

    public List<GameObject> subPrefabs = new List<GameObject>();
    public List<GameObject> subJoinerPrefabs = new List<GameObject>();
    public List<GameObject> extraPrefabs = new List<GameObject>();
    public GameObject clickMarkerObj;

    public List<string> subNames = new List<string>();
    public List<string> extraNames = new List<string>();

    public int postCounter = 0, subCounter = 0, subJoinerCounter = 0, extraCounter = 0;
    public int railACounter = 0, railBCounter = 0;
    public int railsATotalTriCount = 0, railsBTotalTriCount = 0, postsTotalTriCount = 0, subPostsTotalTriCount = 0, extrasTotalTriCount = 0;

    public int currentPostType = 0;

    public int currentExtraType = 0;
    public int lastMenuSelectionOfCurrentRailType = 0; //needed if the user adds a custom rail, but then wants to revert to the previous selction
    public int currentSubpostType = 0;
    public int currentSubJoinerType = 0;

    //List of a List. Each go can have a list of submeshes, so this is a list of those submesh lists. They hold pure umodified versions 
    // of the meshes (from the prefabs on disk), so that they can be restored after being rotated/sheared
    public List<List<Mesh>> origRailPrefabMeshes = new List<List<Mesh>>();
    //public List<List<Mesh>> origRailPrefabMeshesVar1 = new List<List<Mesh>>();
    public List<List<Mesh>> origPostPrefabMeshes = new List<List<Mesh>>();
    //== Random Seeds (rs) save these so we can recall the same randomness with the presets ===
    public int rsPostSpacing = 0; //rsRailARand=1;
    public int categoryIndex = 0;

    //===== Fence height ======
    [Range(0.2f, 10.0f)]
    public float fenceHeight = 1f, fenceWidth = 1f;
    public bool keepGlobalScaleHeightWidthLinked = true;
    //======================== 
    //        Posts 
    //========================
    public const int kNumberOfPostVariations = 5; //the number INCLUDING the main
    public int postMenuIndex = 1;
    public List<Transform> posts = new List<Transform>(); // A list of positions that will be generated for every post position
    public List<GameObject> postPrefabs = new List<GameObject>();
    public List<string> postNames = new List<string>();
    public bool usePosts = true;
    public GameObject userPostObject = null, oldUserPostObject = null;
    public Vector3 postSize = Vector3.one;
    [Tooltip(AFBTooltipsText.mainPostSizeBoost)]
    public Vector3 mainPostSizeBoost = Vector3.one; // Boosts the size of the main (user click-point) posts, not the interpolated posts. Good for extra variation
    [Range(-1.0f, 4.0f)]
    public float postHeightOffset = 0;
    public Vector3 postRotation = Vector3.zero;
    [Tooltip(AFBTooltipsText.lerpPostRotationAtCorners)]
    public bool lerpPostRotationAtCorners = true, lerpPostRotationAtCornersInters = true; // should we rotate the corner posts so they are the average direction of the rails.
    public bool hideInterpolated = false;
    public Vector3 nativePostScale = Vector3.one;
    //============================ Post Variations =================================
    [Range(0.0f, 1.0f)]
    public float postSpacingVariation = 0.05f;
    public float actualInterPostDistance = 3.0f; // This is the final interpost distance (closest to user's request) that gives while number of sections 
    public float postSpacingVariationScalar = 0.2f;// so the display can read 0-1 neatly
    public bool allowRotations_Y_Post = false, allowVertical180Invert_Post = false;
    public bool allowMirroring_X_Post = false, allowMirroring_Z_Post = false, jitterPostVerts = false;
    public Vector3 jitterAmountPost = new Vector3(0.03f, 0.03f, 0.03f);
    public int variationRotationQuantize_Y_Post = 0;
    [Range(0.0f, 1.0f)]
    public float mirrorXPostProbability = 0, mirrorZPostProbability = 0;
    public GameObject postVariation1, postVariation2, postVariation3; //Only used for the editor boxes placeholders
    public List<FenceVariant> postVariants = new List<FenceVariant>(new FenceVariant[kNumberOfPostVariations]);
    //public List<GameObject> postVariationGOs = new List<GameObject>(new GameObject[] {null, null, null, null});
    List<GameObject> nonNullPostGOs = new List<GameObject>(new GameObject[] { null, null, null, null });
    //public VariationSwapOutMode postSwapOutMode = VariationSwapOutMode.linearCycle;
    public bool usePostVariations = false;
    
    public List<FenceVariant> nonNullPostVariants = new List<FenceVariant>();
    public List<GameObject> postDisplayVariationGOs = new List<GameObject>(new GameObject[kNumPostVariations]);
    public Vector3[] varPostPositionOffset = new Vector3[kNumPostVariations];
    public Vector3[] varPostSize = new Vector3[kNumPostVariations];
    public Vector3[] varPostRotation = new Vector3[kNumPostVariations];
    public float[] varPostProbs = new float[kNumPostVariations];
    public List<int> varPrefabIndexPost = new List<int>(new int[kNumPostVariations]);
    public List<int> varMenuIndexPost = new List<int>(new int[kNumPostVariations]);
    public List<SeqVariant> optimalSequencePost = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public List<SeqVariant> userSequencePost = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public bool[] seqPostX = new bool[kMaxNumSeqSteps], seqPostZ = new bool[kMaxNumSeqSteps], seqPostInvert180 = new bool[kMaxNumSeqSteps]; // the sequence orientations
    public Vector3[] seqPostSize = new Vector3[kMaxNumSeqSteps], seqPostOffset = new Vector3[kMaxNumSeqSteps], seqPostRotate = new Vector3[kMaxNumSeqSteps];
    public int[] seqPostVarIndex = new int[kMaxNumSeqSteps];//
    public bool[] usePostVar = new bool[kNumPostVariations];
    public bool[] seqPostStepEnabled = new bool[kMaxNumSeqSteps];

    public SeqInfo postSeqInfo = new SeqInfo();

    //===================   Post Randomization   ======================
    public float minPostHeightLimit = 0.5f, maxPostHeightLimit = 1.5f;
    public float minPostHeightVar = 0.96f, maxPostHeightVar = 1.04f, minSubpostHeightVar = 0.96f, maxSubpostHeightVar = 1.04f;
    public bool allowRandPostRotationVariation = false, allowRandSubpostRotationVariation = false, allowPostHeightVariation = false, allowSubpostHeightVariation = false;
    public Vector3 postRandRotationAmount = new Vector3(0.03f, 0.03f, 0.03f), subpostRandRotationAmount = new Vector3(0.03f, 0.03f, 0.03f);
    public int postRandomScope = 0, subpostRandomScope = 0;
    [Range(0.0f, 1.0f)]
    public float chanceOfMissingPost = 0, chanceOfMissingSubpost = 0;
    public float postQuantizeRotAmount = 90, subpostQuantizeRotAmount = 90;
    public bool allowQuantizedRandomPostRotation = false, allowQuantizedRandomSubpostRotation = false;


    //======================== 
    //        Rails 
    //========================
    public int railSetToolbarChoice = 0; // 0 = Rails A, 1 = Rails B
    public List<string> railNames = new List<string>();
    public List<Transform> railsA = new List<Transform>();
    public List<Transform> railsB = new List<Transform>();
    public List<GameObject> railPrefabs = new List<GameObject>();
    public int currentRailAType = 0, currentRailBType = 0, railAMenuIndex = 0, railBMenuIndex = 0;
    
    public List<int> varPrefabIndexRailA = new List<int>(new int[kNumRailVariations]);
    public List<int> varPrefabIndexRailB = new List<int>(new int[kNumRailVariations]);
    public List<int> varMenuIndexRailA = new List<int>(new int[kNumRailVariations]);
    public List<int> varMenuIndexRailB = new List<int>(new int[kNumRailVariations]);

    public bool rotateFromBaseRailA = false, rotateFromBaseRailB = false;
    
    [Tooltip(AFBTooltipsText.numStackedRailsA)]
    [Range(0, 12)]
    public int numStackedRailsA = 3;
    [Range(1, 12)]
    public int numStackedRailsB = 1;
    public bool useRailsA = true, useRailsB = true;
    public GameObject userRailObject = null;//userRailBObject = null;
    public bool useCustomRailA = false, useCustomRailB = false, useCustomPost = false, useCustomExtra = false;
    [Range(0.02f, 10.0f)]
    public float railASpread = 1.0f, railBSpread = 0.5f;
    public float minGap = 0.1f, maxGap = 1.5f;
    public Vector3 nativeRailAScale = Vector3.one;
    public Vector3 nativeRailBScale = Vector3.one;

    public Vector3 railAPositionOffset = Vector3.zero, railBPositionOffset = Vector3.zero;
    public Vector3 railASize = Vector3.one, railBSize = Vector3.one;
    public Vector3 railARotation = Vector3.zero, railBRotation = Vector3.zero;
    public bool centralizeRails = false;
    public bool autoHideBuriedRails = true;
    [Tooltip(AFBTooltipsText.overlapAtCorners)]
    public bool overlapAtCorners = true;
    [Tooltip(AFBTooltipsText.rotateY)]
    public bool rotateY = false;// used in repetition disguise variations
    [Range(0.0f, 1.0f)]
    public float chanceOfMissingRailA = 0.0f;
    [Range(0.0f, 1.0f)]
    public float chanceOfMissingRailB = 0.0f;
    float sectionInclineAngle = 0.0f; // the angle of the fence as it goes across ground inclines
    // Rail Variations
    [Range(0.0f, 1.0f)]
    public float mirrorXRailProbability = 0, mirrorZRailProbability = 0, verticalInvertRailProbability = 0;
    public int railARandomScope = 0, railBRandomScope = 0;
    public bool railAKeepGrounded = true, railBKeepGrounded = true;
    public SeqInfo railASeqInfo = new SeqInfo(), railBSeqInfo = new SeqInfo();

    //===================   Rail Randomization   ======================
    public float minRailHeightLimit = 0.5f, maxRailHeightLimit = 1.5f;
    public float minRailAHeightVar = 0.97f, maxRailAHeightVar = 1.03f, minRailBHeightVar = 0.9f, maxRailBHeightVar = 1.1f;
    public bool allowRandRailARotationVariation = true, allowRandRailBRotationVariation = true;
    public Vector3 railARandRotationAmount = new Vector3(0.03f, 0.03f, 0.03f), railBRandRotationAmount = new Vector3(0.03f, 0.03f, 0.03f);

    //===================   Rail Variations   ======================
    public bool allowMirroring_X_Rail = false, allowMirroring_Z_Rail = false, allowVertical180Invert_Rail = false;
    public bool jitterRailAVerts = false, jitterRailBVerts = false;
    public Vector3 jitterAmountRail = new Vector3(0.03f, 0.03f, 0.03f);
    //public VariationSwapOutMode railSwapOutMode = VariationSwapOutMode.linearCycle;
    [Range(1, kNumRailVariations - 1)]
    //public int numRailAVarObjectsToUse = 1, numRailBVarObjectsToUse =1;

    public List<FenceVariant> railAVariants = new List<FenceVariant>(new FenceVariant[kNumRailVariations]);//similar to nonNullVariations, but can be null f not active
    public List<FenceVariant> railBVariants = new List<FenceVariant>(new FenceVariant[kNumRailVariations]);

    public List<GameObject> railADisplayVariationGOs = new List<GameObject>(new GameObject[kNumRailVariations]);
    public List<GameObject> railBDisplayVariationGOs = new List<GameObject>(new GameObject[kNumRailVariations]);
    public List<List<Mesh>> railAPreparedMeshVariants = new List<List<Mesh>>(new List<Mesh>[kNumRailVariations]); //prepared mesh for each variations
    public List<List<Mesh>> railBPreparedMeshVariants = new List<List<Mesh>>(new List<Mesh>[kNumRailVariations]);
    public List<FenceVariant> nonNullRailAVariants = new List<FenceVariant>();
    public List<FenceVariant> nonNullRailBVariants = new List<FenceVariant>();
    public bool[] useRailVarA = new bool[kNumRailVariations], useRailVarB = new bool[kNumRailVariations];
    public Vector3[] varRailAPositionOffset = new Vector3[kNumRailVariations], varRailBPositionOffset = new Vector3[kNumRailVariations];
    public Vector3[] varRailASize = new Vector3[kNumRailVariations], varRailBSize = new Vector3[kNumRailVariations];
    public Vector3[] varRailARotation = new Vector3[kNumRailVariations], varRailBRotation = new Vector3[kNumRailVariations];
    [Range(0.01f, 1.0f)]
    public float[] varRailAProbs = new float[kNumRailVariations], varRailBProbs = new float[kNumRailVariations];
    [Range(0.0f, 1.0f)]
    public float[] varRailABackToFront = new float[kNumRailVariations], varRailAMirrorZ = new float[kNumRailVariations],
        varRailAInvert = new float[kNumRailVariations];
    [Range(0.0f, 1.0f)]
    public float[] varRailBBackToFront = new float[kNumRailVariations], varRailBMirrorZ = new float[kNumRailVariations],
        varRailBInvert = new float[kNumRailVariations];

    public List<SeqVariant> userSequenceRailA = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public List<SeqVariant> userSequenceRailB = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public List<SeqVariant> optimalSequenceRailA = new List<SeqVariant>(new SeqVariant[16]);
    public List<SeqVariant> optimalSequenceRailB = new List<SeqVariant>(new SeqVariant[16]);
    //public int numUserSeqStepsRailA = 7, numUserSeqStepsRailB = 7;


    public bool[] varRailABackToFrontBools = new bool[kNumRailVariations], varRailAMirrorZBools = new bool[kNumRailVariations],
         varRailAInvertBools = new bool[kNumRailVariations];

    public bool[] varRailBBackToFrontBools = new bool[kNumRailVariations], varRailBMirrorZBools = new bool[kNumRailVariations],
        varRailBInvertBools = new bool[kNumRailVariations];

    [Tooltip("Enable use of Rail A Variations")]
    public bool useRailAVariations = false, useRailBVariations = false;
    public bool allowRailAHeightVariation = false, allowRailBHeightVariation = false;

    public bool allowIndependentSubmeshVariationA = true, allowIndependentSubmeshVariationB = true;
    public bool useRailASeq = false, useRailBSeq = false;
    public bool scaleVariationHeightToMainHeightA = true, scaleVariationHeightToMainHeightB = true; //if height of variation object differs, then match main

    public VariationMode variationModeRailA = VariationMode.sequenced, variationModeRailB = VariationMode.sequenced;
    public int[] shuffledRailAIndices = new int[1], shuffledRailBIndices = new int[1];

    //public bool[] seqAX = new bool[kMaxNumSeqSteps], seqAZ = new bool[kMaxNumSeqSteps], seqAInvert180 = new bool[kMaxNumSeqSteps]; // the sequence orientations
    public Vector3[] seqRailASize = new Vector3[kMaxNumSeqSteps], seqRailAOffset = new Vector3[kMaxNumSeqSteps], seqRailARotate = new Vector3[kMaxNumSeqSteps];
    public int[] seqRailAVarIndex = new int[kMaxNumSeqSteps];
    public bool[] seqRailAStepEnabled = new bool[kMaxNumSeqSteps], seqRailBStepEnabled = new bool[kMaxNumSeqSteps];
    //======================== 
    //public bool[] seqBX = new bool[kMaxNumSeqSteps], seqBZ = new bool[kMaxNumSeqSteps], seqBInvert180 = new bool[kMaxNumSeqSteps]; // the sequence orientations
    public Vector3[] seqRailBSize = new Vector3[kMaxNumSeqSteps], seqRailBOffset = new Vector3[kMaxNumSeqSteps], seqRailBRotate = new Vector3[kMaxNumSeqSteps];
    public int[] seqRailBVarIndex = new int[kMaxNumSeqSteps];
    public List<int> railASingles = new List<int>(new int[kMaxNumSingles]), railBSingles = new List<int>(new int[kMaxNumSingles]);
    public List<int> postSingles = new List<int>(new int[kMaxNumSingles]);//Temp - Don't rely on these, could change to dictionary in v3.x
    public List<FenceVariant> railASingleVariants = new List<FenceVariant>(), railBSingleVariants = new List<FenceVariant>();
    public RandomRecords railARandRec, railBRandRec, postRandRec;
    
    public bool allowBackToFrontRailA = true, allowMirrorZRailA = true, allowInvertRailA = true;
    public bool allowBackToFrontRailB = true, allowMirrorZRailB = true, allowInvertRailB = true;
    
    //======= Subs ========
    public int subpostMenuIndex = 1;
    public bool useSubposts = false;
    public int subsFixedOrProportionalSpacing = 1;
    [Range(0.1f, 50)]
    public float subSpacing = 0.5f;
    public Vector3 subpostPositionOffset = Vector3.zero;
    public Vector3 subpostSize = Vector3.one;
    public Vector3 subpostRotation = Vector3.zero;
    public bool forceSubsToGroundContour = false;
    public bool addSubpostAtPostPointAlso = false;
    [Tooltip(AFBTooltipsText.subsGroundBurial)]
    [Range(-2.0f, 0.0f)]
    public float subsGroundBurial = 0.0f;
    //List<Material> originalSubMaterials = new List<Material>();
    [Range(0.0f, 1.0f)]
    public float chanceOfMissingSubs = 0.0f;
    public Vector3 nativeSubScale = Vector3.one;

    public bool useWave = false;
    [Range(0.01f, 10.0f)]
    public float frequency = 1;
    [Range(0.0f, 2.0f)]
    public float amplitude = 0.25f;
    [Range(-Mathf.PI * 4, Mathf.PI * 4)]
    public float wavePosition = Mathf.PI / 2;
    public bool useSubJoiners = false;
    [Range(-3.0f, 3.0f)]
    public float subPostSpread = 1;
    
    public List<FenceVariant> nonNullSubpostVariants = new List<FenceVariant>();
    public bool[] seqSubpostStepEnabled = new bool[kMaxNumSeqSteps];
    public bool[] seqSubpostX = new bool[kMaxNumSeqSteps], seqSubpostZ = new bool[kMaxNumSeqSteps], seqSubpostInvert180 = new bool[kMaxNumSeqSteps]; // the sequence orientations
    public int[] seqSubpostVarIndex = new int[kMaxNumSeqSteps];
    public Vector3[] varSubpostPositionOffset = new Vector3[kNumPostVariations];
    public Vector3[] varSubpostSize = new Vector3[kNumPostVariations];
    public Vector3[] varSubpostRotation = new Vector3[kNumPostVariations];
    public Vector3[] seqSubpostSize = new Vector3[kMaxNumSeqSteps], seqSubpostOffset = new Vector3[kMaxNumSeqSteps], seqSubpostRotate = new Vector3[kMaxNumSeqSteps];
    public bool[] useSubpostVar = new bool[kNumPostVariations];
    public List<int> varPrefabIndexSubpost = new List<int>(new int[kNumPostVariations]);
    public List<int> varMenuIndexSubpost= new List<int>(new int[kNumPostVariations]);
    public bool useSubpostVariations = true;
    public List<GameObject> subpostDisplayVariationGOs = new List<GameObject>(new GameObject[kNumPostVariations]);
    public bool useSubpostSeq = false;
    public List<FenceVariant> subpostVariants = new List<FenceVariant>(new FenceVariant[kNumPostVariations]);
    public List<SeqVariant> optimalSequenceSubpost = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public List<SeqVariant> userSequenceSubpost = new List<SeqVariant>(new SeqVariant[kMaxNumSeqSteps]);
    public SeqInfo subpostSeqInfo = new SeqInfo();
    //======================== 
    //        Extras 
    //========================
    public int extraMenuIndex = 0;
    public GameObject userExtraObject = null, oldExtraGameObject = null;
    public bool useExtraGameObject = true, makeMultiArray = false, keepArrayCentral = true;
    public bool currentExtraIsPreset = true;
    public Vector3 extraPositionOffset = Vector3.zero;
    public Vector3 extraSize = Vector3.one;
    public Vector3 extraRotation = Vector3.zero;
    public Vector3 extraGameObjectOriginalScale = Vector3.one;
    public Vector3 multiArraySize = new Vector3(1, 1, 1), multiArraySpacing = new Vector3(1, 1, 1);
    public Vector3 nativeExtraScale = Vector3.one;
    [Tooltip(AFBTooltipsText.relativeScaling)]
    public bool relativeScaling = true;
    [Tooltip(AFBTooltipsText.relativeMovement)]
    public bool relativeMovement = false;
    [Tooltip(AFBTooltipsText.autoRotateExtra)]
    public bool autoRotateExtra = true;
    [Range(0, 21)]
    public int extraFrequency = 1;
    [Range(1, 12)]
    public int numExtras = 2;
    [Range(0.02f, 5f)]
    public float extrasGap = 1;
    [Tooltip(AFBTooltipsText.raiseExtraByPostHeight)]
    public bool raiseExtraByPostHeight = true;
    public bool extrasFollowIncline = true;
    [Range(0.0f, 1.0f)]
    public float chanceOfMissingExtra = 0.0f;

    
    //===== Interpolate =========
    [Tooltip(AFBTooltipsText.subsGroundBurial)]
    public bool interpolate = true;
    [Range(0.25f, 25.0f)]
    public float interPostDist = 3.0f;
    public float baseInterPostDistance = 3.0f;

    
    public bool keepInterpolatedPostsGrounded = true;
    //===== Snapping =========
    public bool snapMainPosts = false;
    public float snapSize = 1.0f;

    //===== Smoothing =========
    [Tooltip(AFBTooltipsText.smooth)]
    public bool smooth = false;
    [Range(0.0f, 1.0f)]
    public float tension = 0.0f;
    [Range(1, 50)]
    public int roundingDistance = 6;
    [Range(0, 45)]
    [Tooltip(AFBTooltipsText.subsGroundBurial)]
    public float removeIfLessThanAngle = 4.5f;
    [Range(0.02f, 10)]
    [Tooltip(AFBTooltipsText.stripTooClose)]
    public float stripTooClose = 0.35f;

    public bool closeLoop = false;
    [Range(0.0f, 0.5f)]
    public float randomPostHeight = 0.1f;
    Vector3 preCloseEndPost;
    public bool showControls = false;

    //public List<AutoFencePreset> presets = new List<AutoFencePreset>(); //Old preset system deprectaed in V3.0
    public int currentPresetIndex = 0, currentScrPresetIndex = 0;

    public Vector3 lastDeletedPoint = Vector3.zero;
    public int lastDeletedIndex = 0;
    [Tooltip(AFBTooltipsText.addColliders)]
    //public bool addColliders = false;
    public int postColliderMode = 2; //0 = single box, 1 = keep original (user), 2 = no colliders,  3 = meshCollider
    public int railAColliderMode = 0; //0 = single box, 1 = keep original (user), 2 = no colliders,  3 = meshCollider
    public int extraColliderMode = 2; //0 = single box, 1 = keep original (user), 2 = no colliders,  3 = meshCollider
    public bool addBoxCollidersToRailB = false;

    public FenceSlopeMode slopeModeRailA = FenceSlopeMode.shear, slopeModeRailB = FenceSlopeMode.shear;
    public int clearAllFencesWarning = 0;

    [Range(0.0f, 90.0f)]    // You can tweak the max range settings 0 - 90. ** all x3 compared to v1.21
    public float randomRoll = 0.0f;
    [Range(0.0f, 20.0f)]
    public float randomYaw = 0.0f;
    [Range(0.0f, 30.0f)]
    public float randomPitch = 0.0f;

    public bool useRandom = true;
    [Range(0.0f, 1.0f)]
    public float affectsPosts = 1.0f, affectsRailsA = 1.0f, affectsRailsB = 1.0f, affectsSubposts = 1.0f, affectsExtras = 1.0f;
    int randomSeed = 417;

    //public AFBPresetManager presetManager = null;//Old preset system deprectaed in V3.0
    public bool weld = true;
    public LayerMask groundLayers;
    public int ignoreControlNodesLayerNum = 8;

    //---------- Cloning & Copying ----------
    public GameObject fenceToCopyFrom = null;
    FenceCloner fenceCloner = null;
    [Tooltip(AFBTooltipsText.globalLift)]
    [Range(-1.0f, 50.0f)]
    public float globalLift = 0.0f; //This lifts the whole fence off the ground. Used for stacking different fences, should be 0 for normal use

    public bool addCombineScripts = false;
    public bool usingStaticBatching = false;
    public int batchingMode = 1; //0=unity static batching, 1=use a combine script, 2 = none

    GameObject tempDebugMarker = null, tempDebugMarker2 = null;
    List<GameObject> tempMarkers = new List<GameObject>();

    Vector3 newPivotPoint = Vector3.zero;
    List<Vector3> overlapPostErrors = new List<Vector3>();
    public List<float> userSubMeshRailOffsets = new List<float>(); //if the user's custom rail contains submeshes, these are their offsets

    float prevRelativeDistance = 0; // used in shearing of rail meshes
    public Vector3 railUserMeshBakeRotations = Vector3.zero, postUserMeshBakeRotations = Vector3.zero;
    public int railBakeRotationMode = 1, postBakeRotationMode = 1, extraBakeRotationMode = 1; // 0 = user custom settings, 1 = auto, 2 = don't rotate mesh
    public GameObject currentCustomRailObject = null, currentCustomPostObject = null, currentCustomExtraObject = null;
    public Vector3 autoRotationResults = Vector3.zero;

    public float railBoxColliderHeightScale = 1.0f; // customizeable BoxColliders on rails/walls
    public float railBoxColliderHeightOffset = 0.0f;
    public bool needsReloading = true, prefabsLoaded = false;
    public bool initialReset = false;
    public Transform finishedFoldersParent = null; // optionally an object that all Finished fences will be parented to.
    public bool listsAndArraysCreated = false;
    public bool launchPresetAssigned = false;
    GameObject guideFirstPostMarker = null;
    public bool switchControlsAlso = false;

    public bool autoHideRailAVar = true, autoHideRailBVar = true;
    public int  optimiseRandomiseToolbarValueA = 0, optimiseRandomiseToolbarValueB = 0;
    bool useMeshRotations = false;
    public bool addLODGroup = false;
    public bool showPreviewLines = true;
    public List<Vector3> previewPoints = new List<Vector3>() {Vector3.zero, Vector3.zero};
    public string prefabsDefaultDirLocation = "Assets/Auto Fence Builder/FencePrefabs_AFWB";
    public string extrasDefaultDirLocation = "Assets/Auto Fence Builder/FencePrefabs_AFWB/_Extras_AFWB";//Initial default locations of prefab folders
    public string postsDefaultDirLocation = "Assets/Auto Fence Builder/FencePrefabs_AFWB/_Posts_AFWB";
    public string railsDefaultDirLocation = "Assets/Auto Fence Builder/FencePrefabs_AFWB/_Rails_AFWB";
    public string presetsDefaultFilePath = "Assets/Auto Fence Builder/Presets_AFWB";
    public string autoFenceBuilderDefaultDirLocation = "Assets/Auto Fence Builder";
    public string currPrefabsDirLocation, currExtrasDirLocation, currPostsDirLocation, currRailsDirLocation, currPresetsDirLocation, currAutoFenceBuilderDirLocation;
    public string scrPresetSaveName = "New Fence Preset_001";
    public bool stretchUVs = true, doThatThing = false; // Doing that thing may cause problems. Don't do that thing
    public int quantizeRotIndexPost = 4, quantizeRotIndexSubpost = 4;
    public bool addScalingToSizeYAfterUserObjectImport = true;
    public bool allowContentFreeUse = false, usingMinimalVersion = false;
    public int currGlobalsToolbarRow1 = 1, currGlobalsToolbarRow2 = -1;
    //=== Random Seeds ====
    int railAHeightVariationSeed = 123;


    //-------------------------------------
    public void InitializeVariationListsAndArrays()
    {
        if (listsAndArraysCreated)
            return;

        railAVariants = FenceVariant.CreateInitialisedFenceVariantList();
        railBVariants = FenceVariant.CreateInitialisedFenceVariantList();
        postVariants = FenceVariant.CreateInitialisedFenceVariantList();
        subpostVariants = FenceVariant.CreateInitialisedFenceVariantList();

        for (int i = 0; i < kNumRailVariations; i++)
        {
            railADisplayVariationGOs[i] = railBDisplayVariationGOs[i] = postDisplayVariationGOs[i] = subpostDisplayVariationGOs[i] = null;

            useRailVarA[i] = useRailVarB[i] = true;
            varRailAProbs[i] = varRailBProbs[i] = 1.0f;
            varRailAPositionOffset[i] = varRailBPositionOffset[i] = varPostPositionOffset[i] = Vector3.zero;
            varRailASize[i] = varRailBSize[i] = varPostSize[i] = Vector3.one;
            varRailARotation[i] = varRailBRotation[i] = varPostRotation[i] = Vector3.zero;

            varRailABackToFront[i] = varRailAMirrorZ[i] = varRailAInvert[i] = 0.0f;
            varRailABackToFront[i] = varRailBMirrorZ[i] = varRailBInvert[i] = 0.0f;

        }
        for (int i = 0; i < kMaxNumSeqSteps; i++)
        {
            userSequenceRailA[i] = new SeqVariant();//default
            userSequenceRailB[i] = new SeqVariant();
            userSequencePost[i] = new SeqVariant();
            userSequenceSubpost[i] = new SeqVariant();
        }
        ClearAllSingles();
        listsAndArraysCreated = true;
        //Debug.Log("InitializeVariationListsAndArrays()");
    }
    //-------------------------------------
    public void ClearAllSingles()
    {
        for (int i = 0; i < kMaxNumSingles; i++)
        {
            railASingles[i] = railBSingles[i] = postSingles[i] = -1;
        }
        railASingleVariants.Clear();
        railBSingleVariants.Clear();
    }
    //-------------------------------------
    public void ClearAllSinglesA()
    {
        for (int i = 0; i < kMaxNumSingles; i++)
        {
            railASingles[i] = -1;
        }
        railASingleVariants.Clear();
    }
    //-------------------------------------
    public void ClearAllSinglesB()
    {
        for (int i = 0; i < kMaxNumSingles; i++)
        {
            railBSingles[i] = -1;
        }
        railBSingleVariants.Clear();
    }
    //-------------------------------------
    public void ClearAllSinglesPosts()
    {
        for (int i = 0; i < kMaxNumSingles; i++)
        {
            postSingles[i] = -1;
        }
       //postSingleVariants.Clear();
    }
    //-----------------------
    public FenceVariant FindSingleVariantWithSectionIndex(AutoFenceCreator.LayerSet layerSet, int inSingleSectionIndex)
    {

        FenceVariant variant = null;

        List<FenceVariant>  singleVariantsList = railASingleVariants;
        if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
            singleVariantsList = railBSingleVariants;

        foreach (FenceVariant thisVariant in singleVariantsList)
        {
            if (thisVariant.singleIndex == inSingleSectionIndex)
            {
                return thisVariant;
            }
        }

        return variant;
    }
    //-----------------------
    public void RemoveSingleVariantFromList(AutoFenceCreator.LayerSet layerSet, FenceVariant singleVariant)
    {
        List<FenceVariant>  singleVariantsList = railASingleVariants;
        if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
            singleVariantsList = railBSingleVariants;

        singleVariantsList.Remove(singleVariant);
    }
    //-------------------------------------
    public void ToggleAllSingleVariants(AutoFenceCreator.LayerSet layerSet, bool enabled)
    {
        List<FenceVariant> singleVariantsList = railASingleVariants;
        if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
            singleVariantsList = railBSingleVariants;

        foreach (FenceVariant thisVariant in singleVariantsList)
        {
            thisVariant.enabled = !thisVariant.enabled;
        }
    }
    //-------------------------------------
    public GameObject GetCurrentPost()
    {
        if(currentPostType == -1)
        {
            Debug.LogWarning("Current Post Missing (-1). Using Default post [0] instead.");
            currentPostType = 0;
            return postPrefabs[0];
        }
        return postPrefabs[currentPostType];
    }
    //====================================================
    //              Awake & Reset
    //===================================================== 
    // We wrap these to have control over prefab loadiing from the editor
    // Although some calls are duplicated in Awake & Reset, this gives the best flexibility to use either/or.
    void Awake()
    { //Debug.Log("AutoFenceCreator  Awake()\n");

        //Debug.Log("Instance num = " + autoFenceInstanceNum);
        
        currPrefabsDirLocation = prefabsDefaultDirLocation;
        currExtrasDirLocation = extrasDefaultDirLocation;
        currPostsDirLocation = postsDefaultDirLocation;
        currRailsDirLocation = railsDefaultDirLocation;
        
        
        InitializeVariationListsAndArrays();
        if (needsReloading == false && postPrefabs.Count > 0 && origRailPrefabMeshes != null && origRailPrefabMeshes.Count > 0)
        {
            AwakeAutoFence();
        }
        needsReloading = true;
        groundLayers = ~((1 << LayerMask.NameToLayer("IgnoreRaycast")));

    }
    //--------------------------
    // Wrap Awake() and Reset() so we can control the order of loading, and ensure the prefabs have been loaded via AutoFenceEditor
    public void AwakeAutoFence()
    { //Debug.Log("AwakeAutoFence()\n");
        GameObject existingFolder = GameObject.Find("Current Fences Folder");
        if (existingFolder != null)
        {
            if (Application.isEditor)
            {
                //print("Awake(): Application.isEditor");
                fencesFolder = existingFolder;
                DestroyImmediate(existingFolder);
                SetupFolders();
                DestroyPools();
                CreatePools();
                SetMarkersActiveStatus(showControls);
                ForceRebuildFromClickPoints();
            }
            else if (Application.isPlaying)
            {
                SetMarkersActiveStatus(false);
            }
        }
    }
    //--------------------------
    // We wrap this to have control over prefab loadiing from the editor
    public void Reset()
    {
        currAutoFenceBuilderDirLocation = autoFenceBuilderDefaultDirLocation;
        currPrefabsDirLocation = prefabsDefaultDirLocation;
        currExtrasDirLocation = extrasDefaultDirLocation;
        currPostsDirLocation = postsDefaultDirLocation;
        currRailsDirLocation = railsDefaultDirLocation;
        currPresetsDirLocation = presetsDefaultFilePath;
        
        //Debug.Log("AutoFenceCreator Reset()\n");
        //Debug.Log("Instance num = " + autoFenceInstanceNum);
        if (prefabsLoaded == true)
            ResetAutoFence(true);
    }
    public void ResetAutoFence(bool destroyExistingFolder = true)
    { //Debug.Log("ResetAutoFence()\n");

        
        DestroyPools();
        keyPoints.Clear();
        GameObject existingFolder = GameObject.Find("Current Fences Folder");
        if (destroyExistingFolder && existingFolder != null)
            DestroyImmediate(existingFolder);
        SetupFolders();
        CreatePools();
        globalScale.y = 1.0f;
        numStackedRailsA = 2;
        railASpread = 0.4f;
        railAPositionOffset.y = 0.36f;
        subpostSize.y = 0.36f;
        roundingDistance = 6;
        centralizeRails = false;
        slopeModeRailA = FenceSlopeMode.shear;
        interpolate = true;
        interPostDist = 4;
        autoHideBuriedRails = false;
        useRailsB = false;
        globalLift = 0.0f;
        railBoxColliderHeightScale = 1.0f;
        railBoxColliderHeightOffset = 0.0f;
        extraPositionOffset.y = globalScale.y * postSize.y;  //initialize so the extra is visible at the top of the post
        extraPositionOffset.y += 0.3f;
        currentExtraType = FindPrefabIndexByName(FencePrefabType.extraPrefab, "RoadLanternAFB_Extra");
        CheckResizePools();
        InitializeVariationListsAndArrays();
        initialReset = true;
    }
    //=====================================================
    //                  Copy Layout & Clone
    //=====================================================
    public void CopyLayoutFromOtherFence(bool rebuild = true, GameObject sourceFence = null)
    {
        if (sourceFence == null)
            sourceFence = fenceToCopyFrom; //afc class variable
        List<Vector3> copiedClickPoints = null;
        if (fenceCloner == null)
            fenceCloner = new FenceCloner();
        if (sourceFence != null)
        {
            copiedClickPoints = fenceCloner.GetClickPointsFromFence(sourceFence);
        }
        if (copiedClickPoints != null)
        {
            ClearAllFences();
            int numClickPoints = copiedClickPoints.Count;
            for (int i = 0; i < numClickPoints; i++)
            {
                //print(copiedClickPoints[i]); 
                clickPoints.Add(copiedClickPoints[i]);
                clickPointFlags.Add(0); // 0 if normal, 1 if break
                keyPoints.Add(copiedClickPoints[i]);
            }
            ForceRebuildFromClickPoints();
        }
    }
    //---------------------------
    /*public void CheckPresetManager(bool loadPartsAlso = false)
    {//Debug.Log("CheckPresetManager()");

        if (presetManager == null)
        {
            presetManager = new AFBPresetManager();
            presetManager.afb = this;
            ///if(loadPartsAlso == true && postPrefabs.Count == 0)
            ///LoadAllParts();
            presetManager.ReadPresetFiles();
        }
    }*/
    //--------------------------
    // Tidy everything up so the folder handles and parts are in the right place
    public void RepositionFinished(GameObject finishedFolder)
    {
        int numCategoryChildren = finishedFolder.transform.childCount;
        Vector3 finishedFolderPosition = finishedFolder.transform.position;
        Transform categoryChild, groupedChild, meshChild;
        for (int k = 0; k < numCategoryChildren; k++)
        {
            categoryChild = finishedFolder.transform.GetChild(k);
            if (categoryChild.name == "Posts" || categoryChild.name == "Rails" || categoryChild.name == "Subs" || categoryChild.name == "Extras")
            {
                categoryChild.position = finishedFolderPosition;
                int numGroupChildren = categoryChild.childCount;
                for (int i = 0; i < numGroupChildren; i++)
                {
                    groupedChild = categoryChild.GetChild(i);
                    if (groupedChild.name.StartsWith("PostsGroupedFolder") || groupedChild.name.StartsWith("RailsAGroupedFolder")
                        || groupedChild.name.StartsWith("RailsBGroupedFolder") || groupedChild.name.StartsWith("SubsGroupedFolder") || groupedChild.name.StartsWith("ExtrasGroupedFolder"))
                    {
                        int numMeshChildren = groupedChild.childCount;
                        for (int j = 0; j < numMeshChildren; j++)
                        {
                            meshChild = groupedChild.GetChild(j);
                            if (meshChild.name.StartsWith("Post") || meshChild.name.StartsWith("Rail") || meshChild.name.StartsWith("Sub")
                                || meshChild.name.Contains("_Extra") || meshChild.name.Contains("_Post") || meshChild.name.Contains("_Rail"))
                                meshChild.position -= (finishedFolderPosition);
                        }
                    }
                }
            }
        }
    }
    //--------------------------
    void RemoveUnwantedColliders()
    { 
        if(railAColliderMode == 2)
        {
            for (int i = 0; i < railsA.Count; i++)
            {
                GameObject rail = railsA[i].gameObject;
                BoxCollider boxCollider = rail.GetComponent<BoxCollider>();
                if(boxCollider != null)
                    DestroyImmediate(boxCollider);

                MeshCollider meshCollider = rail.GetComponent<MeshCollider>();
                if (meshCollider != null)
                    DestroyImmediate(meshCollider);
            }
        }
        if (addBoxCollidersToRailB == false)
        {
            for (int i = 0; i < railsB.Count; i++)
            {
                GameObject rail = railsB[i].gameObject;
                BoxCollider boxCollider = rail.GetComponent<BoxCollider>();
                if (boxCollider != null)
                    DestroyImmediate(boxCollider);
            }
        }
        if (postColliderMode == 2)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                GameObject post = posts[i].gameObject;
                BoxCollider boxCollider = post.GetComponent<BoxCollider>();
                if (boxCollider != null)
                    DestroyImmediate(boxCollider);

                MeshCollider meshCollider = post.GetComponent<MeshCollider>();
                if (meshCollider != null)
                    DestroyImmediate(meshCollider);
            }
        }
        if (extraColliderMode == 2)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                GameObject extra = extras[i].gameObject;
                BoxCollider boxCollider = extra.GetComponent<BoxCollider>();
                if (boxCollider != null)
                    DestroyImmediate(boxCollider);

                MeshCollider meshCollider = extra.GetComponent<MeshCollider>();
                if (meshCollider != null)
                    DestroyImmediate(meshCollider);
            }
        }
    }
    //--------------------------
    public GameObject FinishAndStartNew(Transform parent, string fencename = "Finished AutoFence")
    {
        RemoveUnwantedColliders();

        if(usePosts == false)
        for (int i = 0; i < allPostsPositions.Count; i++)
        {
            CreateClickPointPostsForFinishedFence(i, allPostsPositions[i]);
        }

        DestroyUnused();
        GameObject currentFolder = GameObject.Find("Current Fences Folder");
        if (currentFolder != null)
        {

            currentFolder.name = fencename;
            RepositionFinished(currentFolder);
            finishedFoldersParent = parent;
            if (parent != null)
                currentFolder.transform.parent = parent;
        }
        currentFolder.AddComponent<FenceMeshMerge>();
        
        AddLODGroup(currentFolder);

        
        //-- Clear the references to the old parts ---
        clickPoints.Clear(); clickPointFlags.Clear();
        keyPoints.Clear();
        posts.Clear();
        railsA.Clear();
        railsB.Clear();
        subposts.Clear();
        extras.Clear();
        subJoiners.Clear();
        closeLoop = false;
        gaps.Clear();
        allPostsPositions.Clear();
        railACounter = railBCounter = postCounter = subCounter = extraCounter =  0;
        railsATotalTriCount = railsBTotalTriCount = postsTotalTriCount = extrasTotalTriCount = subPostsTotalTriCount = 0;
        
        globalLift = 0.0f;
        railBoxColliderHeightScale = 1.0f;
        railBoxColliderHeightOffset = 0.0f;
        fencesFolder = null; // break the reference to the old folder

        SetupFolders();
        CreatePools();

        return currentFolder;
    }
    //--------------------------
    public GameObject FinishAndDuplicate(Transform parent, string fencename = "Finished AutoFence")
    {
        DestroyUnused();
        GameObject currFinishAndDuplicateFolder= GameObject.Find("Current Fences Folder");
        if (currFinishAndDuplicateFolder != null)
        {
            currFinishAndDuplicateFolder.name = fencename;
            RepositionFinished(currFinishAndDuplicateFolder);
            finishedFoldersParent = parent;
            if (parent != null)
                currFinishAndDuplicateFolder.transform.parent = parent;
        }
        currFinishAndDuplicateFolder.AddComponent<FenceMeshMerge>();
        
        AddLODGroup(currFinishAndDuplicateFolder);

        //-- Clear the references to the old parts ---
        keyPoints.Clear();
        posts.Clear();
        railsA.Clear();
        railsB.Clear();
        subposts.Clear();
        extras.Clear();
        subJoiners.Clear();
        closeLoop = false;
        gaps.Clear();
        allPostsPositions.Clear();
        railACounter = railBCounter = postCounter = subCounter = extraCounter =  0;
        railsATotalTriCount = railsBTotalTriCount = postsTotalTriCount = extrasTotalTriCount = subPostsTotalTriCount = 0;
        fencesFolder = null; // break the reference to the old folder

        SetupFolders();
        CreatePools();

        ForceRebuildFromClickPoints();
        
        return currFinishAndDuplicateFolder;
    }
    //---------------
    private void AddLODGroup(GameObject folder)
    {
        if (addLODGroup == true)
        {
            LODGroup lodGroup = folder.AddComponent<LODGroup>();
            LOD[] lodArray = new LOD[1];
            Transform[] allChildren = folder.GetComponentsInChildren<Transform>();
            List<Renderer> renderersList = new List<Renderer>();
            for (int i = 0; i < allChildren.Length; i++)
            {
                Renderer renderer = allChildren[i].gameObject.GetComponent<Renderer>();
                if (renderer != null)
                    renderersList.Add(renderer);
            }
            lodArray[0] = new LOD(0.08f, renderersList.ToArray());
            lodGroup.SetLODs(lodArray);
        }
    }
    //--------------------------
    public void ClearAllFences()
    {
        clickPoints.Clear(); clickPointFlags.Clear();
        keyPoints.Clear();
        allPostsPositions.Clear();
        railACounter = railBCounter = postCounter = subCounter = extraCounter =  0;
        railsATotalTriCount = railsBTotalTriCount = postsTotalTriCount = extrasTotalTriCount = subPostsTotalTriCount = 0;
        DestroyPools();
        CreatePools();
        DestroyMarkers();
        closeLoop = false;
        globalLift = 0.0f;

    }

    //=================================================
    //              Create/Destroy Folders
    //=================================================
    public void SetupFolders()
    {
        // Make the Current Fences folder 
        if (fencesFolder == null)
        {
            fencesFolder = new GameObject("Current Fences Folder");
            //fencesFolder.transform.parent = GameObject.Find("Auto Fence Builder").transform;

            folderList.Add(fencesFolder);
            //?Selection.activeGameObject = this.gameObject;
        }
        if (fencesFolder != null)
        { // if it's already there, destroy sub-folders before making new ones
            int childs = fencesFolder.transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                GameObject subFolder = fencesFolder.transform.GetChild(i).gameObject;
                int grandChilds = subFolder.transform.childCount;
                for (int j = grandChilds - 1; j >= 0; j--)
                {
                    GameObject.DestroyImmediate(subFolder.transform.GetChild(j).gameObject);
                }
                DestroyImmediate(subFolder);
            }
        }
        extrasFolder = new GameObject("Extras");
        extrasFolder.transform.parent = fencesFolder.transform;
        postsFolder = new GameObject("Posts");
        postsFolder.transform.parent = fencesFolder.transform;
        railsFolder = new GameObject("Rails");
        railsFolder.transform.parent = fencesFolder.transform;
        subpostsFolder = new GameObject("Subs");
        subpostsFolder.transform.parent = fencesFolder.transform;
    }
    //--------------------------
    //Do this when necessary to check the user hasn't deleted the current working folder
    public void CheckFolders()
    {
        if (fencesFolder == null)
        {
            SetupFolders();
            ClearAllFences();
        }
        else
        {
            if (postsFolder == null)
            {
                postsFolder = new GameObject("Posts");
                postsFolder.transform.parent = fencesFolder.transform;
                ResetPostPool();
            }
            if (railsFolder == null)
            {
                railsFolder = new GameObject("Rails");
                railsFolder.transform.parent = fencesFolder.transform;
                ResetRailPool();
            }
            if (subpostsFolder == null)
            {
                subpostsFolder = new GameObject("Subs");
                subpostsFolder.transform.parent = fencesFolder.transform;
                ResetSubpostPool();
            }
            if (extrasFolder == null)
            {
                extrasFolder = new GameObject("Extras");
                extrasFolder.transform.parent = fencesFolder.transform;
                ResetExtraPool();
            }
        }
    }
    //==================================================
    //      Handle User's Custom Parts
    //==================================================
    public GameObject HandleUserExtraChange(GameObject newUserExtra)
    {
        //Creates a cleaned up GameObject with any children
        userExtraObject = MeshUtilitiesAFB.CreateAFBExtraFromGameObject(newUserExtra);
        if (userExtraObject != null)
            userExtraObject.name = newUserExtra.name;
        return userExtraObject;
    }
    //--------------------------------
    public GameObject HandleUserPostChange(GameObject newUserPost)
    {
        autoRotationResults = Vector3.zero;
        //Creates a cleaned up GameObject with any children
        userPostObject = MeshUtilitiesAFB.CreateCleanUncombinedAFBPostFromGameObject(newUserPost, this);
        if (userPostObject != null)
            userPostObject.name = newUserPost.name;
        return userPostObject;
    }
    //--------------------------------
    public GameObject HandleUserRailChange(GameObject newUserRail)
    {
        autoRotationResults = Vector3.zero;
        userSubMeshRailOffsets = new List<float>(); // used in MeshUtilitiesAFB and during rails build
        //Creates a cleaned up GameObject with any children
        userRailObject = MeshUtilitiesAFB.CreateCleanUncombinedAFBRailFromGameObject(newUserRail, this, railPrefabs[currentRailAType]);
        if (userRailObject != null)
            userRailObject.name = newUserRail.name;
        return userRailObject;
    }
    //---------------------
    public void RebuildWithNewUserPrefab(GameObject newUserPrefab,   LayerSet layerSet)
    {
        if (layerSet == LayerSet.postLayerSet)
        {
            userPostObject = newUserPrefab;
            currentPostType = FindPrefabIndexByName(FencePrefabType.postPrefab, newUserPrefab.name);
            if (currentPostType == -1) // couldn't find it
                currentPostType = 0;
            DestroyPosts();
            CreatePostPool(posts.Count);
        }
        else if (layerSet == LayerSet.railALayerSet)
        {
            userRailObject = newUserPrefab;
            currentRailAType = FindPrefabIndexByName(FencePrefabType.railPrefab, newUserPrefab.name);
            if (currentRailAType == -1) // couldn't find it
                currentRailAType = 0;
            railAVariants[0].go = railPrefabs[currentRailAType];
            DestroyRails();
            CreateRailPool(railsA.Count, LayerSet.railALayerSet);
        }
        else if (layerSet == LayerSet.railBLayerSet)
        {
            userRailObject = newUserPrefab;
            currentRailBType = FindPrefabIndexByName(FencePrefabType.railPrefab, newUserPrefab.name);
            if (currentRailBType == -1) // couldn't find it
                currentRailBType = 0;
            railBVariants[0].go = railPrefabs[currentRailBType];
            DestroyRails();
            CreateRailPool(railsB.Count, LayerSet.railBLayerSet);
        }
        else if (layerSet == LayerSet.extraLayerSet)
        {
            userExtraObject = newUserPrefab;
            currentExtraType = FindPrefabIndexByName(FencePrefabType.extraPrefab, newUserPrefab.name);
            if (currentExtraType == -1) // couldn't find it
                currentExtraType = 0;
            DestroyExtras();
            CreateExtraPool(extras.Count);
        }
        ForceRebuildFromClickPoints();
    }
    //----------------------------------------
    public void CreatePartStringsForMenus()
    {
        string name = "", category = "";
        // --------  Posts ----------
        postNames.Clear();
        int numPostTypes = postPrefabs.Count;
        for (int i = 0; i < numPostTypes; i++)
        {
            name = postPrefabs[i].name;
            category = GetPresetCategoryByName(name);
            if (category != "")
                name = category + "/" + name;
            else
                name = "Other" + "/" + name;
            postNames.Add(name);
            //postNums.Add(i);
        }
        postNames.Sort();

        // --------  Rails ----------
        railNames.Clear();
        int numRailTypes = railPrefabs.Count;
        railNames.Add("-"); // For rails we add a blank "-", so we can indicate no selection in the variation menus
        for (int i = 0; i < numRailTypes; i++)
        {
            name = railPrefabs[i].name;
            category = GetPresetCategoryByName(name);
            if (category != "")
                name = category + "/" + name;
            else
                name = "Other" + "/" + name;
            railNames.Add(name);
        }

        railNames.Sort();
        // --------  Extras ----------
        extraNames.Clear();
        int numExtraTypes = extraPrefabs.Count;
        for (int i = 0; i < numExtraTypes; i++)
        {
            name = extraPrefabs[i].name;
            category = GetPresetCategoryByName(name);
            if (category != "")
                name = category + "/" + name;
            else
                name = "Other" + "/" + name;
            extraNames.Add(name);
        }
        extraNames.Sort();
        // --------  Subs ----------
        subNames.Clear();
        int numSubTypes = subPrefabs.Count;
        for (int i = 0; i < numSubTypes; i++)
        {
            name = subPrefabs[i].name;
            category = GetPresetCategoryByName(name);
            if (category != "")
                name = category + "/" + name;
            else
                name = "Other" + "/" + name;
            subNames.Add(name);
        }
        subNames.Sort();
    }

    //--------------------
    // If there are multiple contiguous gaps found, merge them in to 1 gap by deleting the previous point
    void MergeClickPointGaps()
    {

        for (int i = 2; i < clickPointFlags.Count(); i++)
        {

            if (clickPointFlags[i] == 1 && clickPointFlags[i - 1] == 1) // tow together so keep the last one, deactivate the first one
                DeletePost(i - 1, false);
        }
    }
    //-----------------------------------------------
    // Where the use asked for a break, we remove all inter/spline posts between the break-clickPoint and the previous clickPoint
    void RemoveDiscontinuityBreaksFromAllPostPosition()
    {

        if (allowGaps == false || clickPoints.Count < 3) return;

        //Vector3 previousValidClickPoint = clickPoints[2];
        int clickPointIndex = 0, breakPointIndex = -1, previousValidIndex = 1;

        List<int> removePostsIndices = new List<int>();

        for (int i = 2; i < allPostsPositions.Count; i++)
        { // the first two can not be break points, as they are the minimum 1 single section of fence
            Vector3 thisPostPos = allPostsPositions[i];
            clickPointIndex = clickPoints.IndexOf(thisPostPos);
            if (clickPointIndex != -1)
            { // it's a clickPoint!
                if (clickPointFlags[clickPointIndex] == 1)
                { // it's a break point!
                    breakPointIndex = i; // we will remove all the post between this and previousValidIndex
                    for (int r = previousValidIndex + 1; r < breakPointIndex; r++)
                    {
                        if (removePostsIndices.Contains(r) == false)
                            removePostsIndices.Add(r);
                    }
                }
                else
                    previousValidIndex = i;
            }
        }

        for (int i = removePostsIndices.Count - 1; i >= 0; i--)
        { // comment this out to disable breakPoints
            allPostsPositions.RemoveAt(removePostsIndices[i]);
        }
    }
    //------------------------
    bool IsBreakPoint(Vector3 pos)
    {

        int clickPointIndex = clickPoints.IndexOf(pos);
        if (clickPointIndex != -1)
        { // it's a clickPoint!
            if (clickPointFlags[clickPointIndex] == 1)
            { // it's a break point!
                return true;
            }
        }
        return false;
    }
    //------------
    void OnDrawGizmos()
    {
        //======  Show Gap Lines  ======
        Color lineColor = new Color(.1f, .1f, 1.0f, 0.4f);
        Gizmos.color = lineColor;
        Vector3 a = Vector3.zero, b = Vector3.zero;
        if (showDebugGapLine && allowGaps)
        {
            for (int i = 0; i < gaps.Count(); i += 2)
            {
                a = gaps[i]; a.y += 0.8f;
                b = gaps[i + 1]; b.y += 0.8f;
                Gizmos.DrawLine(a, b); // draw a line to show user gaps
                a.y += 0.3f;
                b.y += 0.3f;
                Gizmos.DrawLine(a, b);
                a.y += 0.3f;
                b.y += 0.3f;
                Gizmos.DrawLine(a, b);
            }
        }
        //======  Show Preview Lines  ======
        Event e = Event.current;
        if (e.shift && clickPoints.Count > 0 && showPreviewLines)
        {
            //Debug.Log("shift \n");
            Gizmos.color = new Color(.2f, .7f, .2f, 0.75f);
            Vector3 p0 = previewPoints[0], p1 = previewPoints[1];
            p0.y += 0.1f;
            p1.y += 0.1f;
            for (int i = 0; i < 8; i++)
            {
                Gizmos.DrawLine(p0, p1);
                p0.y += 0.3f;
                p1.y += 0.3f;
                
            }
            Gizmos.DrawLine(p1, p1 - new Vector3(0, 2.5f, 0));
        }
    }
    //=============================================
    // It's strongly recommended that you don't use Post colliders as they're usually unnecessary ( See 'Settings' button)
    public void CreatePostCollider(GameObject post)
    {//0 = single box, 1 = keep original (user), 2 = no colliders
        BoxCollider postBoxCollider = post.GetComponent<BoxCollider>();

        if (postBoxCollider == null && postColliderMode == 2) // not needed, so return
            return;
        else if (postBoxCollider != null && postColliderMode == 2)
        { // not needed, but exist, so deactivate and return
            DestroyImmediate(postBoxCollider);
            return;
        }

        if (postBoxCollider != null)
            DestroyImmediate(postBoxCollider);
        //====== Simple single BoxCollider ======
        if (postColliderMode == 0 || (postColliderMode == 1 && useCustomPost == false))
        {
            if (useCustomPost == true)// switch the original ones off first
                MeshUtilitiesAFB.SetEnabledStatusAllColliders(post, false);

            postBoxCollider = (BoxCollider)post.AddComponent<BoxCollider>();
            if (postBoxCollider != null)
            {
                postBoxCollider.enabled = true;
            }
        }
        //====== Original collider on user's custom rail object (only avaialbel on user objects) =======
        if (postColliderMode == 1 && useCustomPost == true)
        {
            MeshUtilitiesAFB.SetEnabledStatusAllColliders(post, true);
        }
    }
    //================================================
    public void CreateRailCollider(GameObject rail, Vector3 centrePos) // centrePos only needed when creating a box collider for a user object
    {//0 = single box, 1 = keep original (user), 2 = no colliders

        BoxCollider railCollider = rail.GetComponent<BoxCollider>();

        //0 = single box, 1 = keep original (user), 2 = no colliders
        if (railCollider != null && railAColliderMode == 2)
        { // not needed, but exist, so destroy and return
            DestroyImmediate(railCollider);
            return;
        }

        //User has setting for No Colliders, so we make a basic box collider anyway (for hit detection in Scene) and remove when doing a 'Finish'
        else if (railAColliderMode == 2)// 2 = no colliders
        { 
            BoxCollider boxCollider = rail.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                railCollider = (BoxCollider)rail.AddComponent<BoxCollider>();
            }
            return;
        }


        //====== Simple single BoxCollider ======
        //0 = single box, 1 = keep original (user), 2 = no colliders
        if (railAColliderMode == 0 || (railAColliderMode == 1 && useCustomRailA == false))
        {
            if (railCollider != null)
                DestroyImmediate(railCollider);

            // it's an ordinary single AFB rail
            if (useCustomRailA == false)
            {
                railCollider = (BoxCollider)rail.AddComponent<BoxCollider>();
                if (railCollider != null)
                {
                    /*railCollider.enabled = true;
                    Vector3 newSize = railCollider.size;
                    newSize.y = (postSize.y * globalScale.y) / rail.transform.localScale.y; 
                    newSize.y *= railBoxColliderHeightScale;
                    railCollider.size = newSize;
                    Vector3 newCenter = railCollider.center;
                    newCenter.y = (newSize.y / 2);
                    newCenter.y -= (railAPositionOffset.y * globalScale.y) / rail.transform.localScale.y;
                    newCenter.y += railBoxColliderHeightOffset;
                    railCollider.center = newCenter;*/

                    railCollider.enabled = true;
                    Vector3 newSize = railCollider.size;
                    newSize.y *= railBoxColliderHeightScale;
                    railCollider.size = newSize;
                    Vector3 newCenter = railCollider.center;
                    newCenter.y += railBoxColliderHeightOffset;
                    railCollider.center = newCenter;
                }
            }
            //----- it's a user object, possibly grouped ---
            else
            {
                // Don't delete commented out code - will be used in v4.0+
                // Can't rely on any of the user's GameObjects being suitable for a correctly sized & positioned BoxCollider, so...
                //Make a box collider by adding a cube and scale/position it, then transfer the collider settings to the rail
                /*GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tempCube.transform.parent = rail.transform;
                tempCube.transform.localPosition = Vector3.zero;
                tempCube.transform.localScale = Vector3.one;
                tempCube.transform.localRotation = Quaternion.identity;
                Vector3 cubePos = tempCube.transform.position;
                centrePos.y = rail.transform.position.y;
                Vector3 railPos = rail.transform.position;
                Vector3 shift = centrePos - rail.transform.position;
                tempCube.transform.Translate(-tempCube.transform.right * shift.magnitude, Space.World);
                railCollider = (BoxCollider)tempCube.GetComponent<BoxCollider>();
                if (railCollider != null)
                {
                    railCollider.enabled = true;
                    Vector3 newSize = Vector3.one;
                    newSize.x = (shift.magnitude * 2) / rail.transform.localScale.x;
                    newSize.y = globalScale.y * postSize.y * gs;
                    newSize.z = 0.75f;
                    BoxCollider newCollider = (BoxCollider)rail.AddComponent<BoxCollider>();
                    //--- User Modified ----
                    newSize.y *= railBoxColliderHeightScale;
                    newCollider.size = newSize;
                    newCollider.center = new Vector3(-1.5f, (newSize.y / 2) + railBoxColliderHeightOffset, 0);
                }
                DestroyImmediate(tempCube);*/
                
                railCollider = (BoxCollider)rail.AddComponent<BoxCollider>();
                if (railCollider != null)
                {
                    railCollider.enabled = true;
                    Vector3 newSize = railCollider.size;
                    newSize.y *= railBoxColliderHeightScale;
                    railCollider.size = newSize;
                    Vector3 newCenter = railCollider.center;
                    newCenter.y += railBoxColliderHeightOffset;
                    railCollider.center = newCenter;
                }
            }


        }
        //====== Original collider on user's custom rail =======
        if (railAColliderMode == 1 && useCustomRailA == true)
        {
            if (railCollider != null)
                DestroyImmediate(railCollider);
            MeshUtilitiesAFB.SetEnabledStatusAllColliders(rail, true);
        }
        //====== Mesh Colliders =======
        if (railAColliderMode == 3 )
        {
            BoxCollider boxCollider = rail.GetComponent<BoxCollider>();
            if (boxCollider != null)
                DestroyImmediate(boxCollider);
            MeshCollider meshCollider = rail.GetComponent<MeshCollider>();
            if (meshCollider != null)
                DestroyImmediate(meshCollider);

            meshCollider = (MeshCollider)rail.AddComponent<MeshCollider>();
            MeshUtilitiesAFB.SetEnabledStatusAllColliders(rail, true);
        }
    }
    //=============================================
    public void CreateExtraCollider(GameObject extra)
    {//0 = single box, 1 = keep original (user), 2 = no colliders
        BoxCollider extraBoxCollider = extra.GetComponent<BoxCollider>();

        if (extraBoxCollider == null && extraColliderMode == 2) // not needed, so return
            return;
        else if (extraBoxCollider != null && extraColliderMode == 2)
        { // not needed, but exist, so deactivate and return
            DestroyImmediate(extraBoxCollider);
            return;
        }
        if (extraBoxCollider != null)
            DestroyImmediate(extraBoxCollider);
        //====== Simple single BoxCollider ======
        if (extraColliderMode == 0 || (extraColliderMode == 1 && useCustomExtra == false))
        {
            if (useCustomExtra == true)// switch the original ones off first
                MeshUtilitiesAFB.SetEnabledStatusAllColliders(extra, false);

            extraBoxCollider = (BoxCollider)extra.AddComponent<BoxCollider>();
            if (extraBoxCollider != null)
            {
                extraBoxCollider.enabled = true;
            }
        }
        //====== Original collider on user's custom rail object (only avaialbel on user objects) =======
        if (extraColliderMode == 1 && useCustomExtra == true)
        {
            MeshUtilitiesAFB.SetEnabledStatusAllColliders(extra, true);
        }
    }
    //===================================================
    public Mesh CombineRailMeshes()
    {
        CombineInstance[] combiners = new CombineInstance[railACounter];

        for (int i = 0; i < railACounter; i++)
        {

            GameObject thisRail = railsA[i].gameObject;
            MeshFilter mf = thisRail.GetComponent<MeshFilter>();
            Mesh mesh = (Mesh)Instantiate(mf.sharedMesh);

            Vector3[] vertices = mesh.vertices;
            Vector3[] newVerts = new Vector3[vertices.Length];
            int v = 0;
            while (v < vertices.Length)
            {

                newVerts[v] = vertices[v];
                v++;
            }
            mesh.vertices = newVerts;

            combiners[i].mesh = mesh;

            Transform finalTrans = Instantiate(thisRail.transform) as Transform;
            finalTrans.position += thisRail.transform.parent.position;
            combiners[i].transform = finalTrans.localToWorldMatrix;
            DestroyImmediate(finalTrans.gameObject);
        }

        Mesh finishedMesh = new Mesh();
        finishedMesh.CombineMeshes(combiners);

        return finishedMesh;
    }
    //------------------------
    public void printVariant(SeqVariant v)
    {
        Debug.Log("Invert = " + v.invert + "      BackToFront =  = " + v.backToFront + "      Mirror Z =  = " + v.mirrorZ);
        //Debug.Log("Mirror X =  = " + v.mirrorX);
        //Debug.Log("Mirror Z =  = " + v.mirrorZ);
    }
    //-----------------------
    // Get the original transform.localScale rom the current object. AFB build-in prefabs are always scaled (1,1,1), but users' might differ
    void ResetNativePrefabScales()
    {
        if (currentPostType >= postPrefabs.Count)
            currentPostType = 0;
        if (currentRailAType >= railPrefabs.Count)
            currentRailAType = 0;
        if (currentRailBType >= railPrefabs.Count)
            currentRailBType = 0;
        if (currentSubpostType >= postPrefabs.Count)
            currentSubpostType = 0;
        if (currentExtraType >= extraPrefabs.Count)
            currentExtraType = 0;
        nativePostScale = postPrefabs[currentPostType].transform.localScale;
        nativeRailAScale = railPrefabs[currentRailAType].transform.localScale;
        nativeRailBScale = railPrefabs[currentRailBType].transform.localScale;
        nativeSubScale = postPrefabs[currentSubpostType].transform.localScale;
        nativeExtraScale = extraPrefabs[currentExtraType].transform.localScale;
    }
    //------------------------
    public Mesh GetMainMesh(GameObject go)
    {
        List<Mesh> meshes = MeshUtilitiesAFB.GetAllMeshesFromGameObject(go);
        if (meshes != null && meshes.Count > 0)
            return meshes[0];
        else
            return null;
    }
    //--------------------
    // Called when the layout is required to change
    // Creates, the interpolated posts, the smoothing curve and
    // then calls RebuildFromFinalList where the fence gets put together
    public void ForceRebuildFromClickPoints(AutoFenceCreator.LayerSet layerSet = AutoFenceCreator.LayerSet.allLayerSet)
    {//Debug.Log("ForceRebuildFromClickPoints()");
        //Timer t = new Timer("ForceRebuildFromClickPoints");
        
        if (clickPoints.Count == 0)
        {
            return;
        }
        if (clickPoints.Count == 1) // the first post doesn't need anything else
        {
            allPostsPositions.Clear();
            keyPoints.Clear();
            keyPoints.AddRange(clickPoints);
            AddNextPostAndInters(keyPoints[0], false, true); //interpolateThisPost = false, doRebuild = true
            return;
        }
        CheckResizePools();
        DeactivateEntirePool(layerSet); // Switch off, but don't delete
        MergeClickPointGaps();
        allPostsPositions.Clear();
        keyPoints.Clear();
        keyPoints.AddRange(clickPoints);
        MakeSplineFromClickPoints();
        startPoint = keyPoints[0];
        AddNextPostAndInters(keyPoints[0], false, false);
        for (int i = 1; i < keyPoints.Count; i++)
        {
            endPoint = keyPoints[i];
            AddNextPostAndInters(keyPoints[i], true, false);
            startPoint = keyPoints[i];
        }
        RemoveDiscontinuityBreaksFromAllPostPosition();
        RebuildFromFinalList(layerSet);
        centralizeRails = false;
        //t.End();
    }

    //--------------
    // Every single rail instance has to have it's own unique mesh, because they become re-shaped
    // to fit the slope of the land. 
    List<Mesh> CreatePreparedRailMesh(LayerSet railSet, List<Mesh> railMeshGroup)
    {
        //Check that we've made backups so we don't break the original
        if (origRailPrefabMeshes == null || origRailPrefabMeshes.Count < currentRailAType + 1 || origRailPrefabMeshes[currentRailAType] == null)
        {
            BackupPrefabMeshes(railPrefabs, origRailPrefabMeshes); // build fix
        }
        List<Mesh> preparedMeshSet = new List<Mesh>();
        // For the object and its children
        for (int i = 0; i < railMeshGroup.Count; i++)
        {
            Mesh origMesh = railMeshGroup[i];
            Mesh dupMesh = MeshUtilitiesAFB.DuplicateMesh(origMesh);
            preparedMeshSet.Add(dupMesh);
        }
        return preparedMeshSet;
    }
    //------------------------
    //Create one of each rail types meshes/submeshes. These will be used as a source for cloning each section
    public void CreateAllPreparedMeshVariations(LayerSet railSet)
    { //Debug.Log("CreateAllPreparedMeshVariations()\n");
        float mainMeshHeight = 1;
        // [0] is always the original
        if (railSet == LayerSet.railALayerSet)
        {
            int numVariations = railAVariants.Count;
            if (railAVariants.Count == 0 || railAVariants[0] == null)
                Debug.Log("railAVariants missing /n");
            Mesh mainMesh = GetMainMesh(railAVariants[0].go);
             mainMeshHeight = mainMesh.bounds.size.y;
            for (int i = 0; i < numVariations; i++)
            {
                FenceVariant thisVariant = railAVariants[i];
                GameObject thisGO = thisVariant.go;
                if (thisGO == null)
                    Debug.Log(" CreateAllPreparedMeshVariations{}   railAVariants is null");
                railAPreparedMeshVariants[i] = CreatePreparedRailMesh(LayerSet.railALayerSet, MeshUtilitiesAFB.GetAllMeshesFromGameObject(thisGO));
                //-- If it's a variation and not the main rail, make sure it's height scale matches th main
                if (scaleVariationHeightToMainHeightA == true && i > 0)
                {
                    float variantMeshHeight = railAPreparedMeshVariants[i][0].bounds.size.y;
                    if (variantMeshHeight != mainMeshHeight)
                        MeshUtilitiesAFB.ScaleMeshList(railAPreparedMeshVariants[i], new Vector3(1, mainMeshHeight / variantMeshHeight, 1));
                }
            }
        }
        else if (railSet == LayerSet.railBLayerSet)
        {
            int numVariations = railBVariants.Count;
            if (railBVariants.Count == 0 || railBVariants[0] == null)
                Debug.Log("railBVariants missing /n");
            Mesh mainMesh = GetMainMesh(railBVariants[0].go);
             mainMeshHeight = mainMesh.bounds.size.y;
            for (int i = 0; i < numVariations; i++)
            {
                if (railBVariants[i].go == null)
                    Debug.Log(" CreateAllPreparedMeshVariations{}   railAVariants is null");
                railBPreparedMeshVariants[i] = CreatePreparedRailMesh(LayerSet.railALayerSet, MeshUtilitiesAFB.GetAllMeshesFromGameObject(railBVariants[i].go));
                //-- If it's a variation and not the main rail, make sure it's height scale matches th main
                if (scaleVariationHeightToMainHeightA == true && i > 0)
                {
                    float variantMeshHeight = railBPreparedMeshVariants[i][0].bounds.size.y;
                    if (variantMeshHeight != mainMeshHeight)
                        MeshUtilitiesAFB.ScaleMeshList(railBPreparedMeshVariants[i], new Vector3(1, mainMeshHeight / variantMeshHeight, 1));
                }
            }
        }
    }
    //------------------------
    // This is where the gameobjects actually get built
    // The final list differs from the main clickPoints list in that has now added extra posts for interpolating and smoothing
    public void RebuildFromFinalList(AutoFenceCreator.LayerSet  layerSet = AutoFenceCreator.LayerSet.allLayerSet)
    {
        Timer t = new Timer("RebuildFromFinalList");
        railsATotalTriCount = railsBTotalTriCount = postsTotalTriCount = subPostsTotalTriCount = extrasTotalTriCount = 0;
        UnityEngine.Random.InitState(randomSeed);
        postCounter = 0;
        railACounter = railBCounter = 0;
        subCounter = 0;
        subJoinerCounter = 0;
        extraCounter = 0;
        ResetNativePrefabScales();//Get the original transform.localScale for current post, rail etc.

        Vector3 A = Vector3.zero, B, C = Vector3.zero, prevPostPos = Vector3.zero;
        //Check if we need to increase the pool size before we do any building
        CheckResizePools(); // this will also rebuild the sheared uniqueRailMeshesA via RereateRailMeshes()
        SetMarkersActiveStatus(showControls);
        gaps.Clear();
        tempMarkers.Clear();
        //prevRailDirectionNorm = Vector3.zero;
        overlapPostErrors.Clear();

        // Crete a new single clean fixed Rail mesh which can be duplicated to make the individual sections.
        // These cannot be pre-packed in to the game objects, otherwise the mesh modifications would compound on every rebuild.
        if (numStackedRailsA > 0)
            CreateAllPreparedMeshVariations(LayerSet.railALayerSet);
        if (numStackedRailsB > 0)
            CreateAllPreparedMeshVariations(LayerSet.railBLayerSet);

        // May change in v3.2
        variationModeRailA = VariationMode.sequenced;
        variationModeRailB = VariationMode.sequenced;

        CheckSinglesLengths();
        
        //guideFirstPostMarker = null;
        for (int i = 0; i < allPostsPositions.Count; i++)
        {
            //=====================================================
            //      Create POSTS (and click-markers if enabled)
            //=====================================================
            A = allPostsPositions[i];
            SetupPost(i, A);  // Build Post B(i) at position i. We need to build them even if not used as reference for everything else
            //- Make a temporary marker if posts aren't being used so that the user can see the position of the first click
            if (usePosts == false && allPostsPositions.Count == 1 && i == 0)
            {
                GameObject markerPost = FindPrefabByName(FencePrefabType.postPrefab, "Marker_Post");
                if(markerPost) guideFirstPostMarker = Instantiate(markerPost, A, Quaternion.identity) as GameObject;
                guideFirstPostMarker.transform.parent = postsFolder.transform;
                guideFirstPostMarker.name = "Marker Post - can be deleted";
            }
            //=====================================================
            //      Create Rails, Extras and Subs (check validity & gaps)
            //=====================================================
            if (i > 0)
                prevPostPos = allPostsPositions[i - 1];
            else
                prevPostPos = Vector3.zero;
            if (i < allPostsPositions.Count - 1)
            {
                B = allPostsPositions[i + 1];
                if (i < allPostsPositions.Count - 2)
                    C = allPostsPositions[i + 2];
                else
                    C = B;
                if (A == B)
                {
                    print("Warning: Posts A & B are in identical positions. Enable [Show Move Controls] and delete or move one of them " + i + "  " + (i + 1));
                    allPostsPositions[i + 1] += new Vector3(0.1f, 0, 0.01f);
                }
                else if (IsBreakPoint(allPostsPositions[i + 1]) == false || allowGaps == false)
                {
                    /*if(i<allPostsPositions.Count-1)
                        Debug.Log("Prev=" + (i-1) + "   A="+ (i) + "   B=" + (i+1) +   "   C=" + (i+2) + "    Section" + (i) + "[" + (i) + " to " + (i+1) + "]\n");
                    else 
                        Debug.Log("Prev=" + (i-1) + "   A="+ (i) + "   B=" + (i+1) +   "   C=" + (i+1) + "    Section" + (i) + "[" + (i) + " to " + (i+1) + "]\n");*/
                    if (useRailsA == true && numStackedRailsA > 0 && (layerSet == LayerSet.railALayerSet || layerSet == LayerSet.allLayerSet))
                        BuildRailsForSection(prevPostPos, A, B, C, i, LayerSet.railALayerSet, railAPreparedMeshVariants);  //---- Create Main Rails ----
                    if (useRailsB == true && numStackedRailsB > 0 && (layerSet == LayerSet.railBLayerSet || layerSet == LayerSet.allLayerSet))
                        BuildRailsForSection(prevPostPos, A, B, C, i, LayerSet.railBLayerSet, railBPreparedMeshVariants); //---- Create Seconday Rails ----
                    if (useSubposts == true && (layerSet == LayerSet.subpostLayerSet || layerSet == LayerSet.allLayerSet))
                        BuildSubposts(prevPostPos, A, B, C, i);
                    // if the subposts are replicating the posts, need to build the last one
                    if (useSubposts == true && (subsFixedOrProportionalSpacing == 2 || addSubpostAtPostPointAlso == true) && i == allPostsPositions.Count - 2 && 
                        (layerSet == LayerSet.subpostLayerSet || layerSet == LayerSet.allLayerSet)) 
                        BuildSubposts(prevPostPos, B, A, B, i + 1, true);
                }
                else
                {
                    gaps.Add(A);
                    gaps.Add(B);
                }
            }
            postCounter++;
        }
        // Delete the guide marker once we've got going
        if (guideFirstPostMarker != null && allPostsPositions.Count > 1)
        {
            DestroyImmediate(guideFirstPostMarker);
            guideFirstPostMarker = null;
        }
        RotatePostsFinal(); //rotate each post to correctly follow the fence direction, best to do at the end when all directions have been calc'd
        BuildExtras();// Build the extras last

        //=====  Global Lift. lifts everything off the ground. this should only be used for cloning/layering =======
        if (globalLift > 0.05f || globalLift < -0.05f )
        {
            Transform[] allParts = fencesFolder.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allParts)
            {
                string name = child.gameObject.name;
                if (name.Contains("PostsGroupedFolder") || name.Contains("RailsAGroupedFolder") || name.Contains("RailsBGroupedFolder") || name.Contains("SubsGroupedFolder") || name.Contains("ExtrasGroupedFolder"))
                {
                    child.Translate(0, globalLift, 0);
                }
            }
        }
        //t.End(); // Un-comment to time editor rebuild
    }
    //------------
    // This the real meat of the thing where the fence rails get's assembled. Input positions of two posts A B, and build rails/walls between them 
    // sectionIndex is the post-to-post section we are currently building (effectively, the same as the post number)
    // **Note**  In sheared mode, rails[x]'s mesh may be null at this point, as it was chnaged to a buffer mesh, which may have been cleared ready for rebuilding
    public void BuildRailsForSection(Vector3 prevPostPos, Vector3 posA, Vector3 posB, Vector3 posC, int sectionIndex, LayerSet railSet, List<List<Mesh>> preparedMeshes)
    {//Debug.Log("BuildRailsForSection() \n");
        
        //Timer t = new Timer("RebuildFromFinalList");
        Vector3  prevDirection = Vector3.zero, nextDirection = Vector3.zero, currentVectorDir = posB - posA;
        float distance = Vector3.Distance(posA, posB), currHeading, prevHeading=0, nextHeading = 0, halfRailThickness;
        float alternateHeightDelta = 0;
        float railGap = 0, gap = globalScale.y * gs; // gs = Global Scale
        Vector3 nativeScale = Vector3.one, railPositionOffset = Vector3.zero, railRotation = Vector3.zero, P = posA, Q = posB;
        float railThickness = 0, railMeshLength = 0, railMeshHeight = 0;
        Bounds bounds;
        float newChangeInHeading = 0;
        int numStackedRailsInThisSet = 0, thisRailType = -1;
        //Vector3 positionOffset = Vector3.zero;
        Vector3 size = Vector3.one;
        bool useSeq = false, allowIndependentSubmeshVariation = false, allowRailHeightVariation = false, allowRandRailRotationVariation = false;
        bool useRailVariations = false, jitterRailVerts = false, railKeepGrounded = true, rotateFromBase = false;
        List<SeqVariant> currSeq = null;
        FenceVariant currVariantForSeqStep = null;
        float minRailHeightVar = 0, maxRailHeightVar = 0, randRotXSize = 0, randRotYSize = 0, randRotZSize = 0;
        VariationMode variationMode = variationModeRailA;
        int randomScope = 2, numUserSeqSteps = 0;
        RandomRecords randRec = railARandRec;
        List<FenceVariant> nonNullRailVariants = nonNullRailAVariants;
        List<FenceVariant> railVariants = railAVariants;
        List<int> railSingles = railASingles;
        FenceSlopeMode slopeMode = FenceSlopeMode.shear;

        if (railSet == LayerSet.railALayerSet)
        {
            thisRailType = currentRailAType;
            numStackedRailsInThisSet = numStackedRailsA;
            railGap = railASpread;
            railRotation = railARotation;
            railPositionOffset = railAPositionOffset;
            nativeScale = nativeRailAScale;
            //nativeScale = new Vector3(nativeRailAScale.x, nativeRailAScale.y * railASize.y, nativeRailAScale.z);
            size = railASize;
            useSeq = useRailASeq;
            currSeq = optimalSequenceRailA;
            variationMode = variationModeRailA;
            if (variationModeRailA == VariationMode.sequenced)
                currSeq = userSequenceRailA;
            minRailHeightVar = minRailAHeightVar; maxRailHeightVar = maxRailAHeightVar;
            allowIndependentSubmeshVariation = allowIndependentSubmeshVariationA;
            useRailVariations = useRailAVariations;
            allowRailHeightVariation = allowRailAHeightVariation;
            allowRandRailRotationVariation = allowRandRailARotationVariation;
            randRotXSize = railARandRotationAmount.x;
            randRotYSize = railARandRotationAmount.y;
            randRotZSize = railARandRotationAmount.z;
            jitterRailVerts = jitterRailAVerts;
            randomScope = railARandomScope;
            railKeepGrounded = railAKeepGrounded;
            numUserSeqSteps = railASeqInfo.numSteps;
            slopeMode = slopeModeRailA;
            rotateFromBase = rotateFromBaseRailA;
        }
        else if (railSet == LayerSet.railBLayerSet)
        {
            thisRailType = currentRailBType;
            numStackedRailsInThisSet = numStackedRailsB;
            railGap = railBSpread;
            railRotation = railBRotation;
            railPositionOffset = railBPositionOffset;
            nativeScale = nativeRailBScale;
            //nativeScale = new Vector3(nativeRailBScale.x, nativeRailBScale.y * railBSize.y, nativeRailBScale.z);
            size = railBSize;
            useSeq = useRailBSeq;
            currSeq = optimalSequenceRailB;
            variationMode = variationModeRailB;
            if (variationModeRailB == VariationMode.sequenced)
                currSeq = userSequenceRailB;
            minRailHeightVar = minRailBHeightVar; maxRailHeightVar = maxRailBHeightVar;
            allowIndependentSubmeshVariation = allowIndependentSubmeshVariationB;
            useRailVariations = useRailBVariations;
            allowRailHeightVariation = allowRailBHeightVariation;
            allowRandRailRotationVariation = allowRandRailBRotationVariation;
            randRotXSize = railBRandRotationAmount.x;
            randRotYSize = railBRandRotationAmount.y;
            randRotZSize = railBRandRotationAmount.z;
            jitterRailVerts = jitterRailBVerts;
            randomScope = railBRandomScope;
            railKeepGrounded = railBKeepGrounded;
            numUserSeqSteps = railBSeqInfo.numSteps;
            randRec = railBRandRec;
            nonNullRailVariants = nonNullRailBVariants;
            railVariants = railBVariants;
            railSingles = railBSingles;
            slopeMode = slopeModeRailB;
            rotateFromBase = rotateFromBaseRailB;
        }

        

        P.y = Q.y = 0;
        float horizDistance = Vector3.Distance(P, Q);
        float heightDeltaAB = posA.y - posB.y; //ground position delta
        float heightDeltaBA = posB.y - posA.y; //ground position delta

        if (numStackedRailsInThisSet > 1)
            gap = railGap / (numStackedRailsInThisSet - 1);
        else
            gap = 0;
        gap *= globalScale.y * gs;

        //==========================================================================
        //            Start looping through for each stacked Rail in the section, 
        //==========================================================================
        float missingChance = 0;
        for (int i = 0; i < numStackedRailsInThisSet; i++)
        {
            ///---- Shall we skip this rail? -----------
            if (railSet == LayerSet.railALayerSet)
                missingChance = chanceOfMissingRailA;
            else if (railSet == LayerSet.railBLayerSet)
                missingChance = chanceOfMissingRailB;
            if (missingChance > 0 && UnityEngine.Random.value <= missingChance)
                continue;
            //-----------------------------------------
            int repetitionIndex = (sectionIndex * numStackedRailsInThisSet) + i;
            bool omit = false;
            ///--------- Check For Null Rails -----------
            if (railSet == LayerSet.railALayerSet && railsA == null || railsA.Count <= railACounter || railsA[railACounter] == null)
            {
                Debug.Log("found null rail A\n");
                SaveCustomRailMeshAndAddToPrefabList(userRailObject);
                ResetRailPool();
            }
            if (railSet == LayerSet.railBLayerSet && (railsB == null || railsB.Count <= railBCounter || railsB[railBCounter] == null))
            {
                Debug.Log("found null rail \n");
                SaveCustomRailMeshAndAddToPrefabList(userRailObject);
                ResetRailPool();
            }
            //------- Get GameObject from pool ---------
            GameObject thisRail = null;
            if (railSet == LayerSet.railALayerSet)
            {
                thisRail = railsA[railACounter].gameObject; //get the clone from the pool, which already contains any variations
            }
            else if (railSet == LayerSet.railBLayerSet)
            {
                thisRail = railsB[railBCounter].gameObject;
            }
            if (thisRail == null)
            {
                print("Missing Rail " + i + " Have you deleted one?"); continue;
            }

            thisRail.hideFlags = HideFlags.None;
            thisRail.SetActive(true);
            thisRail.transform.rotation = Quaternion.identity; // make sure it's reset before moving position
            thisRail.transform.position = Vector3.zero;
            thisRail.transform.localScale = Vector3.one;
            //Debug.Log("Clean Position = " + thisRail.transform.position + "               Clean LocalPosition = " + thisRail.transform.localPosition + "\n");
            
            
            int currVariationIndex = -1;
            bool isSingle = false;
            FenceVariant singleVariant = null;

            //=======  Replace Single  =====
            //If the user has adde some single instance variation, we overide the pool and Instantiate a new one 
            int meshIndex = 0;
            if (useRailVariations == true && railSingles[sectionIndex] > -1)
            {
                currVariationIndex = railSingles[sectionIndex];
                singleVariant = FindSingleVariantWithSectionIndex(railSet, sectionIndex); // exists so they have independent scale/position/rotation

                // Check that this variant is still in the sourcer list
                // User may have rest the source list after setting the single, if not, cancel this single
                if (FindFirstInVariants(railSet, singleVariant.go) == -1)
                {
                    RemoveSingleVariantFromList(railSet, singleVariant);
                    railSingles[sectionIndex] = -1;
                }
                
                if (railSingles[sectionIndex] > -1 && singleVariant != null && singleVariant.enabled == true)
                {
                    //Abort early if necessary
                    if (currVariationIndex == kSkipIndex)
                    {
                        if (railSet == LayerSet.railALayerSet) railACounter++;
                        else if (railSet == LayerSet.railBLayerSet) railBCounter++;
                        thisRail.hideFlags = HideFlags.HideInHierarchy; thisRail.SetActive(false);
                        return;
                    }
                    if (railSet == LayerSet.railALayerSet)
                        DestroyImmediate(railsA[railACounter].gameObject);
                    if (railSet == LayerSet.railBLayerSet)
                        DestroyImmediate(railsB[railBCounter].gameObject);

                    thisRail = Instantiate(railVariants[currVariationIndex].go, Vector3.zero, Quaternion.identity) as GameObject;

                    FormatInstantiatedGONameWithSectionAndVariation(thisRail, sectionIndex, currVariationIndex);
                    if (railSet == LayerSet.railALayerSet)
                        railsA[railACounter] = thisRail.transform;
                    if (railSet == LayerSet.railBLayerSet)
                        railsB[railBCounter] = thisRail.transform;

                    isSingle = true;
                    currVariantForSeqStep = singleVariant;
                    meshIndex = currVariationIndex;
                }
            }
            if(isSingle == false)// Find the variation Index by parsing rail.name
            {
                string last4Chars = thisRail.name.Substring(thisRail.name.Length - 5);
                if (last4Chars.StartsWith("_sq"))
                    thisRail.name = thisRail.name.Remove(thisRail.name.Length - 5);

                string variationStr = "" + thisRail.name[thisRail.name.Length - 2];
                currVariationIndex = int.Parse(variationStr);

                if (variationMode == VariationMode.sequenced)
                {
                    currVariantForSeqStep = railVariants[currVariationIndex];
                }
                else
                {
                    currVariantForSeqStep = nonNullRailVariants[currVariationIndex];
                }
            }

            if (currVariantForSeqStep == null || currVariantForSeqStep.go == null)
                Debug.Log(("BuildRailsForSection(): currSeqStepVariant was null"));

            SeqVariant currStepVariant = new SeqVariant();
            int currSeqStepIndex = -1;
            if (variationMode == VariationMode.sequenced)
                currSeqStepIndex = sectionIndex % numUserSeqSteps;
            else if (variationMode == VariationMode.optimal && currSeq.Count > 0)
                currSeqStepIndex = sectionIndex % currSeq.Count;
            if (currSeqStepIndex != -1)
                currStepVariant = currSeq[currSeqStepIndex];

            //-- Name the seq step if needed --
            if (variationMode == VariationMode.sequenced && thisRail.name.EndsWith("]")) //ending with ']' means we haven't named the seq yet 
            {
                string seqIndexString = "";
                seqIndexString  = "_sq" + currSeqStepIndex.ToString("00");
                thisRail.name += seqIndexString;
            }

            //==== Meshes ====
            if(useRailVariations)
            {
                meshIndex = FindFirstInVariants(railSet, currVariantForSeqStep.go);
                if (meshIndex == -1)
                {
                    Debug.LogWarning("meshIndex = -1 \n");
                    meshIndex = 0;
                }
            }

            List<MeshFilter> mfList = MeshUtilitiesAFB.GetAllMeshFiltersFromGameObject(thisRail);
            Mesh thisModdedMesh = null;
            int meshCount = mfList.Count;

            List<Mesh> preparedMeshGroup = preparedMeshes[meshIndex];


            //Duplicate all the meshes in this group with copies from [preparedMeshGroup], ready for any editing
            int triCount = 0;
            for (int m = 0; m < meshCount; m++)
            {
              MeshUtilitiesAFB.ReplaceSharedMeshWithDuplicateOfMesh(mfList[m], preparedMeshGroup[m], preparedMeshGroup[m].name + "[+]");
                Mesh mesh = mfList[m].sharedMesh;
                triCount += mfList[m].sharedMesh.triangles.Length;
            }
            bounds = mfList[0].sharedMesh.bounds;
            railThickness = bounds.size.z * size.z;
            railMeshLength = bounds.size.x;
            railMeshHeight = bounds.size.y;

            //====  Set the size and offsets for the variations  ====
            Vector3 varPosOffset = Vector3.zero;
            Vector3 varSizeMultiplier = Vector3.one;
            Vector3 varRotation = Vector3.zero;
            if (useRailVariations && variationMode == VariationMode.sequenced)
            {
                varPosOffset = currSeq[currSeqStepIndex].pos;
                varSizeMultiplier = currSeq[currSeqStepIndex].size;
                varRotation = currSeq[currSeqStepIndex].rot;
            }
            if (useRailVariations)
            {
                // If this has been modified as a single, apply it here
                if(isSingle && singleVariant != null)
                {
                    varSizeMultiplier = Vector3.Scale(varSizeMultiplier, singleVariant.size);
                    varPosOffset += singleVariant.positionOffset;
                    varRotation += singleVariant.rotation;
                }
                railRotation = railRotation + varRotation;
            }

            //==== Skip Section for Zero Sized Variation  ====
            if ((currVariationIndex > 0 || variationMode == VariationMode.sequenced) && 
            (currStepVariant.stepEnabled == false || varSizeMultiplier.x == 0 || varSizeMultiplier.y == 0 || varSizeMultiplier.z == 0))
            {
                if (railSet == LayerSet.railALayerSet) railACounter++;
                else if (railSet == LayerSet.railBLayerSet) railBCounter++;
                thisRail.hideFlags = HideFlags.HideInHierarchy; thisRail.SetActive(false);
                return;
            }

            if (railSet == LayerSet.railALayerSet)
                railsATotalTriCount += triCount / 3;
            if (railSet == LayerSet.railBLayerSet)
                railsBTotalTriCount += triCount / 3;

            //===========================
            //      Rail Position
            //===========================
            alternateHeightDelta = (sectionIndex % 2) * 0.001f;

            
            //=====================================
            //      Mirror & Invert Variations
            //=====================================
            bool backToFront = false, mirrorZ = false, invert = false;
            if (useRailVariations)
            {
                if (variationMode == VariationMode.sequenced)
                {
                    backToFront = currStepVariant.backToFront;
                    mirrorZ = currStepVariant.mirrorZ;
                    invert = currStepVariant.invert;
                }
                for (int m = 0; m < meshCount; m++)
                {
                    thisModdedMesh = mfList[m].sharedMesh;
                    if (m > 0 && allowIndependentSubmeshVariation)
                    {
                        backToFront = currVariantForSeqStep.backToFront > UnityEngine.Random.value;
                        mirrorZ = currVariantForSeqStep.mirrorZ > UnityEngine.Random.value;
                        invert = currVariantForSeqStep.invert > UnityEngine.Random.value;
                    }
                    if (mirrorZ)
                    {
                        Vector3 mirrorZ_Vec = new Vector3(-1, 1, 1);
                        Mesh mirrorZMesh = MeshUtilitiesAFB.ScaleAndTranslateMesh(thisModdedMesh, mirrorZ_Vec, 
                            new Vector3(thisModdedMesh.bounds.center.x * 2, 0, 0), true);
                        mirrorZMesh = MeshUtilitiesAFB.ReverseNormals(mirrorZMesh);
                        mirrorZMesh.RecalculateNormals();
                        mirrorZMesh.RecalculateTangents();
                        thisModdedMesh = mirrorZMesh;
                    }
                }
            }
            //======== Y Scaling ========
            float randHeightScale = 1;
            //===========================================
            //        Rail Height Variation
            //============================================
            bool randomScopeIsValid = false;
            if (randomScope == 0 && currVariationIndex == 0) // main only && is main
                randomScopeIsValid = true;
            else if (randomScope == 1 && currVariationIndex > 0)
                randomScopeIsValid = true;//variations only and is variation
            else if (randomScope == 2)
                randomScopeIsValid = true;//main and variations, so is always true
            
            if(allowRailHeightVariation && randomScopeIsValid)
                randHeightScale = RandomLookupAFWB.randForRailA.RandomRange(minRailHeightVar, maxRailHeightVar, ref randRec.heightVariation);

            float cumulativeHeightScaling = globalScale.y * size.y * varSizeMultiplier.y * randHeightScale;

                        Vector3 scale = nativeScale;
            if (slopeMode != FenceSlopeMode.shear)//don't scale raillSize.y if sheared, as the vertices are explicitly set instead
                scale.y *= cumulativeHeightScaling;
            //If it's a panel type but NOT sheared, scale it with the fence
            else if (railPrefabs[thisRailType].name.EndsWith("_Panel_Rail") && slopeMode != FenceSlopeMode.shear)
                scale.y *= cumulativeHeightScaling;
            // It's a regular sheared
            else if (slopeMode == FenceSlopeMode.shear)
                scale.y *= cumulativeHeightScaling;


            float cumulativeHeightOffset = railMeshHeight * globalScale.y * varSizeMultiplier.y * (railPositionOffset.y + varPosOffset.y);

            if (railKeepGrounded == true)
            {
                cumulativeHeightOffset = cumulativeHeightScaling / 2;
            }
            else if (railKeepGrounded == false)
            {
                cumulativeHeightOffset = cumulativeHeightScaling / 2;
                cumulativeHeightOffset += railPositionOffset.y;
            }
            if(useRailVariations == true)
            {
                if ((variationMode == VariationMode.optimal || variationMode == VariationMode.random) && currVariationIndex > 0)
                    cumulativeHeightOffset +=  varPosOffset.y;
                else if (variationMode == VariationMode.sequenced)
                    cumulativeHeightOffset += varPosOffset.y;
            }

            Vector3 currDirection = CalculateDirection(posB, posA);
            //=====================================
            //      Rail Rotation From Direction
            //======================================
            GameObject prevRail = null;
            if (railSet == LayerSet.railALayerSet && railACounter > numStackedRailsInThisSet)
                prevRail = railsA[railACounter - numStackedRailsInThisSet].gameObject;
            else if (railSet == LayerSet.railBLayerSet && railBCounter > numStackedRailsInThisSet)
                prevRail = railsB[railBCounter - numStackedRailsInThisSet].gameObject;

            //------------ Simple Non-Overlap Version -----------
            if (sectionIndex == 0 || overlapAtCorners == false || prevRail == null )
            {
                thisRail.transform.Rotate(new Vector3(0, -90, 0));// because we want the length side to be considered 'forward'
                thisRail.transform.Rotate(new Vector3(0, currDirection.y, 0));

                thisRail.transform.position = posA + new Vector3(0, (gap * i) + alternateHeightDelta, 0);
                thisRail.transform.Translate(railPositionOffset.x, 0, railPositionOffset.z);
            }
            else
            {
                prevDirection = CalculateDirection(posA, prevPostPos);
                prevHeading = prevDirection.y;
                halfRailThickness = railThickness * 0.5f;
                currDirection = CalculateDirection(posB, posA);
                currHeading = currDirection.y;
                newChangeInHeading = currHeading - prevHeading;
                newPivotPoint = posA; // set initially to the primary Post point
                Vector3 prevVectorDir = (posA - prevPostPos).normalized;
                Vector3 orthogonalPreviousDirection = Vector3.zero;
                if (Mathf.Abs(newChangeInHeading) < 1f) { }//to do
                else
                {
                    orthogonalPreviousDirection = (-1 * (Quaternion.AngleAxis(90, Vector3.up) * prevVectorDir)).normalized;
                    float sine = Mathf.Sin((90 - newChangeInHeading) * Mathf.Deg2Rad);
                    float orthoScale = halfRailThickness - (sine * halfRailThickness);
                    Vector3 currExtraVector = orthogonalPreviousDirection * orthoScale;

                    if ((newChangeInHeading >= 0 && newChangeInHeading < 90) || (newChangeInHeading <= -270 && newChangeInHeading > -360))
                    {
                        newPivotPoint += currExtraVector;
                    }
                    else if ((newChangeInHeading >= 90 && newChangeInHeading < 180) || (newChangeInHeading <= -180 && newChangeInHeading > -270))
                    {
                        newPivotPoint += currExtraVector;
                        newPivotPoint -= orthogonalPreviousDirection * railThickness;
                    }
                    else if ((newChangeInHeading >= 180 && newChangeInHeading < 270) || (newChangeInHeading <= -90 && newChangeInHeading > -180))
                    {
                        orthogonalPreviousDirection *= -1;
                        sine *= -1;
                        orthoScale = halfRailThickness - (sine * halfRailThickness);
                        currExtraVector = orthogonalPreviousDirection * orthoScale;
                        newPivotPoint -= currExtraVector;
                    }
                    else if ((newChangeInHeading > 270 && newChangeInHeading < 360) || (newChangeInHeading < 0 && newChangeInHeading > -90))
                    {
                        orthogonalPreviousDirection *= -1;
                        currExtraVector = orthogonalPreviousDirection * orthoScale;
                        newPivotPoint += currExtraVector;
                    }
                    //Scale the previous rail to match the new calculation
                    float cosine = Mathf.Cos((90 - newChangeInHeading) * Mathf.Deg2Rad);
                    float adjacentSize = cosine * halfRailThickness;
                    Vector3 adjacentExtraVector = -prevRail.transform.right * adjacentSize;
                    float prevRailRealLength = railMeshLength * prevRail.transform.localScale.x;
                    float newPrevRailLength = prevRailRealLength + adjacentExtraVector.magnitude;
                    float prevRailLengthScalar = newPrevRailLength / prevRailRealLength;
                    Vector3 newPrevRailScale = Vector3.Scale(prevRail.transform.localScale, new Vector3(prevRailLengthScalar, 1, 1));
                    prevRail.transform.localScale = newPrevRailScale;
                }

                thisRail.transform.Rotate(new Vector3(0, -90, 0));// because we want the length side to be considered 'forward'
                Vector3 newEulerDirection = CalculateDirection(posB, newPivotPoint);
                thisRail.transform.RotateAround(newPivotPoint, Vector3.up, newEulerDirection.y);

                distance = Vector3.Distance(newPivotPoint, posB);

                //-- Position - Use Translate for x & z to keep it local relative
                thisRail.transform.position = newPivotPoint + new Vector3(0, (gap * i) + alternateHeightDelta, 0);
                thisRail.transform.Translate(railPositionOffset.x, 0, railPositionOffset.z);
            }

            thisRail.transform.Translate(0, cumulativeHeightOffset, 0);
            
            sectionInclineAngle = -currDirection.x;
            
            float heightAdjustmentForNonNormalizedMeshes = cumulativeHeightScaling * (railMeshHeight - 1.0f) / 2.0f;
            thisRail.transform.Translate(0, heightAdjustmentForNonNormalizedMeshes, 0);

            if (slopeMode == FenceSlopeMode.step)
            {
                if (posB.y > posA.y)
                    thisRail.transform.position += new Vector3(0, posB.y - posA.y, 0);
            }

            //===========================================
            //              Rail Position
            //============================================

            if (useRailVariations)
                thisRail.transform.Translate(new Vector3(varPosOffset.x * gs, 0, varPosOffset.z * gs));

            Vector3 railCentre = CalculateCentreOfRail(thisRail);

            //=====================================================
            //              Rail Rotation from User Settings
            //=====================================================

            Vector3 axisRight = thisRail.transform.right;
            axisRight = currentVectorDir.normalized;
            
            Vector3 rotationCentre = railCentre;
            if(rotateFromBase)
                rotationCentre.y -= railMeshHeight * cumulativeHeightScaling / 2;
            float dHeight = posB.y - posA.y;
            float fwdLength = currentVectorDir.magnitude;
            
            
            rotationCentre.y += (dHeight/2) * (3.0f/fwdLength);
            
            if (railRotation.x != 0)
                thisRail.transform.RotateAround(rotationCentre, axisRight, railRotation.x);
            if (railRotation.z != 0)
                thisRail.transform.RotateAround(rotationCentre, thisRail.transform.forward, railRotation.z);
            if (railRotation.y != 0)
                thisRail.transform.RotateAround(rotationCentre, thisRail.transform.up, railRotation.y);

            //--- Rotate for Slope Incline --------
            if (slopeMode == FenceSlopeMode.slope && sectionInclineAngle != 0)
                thisRail.transform.Rotate(new Vector3(0, 0, sectionInclineAngle)); //Incline. (z and x are swapped because we consider the length of the fence to be 'forward')
            
            //===========================================
            //              Rail Scale
            //============================================

            //--- X ---
            float gainInLength = 0;
            if (slopeMode == FenceSlopeMode.slope)// real length, i.e. hypotenuse
            {
                scale.x *= (distance / 3.0f) * size.x * varSizeMultiplier.x;
                gainInLength = (distance * size.x) - distance;
            }
            else if (slopeMode == FenceSlopeMode.shear)
            {
                if ((overlapAtCorners == false && doThatThing == true) || GetAngleFromZero(sectionInclineAngle) > 16) // this can be tuned, tradeoff for smooth overlaps on level ground, or smooth joints on steep slopes
                {
                    scale.x *= (horizDistance / 3.0f) * size.x * varSizeMultiplier.x; //prevRailLengthScalar
                    gainInLength = (horizDistance * size.x) - horizDistance;
                }
                else
                {
                    scale.x *= (distance / 3.0f) * size.x * varSizeMultiplier.x;
                    gainInLength = (distance * size.x) - distance;
                }
            }
            else if (slopeMode == FenceSlopeMode.step)
            {
                    scale.x *= (horizDistance / 3.0f) * size.x * varSizeMultiplier.x; // distance along ground i.e. adjacent
                    gainInLength = (horizDistance * size.x) - horizDistance;
            }

            
            if (size.x != 1.0f)
                thisRail.transform.Translate(gainInLength / 2, 0, 0);
            
            //--- Z ---
            scale.z *= size.z * globalScale.z * varSizeMultiplier.z;
            
            
            //-- Apply scaling ----
            thisRail.transform.localScale = scale;


            //==============================================================
            //     Small random rotations
            //==============================================================
            float randRotX = 0, randRotY = 0, randRotZ = 0;
            if (allowRandRailRotationVariation && randomScopeIsValid)
            {
                if(useMeshRotations == true)
                {
                    for (int m = 0; m < meshCount; m++)
                    {
                        thisModdedMesh = mfList[m].sharedMesh;
                        // If first mesh, or the object has multiple meshes and we allow them to vary indepenently, get new random values
                        if (m == 0 || allowIndependentSubmeshVariation)
                        {
                            randRotX = UnityEngine.Random.Range(-randRotXSize, randRotXSize);
                            randRotY = UnityEngine.Random.Range(-randRotYSize, randRotYSize);
                            randRotZ = UnityEngine.Random.Range(-randRotZSize, randRotZSize);
                        }

                        Vector3 randRotVec = new Vector3(randRotX, randRotY, randRotZ);
                        thisModdedMesh = MeshUtilitiesAFB.RotateMesh(thisModdedMesh, randRotVec, true, default(Vector3));
                    }
                }
                else
                {
                    railCentre = CalculateCentreOfRail(thisRail);
                    thisRail.transform.RotateAround(railCentre, thisRail.transform.right, UnityEngine.Random.Range(-randRotXSize, randRotXSize));
                    thisRail.transform.RotateAround(railCentre, thisRail.transform.up, UnityEngine.Random.Range(-randRotYSize, randRotYSize));
                    thisRail.transform.RotateAround(railCentre, thisRail.transform.forward, UnityEngine.Random.Range(-randRotZSize, randRotZSize));
                }
            }
            
            //==============================================================
            //     Orientation Flips
            //==============================================================

            if (backToFront && useMeshRotations == false)
            {
                railCentre = CalculateCentreOfRail(thisRail);
                thisRail.transform.RotateAround(railCentre, thisRail.transform.up, 180);
            }
            if (invert && useMeshRotations == false)
            {
                railCentre = CalculateCentreOfRail(thisRail);
                thisRail.transform.RotateAround(railCentre, thisRail.transform.forward, 180);
            }


            //================================================================================= 
            //Omit rails that would intersect with ground/other objects(Hide Colliding Rails) 
            //=================================================================================
            omit = OmitBuriedRails(currentVectorDir, distance, size, omit, thisRail);

            //=============================================================
            //      Shear Mesh     if it's a Panel type, to fit slopes
            //==============================================================
            if (omit == false)
            {
                if (slopeMode == FenceSlopeMode.shear)
                {
                    List<GameObject> goList = MeshUtilitiesAFB.GetAllMeshGameObjectsFromGameObject(thisRail);
                    float relativeDistance = 0, offset = 0, heightChangeFromSlope = 0;
                    for (int m = 0; m < meshCount; m++)
                    {
                        GameObject thisGO = goList[m];
                        thisModdedMesh = mfList[m].sharedMesh;
                        Vector3[] origVerts = thisModdedMesh.vertices;
                        Vector3[] vertices = thisModdedMesh.vertices;

                        for (int v = 0; v < vertices.Length; v++)
                        {
                            relativeDistance = (Mathf.Abs(vertices[v].x)) / DEFAULT_RAIL_LENGTH; // the distance of each vertex from the end
                            if (backToFront)
                            {
                                relativeDistance = 1 - relativeDistance; 
                            }
                            relativeDistance *= -size.x;

                            float regularScaledY = origVerts[v].y;
                            heightChangeFromSlope = relativeDistance * heightDeltaAB * (nativeScale.x / scale.y);
                            if (invert)
                            {
                                heightChangeFromSlope += (heightDeltaAB) * (nativeScale.x / scale.y);
                            }
                            if (meshCount > 1)
                                heightChangeFromSlope *= thisGO.transform.localScale.x;
                            
                            vertices[v].y = regularScaledY  + heightChangeFromSlope;
                        }
                        thisModdedMesh.vertices = vertices;
                        thisModdedMesh.RecalculateBounds();
                        mfList[m].sharedMesh = thisModdedMesh;
                    }
                } 
                
                
                if(doThatThing)
                {
                    currDirection = Quaternion.LookRotation(posB - posA).eulerAngles;
                    currHeading = currDirection.y;
                    prevDirection = Quaternion.LookRotation(posA - prevPostPos).eulerAngles;
                    if (sectionIndex == 0)
                        prevDirection = currDirection;
                    prevHeading = prevDirection.y;
                    nextDirection = Vector3.zero;
                    if (posC != posB) // there is a next panel
                    {
                        nextDirection = Quaternion.LookRotation(posC - posB).eulerAngles;
                        nextHeading = nextDirection.y;
                    }

                    //===================================
                    //            Doing that thing 
                    //===================================
                    thisModdedMesh = mfList[0].sharedMesh;
                    float widthScaling = scale.z;

                    float incomingAngle = currHeading - prevHeading;
                    if (incomingAngle < 0)
                        incomingAngle += 360;

                    float outgoingAngle = nextHeading - currHeading;
                    if (outgoingAngle < 0)
                        outgoingAngle += 360;

                    if (posC == posB) //this is the last section
                        outgoingAngle = 0;

                    Vector3 meshSize = thisModdedMesh.bounds.size;
                    float meshLength = meshSize.x;
                    float meshWidth = meshSize.z;
                    /////float actualLength = meshLength * thisCable.transform.localScale.z;
                    float actualLength = meshLength * (horizDistance / DEFAULT_RAIL_LENGTH) * size.x;

                    float meshLengthScaling = actualLength / meshLength;
                    float halfW = meshWidth * widthScaling / 2.0f;

                    // Bounding box points (anti-clockwise from bottom right
                    Vector3 p0 = new Vector3(0, 0, halfW);
                    Vector3 p1 = new Vector3(meshLength, 0, halfW);
                    Vector3 p2 = new Vector3(meshLength, 0, -halfW);
                    Vector3 p3 = new Vector3(0, 0, -halfW);

                    float inMiterAngle = incomingAngle / 2 * Mathf.Deg2Rad;
                    float outMiterAngle = outgoingAngle / 2 * Mathf.Deg2Rad;

                    float oppIn = Mathf.Tan(inMiterAngle) * halfW; // this is the shift in x position (was * w)
                    float oppOut = Mathf.Tan(outMiterAngle) * halfW;
                    oppIn /= meshLengthScaling;
                    oppOut /= meshLengthScaling;

                    p0 += new Vector3(oppIn, 0, 0);
                    p1 += new Vector3(-oppOut, 0, 0);
                    p2 += new Vector3(oppOut, 0, 0);
                    p3 += new Vector3(-oppIn, 0, 0);

                    float len0 = p1.x - p0.x; // the length of side 0 (the side that contains p0)
                    float len1 = p2.x - p3.x;
                    float scale0 = len0 / meshLength; //the scale of side 0
                    float scale1 = len1 / meshLength; //the scale of side 1
                    float maxScale = scale0 > scale1 ? scale0 : scale1;

                    Vector3 meshCenter = thisModdedMesh.bounds.center;
                    float zPos = 0, zRatio = 0, centerZ = meshCenter.z;
                    float deltaX = 0, vx, newBoundBoxX = 0;
                    float halfScaledDist = widthScaling / halfW;
                    Vector2[] uvs = thisModdedMesh.uv;
                    Vector3[] verts = thisModdedMesh.vertices;
                    for (int v = 0; v < verts.Length; v++)
                    {
                        float uvx = uvs[v].x;
                        vx = verts[v].x;
                        zRatio = Mathf.Abs(verts[v].z - centerZ) * halfScaledDist;

                        if (verts[v].z > 0)
                        {
                            newBoundBoxX = (vx * scale0) - oppIn;
                            deltaX = newBoundBoxX - vx;
                            verts[v].x = vx + (deltaX * zRatio);

                            float dx = verts[v].x - vx;
                            float dxRatio = dx / meshLength;
                            uvs[v].x = uvx - (dxRatio / 2);
                        }
                        else if (verts[v].z < 0)
                        {
                            newBoundBoxX = (vx * scale1) + oppIn;
                            deltaX = newBoundBoxX - vx;
                            verts[v].x = vx + (deltaX * zRatio);

                            float dx = verts[v].x - vx;
                            float dxRatio = dx / meshLength;
                            uvs[v].x = uvx - (dxRatio / 2);
                            /*if (scale1 > 1.0001f)
                                uvs[v].x -= 0.165f * meshLengthScaling;*/
                        }
                        //uvs[v].x *= maxScale;
                    }
                    thisModdedMesh.vertices = verts;
                    if (stretchUVs)
                        thisModdedMesh.uv = uvs;
                }

                //=========== Make/scale collider on first rail in the stack & remove on others ===========
                if (i == 0 && railSet == LayerSet.railALayerSet)
                {
                    CreateRailCollider(thisRail, (posA + posB) / 2);
                }
                else if (i == 0 && railSet == LayerSet.railBLayerSet)
                {
                    // Layer B Gets a Box Collider
                    BoxCollider boxCollider = thisRail.GetComponent<BoxCollider>();
                    if (boxCollider == null)
                    {
                        BoxCollider railCollider = (BoxCollider)thisRail.AddComponent<BoxCollider>();
                    }
                }
            }

            thisRail.isStatic = usingStaticBatching;
            if (railSet == LayerSet.railALayerSet)
                railACounter++;
            else if (railSet == LayerSet.railBLayerSet)
                railBCounter++;

            //====== Organize into subfolders so we can combine for drawcalls, but don't hit the mesh combine limit of 65k ==========
            int numRailsAFolders = (railACounter / objectsPerFolder) + 1;
            int numRailsBFolders = (railBCounter / objectsPerFolder) + 1;

            string railsDividedFolderName = "";
            if (railSet == LayerSet.railALayerSet)
                railsDividedFolderName = "RailsAGroupedFolder" + (numRailsAFolders - 1);
            else if (railSet == LayerSet.railBLayerSet)
                railsDividedFolderName = "RailsBGroupedFolder" + (numRailsBFolders - 1);


            GameObject railsDividedFolder = GameObject.Find("Current Fences Folder/Rails/" + railsDividedFolderName);
            if (railsDividedFolder == null)
            {
                railsDividedFolder = new GameObject(railsDividedFolderName);
                railsDividedFolder.transform.parent = railsFolder.transform;
                railsDividedFolder.transform.localPosition = Vector3.zero;
                if (addCombineScripts)
                {
                    CombineChildrenPlus combineChildren = railsDividedFolder.AddComponent<CombineChildrenPlus>();
                    if (combineChildren != null)
                        combineChildren.combineAtStart = true;
                }
            }
            thisRail.transform.parent = railsDividedFolder.transform;
        }

        //t.End();
    }
    //================================================================
    private bool OmitBuriedRails(Vector3 currentVectorDir, float distance, Vector3 size, bool omit, GameObject thisRail)
    {
        RaycastHit hit;
        int origLayer = thisRail.gameObject.layer;
        thisRail.gameObject.layer = 2; //raycast ignore colliders, we turn it on again at the end
        if (keepInterpolatedPostsGrounded && autoHideBuriedRails)
        {
            float bottom = GetMeshMin(thisRail).y;
            bottom *= size.y;
            Vector3 rayPosition = thisRail.transform.position + new Vector3(0, bottom * 0.8f, 0); // bottom * 0.8 tolerance can be adjusted
            if (Physics.Raycast(rayPosition, currentVectorDir, out hit, distance))
            {
                if (hit.collider.gameObject.name.Contains("_Rail") == false && hit.collider.gameObject.name.Contains("_Post") == false
                    && hit.collider.gameObject.name.Contains("_Extra") == false
                   && hit.collider.gameObject.name.StartsWith("FenceManagerMarker") == false)
                {
                    thisRail.hideFlags = HideFlags.HideInHierarchy;
                    thisRail.SetActive(false);
                    omit = true;
                }
            }
        }
        thisRail.gameObject.layer = origLayer; //restore layer
        return omit;
    }

    //================================================================
    private List<MeshFilter> JitterGameObjectVerts(List<MeshFilter> mfList)
    {
        jitterAmountRail = new Vector3(0.5f, 0.5f, 0.5f);
        int meshCount = mfList.Count;
        for (int m = 0; m < meshCount; m++)
        {
            Mesh thisModdedMesh = mfList[m].sharedMesh;
            thisModdedMesh = MeshUtilitiesAFB.AddRandomVertexOffsets(thisModdedMesh, jitterAmountRail * 0.05f);
            mfList[m].sharedMesh = thisModdedMesh;
        }
        return mfList;
    }
    //================================================================
    void JitterGameObjectVerts(GameObject go)
    {

    }
    //================================================================
    void DestroyBoxCollider(GameObject go)
    {
        BoxCollider boxCollider = go.GetComponent<BoxCollider>();
        if (boxCollider != null)
            DestroyImmediate(boxCollider);
    }
    //================================================================
    Vector3 CalculateCentreOfRail(GameObject rail)
    {
        Vector3 center = rail.transform.position;
        Vector3 newCenter = center;
        Vector3 f = rail.transform.forward;
        Vector3 fwd = new Vector3(f.x, f.y, f.z);

        fwd = new Vector3(f.z, f.y, -f.x); //swap x & z
        fwd *= rail.transform.localScale.x * 3 / 2; // scale by length of fence (native length = 3m, then divide by two for centre)
        newCenter = center - fwd;

        return newCenter;
    }
    //=================================================================
    public void BuildSubposts(Vector3 Prev, Vector3 A, Vector3 B, Vector3 C, int sectionIndex, bool isLastPost = false)
    {
        float distance = Vector3.Distance(A, B);
        Vector3 currentDirection = CalculateDirection(B, A);
        Vector3 directionVector = (B - A).normalized;
        Vector3 forward = directionVector;
        Vector3 right = MeshUtilitiesAFB.RotatePointAroundPivot(directionVector, Vector3.up, new Vector3(0, 90, 0));
        
        int intNumSubs = 1;
        GameObject thisSubJoiner = null;
        float actualSubSpacing = 1;
        if (subsFixedOrProportionalSpacing == 1) // depends on distance between posts
        {
            float idealSubSpacing = subSpacing;
            intNumSubs = (int)Mathf.Round(distance / idealSubSpacing);
            if (idealSubSpacing > distance)
                intNumSubs = 1;
            actualSubSpacing = distance / (intNumSubs + 1);
        }
        else if (subsFixedOrProportionalSpacing == 0)
        {
            intNumSubs = (int)subSpacing;
            actualSubSpacing = distance / (intNumSubs + 1);
        }
        else if (subsFixedOrProportionalSpacing == 2) // replicate main post position
        {
            intNumSubs = 1;
        }

        int start = 0;
        if (addSubpostAtPostPointAlso == true && subsFixedOrProportionalSpacing != 2)
            start = -1;
        
        Vector3 variantScaling = Vector3.one, variantOffset = Vector3.zero, variantRotation = Vector3.zero;
        
        for (int s = start; s < intNumSubs; s++)
        {
            if (UnityEngine.Random.value <= chanceOfMissingSubs)
                continue;
            int repetitionIndex = (sectionIndex * intNumSubs) + s;

            GameObject thisSub = RequestSub(subCounter).gameObject;
            if (thisSub == null)
            {
                print("Missing Sub " + s + " Have you deleted one?");
                continue;
            }

            thisSub.hideFlags = HideFlags.None;
            thisSub.SetActive(true);    
            thisSub.name = "Sub " + subCounter.ToString();
            thisSub.transform.parent = subpostsFolder.transform;
            thisSub.transform.position = A;
            // In stepped mode they take the height position from 'A' (the previous post, rather than the next)
            if (slopeModeRailA == FenceSlopeMode.step)
            {
                Vector3 stepPos = A;
                stepPos.y = A.y;
                thisSub.transform.position = stepPos;
            }
            
            //================= Subpost Variations ================================
            int currSeqIndex = -1;
            SeqVariant currSeqVariant = null;
            if (useSubpostVariations && subpostSeqInfo.numSteps > 1)
            {
                currSeqVariant = new SeqVariant();
                
                currSeqIndex = subCounter % subpostSeqInfo.numSteps;
                if (currSeqIndex != -1)
                    currSeqVariant = userSequenceSubpost[currSeqIndex];
                variantOffset = currSeqVariant.pos;
                variantScaling = currSeqVariant.size;
                variantRotation = currSeqVariant.rot;
                
            }
            //Debug.Log("sectionIndex " + sectionIndex + ":" + s  + "      currSeqIndex " + currSeqIndex + "       stepEnabled " + currSeqVariant.stepEnabled + "   " + thisSub.name + 
                      //"    "  + variantScaling + "\n");
            
            if (currSeqVariant != null && currSeqVariant.stepEnabled == false)
            {
                thisSub.hideFlags = HideFlags.HideInHierarchy;
                thisSub.SetActive(false);
                subCounter++;
                continue;
            }
            
            //=================================
            //            Position
            //=================================
            // Interpolate the subposts position between A & B, but keep Y fixed in Stepped Mode
            //Vector3 right = thisSub.transform.right;
            Vector3 offset = Vector3.zero, varOffset = Vector3.zero;
            
            Vector3 move = directionVector * actualSubSpacing * (s + 1);
            float moveY = move.y;
            float subFinalLength = subpostSize.y * globalScale.y;
            // Modes for subsFixedOrProportionalSpacing: 0 "Fixed Number Between Posts",  1 "Depends on Section Length",  2 "Duplicate Main Post Positions Only"
            if (subsFixedOrProportionalSpacing < 2 && s > -1)
            {
                if (slopeModeRailA == FenceSlopeMode.step)
                    moveY = 0;
                thisSub.transform.position += new Vector3(move.x, moveY, move.z);
                thisSub.transform.position += new Vector3(0, subpostPositionOffset.y * globalScale.y, 0);
                
                offset = right * subpostPositionOffset.z;
                thisSub.transform.position += offset;
                
                offset = forward * subpostPositionOffset.x; 
                thisSub.transform.position += offset;
            }
            else if (subsFixedOrProportionalSpacing == 2 || s == -1)
            {
                thisSub.transform.position = A;
                thisSub.transform.position += new Vector3(0, subpostPositionOffset.y * globalScale.y, 0);
                
                offset = right * subpostPositionOffset.z;
                thisSub.transform.position += offset;
                
                offset = forward * subpostPositionOffset.x; 
                thisSub.transform.position += offset;
                
                
                /*thisSub.transform.position = A;
                thisSub.transform.Translate(0, subpostPositionOffset.y, subpostPositionOffset.z);
                thisSub.transform.position += new Vector3(0, 0, subpostPositionOffset.x);*/
            }

            if (useSubpostVariations == true /*&& variantOffset != Vector3.zero*/)
            {
                varOffset = right * variantOffset.z;
                //Debug.Log("var right  " + right + "         z  " + variantOffset.z + "\n");
                thisSub.transform.position += varOffset;
                
                varOffset = forward * variantOffset.x; 
                //Debug.Log("forward  " + thisSub.transform.forward + "         x  " + subpostPositionOffset.x + "\n");
                thisSub.transform.position += varOffset;
            }
            
            //=================================
            //            Rotation
            //=================================
            thisSub.transform.rotation = Quaternion.identity;
            thisSub.transform.Rotate(new Vector3(0, currentDirection.y, 0), Space.Self);
            if (subsFixedOrProportionalSpacing == 2 && isLastPost == true) // using 'replicate posts only' mode
                thisSub.transform.Rotate(new Vector3(0, 180, 0), Space.Self);

            //thisSub.transform.Translate(0, 0, subpostPositionOffset.z);
            
            thisSub.transform.Rotate(new Vector3(subpostRotation.x, subpostRotation.y, subpostRotation.z), Space.Self);
            if (useSubpostVariations == true && variantRotation != Vector3.zero)
                thisSub.transform.Rotate(variantRotation);
            
            
            
            //===================== Apply sine to height of subposts =======================
            if (useWave && subsFixedOrProportionalSpacing < 2)
            {
                float realMoveForward = move.magnitude;
                float sinValue = Mathf.Sin((((realMoveForward / distance) * Mathf.PI * 2) + wavePosition) * frequency);
                sinValue *= amplitude * globalScale.y;
                subFinalLength = (subpostSize.y) + sinValue + (amplitude * globalScale.y);
                //==== Create Sub Joiners ====
                if (s > 0 && useSubJoiners)
                {
                    thisSubJoiner = RequestSubJoiner(subJoinerCounter++);
                    if (thisSubJoiner != null)
                    {
                        thisSubJoiner.transform.position = thisSub.transform.position + new Vector3(0, (subFinalLength) - .01f, 0);
                        thisSubJoiner.transform.rotation = Quaternion.identity;
                    }
                }
            }
            //=========== Scale ==============      
            Vector3 scale = Vector3.one;
            scale.x *= subpostSize.x * globalScale.x;
            scale.y *= subFinalLength;
            scale.z *= subpostSize.z * globalScale.z;
            
            if (useSubpostVariations == true && variantScaling != Vector3.one)
                scale = Vector3.Scale(scale, variantScaling);
            
            thisSub.transform.localScale = scale;

            //=============== SubPost Spreading ================
            // A quick and dirty spreading/bunching. TODO add a choice of spreading algorithms
            if (Math.Abs(subPostSpread) >= 0.1f)
            {
                float spread = subPostSpread;
                spread *= interPostDist  / distance;

                float halfDist = distance / 2;
                Vector3 vecFromA = A - thisSub.transform.localPosition;
                float distFromA = Math.Abs(vecFromA.magnitude);
                Vector3 vecFromB = B - thisSub.transform.localPosition;
                float distFromB = Math.Abs(vecFromB.magnitude);
                Vector3 moveSpread = Vector3.zero;
                if (Math.Abs(distFromA - distFromB) < 0.1f) { } //TODO
                else if (distFromA < distFromB)
                {
                    moveSpread = distFromA * (distFromA/2) * directionVector * spread;
                }
                else if(distFromA > distFromB)
                {
                    moveSpread = distFromB * (distFromB/2) * - directionVector * spread;
                }
                moveSpread.y = 0;

                moveSpread = -thisSub.transform.InverseTransformVector(moveSpread);
                thisSub.transform.Translate(moveSpread);

                Vector3 flatA = new Vector3(A.x, 0, A.z);
                Vector3 flatB = new Vector3(B.x, 0, B.z);
                Vector3 flatPos = new Vector3(thisSub.transform.localPosition.x, 0, thisSub.transform.localPosition.z);

                Vector3 flatDeltaVec = flatB - flatA;

                float finalFlatDistFromA = (flatPos - flatA).magnitude;
                float distProp = finalFlatDistFromA / flatDeltaVec.magnitude;
                float spanHeightDelta = (B-A).y;
                float yPos = A.y + (spanHeightDelta * distProp);
                thisSub.transform.localPosition = new Vector3(thisSub.transform.localPosition.x, yPos, thisSub.transform.localPosition.z);
            }


            //=============== Sub Joinsers ================
            if (s > 0 && useSubJoiners && thisSubJoiner != null) // need to do this after the final sub calculations
            {
                Vector3 a = subposts[subCounter].transform.position + new Vector3(0, subposts[subCounter].transform.localScale.y, 0);
                Vector3 b = subposts[subCounter - 1].transform.position + new Vector3(0, subposts[subCounter - 1].transform.localScale.y, 0);
                float joinerDist = Vector3.Distance(b, a);
                thisSubJoiner.transform.localScale = new Vector3(joinerDist, thisSubJoiner.transform.localScale.y, thisSubJoiner.transform.localScale.z);
                Vector3 subJoinerDirection = CalculateDirection(a, b);
                thisSubJoiner.transform.Rotate(new Vector3(0, subJoinerDirection.y - 90, -subJoinerDirection.x + 180));
                thisSubJoiner.GetComponent<Renderer>().sharedMaterial = thisSub.GetComponent<Renderer>().sharedMaterial;
            }
            //=============== Force Subs to Ground ================
            if (forceSubsToGroundContour)
            {
                SetIgnoreColliders(true); // temporarily ignore other fence colliders to find distance to ground
                Vector3 currPos = thisSub.transform.position;
                float rayStartHeight = globalScale.y * 2.0f;
                currPos.y += rayStartHeight;
                RaycastHit hit;
                if (Physics.Raycast(currPos, Vector3.down, out hit, 500))
                {
                    if (hit.collider.gameObject != null)
                    {
                        float distToGround = hit.distance + 0.04f - subsGroundBurial; //in the ground a little
                        thisSub.transform.Translate(0, -(distToGround - rayStartHeight), 0);
                        scale.y += (distToGround - rayStartHeight)/2;
                        thisSub.transform.localScale = scale;
                    }
                }
                SetIgnoreColliders(false);
            }
            //== Random Height Variation ==
            if (allowSubpostHeightVariation)
            {
                float randHeightScale = UnityEngine.Random.Range(minSubpostHeightVar, maxSubpostHeightVar);
                thisSub.transform.localScale = Vector3.Scale(thisSub.transform.localScale, new Vector3(1, randHeightScale, 1));
            }
            //================= Add Random Rotations ===========================
            if (allowRandSubpostRotationVariation)
            {
                float xRot = UnityEngine.Random.Range(-subpostRandRotationAmount.x, subpostRandRotationAmount.x);
                float yRot = UnityEngine.Random.Range(-subpostRandRotationAmount.y, subpostRandRotationAmount.y);
                float zRot = UnityEngine.Random.Range(-subpostRandRotationAmount.z, subpostRandRotationAmount.z);
                thisSub.transform.Rotate(new Vector3(xRot, yRot, zRot));
            }
            //================= Add Random Quantized Rotations ===========================
            if (allowQuantizedRandomSubpostRotation)
            {
                int num = UnityEngine.Random.Range(0, 24);
                float totalRotAmount = num * subpostQuantizeRotAmount;
                totalRotAmount = totalRotAmount % 360;
                thisSub.transform.Rotate(new Vector3(0, totalRotAmount, 0));
            }
            //========= Chance of Missing SubPost ==============
            if (UnityEngine.Random.value < chanceOfMissingSubpost)
            {
                thisSub.gameObject.SetActive(false); // deleted when finalized
                thisSub.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }

            subCounter++;
            thisSub.isStatic = usingStaticBatching;
            //====== Organize into subfolders (pun not intended) so we don't hit the mesh combine limit of 65k ==========
            int numSubsFolders = (subCounter / objectsPerFolder) + 1;
            string subsDividedFolderName = "SubsGroupedFolder" + (numSubsFolders - 1);
            GameObject subsDividedFolder = GameObject.Find("Current Fences Folder/Subs/" + subsDividedFolderName);
            if (subsDividedFolder == null)
            {
                subsDividedFolder = new GameObject(subsDividedFolderName);
                subsDividedFolder.transform.parent = subpostsFolder.transform;
                if (addCombineScripts)
                {
                    CombineChildrenPlus combineChildren = subsDividedFolder.AddComponent<CombineChildrenPlus>();
                    if (combineChildren != null)
                        combineChildren.combineAtStart = true;
                }
            }
           
            thisSub.transform.parent = subsDividedFolder.transform;

            subPostsTotalTriCount += MeshUtilitiesAFB.CountAllTrianglesInGameObject(thisSub);
        }
    }
    //----------------------
    float GetAngleFromZero(float angle)
    {
        if (angle <= 180 && angle >= 0)
            return angle;
        if (angle > 180)
            return 360 - angle;
        if (angle < -180)
            return 360 + angle;

        return -angle;
    }
    //==================================================================
    //      Create a Pool of Posts and Rails
    //      We only need the most basic psuedo-pool to allocate enough GameObjects (and resize when needed)
    //      They get activated/deactivated when necessary
    //      As memory isn't an issue at runtime (once the fence is built/finalized, there is NO pool, only the actual objects used), allocating 25% more 
    //      GOs each time reduces the need for constant pool-resizing and laggy performance in the editor.
    //===================================================================
    public void CreatePools()
    {
        CreatePostPool();
        CreateClickMarkerPool();
        CreateRailPool(0, LayerSet.railALayerSet); //Rail A
        CreateRailPool(0, LayerSet.railBLayerSet); // Rail B
        CreateSubpostPool();
        CreateExtraPool();
    }
    //-------------
    void CreateExtraPool(int n = 0, bool append = false)
    {
        // Make sure the post type is valid
        if (currentExtraType == -1 || currentExtraType >= extraPrefabs.Count || extraPrefabs[currentExtraType] == null)
            currentExtraType = 0;
        // Figure out how many to make
        if (n == 0)
            n = defaultPoolSize;
        int start = 0;
        if (append)
        {
            start = extras.Count;
            n = start + n;
        }
        //calcultae number of array clones
        int numClones = 1;
        if (makeMultiArray)
            numClones = ((int)multiArraySize.x * (int)multiArraySize.y * (int)multiArraySize.z); // -1 because we don't clone the root postion one in the array
        int finalCount = (n * numClones);

        // Add n new ones to the posts List<>
        GameObject extra = null;
        if (extraPrefabs.Count > currentExtraType && extraPrefabs[currentExtraType] != null)
        {
            for (int i = start; i < finalCount; i++)
            {
                extra = Instantiate(extraPrefabs[currentExtraType], Vector3.zero, Quaternion.identity) as GameObject;
                extra.SetActive(false);
                extra.hideFlags = HideFlags.HideInHierarchy;
                extras.Add(extra.transform);
                extra.transform.parent = extrasFolder.transform;
            }
        }
        else
            Debug.Log("CreateExtraPool(): Extra was null");
    }
    //-------------
    void CheckClickMarkerPool()
    {
        if (markers.Count < clickPoints.Count)
            CreateClickMarkerPool();
    }
    //-------------
    void CreateClickMarkerPool()
    {
        int n = 8;
        if (clickPoints.Count() >= 8) n = (int)(clickPoints.Count() * 1.25f);
        DestroyMarkers();
        //Create Markers & deactivate them
        if (clickMarkerObj != null)
        {
            for (int i = 0; i < n; i++)
            {
                GameObject marker = Instantiate(clickMarkerObj, Vector3.zero, Quaternion.identity) as GameObject;
                marker.SetActive(false);
                marker.hideFlags = HideFlags.HideInHierarchy;
                markers.Add(marker.transform);
                marker.transform.parent = postsFolder.transform;
            }
        }//print("CreateClickMarkerPool():    " + clickPoints.Count() + " ClickPoints           "  +  markers.Count + " Markers");
    }

    //-------------
    void CreatePostPool(int n = 0, bool append = false)
    {//Debug.Log("CreatePostPool()");
        if (postPrefabs.Count == 0)
        {
            Debug.Log("Post prefabs not loaded");
            return;
        }

        // Make sure the post type is valid
        if (currentPostType == -1 || currentPostType >= postPrefabs.Count || postPrefabs[currentPostType] == null)
            currentPostType = 0;
        // Figure out how many to make
        if (n == 0)
            n = defaultPoolSize;
        int start = 0;
        if (append)
        {
            start = posts.Count;
            n = start + n;
        }

        int numVariants = postVariants.Count;
        List<FenceVariant> theseVariants = postVariants;

        if (currentPostType >= postPrefabs.Count)
            Debug.Log("CreatePostPool()  index problem ");

        GameObject post = null, thisPostSourceGO = postPrefabs[currentPostType];
        int variantIndex = 0;
        for (int i = start; i < n; i++)
        {
            if (usePostVariations && numVariants > 1)
            {
                int seqStep = i % postSeqInfo.numSteps;
                variantIndex = userSequencePost[seqStep].objIndex;
                thisPostSourceGO = theseVariants[variantIndex].go;

            }
            if (thisPostSourceGO == null)
                thisPostSourceGO = postPrefabs[currentPostType];

            if (thisPostSourceGO == null)
                Debug.LogWarning("Missing post prefab in CreatePostPool()");

            post = Instantiate(thisPostSourceGO, Vector3.zero, Quaternion.identity) as GameObject;
            post.SetActive(false);
            post.hideFlags = HideFlags.HideInHierarchy;

            /*string meshName = MeshUtilitiesAFB.GetMeshFromGameObject(thisPostSourceGO).name;
            string uniqueName = meshName + "_" + i.ToString();
            MeshUtilitiesAFB.ReplaceAllMeshesInGameObjectWithUniqueDuplicates(post);
            Mesh m = MeshUtilitiesAFB.GetMeshFromGameObject(post);
            m.name = uniqueName;*/

            string id = "[" + i + " v" + variantIndex + "]";
            post.name = post.name.Replace("(Clone)", id);
            posts.Add(post.transform);
            post.transform.parent = postsFolder.transform;
            //Debug.Log(post.transform.localScale.y + "\n");
        }

        for (int i = 0; i < posts.Count; i++)
        {
            //Debug.Log("CreatePostPool " + posts[i].transform.localScale.y + "\n");
        }
    }
    //-----------------------------
    void CreateSubpostPool(int n = 0, bool append = false)
    {
        // Make sure the post type is valid
        if (currentSubpostType == -1 || currentSubpostType >= postPrefabs.Count || postPrefabs[currentSubpostType] == null)
            currentSubpostType = 0;
        if (n == 0)
            n = defaultPoolSize * 2;
        int start = 0;
        if (append)
        {
            start = subposts.Count;
            n = start + n;
        }
        
        int numVariants = subpostVariants.Count;
        List<FenceVariant> theseVariants = subpostVariants;

        if (currentSubpostType >= postPrefabs.Count)
            Debug.LogWarning("CreateSubpostPool()  index problem ");

        GameObject subpost = null, thisSubpostSourceGO = postPrefabs[currentSubpostType];
        int variantIndex = 0;
        for (int i = start; i < n; i++)
        {
            if (useSubpostVariations && numVariants > 1)
            {
                int seqStep = i % subpostSeqInfo.numSteps;
                variantIndex = userSequenceSubpost[seqStep].objIndex;
                thisSubpostSourceGO = theseVariants[variantIndex].go;

            }
            if (thisSubpostSourceGO == null)
                thisSubpostSourceGO = postPrefabs[currentPostType];

            if (thisSubpostSourceGO == null)
                Debug.LogWarning("Missing subpost prefab in CreatePostPool()");

            subpost = Instantiate(thisSubpostSourceGO, Vector3.zero, Quaternion.identity) as GameObject;
            subpost.SetActive(false);
            subpost.hideFlags = HideFlags.HideInHierarchy;

            /*string meshName = MeshUtilitiesAFB.GetMeshFromGameObject(thisSubpostSourceGO).name;
            string uniqueName = meshName + "_" + i.ToString();
            MeshUtilitiesAFB.ReplaceAllMeshesInGameObjectWithUniqueDuplicates(subpost);
            Mesh m = MeshUtilitiesAFB.GetMeshFromGameObject(subpost);
            m.name = uniqueName;*/
            
            
            string id = "[" + i + " v" + variantIndex + "]";
            subpost.name = subpost.name.Replace("(Clone)", id);
            subposts.Add(subpost.transform);
            subpost.transform.parent = subpostsFolder.transform;
            //Debug.Log(post.transform.localScale.y + "\n");
        }

        for (int i = start; i < n; i++)
        {
            if (subJoinerPrefabs[0] == null) continue;
            GameObject subJoiner = Instantiate(subJoinerPrefabs[0], Vector3.zero, Quaternion.identity) as GameObject;
            subJoiner.SetActive(false);
            subJoiner.hideFlags = HideFlags.HideInHierarchy;
            subJoiners.Add(subJoiner.transform);
            subJoiner.transform.parent = subpostsFolder.transform;
        }
        //Debug.Log("Pooled " + (n-start) + " subposts");
    }
    //-------------
    // Find the index of the input GO in the non-null list
    public int FindFirstInNonNullRailVariants(LayerSet railsSet, GameObject go)
    {
        int index = -1, count = nonNullRailAVariants.Count;
        if (railsSet == LayerSet.railALayerSet)
        {
            for (int i = 0; i < count; i++)
            {
                if (nonNullRailAVariants[i].go == go)
                    return i;
            }
        }
        if (railsSet == LayerSet.railBLayerSet)
        {
            count = nonNullRailBVariants.Count;
            for (int i = 0; i < count; i++)
            {
                if (nonNullRailBVariants[i].go == go)
                    return i;
            }
        }
        return index;
    }
    //-------------
    // Find the index of the input GO in the full variant list
    public int FindFirstInVariants(LayerSet railsSet, GameObject go)
    {
        int index = -1, count = railAVariants.Count;
        if (railsSet == LayerSet.railALayerSet)
        {
            for (int i = 0; i < count; i++)
            {
                if (railAVariants[i].go == go)
                    return i;
            }
        }
        if (railsSet == LayerSet.railBLayerSet)
        {
            count = railBVariants.Count;
            for (int i = 0; i < count; i++)
            {
                if (railBVariants[i].go == go)
                    return i;
            }
        }
        return index;
    }
    //-------------
    // Find the index of the input GO in the full variant list
    public int FindFirstInVariants(List<FenceVariant> variantList, GameObject go)
    {
        int index = -1, count = variantList.Count;
        for (int i = 0; i < count; i++)
        {
            if (railAVariants[i].go == go)
                return i;
        }
        return index;
    }
    //-------------
    public List<FenceVariant> CreateUsedVariantsList(LayerSet layerSet)
    {
        List<FenceVariant> uniqueVaraints = null;
        if (layerSet == LayerSet.railALayerSet)
        {
            nonNullRailAVariants = CreateUniqueVariantList(railAVariants);
            return nonNullRailAVariants;
        }
        else if (layerSet == LayerSet.railBLayerSet)
        {
            nonNullRailBVariants = CreateUniqueVariantList(railAVariants);
            return nonNullRailBVariants;
        }
        else if (layerSet == LayerSet.postLayerSet)
        {
            nonNullPostVariants = CreateUniqueVariantList(postVariants);
            return nonNullPostVariants;
        }
        else if (layerSet == LayerSet.subpostLayerSet)
        {
            nonNullSubpostVariants = CreateUniqueVariantList(subpostVariants);
            return nonNullSubpostVariants;
        }

        return null;


        //-- Always ensure the main base object is set
        /*if (railAVariants == null || railAVariants.Count == 0 || railAVariants[0] == null || nonNullRailAVariants == null)
            Debug.Log("Missing railAVariants in CreateUsedVariantsList() \n");
        if (railAVariants.Count > kNumRailVariations || nonNullRailAVariants.Count > kNumRailVariations)
            Debug.Log("Too Many Rail Variants in CreateUsedVariantsList() \n");

        railAVariants[0].go = railPrefabs[currentRailAType];
        railAVariants[0].enabled = true;
        List<FenceVariant> source = railAVariants;
        List<FenceVariant> dest = nonNullRailAVariants;

        if (layerSet == LayerSet.railBLayerSet)
        {
            railBVariants[0].go = railPrefabs[currentRailBType];
            railBVariants[0].enabled = true;
            source = railBVariants;
            dest = nonNullRailBVariants;
        }
        int count = source.Count;
        if (layerSet == LayerSet.postLayerSet)
        {
            postVariants[0].go = postPrefabs[currentPostType];
            //we don't need a nonNull list for posts as they are only in Sequence mode, so enable them all and return
            for (int i = 0; i < count; i++)
            {
                //postVariants[i].enabled = true;
            }
            return null;
        }
        if (dest == null)
        {
            dest = new List<FenceVariant>();
        }
        dest.Clear();
        
        for (int i = 0; i < count; i++)
        {
            FenceVariant thisVariant = source[i];
            if (thisVariant != null && thisVariant.go != null && thisVariant.enabled == true)
            { 
                dest.Add(thisVariant);
            }
        }
        return dest;*/
    }
    //--------------------
    public List<FenceVariant> CreateUniqueVariantList(List<FenceVariant> sourceVariantList)
    {
        List<FenceVariant> uniqueList = new List<FenceVariant>();

        GameObject mainGO = sourceVariantList[0].go;

        foreach (var source in sourceVariantList)
        {
            bool found = false;
            foreach (var dest in uniqueList)
            {
                if (source.go == dest.go)
                {
                    found = true;
                    break;
                }
            }
            if(found == false)
            {
                uniqueList.Add(source);
            }
        }
        return uniqueList;
    }
    //-----------------------------
    void CreateRailPool(int n, LayerSet railsSet, bool append = false)
    {
        if (railsSet == LayerSet.railBLayerSet && useRailsB == false)
            return;

        int numStackedRails = 1, railType = 0, count = 0, numUserSeqSteps = railASeqInfo.numSteps;
        int[] shuffledRailIndices = shuffledRailAIndices;
        bool useRailVariations = useRailAVariations;
        VariationMode variationMode = variationModeRailA;
        List<SeqVariant> userSequence = userSequenceRailA;
        List<SeqVariant> optimalSequence = optimalSequenceRailA;
        int optimalSequenceLength = optimalSequenceRailA.Count;

        if (railsSet == LayerSet.railALayerSet)
        {//rail A
            railType = currentRailAType;
            numStackedRails = numStackedRailsA;
            count = railsA.Count;
            //Debug.Log("CreateRail A Pool()\n");
        }
        else if (railsSet == LayerSet.railBLayerSet)
        {//rail B
            railType = currentRailBType;
            variationMode = variationModeRailB;
            numStackedRails = numStackedRailsB;
            count = railsB.Count;
            numUserSeqSteps = railBSeqInfo.numSteps;
            userSequence = userSequenceRailB;
            useRailVariations = useRailBVariations;
           // Debug.Log("CreateRail B Pool()\n");
        }
        if (railType == -1 || railType >= railPrefabs.Count || railPrefabs[railType] == null)
        {
            Debug.Log("Invalid railType in CreateRailPool(), setting type to 0");
            railType = 0;
        }


        if (n == 0)
            n = defaultPoolSize * numStackedRails;
        int start = 0;
        if (append)
        {
            start = count;
            n = start + n;
        } //Debug.Log("CreateRailPool:   " + start + " to " + n + "  , Added " + (n - start));


        List<FenceVariant> theseVariants = null;
        if (railsSet == LayerSet.railALayerSet)
        {
            if (railAVariants.Count == 0)
                Debug.Log("CreateRailPool()        Missing railAVariants");
            theseVariants = CreateUniqueVariantList(railAVariants);
            if (variationMode == VariationMode.sequenced)
                theseVariants = railAVariants;
        }
        else if (railsSet == LayerSet.railBLayerSet)
        {
            if (railBVariants.Count == 0)
                Debug.Log("CreateRailPool()        Missing railBVariants");
            theseVariants = CreateUniqueVariantList(railBVariants);
            if (variationMode == VariationMode.sequenced)
                theseVariants = railBVariants;
        }

        int variantIndex = 0, numVariants = theseVariants.Count;

        GameObject rail = null, thisRailSourceGO = railPrefabs[railType];
        int seqStep = 0;
        for (int i = start; i < n; i++)
        {
            if (useRailVariations && numVariants > 1)
            {
                if (variationMode == VariationMode.sequenced)//always true in v3.1
                {
                    seqStep = i % numUserSeqSteps;
                    variantIndex = userSequence[seqStep].objIndex;
                    thisRailSourceGO = theseVariants[variantIndex].go;
                }

                if (thisRailSourceGO == null)
                    thisRailSourceGO = railPrefabs[railType];
            }

            rail = Instantiate(thisRailSourceGO, Vector3.zero, Quaternion.identity) as GameObject;
            // Remove "(Clone)" substring created by Instantiate(), and append the variant index
            FormatInstantiatedGONameWithSectionAndVariation(rail, i, variantIndex);
            rail.SetActive(false);
            rail.hideFlags = HideFlags.HideInHierarchy;
            rail.transform.parent = railsFolder.transform;

            if (railsSet == LayerSet.railALayerSet)
                railsA.Add(rail.transform);
            else if (railsSet == LayerSet.railBLayerSet)
                railsB.Add(rail.transform);
        }
    }
    //-----------------------------
    void FormatInstantiatedGONameWithSectionAndVariation(GameObject rail, int sectionIndex, int variantIndex)
    {
        string idStr = "[" + sectionIndex + " v" + variantIndex + "]";
        string name = rail.name.Replace("(Clone)", idStr);
        rail.name = name;
    }
    
    //-----------------------------
    // Increase pool size by 10-25% more than required if necessary (making surplus saves us having to remake pool every time a clickpoint is added)
    public void CheckResizePools()
    {
        if (clickPoints.Count > posts.Count)
        { //can only happen under extreme conditions such as user deleting/replacing this script. If so rebuild pool entirely
            CreatePools();
        }
        float multiplier = 1f;// These are temporarily hand-tuned
        if (allPostsPositions.Count >= 200) multiplier = 1.1f;
        else if (allPostsPositions.Count >= 100) multiplier = 1.25f;
        else if (allPostsPositions.Count >= 75) multiplier = 1.334f;
        else multiplier = 1.5f;

        //-- Posts---
        if (allPostsPositions.Count >= posts.Count - 1)
        {
            CreatePostPool((int)(allPostsPositions.Count * multiplier) - posts.Count, true); // add 25% more than needed, append is true
        }
        //-- Rails---
        if (allPostsPositions.Count * (numStackedRailsA) >= railsA.Count - 1)
        {
            CreateRailPool((int)((allPostsPositions.Count * (numStackedRailsA) * multiplier) - railsA.Count), LayerSet.railALayerSet, true);
        }
        //-- Rails B---
        if (useRailsB && allPostsPositions.Count * (numStackedRailsB) >= railsB.Count - 1)
        {
            CreateRailPool((int)((allPostsPositions.Count * (numStackedRailsB) * multiplier) - railsB.Count), LayerSet.railBLayerSet, true);
        }
        //-- Click point Markers---
        if (clickPoints.Count >= markers.Count - 1)
        {
            CreateClickMarkerPool();
        }
        //-- Extras--- we use the same number as posts
        if (allPostsPositions.Count * (multiArraySize.x * multiArraySize.y * multiArraySize.z) >= extras.Count - 1)
        {
            CreateExtraPool((int)(allPostsPositions.Count * multiplier) - extras.Count, true); // add 25% more than needed, append is true
        }
    }
    //---------------------- it's harder to predict how many subposts there might be, so better to adjust storage when one is needed
    Transform RequestSub(int index)
    {
        if (index >= subposts.Count - 1)
        {
            CreateSubpostPool((int)(subposts.Count * 0.25f), true); // add 25% more, append is true
        }
        return subposts[index];
    }
    //---------------------- Allocation is handled by Subs ---------
    GameObject RequestSubJoiner(int index)
    {
        if (subJoiners[index] == null || subJoiners[index].gameObject == null) return null;
        GameObject thisSubJoiner = subJoiners[index].gameObject;
        thisSubJoiner.hideFlags = HideFlags.None;
        thisSubJoiner.SetActive(true);
        thisSubJoiner.name = "SubJoiner " + index.ToString();
        thisSubJoiner.transform.parent = subpostsFolder.transform;
        return thisSubJoiner;
    }
    //-----------------------
    public void ResetAllPools()
    {
        ResetRailPool();
        ResetPostPool();
        ResetExtraPool();
        ResetSubpostPool();
    }
    //---------------
    public void ResetPool(AutoFenceCreator.LayerSet layerSet)
    {
        if (layerSet == AutoFenceCreator.LayerSet.postLayerSet)
        {
            ResetPostPool();
        }
        else if (layerSet == AutoFenceCreator.LayerSet.railALayerSet)
        {
            ResetRailAPool();
        }
        else if (layerSet == AutoFenceCreator.LayerSet.railBLayerSet)
        {
            ResetRailBPool();
        }
        else if (layerSet == AutoFenceCreator.LayerSet.extraLayerSet)
        {
            ResetExtraPool();
        }
        else if (layerSet == AutoFenceCreator.LayerSet.subpostLayerSet)
        {
            ResetSubpostPool();
        }
    }
    //-----------------------
    // resetting is necessary when a part has been swapped out, we need to banish all the old ones
    public void ResetPostPool()
    {
        DestroyPosts();
        CreatePostPool(posts.Count);
        DestroyMarkers();
        CreateClickMarkerPool();
    }
    //---------
    public void ResetRailPool()
    {
        DestroyRails();
        CreateRailPool(railsA.Count, LayerSet.railALayerSet);
        CreateRailPool(railsB.Count, LayerSet.railBLayerSet);
    }
    //---------
    public void ResetRailAPool()
    {
        DestroyRailsA();
        CreateRailPool(railsA.Count, LayerSet.railALayerSet);
    }
    //---------
    public void ResetRailBPool()
    {
        DestroyRailsB();
        CreateRailPool(railsB.Count, LayerSet.railBLayerSet);
    }
    //---------
    public void ResetSubpostPool()
    {
        DestroySubposts();
        CreateSubpostPool(subposts.Count);
    }
    //-----------------------
    public void ResetExtraPool()
    {
        DestroyExtras();
        if (useExtraGameObject)
            CreateExtraPool((int)(allPostsPositions.Count * 1.25f)); // use the same number as posts
    }
    //---------
    public void DestroyPosts()
    {
        //int d = 0;
        for (int i = 0; i < posts.Count; i++)
        {
            if (posts[i] != null)
            {
                DestroyImmediate(posts[i].gameObject);
                //d++;
            }
        }
        posts.Clear();
        //Debug.Log("DestroyPosts()  " + d + "\n"); // Uncomment to follow Reset() chain
    }
    //---------
    public void DestroyExtras()
    {
        for (int i = 0; i < extras.Count; i++)
        {
            if (extras[i] != null)
                DestroyImmediate(extras[i].gameObject);
        }
        extras.Clear();
    }
    //---------
    void DestroyMarkers()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null)
                DestroyImmediate(markers[i].gameObject);
        }
        markers.Clear();
    }
    //---------
    public void DestroyRails()
    {
        DestroyRailsA();
        DestroyRailsB();
    }
    //---------
    public void DestroyRailsA()
    {
        for (int i = 0; i < railsA.Count; i++)
        {
            if (railsA[i] != null)
                DestroyImmediate(railsA[i].gameObject);
        }
        railsA.Clear();
    }
    //---------
    public void DestroyRailsB()
    {
        for (int i = 0; i < railsB.Count; i++)
        {
            if (railsB[i] != null)
                DestroyImmediate(railsB[i].gameObject);
        }
        railsB.Clear();
    }
    //---------
    void DestroySubposts()
    {
        for (int i = 0; i < subposts.Count; i++)
        {
            if (subposts[i] != null)
                DestroyImmediate(subposts[i].gameObject);
        }
        for (int i = 0; i < subJoiners.Count; i++)
        {
            if (subJoiners[i] != null)
                DestroyImmediate(subJoiners[i].gameObject);
        }
        subposts.Clear();
        subJoiners.Clear();
    }
    //---------
    public void DestroyPools()
    {//Debug.Log("DestroyPools()");       
        DestroyPosts();
        DestroyMarkers();
        DestroyRails();
        DestroySubposts();
        DestroyExtras();
        //DestroySubj();
        //subJoinerPrefabs
    }
    //--------------------------
    //We created pools of rails/posts/extras for efficiency, and hid/set inactive the one we weren't using.
    //This destroys all those unused game objetcs.
    public void DestroyUnused()
    {
        for (int i = 0; i < posts.Count; i++)
        {
            if (posts[i].gameObject != null)
            {
                if (posts[i].gameObject.hideFlags == HideFlags.HideInHierarchy && posts[i].gameObject.activeSelf == false)
                    DestroyImmediate(posts[i].gameObject);
            }
        }
        for (int i = 0; i < railsA.Count; i++)
        {
            if (railsA[i].gameObject != null)
            {
                if (railsA[i].gameObject.hideFlags == HideFlags.HideInHierarchy && railsA[i].gameObject.activeSelf == false)
                    DestroyImmediate(railsA[i].gameObject);
            }
        }
        for (int i = 0; i < railsB.Count; i++)
        {
            if (railsB[i].gameObject != null)
            {
                if (railsB[i].gameObject.hideFlags == HideFlags.HideInHierarchy && railsB[i].gameObject.activeSelf == false)
                    DestroyImmediate(railsB[i].gameObject);
            }
        }
        for (int i = 0; i < subposts.Count; i++)
        {
            if (subposts[i].gameObject != null)
            {
                if (subposts[i].gameObject.hideFlags == HideFlags.HideInHierarchy && subposts[i].gameObject.activeSelf == false)
                {
                    DestroyImmediate(subposts[i].gameObject);
                    if (subJoiners[i].gameObject != null)
                        DestroyImmediate(subJoiners[i].gameObject);
                }
            }
        }
        for (int i = 0; i < extras.Count; i++)
        {
            if (extras[i].gameObject != null)
            {
                if (extras[i].gameObject.hideFlags == HideFlags.HideInHierarchy && extras[i].gameObject.activeSelf == false)
                    DestroyImmediate(extras[i].gameObject);
            }
        }

        DestroyMarkers();
    }
    //-------------
    public void CheckStatusOfAllClickPoints()
    {
        for (int i = 0; i < postCounter + 1; i++)
        {
            if (clickPoints.Contains(posts[i].position))
            {
                int index = clickPoints.IndexOf(posts[i].position);
                if (posts[i].gameObject.activeInHierarchy == false)
                {
                    DeletePost(index);
                }
            }
        }
    }
    //--------------
    public void DeactivateEntirePool(AutoFenceCreator.LayerSet layerSet = AutoFenceCreator.LayerSet.allLayerSet)
    {
        if (layerSet == kAllLayer || layerSet == kPostLayer)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                if (posts[i] != null)
                {
                    posts[i].gameObject.SetActive(false);
                    posts[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                    posts[i].position = Vector3.zero;
                }
            }
        }

        if (layerSet == kAllLayer || layerSet == kRailALayer)
        {
            for (int i = 0; i < railsA.Count; i++)
            {
                if (railsA[i] != null)
                {
                    railsA[i].gameObject.SetActive(false);
                    railsA[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }

            }
        }

        if (layerSet == kAllLayer || layerSet == kRailBLayer)
        {
            for (int i = 0; i < railsB.Count; i++)
            {
                if (railsB[i] != null)
                {
                    railsB[i].gameObject.SetActive(false);
                    railsB[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }

            }
        }

        if (layerSet == kAllLayer || layerSet == kSubpostLayer)
        {
            for (int i = 0; i < subposts.Count; i++)
            {
                if (subposts[i] != null)
                {
                    subposts[i].gameObject.SetActive(false);
                    subposts[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
            }
            for (int i = 0; i < subJoiners.Count; i++)
            {
                if (subJoiners[i] != null)
                {
                    subJoiners[i].gameObject.SetActive(false);
                    subJoiners[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
            }
        }
        // extra objects
        if (layerSet == kAllLayer || layerSet == kExtraLayer)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                if (extras[i] != null)
                {
                    extras[i].gameObject.SetActive(false);
                    extras[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                    extras[i].position = Vector3.zero;
                }
            }
        }
        // markers
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null)
            {
                markers[i].gameObject.SetActive(false);
                markers[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                markers[i].position = Vector3.zero;
            }
        }
        
    }
    //------------
    // we sometimes need to disable these when raycasting posts to the ground
    // but we need them back on when control-click-deleting them
    public void SetIgnoreClickMarkers(bool inIgnore)
    {
        int layer = 0; //default layer
        if (inIgnore)
            layer = 2;// 'Ignore Raycast' layer

        CheckClickMarkerPool();
        for (int i = 0; i < clickPoints.Count; i++)
        {
            if (markers[i] != null)
                markers[i].gameObject.layer = layer;
        }
    }

    //-----------
    //sets parts to be either on a regular layer, or on a special layer that ignores raycasts
    //useful to switch these on and off when we want to do a raycast, but IGNORE existing fence objects
    public void SetIgnoreColliders(bool inIgnore)
    {
        int layer = 0; //default layer
        if (inIgnore)
            layer = 2;// 'Ignore Raycast' layer
        for (int i = 0; i < posts.Count; i++)
        {
            if (posts[i] != null)
                posts[i].gameObject.layer = layer;
        }

        for (int i = 0; i < railsA.Count; i++)
        {
            if (railsA[i] != null)
                railsA[i].gameObject.layer = layer;
        }
        for (int i = 0; i < railsB.Count; i++)
        {
            if (railsB[i] != null)
                railsB[i].gameObject.layer = layer;
        }
        SetIgnoreClickMarkers(inIgnore);
    }
    //----------------
    // set on each rebuild
    public void SetMarkersActiveStatus(bool newState)
    {
        CheckClickMarkerPool();
        for (int i = 0; i < clickPoints.Count; i++)
        {
            if (markers[i] != null)
            {
                markers[i].GetComponent<Renderer>().enabled = newState;
                markers[i].gameObject.SetActive(newState);
                if (newState == true)
                    markers[i].hideFlags = HideFlags.None;
                else
                    markers[i].hideFlags = HideFlags.HideInHierarchy;
            }
        }
    }
    //------------------
    public void ManageLoop(bool loop)
    {
        if (loop)
            CloseLoop();
        else
            OpenLoop();
    }
    //------------------
    public void CloseLoop()
    {
        if (clickPoints.Count < 3)
        {// prevent user from closing if less than 3 points
            closeLoop = false;
        }
        if (clickPoints.Count >= 3 && clickPoints[clickPoints.Count - 1] != clickPoints[0])
        {
            clickPoints.Add(clickPoints[0]); // copy the first clickPoint
            clickPointFlags.Add(clickPointFlags[0]);
            ForceRebuildFromClickPoints();
            //?SceneView.RepaintAll();
        }
    }
    //------------------
    public void OpenLoop()
    {
        if (clickPoints.Count >= 3)
        {
            clickPoints.RemoveAt(clickPoints.Count - 1); // remove the last clickPoint (the closer)
            ForceRebuildFromClickPoints();
            //?SceneView.RepaintAll();
        }
    }
    //---------------
    public void DeletePost(int index, bool rebuild = true)
    {
        if (clickPoints.Count > 0 && index < clickPoints.Count)
        {
            lastDeletedIndex = index;
            lastDeletedPoint = clickPoints[index];
            clickPoints.RemoveAt(index); clickPointFlags.RemoveAt(index);
            handles.RemoveAt(index);
            ForceRebuildFromClickPoints();
        }
    }
    //---------------------
    public void InsertPost(Vector3 clickPosition)
    {
        // Find the nearest post and connecting lines to the click position
        float nearest = 1000000;
        int insertPosition = -1;
        for (int i = 0; i < clickPoints.Count - 1; i++)
        {
            float distToLine = CalcDistanceToLine(clickPoints[i], clickPoints[i + 1], clickPosition);
            if (distToLine < nearest)
            {
                nearest = distToLine;
                insertPosition = i + 1;
            }
        }
        if (insertPosition != -1)
        {
            clickPoints.Insert(insertPosition, clickPosition);
            clickPointFlags.Insert(insertPosition, clickPointFlags[0]);

            ForceRebuildFromClickPoints();
            //-- Update handles ----
            handles.Clear();
            for (int i = 0; i < clickPoints.Count; i++)
            {
                handles.Add(clickPoints[i]);
            }
        }
    }
    //-------------------
    public float GetAngleAtPost(int i, List<Vector3> posts)
    {
        if (i >= posts.Count - 1 || i <= 0) return 0;

        Vector3 vecA = posts[i] - posts[i - 1];
        Vector3 vecB = posts[i + 1] - posts[i];
        float angle = Vector3.Angle(vecA, vecB);
        return angle;
    }
    //------------------
    float CalcDistanceToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 pt)
    {
        Vector3 direction = lineEnd - lineStart;
        Vector3 startingPoint = lineStart;

        Ray ray = new Ray(startingPoint, direction);
        float distance = Vector3.Cross(ray.direction, pt - ray.origin).magnitude;

        if (((lineStart.x > pt.x && lineEnd.x > pt.x) || (lineStart.x < pt.x && lineEnd.x < pt.x)) && // it's before or after both x's
           ((lineStart.z > pt.z && lineEnd.z > pt.z) || (lineStart.z < pt.z && lineEnd.z < pt.z))) // it's before or after both z's
        {
            return float.MaxValue;
        }
        return distance;
    }
    //---------------------
    // Called from a loop of clicked array points [Rebuild()] or from a Click in OnSceneGui
    public void AddNextPostAndInters(Vector3 keyPoint, bool interpolateThisPost = true, bool doRebuild = true)
    {
        interPostPositions.Clear();
        float distance = Vector3.Distance(startPoint, keyPoint);
        //float distance = CalculateGroundDistance(startPoint, keyPoint);
        float interDist = interPostDist;

        if (interpolate && distance > interDist && interpolateThisPost)
        {
            int numSpans = (int)Mathf.Round(distance / interDist);
            float fraction = 1.0f / numSpans, min = 1, max = 1;
            float x, dx = (keyPoint.x - startPoint.x) * fraction;
            float y, dy = (keyPoint.y - startPoint.y) * fraction;
            float z, dz = (keyPoint.z - startPoint.z) * fraction;
            actualInterPostDistance = new Vector3(dx, dy, dz).magnitude;


            for (int i = 0; i < numSpans - 1; i++)
            {
                min = 1 -(postSpacingVariation * postSpacingVariationScalar); max = 1 + (postSpacingVariation* postSpacingVariationScalar);
                float r = UnityEngine.Random.Range(min, max);
                //r = 1;
                //Debug.Log(r);
                x = startPoint.x + (dx * r * (i + 1));
                y = startPoint.y + (dy * r * (i + 1));
                z = startPoint.z + (dz * r * (i + 1));

                /*x = startPoint.x + (dx * (i+1));
                y = startPoint.y + (dy * (i+1));
                z = startPoint.z + (dz * (i+1));*/
                Vector3 interPostPos = new Vector3(x, y, z);
                interPostPositions.Add(interPostPos);
            }
            if (keepInterpolatedPostsGrounded)
                Ground(interPostPositions);
            allPostsPositions.AddRange(interPostPositions);
        }
        //Create last post where user clicked
        allPostsPositions.Add(keyPoint); // make a copy so it's independent of the other being destroyed
        if (doRebuild)
            RebuildFromFinalList();
    }
    //---------------------
    // often we need to know the flat distance, ignoring any height difference
    float CalculateGroundDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        float distance = Vector3.Distance(a, b);

        return distance;
    }


    //------------
    //sometimes the post postion y is offset, so to test for a match only use x & z
    /*bool IsClickPointXZ()
    {
    }*/
    //------------
    void UpdateColliders()
    {

        if (useExtraGameObject)
        {
            MeshUtilitiesAFB.UpdateAllColliders(extraPrefabs[currentExtraType]);
        }

    }
    //------------
    //-- This MUST be called after RotatePostsFinal(), it's dependent on the posts' data --
    void BuildExtras(/*int n,  Transform postTrans*/)
    {
        if (useExtraGameObject == false)
            return;

        extrasTotalTriCount = 0;
        Transform postTrans = null;
        float midPointHeightDelta = 0;
        bool lastPost = false;
        for (int n = 0; n < postCounter; n++)
        {
            if (n == postCounter - 1)
                lastPost = true;
            
            midPointHeightDelta = 0;
            postTrans = posts[n];

            bool isClickPoint = false;
            if (postTrans.gameObject.name.EndsWith("click"))
            {
                isClickPoint = true;
            }
            if (UnityEngine.Random.value <= chanceOfMissingExtra)
                continue;
            if (extraFrequency == 0 && isClickPoint == false) // 0 = on main click points only
                continue;
            // 0 = main posts only, 1 = all posts, 2-20 = spaced out posts, 21 = interpolated posts only
            else if (extraFrequency != 0 && extraFrequency != 21 && n % extraFrequency != 0 && extraFrequency != -1 && n != postCounter - 1)
                continue;

            if (extraFrequency == 20 && n != 0 && n != postCounter - 1) // only keep first and last
                continue;
            if (extraFrequency == 21 && isClickPoint == true) // keep only the interpolated, not main
                continue;

            if (relativeMovement == true)
            {
                if (extraPositionOffset.z > 1.0f)
                    extraPositionOffset = new Vector3(extraPositionOffset.x, extraPositionOffset.y, 1.0f);
                if (extraPositionOffset.z < 0.0f)
                    extraPositionOffset = new Vector3(extraPositionOffset.x, extraPositionOffset.y, 0.0f);
            }

            float distanceToNextPost = 3, distanceToPrevPost = 3;
            Vector3 nextPostPos = Vector3.zero, prevPostPos = Vector3.zero, postPos = postTrans.position;
            postPos.y -= postHeightOffset; // make sure we're resing the natural grounded position

            //------  Find Non-useable cases and return  ---------
            if (extras.Count < n + 1 || extras[n] == null)
                continue;
            if (useExtraGameObject == false)
                continue;
            if (n == postCounter - 1 && relativeMovement == true && extraPositionOffset.z > 0.25f) // we don't need the last post if it's been pushed past the end
                continue;


            //----- Calculate values needed if the Extra is aligned with the rail (midwy between posts ---
            //float  distanceToNextPost = 0;
            if (n < postCounter - 1)
            {
                nextPostPos = posts[n + 1].position;
                distanceToNextPost = Vector3.Distance(postPos, nextPostPos);
                midPointHeightDelta = nextPostPos.y - postPos.y;
            }
            if (n > 0)
            {
                prevPostPos = posts[n - 1].position;
                distanceToPrevPost = Vector3.Distance(prevPostPos, postPos);
            }
            
            Vector3 directionVector = (nextPostPos - postPos).normalized;
            if(lastPost == true)
                directionVector = (postPos - prevPostPos).normalized;
            Vector3 forward = directionVector;
            Vector3 right = MeshUtilitiesAFB.RotatePointAroundPivot(directionVector, Vector3.up, new Vector3(0, 90, 0));

            //----- Setup the initial object --------
            if (extras.Count <= extraCounter)
                continue;
            GameObject thisExtra = extras[extraCounter++].gameObject;
            thisExtra.SetActive(true);
            thisExtra.hideFlags = HideFlags.None;
            thisExtra.layer = 8;


            //------  Scaling -----------
            thisExtra.transform.localScale = Vector3.Scale(nativeExtraScale, extraSize);
            //thisExtra.transform.localScale = Vector3.Scale(thisExtra.transform.localScale, extraSize);
            if (relativeScaling == true)
            {
                if (extraPositionOffset.z >= 0 && n < postCounter - 1)
                    thisExtra.transform.localScale = Vector3.Scale(thisExtra.transform.localScale, new Vector3(1, 1, (distanceToNextPost / 3))); // so that they scale proportionaly for different distances
                if (extraPositionOffset.z < 0 && n > 0)
                    thisExtra.transform.localScale = Vector3.Scale(thisExtra.transform.localScale, new Vector3(1, 1, (distanceToPrevPost / 3)));
            }
            //---- Set up Colliders ----
            if (extraColliderMode == 0)//single box
                MeshUtilitiesAFB.CreateCombinedBoxCollider(thisExtra, true);
            else if (extraColliderMode == 1)// all original
                MeshUtilitiesAFB.SetEnabledStatusAllColliders(thisExtra, true);
            else if (extraColliderMode == 2)//none
                MeshUtilitiesAFB.RemoveAllColliders(thisExtra);


            //------  Rotation -----------
            if (autoRotateExtra == true) // this should always be on except for single object placement
            {
                thisExtra.transform.rotation = postTrans.rotation;

                GameObject nonLerp = new GameObject(); // because Unity doesn't allow making independent Transforms;
                Vector3 eulerDirectionNext = Vector3.zero;
                nonLerp.transform.rotation = Quaternion.identity;
                if (n < postCounter - 1 && extraPositionOffset.z > 0)
                {
                    eulerDirectionNext = CalculateDirection(nextPostPos, postPos);
                    nonLerp.transform.Rotate(0, eulerDirectionNext.y, 0);
                    nonLerp.transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
                    thisExtra.transform.rotation = nonLerp.transform.rotation;
                }
                else if (n > 0 && extraPositionOffset.z < 0)
                {
                    eulerDirectionNext = CalculateDirection(postPos, prevPostPos);
                    nonLerp.transform.Rotate(0, eulerDirectionNext.y, 0);
                    nonLerp.transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
                    thisExtra.transform.rotation = nonLerp.transform.rotation;
                }
                else
                {
                    thisExtra.transform.rotation = postTrans.rotation;
                    nonLerp.transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
                }
                if (nonLerp != null)
                    DestroyImmediate(nonLerp);
            }
            else
                thisExtra.transform.rotation = Quaternion.identity;

            if (extrasFollowIncline == true && n < postCounter - 1)
            {
                float sectionInclineAngle = CalculateDirection(posts[n + 1].position, posts[n].position).x;
                thisExtra.transform.Rotate(sectionInclineAngle, 0, 0);
            }

            //------  Position -----------
            Vector3 offsetRight = Vector3.zero;
            Vector3 offsetForward = Vector3.zero;
            thisExtra.transform.position = postPos;
            float moveZ = 0;
            if (relativeMovement == false)
            {
                offsetRight = right * extraPositionOffset.z;
                offsetForward = forward * extraPositionOffset.x; 
            }

            else if (relativeMovement == true)
            {
                if (extraPositionOffset.z > 0)
                {
                    offsetRight = right * extraPositionOffset.z * distanceToNextPost;
                    offsetForward = forward * extraPositionOffset.x * distanceToNextPost; 
                    
                    midPointHeightDelta *= extraPositionOffset.z  * distanceToNextPost;
                }
                if (extraPositionOffset.z < 0)
                {
                    offsetRight = right * -extraPositionOffset.z * distanceToNextPost;
                    offsetForward = forward * -extraPositionOffset.x * distanceToNextPost; 
                    
                    midPointHeightDelta *= extraPositionOffset.z  * distanceToNextPost;
                }
            }
            // --- Calculate effect  Main Post Height Boost ------
            float postTopHeight = 0;
            if (raiseExtraByPostHeight == true)
            {
                postTopHeight = globalScale.y * postSize.y;
                if (isClickPoint)
                    postTopHeight *= mainPostSizeBoost.y;

                postTopHeight += postHeightOffset;
            }

            thisExtra.transform.Translate(offsetForward.x, extraPositionOffset.y + postTopHeight, offsetForward.z);
            thisExtra.transform.Translate(offsetRight.x, 0, offsetRight.z);

            if (relativeMovement == true)
                thisExtra.transform.Translate(0, midPointHeightDelta, 0);

            // -- Final Rotation -------- have to apply this after the trnslation, so that the forward direction is not confused
            thisExtra.transform.Rotate(extraRotation.x, extraRotation.y, extraRotation.z);

            thisExtra.isStatic = usingStaticBatching;

            //Put them in to the Posts folder for now... this will change soon
            //====== Put In Folders ==========
            int numExtrasFolders = (extraCounter / objectsPerFolder) + 1;
            string extrasGroupFolderName = "ExtrasGroupedFolder" + (numExtrasFolders - 1);
            GameObject extrasGroupedFolder = GameObject.Find("Current Fences Folder/Extras/" + extrasGroupFolderName);
            if (extrasGroupedFolder == null)
            {
                extrasGroupedFolder = new GameObject(extrasGroupFolderName);
                extrasGroupedFolder.transform.parent = extrasFolder.transform;
                if (addCombineScripts)
                {
                    CombineChildrenPlus combineChildren = extrasGroupedFolder.AddComponent<CombineChildrenPlus>();
                    if (combineChildren != null)
                        combineChildren.combineAtStart = true;
                }
            }
            thisExtra.transform.parent = extrasGroupedFolder.transform;

            extrasTotalTriCount += MeshUtilitiesAFB.CountAllTrianglesInGameObject(thisExtra);
            //=================  Deal with Arrays or Stacks =========================
            if (makeMultiArray)
            {
                int sizeY = (int)multiArraySize.y;
                GameObject cloneExtra = null;
                int z = 0;
                for (int y = 0; y < sizeY; y++)
                {
                    if (y == 0)
                        continue; //we don't clone the one in the root position
                    cloneExtra = extras[extraCounter++].gameObject;
                    cloneExtra.transform.position = thisExtra.transform.position;
                    cloneExtra.transform.rotation = thisExtra.transform.rotation;
                    cloneExtra.transform.localScale = thisExtra.transform.localScale;
                    cloneExtra.SetActive(true);
                    cloneExtra.hideFlags = HideFlags.None;
                    //cloneExtra.transform.Translate(x * multiArraySpacing.x,  y * multiArraySpacing.y, z * multiArraySpacing.z);
                    cloneExtra.transform.Translate(0, y * extrasGap, z);
                    /*if(keepArrayCentral){
                                Vector3 offset = Vector3.zero;
                                offset.x = -((sizeX-1) * multiArraySpacing.x);
                                offset.z = -((sizeZ-1) * multiArraySpacing.z);
                                cloneExtra.transform.Translate(offset);
                            }*/
                    cloneExtra.transform.parent = extrasGroupedFolder.transform;
                    extrasTotalTriCount += MeshUtilitiesAFB.CountAllTrianglesInGameObject(cloneExtra);
                }
            }
        }
    }
    //-------------------
    //- this is the real angle (0-360) as opposed to -180/0/+180 that the Unity methods give.
    float GetRealAngle(Transform postA, Transform postB)
    {
        Vector3 referenceForward = postA.forward;
        Vector3 newDirection = postB.position - postA.transform.position;
        float angle = Vector3.Angle(newDirection, referenceForward);
        float sign = (Vector3.Dot(newDirection, postA.right) > 0.0f) ? 1.0f : -1.0f;
        float finalAngle = sign * angle;
        if (finalAngle < 0) finalAngle = finalAngle + 360;
        return finalAngle;
    }
    //------------
    // This is only used when we are Finishing a fence with no posts. We need to save the click-points as posts so that the finished fence can be re-edited
    void CreateClickPointPostsForFinishedFence(int n, Vector3 postPoint)
    {
        bool isClickPoint = false;
        if (clickPoints.Contains(postPoint))
        {
            isClickPoint = true;
        }
       
        if (posts == null || posts.Count == 0 || posts[0] == null)
            Debug.LogWarning("Missing Post Instance in CreateClickPointPostsForFinishedFence()" );
        else if(isClickPoint == true)
        {
            GameObject markerPost = FindPrefabByName(FencePrefabType.postPrefab, "Marker_Post");
            GameObject thisPost = GameObject.Instantiate(markerPost);
            
            thisPost.SetActive(false);
            thisPost.hideFlags = HideFlags.None;
            // Name it if it is a click point, remove old name first
            bool nameContainsClick = thisPost.name.Contains("_click");
            if (nameContainsClick)
                thisPost.name = thisPost.name.Remove(thisPost.name.IndexOf("_click"), 6);
            
            if (isClickPoint == true /*&& thisPost.name.Contains("_click") == false*/)
                thisPost.name += "_click";
            //Set not to interfere with the picking of the control nodes which coincide with Posts. v2.3 removed after Finalize. Editable in Setting Window
            thisPost.layer = ignoreControlNodesLayerNum;


            //=========== Position ==============
            thisPost.transform.position = postPoint;
            thisPost.transform.position += new Vector3(0, postHeightOffset * globalScale.y, 0);


            thisPost.isStatic = false;

            if (usePosts == true)
                postsTotalTriCount += MeshUtilitiesAFB.CountAllTrianglesInGameObject(thisPost);


            //====== Organize into subfolders (pun not intended) so we don't hit the mesh combine limit of 65k ==========
            int numPostsFolders = (postCounter / objectsPerFolder) + 1;
            string postsDividedFolderName = "PostsGroupedFolder" + (numPostsFolders - 1);
            GameObject postsDividedFolder = GameObject.Find("Current Fences Folder/Posts/" + postsDividedFolderName);
            if (postsDividedFolder == null)
            {
                postsDividedFolder = new GameObject(postsDividedFolderName);
                postsDividedFolder.transform.parent = postsFolder.transform;
            }

            thisPost.transform.parent = postsDividedFolder.transform;
            CreatePostCollider(thisPost); // Just don't!
        }

    }
    //------------
    // Sets the post in the pool with all the correct attributes, and show a click-marker if they are enabled
    void SetupPost(int n, Vector3 postPoint)
    {
        /*for (int i = 0; i < posts.Count; i++)
        {
            Debug.Log(posts[i].transform.localScale.y + "\n");
        }*/

        Vector3 variantScaling = Vector3.one, variantOffset = Vector3.zero;
        bool isClickPoint = false;
        //postPoint.y += postHeightOffset;
        if (clickPoints.Contains(postPoint))
        {
            isClickPoint = true;
        }
        if (posts == null || posts.Count == 0 || posts[0] == null)
            Debug.LogWarning("Missing Post Instance in SetupPost()" );
        else
        {
            GameObject thisPost = posts[n].gameObject;
            bool isMainVariant = thisPost.name.Contains("v0]");
            thisPost.SetActive(true);
            thisPost.hideFlags = HideFlags.None;
            // Name it if it is a click point, remove old name first
            bool nameContainsClick = thisPost.name.Contains("_click");
            if (nameContainsClick)
                thisPost.name = thisPost.name.Remove(thisPost.name.IndexOf("_click"), 6);
            
            if (isClickPoint == true /*&& thisPost.name.Contains("_click") == false*/)
                thisPost.name += "_click";
            //Set not to interfere with the picking of the control nodes which coincide with Posts. v2.3 removed after Finalize. Editable in Setting Window
            thisPost.layer = ignoreControlNodesLayerNum;

            //================= Post Variations ================================
            if (usePostVariations && postSeqInfo.numSteps > 1)
            {
                SeqVariant currSeqVariant = new SeqVariant();
                int currSeqIndex = -1;
                currSeqIndex = n % postSeqInfo.numSteps;
                if (currSeqIndex != -1)
                    currSeqVariant = userSequencePost[currSeqIndex];
                variantScaling = currSeqVariant.size;
            }

            //=========== Position [ Rotation is handled in RotatePostsFinal() ] ==============
            thisPost.transform.position = postPoint;
            thisPost.transform.position += new Vector3(0, postHeightOffset * globalScale.y, 0);
            //=========== Scale ==============
            thisPost.transform.localScale = Vector3.Scale(nativePostScale, new Vector3(postSize.x * globalScale.x, postSize.y * globalScale.y, postSize.z * globalScale.z));
            //======= Variation Scale ========
            if (usePostVariations == true && variantScaling != Vector3.one)
                thisPost.transform.localScale = Vector3.Scale(thisPost.transform.localScale, variantScaling);
            //======= Main Boost Scale ========
            if (isClickPoint == true)
                thisPost.transform.localScale = Vector3.Scale(thisPost.transform.localScale, mainPostSizeBoost);

            //== Random Height Variation ==
            if (allowPostHeightVariation)
            {
                if (postRandomScope == 2 || (postRandomScope == 0 && isMainVariant) || (postRandomScope == 1 && isMainVariant == false))
                {
                    float randHeightScale = UnityEngine.Random.Range(minPostHeightVar, maxPostHeightVar);
                    thisPost.transform.localScale = Vector3.Scale(thisPost.transform.localScale, new Vector3(1, randHeightScale, 1));  
                }
            }

            if (postNames[currentPostType] == "_None_Post" || usePosts == false || (isClickPoint == false && hideInterpolated == true)) // don't show it if it's a none post, but it's still built as a reference for other objects
            {
                thisPost.SetActive(false);
                thisPost.hideFlags = HideFlags.HideInHierarchy;
            }

            thisPost.isStatic = usingStaticBatching;

            if (usePosts == true)
                postsTotalTriCount += MeshUtilitiesAFB.CountAllTrianglesInGameObject(thisPost);


            //====== Organize into subfolders (pun not intended) so we don't hit the mesh combine limit of 65k ==========
            int numPostsFolders = (postCounter / objectsPerFolder) + 1;
            string postsDividedFolderName = "PostsGroupedFolder" + (numPostsFolders - 1);
            GameObject postsDividedFolder = GameObject.Find("Current Fences Folder/Posts/" + postsDividedFolderName);
            if (postsDividedFolder == null)
            {
                postsDividedFolder = new GameObject(postsDividedFolderName);
                postsDividedFolder.transform.parent = postsFolder.transform;
                if (addCombineScripts)
                {
                    CombineChildrenPlus combineChildren = postsDividedFolder.AddComponent<CombineChildrenPlus>();
                    if (combineChildren != null)
                        combineChildren.combineAtStart = true;
                }
            }

            thisPost.transform.parent = postsDividedFolder.transform;
            CreatePostCollider(thisPost); // Just don't!
        }

        //====== Set Up Yellow Click Markers =======
        if (isClickPoint)
        {
            int clickIndex = clickPoints.IndexOf(postPoint);
            if (clickIndex != -1)
            {
                GameObject marker = markers[clickIndex].gameObject;
                marker.SetActive(true);
                //marker.hideFlags = HideFlags.None;
                marker.hideFlags = HideFlags.HideInHierarchy;
                Vector3 markerPos = postPoint;
                float h = (globalScale.y * postSize.y * mainPostSizeBoost.y * variantScaling.y) + postHeightOffset + variantOffset.y + globalLift;
                if (h < 1) h = 1;
                float markerHeightBoost = 1.1f;
                markerPos.y += (h * markerHeightBoost);
                /*if(postSize.y > 1)
                    markerPos.y += (globalScale.y * (postSize.y-1));
                if(mainPostSizeBoost.y > 1)
                    markerPos.y += (globalScale.y * mainPostSizeBoost.y);*/
                marker.transform.position = markerPos;
                marker.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                marker.name = "FenceManagerMarker_" + clickIndex.ToString();
            }
        }
    }
    //--------------------
    // this is done at the end because depending on the settings the post rotation/direction need updating
    void RotatePostsFinal()
    {
        //return;
        
        if (posts == null || posts.Count == 0 || posts[0] == null)
        {
            Debug.LogWarning("Missing Post Instance in RotatePostsFinal()");
            return;
        }
        bool disablePost = false;
        if (postCounter >= 2)
        {
            Vector3 A = Vector3.zero, B = Vector3.zero;
            Vector3 eulerDirection = Vector3.zero;
            Vector3 eulerDirectionNext = Vector3.zero;
            Vector3 variantRotation = Vector3.zero, variantOffset = Vector3.zero;

            if (posts[0] == null) return;
            //if(postNames[currentPostType] == "_None_Post")
            //return;
            // FIRST post is angled straight in the direction of the outgoing rail
            A = posts[0].transform.position;
            B = posts[1].transform.position;
            eulerDirection = CalculateDirection(A, B);
            posts[0].transform.rotation = Quaternion.identity;
            posts[0].transform.Rotate(0, eulerDirection.y + 180, 0);
            posts[0].transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
            //================= Add Random Rotations ===========================

            // main, but not first and last which are handled separately
            for (int i = 1; i < postCounter - 1; i++)
            {
                A = posts[i].transform.position;
                B = posts[i - 1].transform.position;
                if (A != B)
                    eulerDirection = CalculateDirection(A, B);

                bool isClickPoint = false;
                if (posts[i].name.EndsWith("click"))
                    isClickPoint = true;
                if ((isClickPoint == false && lerpPostRotationAtCornersInters == true) || (isClickPoint == true && lerpPostRotationAtCorners == true))
                { // interpolare the rotation bewteen the direction of incoming & outgoing rails (always do for interpolated)
                    posts[i].transform.rotation = Quaternion.identity;
                    posts[i].transform.Rotate(0, eulerDirection.y, 0);
                    if (i + 1 >= posts.Count)
                        continue;
                    float angle = GetRealAngle(posts[i].transform, posts[i + 1].transform);
                    posts[i].transform.Rotate(0, angle / 2 - 90, 0);
                    posts[i].transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
                }
                else
                {
                    posts[i].transform.rotation = Quaternion.identity;
                    A = posts[i + 1].transform.position;
                    B = posts[i].transform.position;
                    eulerDirectionNext = CalculateDirection(A, B);
                    posts[i].transform.Rotate(0, eulerDirectionNext.y, 0);
                    posts[i].transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
                }

                //================= Post Variations ================================
                variantRotation = Vector3.zero; 
                variantOffset = Vector3.zero;
                disablePost = false;
                if (usePostVariations && postSeqInfo.numSteps > 1)
                {
                    SeqVariant currSeqVariant = userSequencePost[0];//index doesn't matter for now
                    int currSeqIndex = -1;
                    currSeqIndex = i % postSeqInfo.numSteps;
                    if (currSeqIndex != -1)
                        currSeqVariant = userSequencePost[currSeqIndex];
                    variantRotation = currSeqVariant.rot;
                    variantOffset = currSeqVariant.pos;

                    posts[i].transform.Rotate(variantRotation);
                    if (currSeqVariant.stepEnabled == false)
                        disablePost = true;
                }

                //================= Add Random Rotations ===========================
                if (allowRandPostRotationVariation)
                {
                    float xRot = UnityEngine.Random.Range(-postRandRotationAmount.x, postRandRotationAmount.x);
                    float yRot = UnityEngine.Random.Range(-postRandRotationAmount.y, postRandRotationAmount.y);
                    float zRot = UnityEngine.Random.Range(-postRandRotationAmount.z, postRandRotationAmount.z);
                    posts[i].transform.Rotate(new Vector3(xRot, yRot, zRot));
                }
                //================= Add Random Quantized Rotations ===========================
                if (allowQuantizedRandomPostRotation)
                {
                    int num = UnityEngine.Random.Range(0, 24);
                    float totalRotAmount = num * postQuantizeRotAmount;
                    totalRotAmount = totalRotAmount % 360;
                    posts[i].transform.Rotate(new Vector3(0, totalRotAmount, 0));
                }

                
                //We have to do this last so that it doesn't confuse the rotations
                if (usePostVariations && postSeqInfo.numSteps > 1)
                {
                    posts[i].transform.position += variantOffset;
                }

                if(disablePost == true || UnityEngine.Random.value < chanceOfMissingPost)
                {
                    posts[i].gameObject.SetActive(false); // deleted when finalized
                    posts[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
            }

            // LAST post is angled straight in the direction of the incoming rail
            A = posts[postCounter - 1].transform.position;
            B = posts[postCounter - 2].transform.position;
            eulerDirection = CalculateDirection(A, B);
            posts[postCounter - 1].transform.rotation = Quaternion.identity;
            posts[postCounter - 1].transform.Rotate(0, eulerDirection.y, 0);
            posts[postCounter - 1].transform.Rotate(postRotation.x, postRotation.y, postRotation.z);
           
            //we have to handle the first and last separately
            if (usePostVariations && postSeqInfo.numSteps > 1)
            {
                //Last
                SeqVariant currSeqVariant = new SeqVariant();
                int currSeqIndex = -1;
                currSeqIndex = (postCounter - 1) % postSeqInfo.numSteps;
                if (currSeqIndex != -1)
                    currSeqVariant = userSequencePost[currSeqIndex];
                variantRotation = currSeqVariant.rot;
                posts[postCounter - 1].transform.Rotate(variantRotation);

                variantOffset = currSeqVariant.pos;
                posts[postCounter - 1].transform.position += variantOffset;

                if (allowRandPostRotationVariation)
                {
                    float xRot = UnityEngine.Random.Range(-postRandRotationAmount.x, postRandRotationAmount.x);
                    float yRot = UnityEngine.Random.Range(-postRandRotationAmount.y, postRandRotationAmount.y);
                    float zRot = UnityEngine.Random.Range(-postRandRotationAmount.z, postRandRotationAmount.z);
                    posts[postCounter - 1].transform.Rotate(new Vector3(xRot, yRot, zRot));
                }
                if (allowQuantizedRandomPostRotation)
                {
                    int num = UnityEngine.Random.Range(0, 24);
                    float totalRotAmount = num * postQuantizeRotAmount;
                    totalRotAmount = totalRotAmount % 360;
                    posts[postCounter - 1].transform.Rotate(new Vector3(0, totalRotAmount, 0));
                }
                if (currSeqVariant.stepEnabled == false)
                {
                    posts[postCounter - 1].gameObject.SetActive(false); // deleted when finalized
                    posts[postCounter - 1].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }


                //First
                currSeqIndex = 0;
                currSeqVariant = userSequencePost[currSeqIndex];
                variantRotation = currSeqVariant.rot;
                posts[0].transform.Rotate(variantRotation);

                variantOffset = currSeqVariant.pos;
                posts[0].transform.position += variantOffset;

                if (allowRandPostRotationVariation)
                {
                    float xRot = UnityEngine.Random.Range(-postRandRotationAmount.x, postRandRotationAmount.x);
                    float yRot = UnityEngine.Random.Range(-postRandRotationAmount.y, postRandRotationAmount.y);
                    float zRot = UnityEngine.Random.Range(-postRandRotationAmount.z, postRandRotationAmount.z);
                    posts[0].transform.Rotate(new Vector3(xRot, yRot, zRot));
                }
                if (allowQuantizedRandomPostRotation)
                {
                    int num = UnityEngine.Random.Range(0, 24);
                    float totalRotAmount = num * postQuantizeRotAmount;
                    totalRotAmount = totalRotAmount % 360;
                    posts[0].transform.Rotate(new Vector3(0, totalRotAmount, 0));
                }
                if (currSeqVariant.stepEnabled == false)
                {
                    posts[0].gameObject.SetActive(false); // deleted when finalized
                    posts[0].gameObject.hideFlags = HideFlags.HideInHierarchy;
                }

            }

        }
    }


    //-------------------
    // we have to do this recursively one at a time because removing one will alter the angles of the others
    void ThinByAngle(List<Vector3> posList)
    {
        if (removeIfLessThanAngle < 0.01f) return;

        float minAngle = 180;
        int minAngleIndex = -1;
        for (int i = 1; i < posList.Count - 1; i++)
        {
            Vector3 vecA = posList[i] - posList[i - 1];
            Vector3 vecB = posList[i + 1] - posList[i];
            float angle = Vector3.Angle(vecA, vecB);
            if (!clickPoints.Contains(posList[i]) && angle < minAngle)
            {
                minAngle = angle;
                minAngleIndex = i;
            }
        }
        if (minAngleIndex != -1 && minAngle < removeIfLessThanAngle) // we found one
        {
            posList.RemoveAt(minAngleIndex);
            ThinByAngle(posList);
        }
    }
    //-------------------
    // we have to do this recursively one at a time because removing one will alter the distances of the others
    void ThinByDistance(List<Vector3> posList)
    {
        float minDist = 10000;
        int minDistIndex = -1;
        float distToPre, distToNext, distToNextNext;
        for (int i = 1; i < posList.Count - 1; i++)
        {
            if (!IsClickPoint(posList[i]))
            {
                distToNext = Vector3.Distance(posList[i], posList[i + 1]);
                if (distToNext < stripTooClose)
                {
                    // close to neighbour, do we strip this one or the neighbour? Strip the one that has the other closest neighbour
                    // but only if it is not a clickpoint
                    if (!IsClickPoint(posList[i + 1]))
                    {
                        distToPre = Vector3.Distance(posList[i], posList[i - 1]);
                        distToNextNext = Vector3.Distance(posList[i + 1], posList[i + 2]);

                        if (distToPre < distToNextNext)
                        {
                            minDist = distToNext;
                            minDistIndex = i;
                        }
                        else
                        {
                            minDist = distToNext;
                            minDistIndex = i + 1;
                        }
                    }
                    else
                    {
                        minDist = distToNext;
                        minDistIndex = i;
                    }
                }
            }
        }
        if (minDistIndex != -1 && minDist < stripTooClose) // we found one
        {
            posList.RemoveAt(minDistIndex);
            ThinByDistance(posList);
        }
    }
    //-------------------
    int FindClickPointIndex(Vector3 pos)
    {
        return clickPoints.IndexOf(pos);
    }
    //-------------------
    bool IsClickPoint(Vector3 pos)
    {
        if (clickPoints.Contains(pos))
            return true;
        return false;
    }
    //-------------------
    void MakeSplineFromClickPoints()
    {
        // SplineFillMode {fixedNumPerSpan = 0, equiDistant, angleDependent};
        if (smooth == false || roundingDistance == 0 || clickPoints.Count < 3)
            return; //abort
        //-- Add 2 at each end before interpolating
        List<Vector3> splinedList = new List<Vector3>();
        Vector3 dirFirstTwo = (clickPoints[1] - clickPoints[0]).normalized;
        Vector3 dirLastTwo = (clickPoints[clickPoints.Count - 1] - clickPoints[clickPoints.Count - 2]).normalized;

        if (closeLoop)
        {
            splinedList.Add(clickPoints[clickPoints.Count - 3]);
            splinedList.Add(clickPoints[clickPoints.Count - 2]);
        }
        else
        {
            splinedList.Add(clickPoints[0] - (2 * dirFirstTwo));
            splinedList.Add(clickPoints[0] - (1 * dirFirstTwo));
        }

        splinedList.AddRange(clickPoints);
        if (closeLoop)
        {
            splinedList.Add(clickPoints[1]);
            splinedList.Add(clickPoints[2]);
        }
        else
        {
            splinedList.Add(clickPoints[clickPoints.Count - 1] + (2 * dirLastTwo));
            splinedList.Add(clickPoints[clickPoints.Count - 1] + (1 * dirLastTwo));
        }
        //int points = 51 - roundingDistance;
        splinedList = CreateCubicSpline3D(splinedList, roundingDistance, SplineFillMode.equiDistant, tension);
        ThinByAngle(splinedList);
        ThinByDistance(splinedList);
        //---------------------------
        keyPoints.Clear();
        keyPoints.AddRange(splinedList);

        Ground(keyPoints);
    }
    //--------------------
    // lower things to ground level
    public void Ground(List<Vector3> vec3List)
    {
        RaycastHit hit;
        Vector3 pos, highPos;
        float extraHeight = 4;//((globalScale.y * postSize.y)/2) + postHeightOffset;
        SetIgnoreColliders(true);

        for (int i = 0; i < vec3List.Count; i++)
        {
            highPos = pos = vec3List[i];
            highPos.y += extraHeight;
            if (Physics.Raycast(highPos, Vector3.down, out hit, 500)) // First check from above, looking down
            //if (Physics.Raycast(highPos, Vector3.down, out hit, 500, groundLayers)) // First check from above, looking down
            {
                if (hit.collider.gameObject != null)
                {
                    pos += new Vector3(0, -(hit.distance - extraHeight), 0);
                }
            }
            else if (Physics.Raycast(pos, Vector3.up, out hit, 500)) // maybe we've gone below... check upwards
            {
                if (hit.collider.gameObject != null)
                {
                    pos += new Vector3(0, +hit.distance, 0);
                }
            }
            vec3List[i] = pos;
        }
        SetIgnoreColliders(false);
    }
    //-------------------------
    //Ensure the Singles lists grow with the number of post positions
    void CheckSinglesLengths()
    {
        int shortage = 0;
        if (allPostsPositions.Count > railASingles.Count)
        {
            shortage = allPostsPositions.Count - railASingles.Count;
            railASingles.AddRange( GetNewSingles(shortage+10) );
        }
        if (allPostsPositions.Count > railBSingles.Count)
        {
            shortage = allPostsPositions.Count - railBSingles.Count;
            railBSingles.AddRange( GetNewSingles(shortage+10) );
        }
        if (allPostsPositions.Count > postSingles.Count)
        {
            shortage = allPostsPositions.Count - postSingles.Count;
            postSingles.AddRange( GetNewSingles(shortage+10) );
        }
    }
    //--------------------------------------------
    int[] GetNewSingles(int n)
    {
        int[] newSingles = new int[n];
        for (int i = 0; i < n; i++)
        {
            newSingles[i] = -1;
        }
        return newSingles;
    }
    //--------------------------------------------
    public Vector3 CalculateDirection(Vector3 A, Vector3 B)
    {

        //if(postCounter < 1) return Vector3.zero;
        if (B - A == Vector3.zero)
        {
            //Debug.Log("Same Position in CalculateDirection()");
            B.x += .00001f;
        }
        Quaternion q2 = Quaternion.LookRotation(B - A);
        Vector3 euler = q2.eulerAngles;
        return euler;
    }
    //----------------------------------
    List<Vector3> CreateCubicSpline3D(List<Vector3> inNodes, int numInters,
                                                     SplineFillMode fillMode = SplineFillMode.fixedNumPerSpan,
                                                     float tension = 0, float bias = 0, bool addInputNodesBackToList = true)
    {
        int numNodes = inNodes.Count;
        if (numNodes < 4) return inNodes;

        float mu, interpX, interpZ;
        int numOutNodes = (numNodes - 1) * numInters;
        List<Vector3> outNodes = new List<Vector3>(numOutNodes);

        int numNewPoints = numInters;
        for (int j = 2; j < numNodes - 3; j++) // don't build first  fake ones
        {
            outNodes.Add(inNodes[j]);
            Vector3 a, b, c, d;
            a = inNodes[j - 1];
            b = inNodes[j];
            c = inNodes[j + 1];
            if (j < numNodes - 2)
                d = inNodes[j + 2];
            else
                d = inNodes[numNodes - 1];

            if (fillMode == SplineFillMode.equiDistant) //equidistant posts, numInters now refers to the requested distance between the new points
            {
                float dist = Vector3.Distance(b, c);
                numNewPoints = (int)Mathf.Round(dist / numInters);
                if (numNewPoints < 1) numNewPoints = 1;
            }

            float t = tension;
            if (IsBreakPoint(inNodes[j]) || IsBreakPoint(inNodes[j + 2]))
            { // this will prevent falsely rounding in to gaps/breakPoints
                t = 1.0f;
            }

            for (int i = 0; i < numNewPoints; i++)
            {
                mu = (1.0f / (numNewPoints + 1.0f)) * (i + 1.0f);
                interpX = HermiteInterpolate(a.x, b.x, c.x, d.x, mu, t, bias);
                interpZ = HermiteInterpolate(a.z, b.z, c.z, d.z, mu, t, bias);
                outNodes.Add(new Vector3(interpX, b.y, interpZ));
            }
        }
        if (addInputNodesBackToList)
        {
            outNodes.Add(inNodes[numNodes - 3]);
        }
        return outNodes;
    }
    float HermiteInterpolate(float y0, float y1, float y2, float y3, float mu, float tension, float bias)
    {
        float mid0, mid1, mid2, mid3;
        float a0, a1, a2, a3;
        mid2 = mu * mu;
        mid3 = mid2 * mu;
        mid0 = (y1 - y0) * (1 + bias) * (1 - tension) / 2;
        mid0 += (y2 - y1) * (1 - bias) * (1 - tension) / 2;
        mid1 = (y2 - y1) * (1 + bias) * (1 - tension) / 2;
        mid1 += (y3 - y2) * (1 - bias) * (1 - tension) / 2;
        a0 = 2 * mid3 - 3 * mid2 + 1;
        a1 = mid3 - 2 * mid2 + mu;
        a2 = mid3 - mid2;
        a3 = -2 * mid3 + 3 * mid2;
        return (a0 * y1 + a1 * mid0 + a2 * mid1 + a3 * y2);
    }
    //---------------------------
    /*public int FindAnyPrefabByName(string prefabName, bool warnMissing = true){
        int prefab = FindPrefabIndexByName(FencePrefabType.postPrefab, prefabName, warnMissing);
    }*/
    //---------------------------
    public int FindRailPrefabIndexByName(string prefabName, bool warnMissing = true)
    {
        return FindPrefabIndexByName(FencePrefabType.railPrefab, prefabName, warnMissing);
    }
    //---------------------------
    public int FindPostPrefabIndexByName(string prefabName, bool warnMissing = true)
    {
        return FindPrefabIndexByName(FencePrefabType.postPrefab, prefabName, warnMissing);
    }
    //---------------------------
    public int FindExtraPrefabIndexByName(string prefabName, bool warnMissing = true)
    {
        return FindPrefabIndexByName(FencePrefabType.extraPrefab, prefabName, warnMissing);
    }
    //---------------------------
    public int FindPrefabIndexByName(FencePrefabType prefabType, string prefabName, bool warnMissing = true)
    {
        if (prefabType == FencePrefabType.railPrefab)
        { 
            for (int i = 0; i < railPrefabs.Count; i++)
            {
                if (railPrefabs[i] == null)
                    continue;
                string name = railPrefabs[i].name;
                if (name == prefabName)
                    return i;
            }
        }
        if (prefabType == FencePrefabType.postPrefab)
        {
            for (int i = 0; i < postPrefabs.Count; i++)
            {
                if (postPrefabs[i] == null)
                    continue;
                string name = postPrefabs[i].name;
                if (name == prefabName)
                    return i;
            }
        }
        if (prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < extraPrefabs.Count; i++)
            {
                if (extraPrefabs[i] == null)
                    continue;
                string name = extraPrefabs[i].name;
                if (name == prefabName)
                    return i;
            }
        }

        if (warnMissing && prefabName != "-")
        {
           //print("Couldn't find prefab with name: " + prefabName + ".Is it a User Object that's been deleted?  (" + prefabType + ")\n");
        }
        return -1;
    }
   //---------------------------
    public GameObject FindPrefabByName(FencePrefabType prefabType, string prefabName, bool warnMissing = true)
    {
        if (prefabType == FencePrefabType.railPrefab || prefabType == FencePrefabType.extraPrefab)
        { 
            for (int i = 0; i < railPrefabs.Count; i++)
            {
                if (railPrefabs[i] == null)
                    continue;
                string name = railPrefabs[i].name;
                if (name == prefabName)
                    return railPrefabs[i];
            }
        }
        if (prefabType == FencePrefabType.postPrefab || prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < postPrefabs.Count; i++)
            {
                if (postPrefabs[i] == null)
                    continue;
                string name = postPrefabs[i].name;
                if (name == prefabName)
                    return postPrefabs[i];
            }
        }
        if (prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < extraPrefabs.Count; i++)
            {
                if (extraPrefabs[i] == null)
                    continue;
                string name = extraPrefabs[i].name;
                if (name == prefabName)
                    return extraPrefabs[i];
            }
        }

        if (warnMissing && prefabName != "-")
        {
           print("Couldn't find prefab with name: " + prefabName + ".Is it a User Object that's been deleted?  (" + prefabType + ")\n");
        }
        return null;
    }
    //---------------------------
    public int FindPrefabIndexInNamesList(FencePrefabType prefabType, string prefabName, bool warnMissing = true)
    {
        if (prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < extraNames.Count; i++)
            {
                if (extraNames[i] == null)
                    continue;
                string name = extraNames[i];
                if (name.Contains(prefabName))
                    return i;
            }
        }
        
        if (prefabType == FencePrefabType.railPrefab || prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < railNames.Count; i++)
            {
                if (railNames[i] == null)
                    continue;
                string name = railNames[i];
                if (name.Contains(prefabName))
                    return i;
            }
        }
        if (prefabType == FencePrefabType.postPrefab || prefabType == FencePrefabType.extraPrefab)
        {
            for (int i = 0; i < postNames.Count; i++)
            {
                if (postNames[i] == null)
                    continue;
                string name = postNames[i];
                if (name.Contains(prefabName))
                    return i;
            }
        }
        

        if (warnMissing && prefabName != "-")
        {
            print("Couldn't find prefab with name: " + prefabName + ".Is it a User Object that's been deleted?  (" + prefabType + ")\n");
        }
        return -1;
    }
    //--------------------------
    /*public int FindPrefabIndexInNamesList(FencePrefabType prefabType, string prefabName, bool warnMissing = true)
    {
        List<string> namesList = postNames;
        if (prefabType == FencePrefabType.railPrefab)
            namesList = railNames;
        if (prefabType == FencePrefabType.extraPrefab)
            namesList = extraNames;
        //Its a Post or Railor Extra prefab, look for it in their lists
        if (prefabType != FencePrefabType.anyPrefab)
        {
            for (int i = 0; i < namesList.Count; i++)
            {
                if (namesList[i] == null)
                    continue;
                string name = namesList[i];
                if (name.Contains(prefabName))
                    return i;
            }
        }
        //if(warnMissing)
        // re-enable print ("Couldn't find prefab with name: " + prefabName + ".Is it a User Object that's been deleted? A default prefab will be used instead.\n");
        return 0;
    }*/
    //---------------------------
    public string GetPartialTimeString(bool includeDate = false)
    {
        DateTime currentDate = System.DateTime.Now;
        string timeString = currentDate.ToString();
        timeString = timeString.Replace("/", "-"); // because the / in that will upset the path
        timeString = timeString.Replace(":", "-"); // because the / in that will upset the path
        if (timeString.EndsWith(" AM") || timeString.EndsWith(" PM"))
        { // windows??
            timeString = timeString.Substring(0, timeString.Length - 3);
        }
        if (includeDate == false)
            timeString = timeString.Substring(timeString.Length - 8);
        return timeString;
    }
    //---------------------------
    // custom, beacuse by default .ToString() only writes 1 decimal place, we want 3
    string VectorToString(Vector3 vec)
    {
        string vecString = "(";
        vecString += vec.x.ToString("F3") + ", ";
        vecString += vec.y.ToString("F3") + ", ";
        vecString += vec.z.ToString("F3") + ")";
        return vecString;
    }

    //---------------------------
    public void Randomize()
    {
        usePosts = true;
        currentPostType = (int)(UnityEngine.Random.value * postPrefabs.Count);
        currentRailAType = (int)(UnityEngine.Random.value * railPrefabs.Count);
        currentRailBType = (int)(UnityEngine.Random.value * railPrefabs.Count);
        string railName = railPrefabs[currentRailAType].name;
        currentSubpostType = (int)(UnityEngine.Random.value * subPrefabs.Count);

        // Extras
        currentExtraType = (int)(UnityEngine.Random.value * extraPrefabs.Count);
        if (Mathf.Round(UnityEngine.Random.value) > 0.5f)
        {
            useExtraGameObject = true;
            extraPositionOffset = Vector3.zero;
            extraSize = Vector3.one * UnityEngine.Random.Range(0.5f, 2.0f);
            if (Mathf.Round(UnityEngine.Random.value) > 0.5f)
            {
                relativeMovement = true;
                relativeScaling = true;
                extraPositionOffset.x = 0.5f;
            }
        }
        else
            useExtraGameObject = false;

        // rail B
        if (Mathf.Round(UnityEngine.Random.value) > 0.5f)
        {
            useRailsB = true;
            railBPositionOffset = Vector3.zero;
            railBPositionOffset.y = 0.2f;
            railBSize = Vector3.one * UnityEngine.Random.Range(0.5f, 2.0f);

        }
        else
            useRailsB = false;

        globalScale.y = (UnityEngine.Random.Range(0.5f, 3.5f) + UnityEngine.Random.Range(0.5f, 4.0f) + UnityEngine.Random.Range(0.5f, 4.0f) + UnityEngine.Random.Range(0.5f, 4.0f)) / 4;

        railASize.y = 1;
        if (railName.EndsWith("_Panel_Rail"))
        {
            numStackedRailsA = 1;
            railASize.y = globalScale.y * 0.85f;
        }
        else
            numStackedRailsA = ((int)(((UnityEngine.Random.value * 4) + (UnityEngine.Random.value * 4) + (UnityEngine.Random.value * 4)) / 3)) + 1; //psuedo-central distribution 

        railASpread = UnityEngine.Random.value * 0.6f + 0.2f;
        railBSpread = UnityEngine.Random.value * 0.5f + 0.1f;
        //------ Centralize ---------
        float gap = globalScale.y;
        if (numStackedRailsA > 1)
            gap = globalScale.y / (numStackedRailsA - 1);
        gap *= railASpread;
        float maxY = (1 - railASpread) * 0.9f;
        railAPositionOffset.y = UnityEngine.Random.Range(0.1f, maxY);
        if (railName.EndsWith("_Panel_Rail"))
            railAPositionOffset.y = 0.51f;
        //-----------------------------------------
        useSubposts = false;
        if (Mathf.Round(UnityEngine.Random.value) > 0.5f)
        {
            useSubposts = true;
            subSpacing = UnityEngine.Random.Range(0.4f, 5);
            subpostSize.y = UnityEngine.Random.Range(0.5f, 1);
        }
        SetPostType(currentPostType, false);
        SetRailAType(currentRailAType, false);
        SetRailBType(currentRailBType, false);
        SetSubType(currentSubpostType, false);
        SetExtraType(currentExtraType, false);
        ForceRebuildFromClickPoints();
    }
    //-------------------------------
    public void SetPostType(int type, bool doRebuild)
    {
        currentPostType = type;
        if (postPrefabs[currentPostType].name.StartsWith("[User]"))
            useCustomPost = true;
        else
            useCustomPost = false;
        DeactivateEntirePool(kPostLayer);
        ResetPostPool();

        if (doRebuild)
            ForceRebuildFromClickPoints(kPostLayer);
    }
    //-------------------------------
    public void SetExtraType(int type, bool doRebuild)
    {
        currentExtraType = type;
        if (extraPrefabs[currentExtraType].name.StartsWith("[User]"))
            useCustomExtra = true;
        else
            useCustomExtra = false;
        DeactivateEntirePool(kExtraLayer);
        ResetExtraPool();

        if (doRebuild)
            ForceRebuildFromClickPoints(kExtraLayer);
    }
    //--------------------------------------------
    public void SetRailAType(int railType, bool doRebuild, bool resetRailPool = true)
    {
        if (railType == -1)
        {
            Debug.Log("Couldn't find this Rail prefab. Is it a custom one that has been deleted?");
            railType = 0;
        }
        currentRailAType = railType;
        if (railPrefabs[currentRailAType].name.StartsWith("[User]"))
            useCustomRailA = true;
        else
            useCustomRailA = false;

        if (railPrefabs[currentRailAType].name.EndsWith("Panel_Rail") == true)
        { // always change to 'shear' for panel fences
            slopeModeRailA = FenceSlopeMode.shear;
        }
        if (resetRailPool)
        {
            DeactivateEntirePool(kRailALayer);
            ResetRailAPool();
        }
        if (doRebuild)
            ForceRebuildFromClickPoints(kRailALayer);
    }
    //--------------------------------------------
    public void SetRailBType(int railType, bool doRebuild, bool resetRailPool = true)
    {
        if (railType == -1)
        {
            Debug.Log("Couldn't find this Rail prefab. Is it a custom one that has been deleted?");
            railType = 0;
        }
        currentRailBType = railType;
        if (railPrefabs[currentRailBType].name.StartsWith("[User]"))
            useCustomRailB = true;
        else
            useCustomRailB= false;

        if (railPrefabs[currentRailBType].name.EndsWith("Panel_Rail") == true)
        { // always change to 'shear' for panel fences
            slopeModeRailB = FenceSlopeMode.shear;
        }
        if (resetRailPool)
        {
            DeactivateEntirePool(kRailBLayer);
            ResetRailBPool();
        }
        if (doRebuild)
            ForceRebuildFromClickPoints(kRailBLayer);
    }
    //-----------------
    public void SetSubType(int type, bool doRebuild)
    {
        currentSubpostType = type;
        DeactivateEntirePool(kSubpostLayer);
        ResetSubpostPool();
        if (doRebuild)
            ForceRebuildFromClickPoints(kSubpostLayer);
    }
    //------------------
    // Make Copies of ALL possible mesh data in case any of it gets mangled
    // These are saved in Lists of GO Lists, as each GO could have children
    public void BackupPrefabMeshes(List<GameObject> sourcePrefabs, List<List<Mesh>> destinationMeshes)
    {
        destinationMeshes.Clear();
        List<Mesh> submeshList;
        for (int i = 0; i < sourcePrefabs.Count(); i++)
        {
            if (sourcePrefabs[i] == null)
            { // if the user has deleted a prefab.
                //sourcePrefabs[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Debug.LogWarning("A prefab was missing.Index: " + i);
                continue;
            }
            submeshList = MeshUtilitiesAFB.GetAllMeshesFromGameObject(sourcePrefabs[i]);
            if (submeshList.Count == 0)
                Debug.Log("Couldn't find mesh in GetRailMeshesFromPrefabs()");

            destinationMeshes.Add(submeshList);
        }
    }
    //-------------------
    void SaveCustomRailMeshAndAddToPrefabList(GameObject customRail)
    {
        if (railPrefabs.Count == 0)
            return;
        railPrefabs.Insert(0, customRail);
    }

    //---------------------------
    public void ZeroAllRandom(bool rebuild = true)
    {
        randomPostHeight = 0;
        randomRoll = 0;
        randomYaw = 0;
        randomPitch = 0;
        chanceOfMissingRailA = 0;
        chanceOfMissingRailB = 0;
        chanceOfMissingSubs = 0;

        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public void SeedRandom(bool rebuild = true)
    {
        randomSeed = (int)System.DateTime.Now.Ticks;
        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public int GetNewRandomSeed()
    {
        int newSeed = (int)System.DateTime.Now.Ticks;
        return newSeed;
    }
    //-----------------------------
    float GetMeshHeight(GameObject go)
    {
        float height = 0;
        Mesh mesh = null;
        MeshFilter mf = (MeshFilter)go.GetComponent<MeshFilter>();
        if (mf != null)
            mesh = mf.sharedMesh;
        if (mesh != null)
            height = mesh.bounds.size.y;
        return height;
    }
    //-----------------------------
    Vector3 GetMeshMin(GameObject go)
    {
        Bounds bounds = MeshUtilitiesAFB.GetCombinedBoundsOfAllMeshesInGameObject(go);
        return bounds.min;
    }
    //---------------------------
    // Set the bottom rail/wall to be flush with ground
    public void GroundRails(LayerSet railSet, bool rebuild = true)
    {

        GameObject rail = null;
        float userHeightScale = 0;
        if (railSet == LayerSet.railALayerSet)
        {
            rail = railPrefabs[currentRailAType];
            userHeightScale = railASize.y;
        }
        else if (railSet == LayerSet.railBLayerSet)
        {
            rail = railPrefabs[currentRailBType];
            userHeightScale = railBSize.y;
        }

        //TODO
        float meshHeight = GetMeshHeight(rail);
        float finalRailHeight = meshHeight * userHeightScale * globalScale.y;
        float bottom = -finalRailHeight / 2;

        if (railSet == LayerSet.railALayerSet)
            railAPositionOffset = new Vector3(railAPositionOffset.x, 0, railAPositionOffset.z);
        else if (railSet == LayerSet.railBLayerSet)
            railBPositionOffset = new Vector3(railBPositionOffset.x, 0, railBPositionOffset.z);

        ForceRebuildFromClickPoints();
    }
    //---------------------------
    // Set the bottom rail/wall to be flush with ground
    public void CentralizeRails(LayerSet railSet, bool rebuild = true)
    {

        GameObject rail = null;
        int numRails = 1;
        if (railSet == LayerSet.railALayerSet)
        {
            rail = railPrefabs[currentRailAType];
            numRails = numStackedRailsA;
        }
        else if (railSet == LayerSet.railBLayerSet)
        {
            rail = railPrefabs[currentRailBType];
            numRails = numStackedRailsB;
        }


        float startHeight = 0, totalHeight = gs * postSize.y;
        //float centreheight = totalHeight/2.0f;
        float singleGapSize = totalHeight / ((numRails - 1) + 2); // +2 because we have a gap at top and bottom

        if (numStackedRailsA > 1)
        {
            railASpread = singleGapSize * (numStackedRailsA - 1);
            //startHeight = railASpread/(numStackedRailsA+1);
            startHeight = (totalHeight / 2) - (railASpread / 2);
        }
        else
        {
            railASpread = 0.5f;
            startHeight = totalHeight / 2;
        }

        railAPositionOffset = new Vector3(railAPositionOffset.x, startHeight, railAPositionOffset.z);
        ForceRebuildFromClickPoints();
    }

    //---------------------------
    public void ResetRailATransforms(bool rebuild = true)
    {

        numStackedRailsA = 1;
        railASpread = 0.5f;
        railAPositionOffset = new Vector3(0, 0.25f, 0);
        railASize = Vector3.one;
        railARotation = Vector3.zero;
        overlapAtCorners = true;
        autoHideBuriedRails = false;
        slopeModeRailA = FenceSlopeMode.shear;
        GroundRails(LayerSet.railALayerSet);
        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public void ResetRailBTransforms(bool rebuild = true)
    {

        numStackedRailsB = 1;
        railBSpread = 0.5f;
        railBPositionOffset = new Vector3(0, 0.25f, 0);
        railBSize = Vector3.one;
        railBRotation = Vector3.zero;
        overlapAtCorners = true;
        autoHideBuriedRails = false;
        slopeModeRailA = FenceSlopeMode.shear;
        GroundRails(LayerSet.railBLayerSet);
        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public void ResetPostTransforms(bool rebuild = true)
    {
        postHeightOffset = 0;
        postSize = Vector3.one;
        mainPostSizeBoost = Vector3.one;
        postRotation = Vector3.zero;

        usePosts = true;
        //if (currentPostType == 0)
            //currentPostType = FindPrefabIndexByName(FencePrefabType.postPrefab, "BrickSquare_Post");

        hideInterpolated = false;

        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public void ResetSubpostTransforms(bool rebuild = true)
    {

        postHeightOffset = 0;

        subpostPositionOffset = Vector3.zero;
        subpostSize = Vector3.one;
        subpostRotation = Vector3.zero;

        useSubposts = true;
        //if (currentSubpostType == 0)
           // currentSubpostType = FindPrefabIndexByName(FencePrefabType.postPrefab, "BrickSquare_Post");

        subSpacing = 3.0f;

        useWave = false;

        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }
    //---------------------------
    public void ResetExtraTransforms(bool rebuild = true)
    {

        extraPositionOffset = new Vector3(0, 0, 0);
        extraSize = Vector3.one;
        //mainPostSizeBoost = Vector3.one;
        extraRotation = Vector3.zero;

        autoRotateExtra = true;
        relativeMovement = relativeScaling = false;
        extrasFollowIncline = false;
        raiseExtraByPostHeight = true;

        makeMultiArray = false;
        numExtras = 2;
        extrasGap = 0.75f;

        if (rebuild == true)
            ForceRebuildFromClickPoints();
    }

    //---------------------------
    public int GetSlopeModeAsInt(AutoFenceCreator.FenceSlopeMode mode)
    {
        if (mode == AutoFenceCreator.FenceSlopeMode.slope) return 0;
        if (mode == AutoFenceCreator.FenceSlopeMode.step) return 1;
        if (mode == AutoFenceCreator.FenceSlopeMode.shear) return 2;
        return 2;
    }
    
    public string GetPresetCategoryByName(string name)
    {
        //name = inName.Trim();

        string category = GetPresetCategoryFromName(name);
        if (category != "")
            return category;

        if (name.StartsWith("[User]"))
            category = "User";

        if (name.EndsWith("_Extra"))
            category = "Extra";

        else if (name.StartsWith("_Template") || name.StartsWith("Template"))
            category = " Basic Templates";
        else if (name.StartsWith("Demo"))
            category = " Demo Usage";
        else if (name.Contains("Concrete") && name.Contains("Wood"))
            category = " Concrete & Wood";

        else if (name.Contains("Brick") || name.Contains("CinderBlock") || name.Contains("Cinderblock"))
            category = " Brick";
        else if (name.Contains("Concrete"))
            category = " Concrete";
        else if (name.Contains("Metal") || name.Contains("Steel") || name.Contains("Rusty") || name.Contains("Girder")
                 || name.Contains("Chrome") || name.Contains("Iron") || name.Contains("Aluminium") || name.Contains("Rebar")
            || name.Contains("Gold") || name.Contains("Silver"))
            category = " Metal";
        else if (name.Contains("Railings"))
            category = " Railings";
        else if (name.Contains("Stone") || name.Contains("Castle"))
            category = " Stone";
        else if (name.Contains("Wood") || name.Contains("Fortress"))
            category = " Wood";
        else if (name.Contains("Test"))
            category = " Test";
        else if (name.Contains("Goose"))
            category = " Goose";
        else if (name.Contains("Wire"))
            category = " Wire";
        else if (name.Contains("SciFi"))
            category = " SciFi";
        else if (name.Contains("Industrial"))
            category = " Industrial";
        else if (name.Contains("Military") || name.Contains("Sandbag") || name.Contains("Cheval De Frise") || name.Contains("CzechHedgehog") )
            category = " Military";
        else if (name.Contains("Urban"))
            category = " Urban";
        else if (name.Contains("Residential"))
            category = " Residential";
        else
            category = " Other";

        return category;

    }

    public string GetPresetCategoryFromName(string presetName)
    {

        string category = "";

        if (presetName.Contains("/"))
        {
            int index = presetName.IndexOf("/");
            category = presetName.Substring(0, index);
        }
        return category;
    }
    public string GetPresetNameWithoutCategory(string presetName)
    {
        string name = presetName;

        if (name.Contains("/"))
        {
            int index = name.IndexOf("/");
            name = presetName.Substring(index + 1, (presetName.Length) - (index + 1));
        }
        return name;
    }

}

