using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

public class Controller : NetworkBehaviour {

	public struct MyInput{
		public Vector3 myDir;
		public float myDT;
		public int myTimeStamp;
		public Quaternion myRot;
		public MyInput(Vector3 newDir, float newDT, int newTimeStamp, Quaternion newRot){
			myDir = newDir;
			myDT = newDT;
			myTimeStamp = newTimeStamp;
			myRot = newRot;
		}
	}

	public struct MyOutput{
		public Vector3 myResult;
		public int myTimeStamp;
		public MyOutput(Vector3 newResult, int newTimeStamp){
			myResult = newResult;
			myTimeStamp = newTimeStamp;
		}
	}

	public static Controller player = null;
	public CharacterController cc = null;
	public ObjInfo info = null;
	public float spd = 5;
	public Vector3 gravity = Vector3.zero;
	public List<MyInput> inputs = new List<MyInput>();
	public int timeStamp = 0;
	public int recTimeStamp = 0;
	public Vector3 accDeltaPos = Vector3.zero;
	public Dictionary<int, Vector3> results = new Dictionary<int, Vector3>();
	public List<MyOutput> recResults = new List<MyOutput>();

	public GameObject camHold;

	Quaternion lastRealRot = Quaternion.identity;


	void Start(){
		if (isLocalPlayer) {
			if (Camera.main) {
				Camera.main.enabled = false;
			}
			camHold.SetActive (true);
		}
		lastRealRot = transform.rotation;
	}

	// Update is called once per frame
	void Update () {

		if (isLocalPlayer) {
			Vector3 finalMove = Vector3.zero;

			Vector3 dir = Vector3.ClampMagnitude (new Vector3 (Joystick.padAxis.x + Input.GetAxis ("Horizontal"), 0, Joystick.padAxis.y + Input.GetAxis ("Vertical")), 1);

			if (dir != Vector3.zero) {
				if (cc.isGrounded) {
					finalMove += dir * spd * Time.deltaTime;
					gravity = Vector3.zero;
				}
				transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.LookRotation (dir.normalized, transform.up), 8 * Time.deltaTime);
			}
			if (!cc.isGrounded) {
				gravity += Physics.gravity * Time.deltaTime;
				finalMove += gravity;
			}

			cc.Move (finalMove);
			if (isServer) {
				RpcSyncDummy (transform.position, transform.rotation, timeStamp);
				if (timeStamp == int.MaxValue) {
					timeStamp = 0;
				} else {
					timeStamp++;
				}
			} else {

				if (dir != Vector3.zero) {
					CmdSendInput (dir, Time.deltaTime, timeStamp, transform.rotation);
					results.Add (timeStamp, transform.position);
					if (timeStamp == int.MaxValue) {
						timeStamp = 0;
					} else {
						timeStamp++;
					}
				}

				if (recResults.Count > 0) {
					Vector3 latestResult = Vector3.zero;
					while (recResults.Count > 0) {
						if (recTimeStamp < recResults [0].myTimeStamp) {
							recTimeStamp = recResults [0].myTimeStamp;
							latestResult = recResults [0].myResult;

						}
						recResults.RemoveAt (0);

					}
					accDeltaPos = latestResult - results [recTimeStamp];
					List<int> keys = new List<int> (results.Keys);
					for (int a = 0; a < keys.Count; a++) {
						if (keys [a] <= recTimeStamp) {
							results.Remove (keys [a]);
						}
					}
				}


				Vector3 myLerp = Vector3.Lerp (Vector3.zero, accDeltaPos, 8 * Time.deltaTime);

				List<int> keys2 = new List<int> (results.Keys);
				for (int b = 0; b < keys2.Count; b++){
					results [keys2[b]] = results[keys2[b]] + myLerp;
				}

				cc.Move (myLerp);
				accDeltaPos -= myLerp;
			}
		} else {
			if (isServer) {
				if (inputs.Count > 0) {
					Vector3 tempPos = transform.position;
					transform.position = transform.position + accDeltaPos;
					Vector3 finalMove = Vector3.zero;
					while (inputs.Count > 0) {

						if (recTimeStamp < inputs [0].myTimeStamp) {
							recTimeStamp = inputs [0].myTimeStamp;
							lastRealRot = inputs [0].myRot;
						}

						if (cc.isGrounded) {
							finalMove += inputs [0].myDir * spd * inputs [0].myDT;
						}
						if (!cc.isGrounded) {
							gravity += Physics.gravity * inputs [0].myDT;
							finalMove += gravity;
						}


						inputs.RemoveAt (0);
					}
					cc.Move (finalMove);
					RpcCheckResult (transform.position, recTimeStamp);
					RpcSyncDummy (transform.position, lastRealRot, recTimeStamp);
					accDeltaPos = transform.position - tempPos;
					transform.position = tempPos;
				}

			}

			Vector3 lerp = Vector3.Lerp (Vector3.zero, accDeltaPos, 8 * Time.deltaTime);

			cc.Move (lerp);
			accDeltaPos -= lerp;

			transform.rotation = Quaternion.Lerp (transform.rotation, lastRealRot, 8 * Time.deltaTime);


		} 


	}

	[Command]
	public void CmdSendInput(Vector3 leDir, float leDT, int leTimeStamp, Quaternion leRot){
		inputs.Add (new MyInput (leDir, leDT, leTimeStamp, leRot));
	}

	[ClientRpc]
	public void RpcCheckResult(Vector3 pos, int leTimeStamp){
		if (isLocalPlayer) {
			recResults.Add (new MyOutput (pos, leTimeStamp));
		}
	}

	[ClientRpc]
	public void RpcSyncDummy(Vector3 pos, Quaternion rot, int leTimeStamp){
		if (!isLocalPlayer && leTimeStamp > recTimeStamp){
			recTimeStamp = leTimeStamp;
			accDeltaPos = pos - transform.position;
			lastRealRot = rot;
		}
	}
}
