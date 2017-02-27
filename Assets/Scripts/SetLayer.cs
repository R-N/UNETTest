using UnityEngine;
using System.Collections;

public class SetLayer : MonoBehaviour {
	int defaultLayer = 0;
	GameObject myGO = null;
	// Use this for initialization
	void Awake(){
		myGO = gameObject;
		defaultLayer = myGO.layer;
	}
	
	// Update is called once per frame
	void Update () {
		myGO.layer = defaultLayer;
	}
}
