using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {
	Vector3 pointA = Vector3.zero;
	Vector3 pointB = new Vector3(0, 5, 0);
	Vector3 dirPerSecond =  new Vector3(0, 0.5f, 0);
	Vector3 delta = new Vector3(0,5,0);
	float sqAB = 5;
	
	// Use this for initialization
	void Start () {
		delta = pointB - pointA;
		sqAB = delta.sqrMagnitude;
		dirPerSecond = delta.normalized * 0.5f;
	}
	
	// Update is called once per frame
	void Update () {
		transform.Translate(dirPerSecond * Time.deltaTime);
		while ((transform.position - pointA).sqrMagnitude > sqAB){
			transform.position -= delta;
		}
	}
}
