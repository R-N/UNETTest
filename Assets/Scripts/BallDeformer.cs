using UnityEngine;
using System.Collections;

public class BallDeformer : MonoBehaviour {
	Mesh myMesh = null;
	public MeshFilter myFilter = null;
	public LayerMask myMask;

	float radius = 1.5f;
	float radius1 = 1.5f;
	float radius2 = 2f;
	Vector3[] tempVert = new Vector3[0];
	Vector3[] vertNorm = new Vector3[0];
	Vector3[] rotatedVertNorm = new Vector3[0];

	Quaternion lastRot = Quaternion.identity;

	float degPerS = 45;
	float curDeg = 0;


	// Use this for initialization
	void Start () {
		myMesh = new Mesh ();
		tempVert = new Vector3[myFilter.sharedMesh.vertices.Length];
		myFilter.sharedMesh.vertices.CopyTo (tempVert,0);
		myMesh.vertices = tempVert;
		rotatedVertNorm = new Vector3[tempVert.Length];
		vertNorm = new Vector3[tempVert.Length];
		lastRot = transform.rotation;
		for (int a = 0; a < tempVert.Length; a++) {
			vertNorm [a] = tempVert [a].normalized;
			rotatedVertNorm [a] = lastRot * vertNorm[a];
		}

		int[] temp4 = new int[myFilter.sharedMesh.triangles.Length];
		myFilter.sharedMesh.triangles.CopyTo (temp4,0);
		myMesh.triangles = temp4;

		Vector2[] temp2 = new Vector2[myFilter.sharedMesh.uv.Length];
		myFilter.sharedMesh.uv.CopyTo (temp2,0);
		myMesh.uv = temp2;

		Vector3[] temp3 = new Vector3[myFilter.sharedMesh.normals.Length];
		myFilter.sharedMesh.normals.CopyTo (temp3,0);
		myMesh.normals = temp3;

		myMesh.RecalculateBounds ();

		myFilter.mesh = myMesh;
	}
	
	void LateUpdate(){
		curDeg += degPerS * Time.deltaTime;
		while (curDeg >= 360) {
			curDeg -= 360;
		}
		radius = Mathf.Lerp (radius1, radius2, 0.5f * (Mathf.Sin (Mathf.Deg2Rad * curDeg) + 1));
		Vector3 pos = transform.position;
		Quaternion rot = transform.rotation;

		if (rot != lastRot){
			for (int a = 0; a < rotatedVertNorm.Length; a++){
				rotatedVertNorm [a] = rot * vertNorm [a];
			}
			lastRot = rot;
		}

		RaycastHit hit;
		bool updated = false;
		for (int a = 0; a < tempVert.Length; a++){
			Vector3 pendingVert;
			if (Physics.Raycast (pos, rotatedVertNorm [a], out hit, radius, myMask, QueryTriggerInteraction.Ignore)) {
				pendingVert = hit.distance * vertNorm [a];
			} else {
				pendingVert = radius * vertNorm [a];
			}
			if (pendingVert != tempVert [a]) {
				tempVert [a] = pendingVert;
				updated = true;
			}
		}
		if (updated) {
			myFilter.mesh.vertices = tempVert;
		}
	}
}
