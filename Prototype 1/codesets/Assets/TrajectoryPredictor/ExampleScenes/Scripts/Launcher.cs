
using UnityEngine;
using System.Collections;

public class Launcher : MonoBehaviour {

	public GameObject objToLaunch;
	public bool launch;
	public bool continuousLaunch;
	public Vector2 forceRange = new Vector2(100,200);
	public Texture[] lineTextures;

	void Start(){
		Time.fixedDeltaTime = 0.01f;
	}

	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.T))
			launch = true;
		if(Input.GetKey(KeyCode.Y))
			launch = true;
		
		if (launch) {
			launch = false;
			Launch();
			if(continuousLaunch){
				launch = true;
			}
		}
	}

	GameObject launchObjParent;
	void Launch(){
		if(!launchObjParent){
			launchObjParent = new GameObject();
			launchObjParent.name = "Launched Objects";
		}
		GameObject lInst = Instantiate (objToLaunch);
		lInst.transform.SetParent(launchObjParent.transform);
		Rigidbody rbi = lInst.GetComponent<Rigidbody> ();
        lInst.transform.position = transform.GetChild(0).Find("LP").position;
        rbi.rotation = transform.GetChild(0).Find("LP").rotation;
        rbi.AddRelativeForce(new Vector3(Random.Range(-30f, 30f), Random.Range(-10f, 10f), Random.Range(forceRange.x, forceRange.y)));

        Renderer tR = lInst.GetComponent<Renderer>();
		tR.material = Instantiate(tR.material) as Material;
		tR.material.color = RandomColor();

		TrajectoryPredictor tp = lInst.GetComponent<TrajectoryPredictor>();
		tp.lineStartColor = tR.material.color;
		tp.lineEndColor = tR.material.color;

		switch(Random.Range(0, 3)){

		case 0: tp.lineTexture = lineTextures[0];
			break;

		case 1: tp.lineTexture = lineTextures[1];
			tp.textureTilingMult = 0.35f;
			tp.lineWidth = 0.2f;
			break;

		case 2: tp.lineWidth = 0.1f;
			break;
		}

	}

	Color RandomColor(){
		float r = Random.Range (0.0f, 1.0f);
		float g = Random.Range (0.0f, 1.0f);
		float b = Random.Range (0.0f, 1.0f);
		return new Color(r,g,b);
	}

}
