/*
The code snippet Orbiter below has been sourced from
[https://assetstore.unity.com/packages/tools/physics/trajectory-predictor-55752
The code snippet appears in its original form
*/

using UnityEngine;
using System.Collections;

public class Orbiter : MonoBehaviour {

    public Transform planet;
    public Vector3 startVelocity;
    public float maxOrbitDist = 500f;
    public float gravityMult = 5f;

    Rigidbody rb;

    private void Awake() {
        TrajectoryPredictor trajectory = GetComponent<TrajectoryPredictor>();        
        trajectory.OnPredictionIterationStep += HandlePredictionGravity;

        rb = GetComponent<Rigidbody>();
        rb.velocity = startVelocity;
    }

    void HandlePredictionGravity(ref Vector3 currentIterationVel, Vector3 currentIterationPos, TrajectoryPredictor tpInstance) {
        currentIterationVel += GetGravForce(currentIterationPos);
    }


    private void FixedUpdate() {
        rb.AddForce(GetGravForce(transform.position) * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }


    Vector3 GetGravForce(Vector3 gravAtPos) {
        Vector3 dif = planet.position - gravAtPos;
        float orbitPercentage = 1f - Mathf.Clamp01(dif.sqrMagnitude / maxOrbitDist);

        Vector3 gravForce = dif.normalized * orbitPercentage * gravityMult;

        return gravForce;
    }

}
// End code snippet Orbiter 