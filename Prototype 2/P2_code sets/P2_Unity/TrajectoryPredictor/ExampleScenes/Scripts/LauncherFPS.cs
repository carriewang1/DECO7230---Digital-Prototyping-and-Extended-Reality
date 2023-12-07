/*
The code snippet (4. Change the scene view with mouse up and down) below has been sourced from
http://answers.unity3d.com/answers/221574/view.html
The code snippet appears in its original form
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LauncherFPS : MonoBehaviour {

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
		tp.drawDebugOnPrediction = true;
        tp.reuseLine = true; //set this to true so the line renderer gets reused every frame on prediction
		tp.accuracy = 0.99f;
		tp.lineWidth = 0.03f;
		tp.iterationLimit = 600;
	}

	// Update is called once per frame
	void Update () {

		//input stuff
		if(Input.GetMouseButtonDown(0))
			launch = true;

		if(Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.UpArrow))
			force += moveSpeed / 10f;
		if(Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.DownArrow))
			force -= moveSpeed / 10f;

		force = Mathf.Clamp(force, 10f, 150f);

		if (launch) {
			launch = false;
			Launch();
		}
	}

	void LateUpdate(){
		//set line duration to delta time so that it only lasts the length of a frame
		tp.debugLineDuration = Time.unscaledDeltaTime;
		//tell the predictor to predict a 3d line. this will also cause it to draw a prediction line
		//because drawDebugOnPredict is set to true
		tp.Predict3D(launchPoint.position, launchPoint.forward * force, Physics.gravity);

		//this static method can be used as well to get line info without needing to have a component and such
		//TrajectoryPredictor.GetPoints3D(launchPoint.position, launchPoint.forward * force, Physics.gravity);

		//info text stuff
		if(infoText){
			//this will check if the predictor has a hitinfo and then if it does will update the onscreen text
			//to say the name of the object the line hit;
			if(tp.hitInfo3D.collider)
				infoText.text = "Hit Object: " + tp.hitInfo3D.collider.gameObject.name;
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
		Rigidbody rbi = lInst.GetComponent<Rigidbody> ();
		lInst.transform.position = launchPoint.position;
		lInst.transform.rotation = launchPoint.rotation;
		rbi.velocity = launchPoint.forward * force;

		Renderer tR = lInst.GetComponent<Renderer>();
		tR.material = Instantiate(tR.material) as Material;
		tR.material.color = RandomColor();
	}

	Color RandomColor(){
		float r = Random.Range (0.0f, 1.0f);
		float g = Random.Range (0.0f, 1.0f);
		float b = Random.Range (0.0f, 1.0f);
		return new Color(r,g,b);
	}
}
// End code snippet (4. Change the scene view with mouse up and down)
