/*
The code (1. intended to destroy the attached GameObject after a delay of 7.5 seconds ),below has been sourced from
https://assetstore.unity.com/packages/tools/physics/trajectory-predictor-55752
The code snippet appears in its original form
*/

using UnityEngine;
using System.Collections;

public class LaunchDelayDestroy : MonoBehaviour {

	void Start () {
		StartCoroutine(DestroyCo());
	}

	IEnumerator DestroyCo(){
		yield return new WaitForSeconds(7.5f);
		Destroy(gameObject);
	}
}

// End code snippet (1. intended to destroy the attached GameObject after a delay of 7.5 seconds)
