using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Physics/Trajectory Predictor")]
public class TrajectoryPredictor : MonoBehaviour {

    public enum lineMode {
        DrawRayEditorOnly = 1,
        LineRendererBoth = 2
    };
    public enum predictionMode {
        Prediction2D = 2,
        Prediction3D = 3
    };

    [Header("Prediction Settings")]
    [Range(0.7f, 0.9999f)]
    [Tooltip("The accuracy of the prediction. This controls the distance between steps in calculation.")]
    public float accuracy = 0.98f;
    [Tooltip("Limit on how many steps the prediction can take before stopping.")]
    public int iterationLimit = 150;
    [Tooltip("Whether the prediction should be a 2D or 3D line.")]
    public predictionMode predictionType = predictionMode.Prediction3D;
    [Tooltip("The layer mask to use for raycasting when calculating the prediction. This setting only matters when checkForCollision is on.")]
    public LayerMask raycastMask = -1;
    [Tooltip("Stop the prediction where the line hits an object? This check works by using raycasts, so you can use the mask and putting objects on different layers to make them not be checked for collision.")]
    public bool checkForCollision = true;


    // bounceOnCollision is commented out because the physically interaction between two objects in unity is somewhat too difficult to simulate 
    // properly in a simple fashion. The current implementation is somewhat inaccurate because it does not take into account things like 
    // static and dynamic friction of the two bodies and combine factors of physics materials among other factors.

    //[Tooltip("If checkForCollision is set to true this will perform a bounce at the impact location using the objects physics material.")]
    //public bool bounceOnCollision = true
    private static bool bounceOnCollision = false;


    [Header("Line Settings")]
    [Tooltip("The type of line to draw for debug stuff. DrawRay: uses built in Debug.DrawRay only visible in editor." +
        " LineRenderer: uses a line renderer on a separate created GameObject to draw the line, is visble in editor and play mode")]
    public lineMode debugLineMode = lineMode.LineRendererBoth;
    [Tooltip("Draw a debug line on object start? (Requires a rigidbody or rigidbody2D)")]
    public bool drawDebugOnStart = false;
    [Tooltip("Draw a debug line on object update? (Requires a rigidbody or rigidbody2D)")]
    public bool drawDebugOnUpdate = false;
    [Tooltip("Draw a debug line when predicting the trajectory")]
    public bool drawDebugOnPrediction = false;
    [Tooltip("Duration the prediction line lasts for. When predicting every frame its a good idea to update this value to Time.unscaledDeltaTime every frame."
        + "(This is done automatically if you use the drawDebugOnUpdate option)")]
    public float debugLineDuration = 4f;
    [Tooltip("Number of frames that pass before the line is refreshed. Increasing this number could significantly improve performance with a large amount of lines being predicted at once."
        + "(Only used if drawDebugOnUpdate is enabled.)")]
    [Range(1, 10)]
    public int debugLineUpdateRate = 1;
    [Tooltip("If using the linerenderer, will reuse the gameobject and line renderer components instead of destroying and recreating them every time. " +
        "This option improves performance for multiple succesive predictions DON'T use this for one-off predictions, " +
        "as it will not take line duration into account and the line will stick around forever until the component is destroyed. " +
        "NOTE: this option is automatically used by drawDebugOnUpdate and does not need to be enabled here for that to work.")]
    public bool reuseLine = false;
    [Tooltip("The name of the layer the line is drawn on. Only checked once on start")]
    public string lineSortingLayerName;
    private int lineSortingLayer = 0;

    [Tooltip("The order in the sorting layer the line is drawn.")]
    public int lineSortingOrder = 0;

    [Header("Line Appearance")]
    [Tooltip("Thickness of the debug line when using the line renderer mode.")]
    public float lineWidth = 0.05f;
    [Tooltip("Start color of the debug line")]
    public Color lineStartColor = Color.white;
    [Tooltip("End color of the debug line")]
    public Color lineEndColor = Color.white;
    [Tooltip("If provided, this shader will be used on the LineRenderer. (Recommended is the particles section of shaders)")]
    public Shader lineShader;
    [Tooltip("If provided, this texture will be added to the material of the LineRenderer. (A couple textures come packaged with the script)")]
    public Texture lineTexture;
    [Tooltip("Value to scale the tiling of the line texture by.")]
    public float textureTilingMult = 1f;

