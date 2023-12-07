/*
The code snippet LauchDelayDestory below has been sourced from
[https://assetstore.unity.com/packages/tools/physics/trajectory-predictor-55752
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
// End code snippet LauchDelayDestory
