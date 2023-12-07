/*
The code snippet (3.user move around with keyboard control ) below has been sourced from
https://assetstore.unity.com/packages/tools/physics/trajectory-predictor-55752
The code snippet appears in its original form
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Launcher2D : MonoBehaviour {

	public GameObject objToLaunch;
	public Transform launchPoint;
	public Text infoText;
	public bool launch;
	public float force = 150f;
	public float moveSpeed = 1f;

	//create a trajectory predictor in code
	TrajectoryPredictor tp;
	void Start(){
		tp = gameObject.AddComponent<TrajectoryPredictor>();
		tp.predictionType = TrajectoryPredictor.predictionMode.Prediction2D;
		tp.drawDebugOnPrediction = true;
		tp.accuracy = 0.99f;
		tp.lineWidth = 0.025f;
		tp.iterationLimit = 300;
	}

	// Update is called once per frame
	void Update () {
		
		//input stuff
		if(Input.GetKeyDown(KeyCode.T))
			launch = true;
		if(Input.GetKey(KeyCode.Y))
			launch = true;
		
		if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
			transform.Rotate(new Vector3(0f, 0f, moveSpeed));
		if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
			transform.Rotate(new Vector3(0f, 0f, -moveSpeed));
		
		if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
			force += moveSpeed / 10f;
		if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
			force -= moveSpeed / 10f;

		force = Mathf.Clamp(force, 1f, 20f);

		if (launch) {
			launch = false;
			Launch();
		}

		//set line duration to delta time so that it only lasts the length of a frame
		tp.debugLineDuration = Time.unscaledDeltaTime;
		//tell the predictor to predict a 2d line. this will also cause it to draw a prediction line
		//because drawDebugOnPredict is set to true
		tp.Predict2D(launchPoint.position, launchPoint.right * force, Physics2D.gravity);

		//this static method can be used as well to get line info without needing to have a component and such
			//TrajectoryPredictor.GetPoints2D(launchPoint.position, launchPoint.right * force, Physics2D.gravity);


		//info text stuff
		if(infoText){
			//this will check if the predictor has a hitinfo and then if it does will update the onscreen text
			//to say the name of the object the line hit;
			if(tp.hitInfo2D)
				infoText.text = "Hit Object: " + tp.hitInfo2D.collider.gameObject.name;
		}
	}

	GameObject launchObjParent;
	void Launch(){
		if(!launchObjParent){
			launchObjParent = new GameObject();
			launchObjParent.name = "Launched Objects";
		}
		GameObject lInst = Instantiate (objToLaunch);
		lInst.name = "Ball";
		lInst.transform.SetParent(launchObjParent.transform);
		Rigidbody2D rbi = lInst.GetComponent<Rigidbody2D> ();
		lInst.transform.position = launchPoint.position;
		lInst.transform.rotation = launchPoint.rotation;
		rbi.velocity = launchPoint.right * force;
	}
}

// End code snippet (3.  move around with keyboard control )