    [HideInInspector]
    public RaycastHit hitInfo3D;
    [HideInInspector]
    public RaycastHit2D hitInfo2D;
    [HideInInspector]
    public List<Vector3> predictionPoints = new List<Vector3>();

    private Rigidbody rb;
    private Rigidbody2D rb2;

    void Awake() {
        if (predictionType == predictionMode.Prediction2D) {
            rb2 = GetComponent<Rigidbody2D>();
        } else {
            rb = GetComponent<Rigidbody>();
        }

        lineSortingLayer = SortingLayer.NameToID(lineSortingLayerName);
    }

    private void Start() {
        if (drawDebugOnStart) {
            if (rb || rb2) {
                bool wasB = drawDebugOnPrediction;
                drawDebugOnPrediction = true;
                if (predictionType == predictionMode.Prediction2D) {
                    Predict2D(rb2);
                } else {
                    Predict3D(rb);
                }
                drawDebugOnPrediction = wasB;
            } else {
                Debug.LogWarning("Debug on object start requires a rigibody on the object.");
            }
        } else if (drawDebugOnUpdate) {
            if (!rb && !rb2) {
                Debug.LogWarning("Debug on object update requires a rigibody on the object.");
                drawDebugOnUpdate = false;
            }
        }
    }

    private Material _lineMat;
    private bool triedGetMat = false;
    private Material lineMat {
        get {
            if (!triedGetMat) {
                triedGetMat = true;
                Shader spriteDefault;
                if (lineShader) {
                    spriteDefault = lineShader;
                } else
                    spriteDefault = Shader.Find("Particles/Alpha Blended");

                if (!spriteDefault)
                    spriteDefault = Shader.Find("Mobile/Particles/Alpha Blended");

                if (spriteDefault) {
                    _lineMat = new Material(spriteDefault);
                }
            }

            return _lineMat;
        }
    }


    // Update is called once per frame

    private int frameCount = 0;
    void Update() {
        if (drawDebugOnUpdate) {
            if (rb || rb2) {
                frameCount++;
                if (frameCount % debugLineUpdateRate == 0) {
                    frameCount = 0;
                    debugLineDuration = Time.unscaledDeltaTime * debugLineUpdateRate;
                    bool wasB = drawDebugOnPrediction; bool wasReuse = reuseLine;
                    drawDebugOnPrediction = true; reuseLine = true;
                    if (predictionType == predictionMode.Prediction2D) {
                        if (rb2.velocity.sqrMagnitude > 0.05f)
                            Predict2D(rb2);
                    } else {
                        if (rb.velocity.sqrMagnitude > 0.05f)
                            Predict3D(rb);
                    }
                    drawDebugOnPrediction = wasB;
                    reuseLine = wasReuse;
                }
            }
        }
    }

    private GameObject debugLineObj;
    [HideInInspector] public LineRenderer debugLine;
    
    private void LineDebug(List<Vector3> pointList) {
        if (!debugLineObj || (!reuseLine && debugLineObj)) {
            StopAllCoroutines();
            Destroy(debugLineObj);

            debugLineObj = new GameObject();
            debugLineObj.name = "Debug Line";
            if (transform)
                debugLineObj.transform.SetParent(transform);
            debugLine = debugLineObj.AddComponent<LineRenderer>();

            debugLine.useWorldSpace = true;
            debugLine.receiveShadows = false;
            debugLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            debugLine.sortingOrder = lineSortingOrder;
            debugLine.sortingLayerID = lineSortingLayer;
            debugLine.sharedMaterial = lineMat;

            if (lineTexture) {
                debugLine.sharedMaterial.mainTexture = lineTexture;                
            }
        }

#if UNITY_5_5_OR_NEWER
        debugLine.startColor = lineStartColor;
        debugLine.endColor = lineEndColor;
        debugLine.startWidth = lineWidth;
        debugLine.endWidth = lineWidth;
        debugLine.positionCount = pointList.Count;
#else
        debugLine.SetColors(lineStartColor, lineEndColor);
        debugLine.SetWidth(lineWidth, lineWidth);
        debugLine.SetVertexCount(pointList.Count);
#endif

        if(lineTexture)
            debugLine.sharedMaterial.mainTextureScale = new Vector2(lineDistance / lineWidth / 2f * textureTilingMult, 1f);

        debugLine.SetPositions(pointList.ToArray());

        if (!reuseLine)
            StartCoroutine(KillLineDelay(debugLineDuration));
    }

