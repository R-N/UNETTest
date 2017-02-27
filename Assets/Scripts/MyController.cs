using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

public class MyController : Controller_5 {
	Vector3 lastDir = Vector3.zero;
	public Renderer rend = null;
	public Color plyColor = Color.green;
	public Material localCharMat = null;
	public Material localAuraMat = null;
	//public Renderer auraRend = null;
	//public DecalTry3 decal = null;

	float curNoise = 0;
	float noiseDegPerS = 45f;
	float noiseDegPerM = 45f;

	Vector3 prevPos = Vector3.zero;
	bool doNoise=false;

	public GameObject wisp = null;
	public GameObject wispLocal = null;

	//Called when player connected
	public override void PlayerStart(){
		if (isLocalPlayer) {
			localAuraMat.color = plyColor;
			localCharMat.color = plyColor;
			rend.material = localCharMat;
			//auraRend.material = localAuraMat;
			/*if (decal.colors.Count > 0) {
				decal.colors [0] = plyColor;
			} else {
				decal.colors.Add (plyColor);
			}*/
			wisp.SetActive (false);
			wispLocal.SetActive (true);
		} else {
			prevPos = transform.position;
			doNoise = true;
		}
	}

	void LateUpdate(){

		curNoise += noiseDegPerS * Time.deltaTime;
		curNoise += noiseDegPerM * Vector3.Distance (prevPos, transform.position);
		prevPos = transform.position;
		while (curNoise >= 360) {
			curNoise -= 360;
		}
		rend.material.SetFloat ("noiseMod", curNoise * Mathf.Deg2Rad);
	}

	//Called on fixedupdate
	public override void PlayerUpdate(PlyMoveInput input, float dt){
		if (grounded) {
			if (GetButtonDown(input.bts[JumpBt])) {
				AddGravitySelf(-PlyGravity.gravNorm * 2.5f * mvSpd);
				lastDir = input.dir;
			} else if (GetButtonDown(input.bts[DashBt])) {
				AddImpulseSelf (input.dir.normalized * 4 * mvSpd, 8 * mvSpd);
			}else{
				Move (input.dir * mvSpd * dt);
			}
		} else {
			Move (lastDir * mvSpd * dt);
		}

		if (input.dir != Vector3.zero)
			transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation (input.dir.normalized), angularSpeed * dt);
	}

	void Update(){
		base.Update ();
		if (isLocalPlayer) {
			if (CrossPlatformInputManager.GetButtonDown ("SkillBt")) {
				CmdStomp (transform.position);
			}
		}
	}

	[Command]
	void CmdStomp(Vector3 pos){
		Collider[] cols = Physics.OverlapSphere (pos, 6);
		Controller_5 con = null;
		foreach (Collider a in cols) {
			con = a.GetComponent<Controller_5> ();
			if (con != null && con != this) {
				Vector3 delta = a.transform.position - pos;
				float mag = delta.magnitude;
				float dist = Mathf.Clamp(6 - mag, 0, 6);
				con.AddImpulseExt (delta.normalized * 4 * dist, 8 * dist);
			}
		}
	}

}