    private void OnDestroy() {
        if (debugLineObj)
            Destroy(debugLineObj);
    }

    private IEnumerator KillLineDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (debugLineObj)
            Destroy(debugLineObj);

        debugLineObj = null;
        debugLine = null;
    }

    private float drag;
    private Vector3 vel;
    private Vector3 pos;
    private Vector3 grav;
    ///<summary>
    ///Given these values, perform velocity prediction in 3D. 
    ///Results can be found in the instance of the class' hitInfo and predictionPoints variables.
    ///</summary>
    public void Predict3D(Vector3 startPos, Vector3 velocity, Vector3 gravity, float linearDrag = 0f) {
        drag = linearDrag;
        vel = velocity;
        pos = startPos;
        grav = gravity;

        PerformPrediction();
    }
    ///<summary>
    ///Given these values, perform velocity prediction in 2D. 
    ///Results can be found in the instance of the class' hitInfo and predictionPoints variables.
    ///</summary>
    public void Predict2D(Vector3 startPos, Vector2 velocity, Vector2 gravity, float linearDrag = 0f) {
        drag = linearDrag;
        vel = velocity;
        pos = startPos;
        grav = gravity;

        PerformPrediction();
    }
    ///<summary>
    ///Given a RigidBody, perform velocity prediction in 3D.
    ///Results can be found in the instance of the class' hitInfo and predictionPoints variables.
    ///</summary>
    public void Predict3D(Rigidbody rb) {
        drag = rb.drag;
        vel = rb.velocity;
        pos = rb.position;        
        grav = rb.useGravity ? Physics.gravity : Vector3.zero;

        PerformPrediction();
    }
    ///<summary>
    ///Given a RigidBody2D, perform velocity prediction in 2D.
    ///Results can be found in the instance of the class' hitInfo and predictionPoints variables.
    ///</summary>
    public void Predict2D(Rigidbody2D rb) {
        drag = rb.drag;
        vel = rb.velocity;
        pos = rb.position;
        grav = rb.gravityScale * Physics2D.gravity;

        PerformPrediction();
    }

    ///<summary>
    ///Given these values, get an array of points representing the trajectory in 3D without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static Vector3[] GetPoints3D(Vector3 startPos, Vector3 velocity, Vector3 gravity, float linearDrag = 0f,
        float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision;
        pt.Predict3D(startPos, velocity, gravity, linearDrag);
        Destroy(tObj);
        return pt.predictionPoints.ToArray();
    }
    ///<summary>
    ///Given these values, get an array of points representing the trajectory in 2D without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static Vector3[] GetPoints2D(Vector3 startPos, Vector2 velocity, Vector2 gravity, float linearDrag = 0f,
        float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision; pt.predictionType = predictionMode.Prediction2D;
        pt.Predict2D(startPos, velocity, gravity, linearDrag);
        Destroy(tObj);
        return pt.predictionPoints.ToArray();
    }
    ///<summary>
    ///Given these values, get an array of points representing the trajectory in 3D without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static Vector3[] GetPoints3D(Rigidbody rb, float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision;
        pt.Predict3D(rb);
        Destroy(tObj);
        return pt.predictionPoints.ToArray();
    }
    ///<summary>
    ///Given these values, get an array of points representing the trajectory in 2D without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static Vector3[] GetPoints2D(Rigidbody2D rb, float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision; pt.predictionType = predictionMode.Prediction2D;
        pt.Predict2D(rb);
        Destroy(tObj);
        return pt.predictionPoints.ToArray();
    }
    ///<summary>
    ///Given these values, get a RaycastHit of where the trajectory collides with an object,
    /// without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static RaycastHit GetHitInfo3D(Vector3 startPos, Vector3 velocity, Vector3 gravity, float linearDrag = 0f,
        float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision;
        pt.Predict3D(startPos, velocity, gravity, linearDrag);
        Destroy(tObj);
        return pt.hitInfo3D;
    }
    ///<summary>
    ///Given these values, get a RaycastHit of where the trajectory collides with an object,
    /// without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static RaycastHit GetHitInfo3D(Rigidbody rb, float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision;
        pt.Predict3D(rb);
        Destroy(tObj);
        return pt.hitInfo3D;
    }
    ///<summary>
    ///Given these values, get a RaycastHit2D of where the trajectory collides with an object,
    /// without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static RaycastHit2D GetHitInfo2D(Vector3 startPos, Vector2 velocity, Vector2 gravity, float linearDrag = 0f,
        float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision; pt.predictionType = predictionMode.Prediction2D;
        pt.Predict2D(startPos, velocity, gravity, linearDrag);
        Destroy(tObj);
        return pt.hitInfo2D;
    }
    ///<summary>
    ///Given these values, get a RaycastHit2D of where the trajectory collides with an object,
    /// without needing to create an instance of the class or use it as a component.
    ///</summary>
    public static RaycastHit2D GetHitInfo2D(Rigidbody2D rb, float accuracy = 0.985f, int iterationLimit = 150, bool stopOnCollision = true, int rayCastMask = -1) {
        GameObject tObj = new GameObject();
        tObj.name = "TrajectoryPredictionObj";
        TrajectoryPredictor pt = tObj.AddComponent<TrajectoryPredictor>();
        pt.raycastMask = rayCastMask;
        pt.accuracy = accuracy; pt.iterationLimit = iterationLimit; pt.checkForCollision = stopOnCollision; pt.predictionType = predictionMode.Prediction2D;
        pt.Predict2D(rb);
        Destroy(tObj);
        return pt.hitInfo2D;
    }

    //last prediction line total distance travelled
    [HideInInspector] public float lineDistance = 0f;

    /// <summary>
    /// An event that is called at every iteration of the prediction so you can write custom code for custom physics like objects orbiting a planet.
    /// </summary>
    /// <param name="currentIterationVel">Modify this value to apply your custom velocity to be taken into account in the prediction at each interval.</param>
    /// <param name="currentIterationPos">Current world position of the prediction at this interval.</param>
    /// <param name="tpInstance">Reference to the instance of the trajectory predictor doing the calculations.</param>
    public delegate void PredictionIterationEvent(ref Vector3 currentIterationVel, Vector3 currentIterationPos, TrajectoryPredictor tpInstance);


    /// <summary>
    /// An event that is called at every iteration of the prediction so you can write custom code for custom physics like objects orbiting a planet.
    /// </summary>
    public PredictionIterationEvent OnPredictionIterationStep;

    private void PerformPrediction() {
        Vector3 dir = Vector3.zero;
        Vector3 toPos;
        bool done = false;
        int iter = 0;
        lineDistance = 0f;

        float compAcc = 1f - accuracy;
        Vector3 gravAdd = grav * compAcc;
        float dragMult = Mathf.Clamp01(1f - drag * compAcc);
        predictionPoints.Clear();
        while (!done && iter < iterationLimit) {
            if(OnPredictionIterationStep != null) {
                Vector3 iterationVel = new Vector3();
                OnPredictionIterationStep(ref iterationVel, pos, this);
                vel += iterationVel * compAcc;
            }
            vel += gravAdd;
            vel *= dragMult;
            toPos = pos + vel * compAcc;
            dir = toPos - pos;
            predictionPoints.Add(pos);

            float dist = Vector3.Distance(pos, toPos);
            lineDistance += dist;
            if (checkForCollision) {
                if (predictionType == predictionMode.Prediction2D) {
                    RaycastHit2D hit = Physics2D.Raycast(pos, dir, dist, raycastMask);
                    if (hit) {
                        if (hit.collider.transform)
                            if (hit.collider.transform != transform) {
                                hitInfo2D = hit;
                                done = true;
                                predictionPoints.Add(hit.point);
                            }
                    }
                } else {
                    Ray ray = new Ray(pos, dir);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, dist, raycastMask)) {
                        hitInfo3D = hit;
                        predictionPoints.Add(hit.point);
                        if (bounceOnCollision) {
                            Collider col = rb.GetComponent<Collider>();
                            if (col) {
                                PhysicMaterial pMat = col.sharedMaterial;
                                if (pMat) {
                                    toPos = hit.point;
                                    vel = Vector3.Reflect(vel, hit.normal) * pMat.bounciness;
                                }
                            }
                        } else {
                            done = true;
                        }
                    }
                }
            }
            if (drawDebugOnPrediction && debugLineMode == lineMode.DrawRayEditorOnly)
                Debug.DrawRay(pos, dir, lineStartColor, debugLineDuration);

            pos = toPos;
            iter++;
        }
        if (drawDebugOnPrediction && debugLineMode == lineMode.LineRendererBoth)
            LineDebug(predictionPoints);
    }



    //End class
}
