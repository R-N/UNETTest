using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

public class Controller_2 : NetworkBehaviour {

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

	public struct TempOutput{
		public Vector3 myPos;
		public Quaternion myRot;
		public float myDT;
		public int myTimeStamp;
		public TempOutput(Vector3 newPos, Quaternion newRot, float newDT, int newTimeStamp){
			myPos = newPos;
			myRot = newRot;
			myDT = newDT;
			myTimeStamp = newTimeStamp;
		}
	}

	public class DummyOutput{
		public Vector3 myPos;
		public Quaternion myRot;
		public float myDT;
		public float totalDT;
		public DummyOutput(Vector3 newPos, Quaternion newRot, float newDT, float newTotDT){
			myPos = newPos;
			myRot = newRot;
			myDT = newDT;
			totalDT = newTotDT;
		}
	}

	public static Controller_2 player = null;
	public CharacterController cc = null;
	public ObjInfo info = null;
	public float spd = 5;
	public Vector3 gravity = Vector3.zero;
	public List<MyInput> inputs = new List<MyInput>();
	public int timeStamp = int.MinValue;
	public int recTimeStamp = int.MinValue;
	public Dictionary<int, Vector3> results = new Dictionary<int, Vector3>();
	public List<MyOutput> recResults = new List<MyOutput>();
	public Dictionary<int, DummyOutput> dumOutputs = new Dictionary<int, DummyOutput>();
	public List<int> dumTS = new List<int>();

	public GameObject camHold;

	Quaternion lastRealRot = Quaternion.identity;

	bool playerupdated = false;
	bool serverupdated = false;

	float angularSpeed = 270;
	Vector3 tempPos = Vector3.zero;

	Vector3 resPos = Vector3.zero;
	Vector3 posCorrection = Vector3.zero;

	Vector3 prevDir = Vector3.zero;
	
	Vector2 inputAxis = Vector2.zero;

	float sqMinMove = 0.000001f;

	float dummySpd = 1;
	float dummySpd2 = 1;
	float dummyTime = 0.15f;

	float dummyAccel = 0.1f;


	float totalDT = 0;
	bool waitingForNetwork = false;

	float actualDT = 0;

	float pendingDTThreshold = 0.21f;
	float processedDTThreshold = 0.12f;
	float maxDummySpd = 1.25f;

	float dummySpdIncreaseTime = 0.12f;
	float dummySpdDecreaseTime = 0.05f;

	Vector3 dummyPos = Vector3.zero;
	Quaternion dummyRot = Quaternion.identity;

	void Start(){
		if (isLocalPlayer) {
			if (Camera.main) {
				Camera.main.enabled = false;
			}
			camHold.SetActive (true);
		}
		lastRealRot = transform.rotation;
		tempPos = transform.position;
		resPos = transform.position;
		dummyPos = transform.position;
		dummyRot = transform.rotation;

		waitingForNetwork = true;
		StartCoroutine (WaitForNetwork ());
	}

	IEnumerator WaitForNetwork(){
		for (int a = 0; a < 108000 && waitingForNetwork; a++){
			if (isServer) {
				if (isLocalPlayer) {
					StartCoroutine (SendResultsLP (0.1f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					break;
				} else {
					StartCoroutine (SendResultsClient (0.1f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					break;
				}
			}else{
				if (isLocalPlayer) {
					StartCoroutine (SendInputs (0.05f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					break;
				}
			}
			yield return new WaitForEndOfFrame ();
		}
	}


	IEnumerator SendResultsLP(float interval){
		while (true) {
			if (playerupdated) {
				if (timeStamp == int.MaxValue) {
					timeStamp = int.MinValue;
				} else {
					timeStamp++;
				}
				RpcSendResult (transform.position, transform.rotation, timeStamp, totalDT);
				totalDT = 0;
				playerupdated = false;
				yield return new WaitForSeconds (interval);
			} else {
				yield return new WaitForEndOfFrame ();
			}
		}
	}

	IEnumerator SendResultsClient(float interval){
		while (true) {
			if (playerupdated) {
				RpcSendResult (resPos, lastRealRot, recTimeStamp, totalDT);
				totalDT = 0;
				playerupdated = false;
				yield return new WaitForSeconds (interval);
			} else {
				yield return new WaitForEndOfFrame ();
			}
		}
	}

	IEnumerator SendInputs(float interval){
		while (true) {
			if (inputs.Count > 0) {
				while (inputs.Count > 0) {
					CmdSendInput (inputs [0].myDir, inputs[0].myDT, inputs[0].myTimeStamp, inputs[0].myRot);
					inputs.RemoveAt (0);
				}
				yield return new WaitForSeconds (interval);
			} else {
				yield return new WaitForEndOfFrame ();
			}
		}
	}

	void Update(){
		actualDT = Time.deltaTime;
		if (cc.isGrounded) {
			cc.Move (new Vector3 (0, -cc.skinWidth, 0));
		}
		
		if (isLocalPlayer){
			inputAxis = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
		}else {
			if (isServer) {
				playerupdated = false;
				if (inputs.Count > 0) {
					playerupdated = true;
					serverupdated = true;
					tempPos = transform.position;
					transform.position = resPos;
					Dictionary<int, TempOutput> tempList = new Dictionary<int, TempOutput> ();
					List<int> tss = new List<int> ();
					int tempTS = recTimeStamp;
					float processedDT = 0;
					while (inputs.Count > 0) {
						if ((totalDT > pendingDTThreshold || processedDT > processedDTThreshold) && actualDT < processedDTThreshold) {
							while (inputs.Count > 0) {
								if (recTimeStamp < inputs [0].myTimeStamp) {
									recTimeStamp = inputs [0].myTimeStamp;
									lastRealRot = inputs [0].myRot;
								}
								inputs.RemoveAt (0);
							}
							if (!dumOutputs.ContainsKey (recTimeStamp)) {
								tempList.Add (recTimeStamp, new TempOutput (transform.position, lastRealRot, 0.0001f, recTimeStamp));
								tss.Add (recTimeStamp);
							}
							break;
						}

						Vector3 finalMove = Vector3.zero;

						if (recTimeStamp < inputs [0].myTimeStamp) {
							recTimeStamp = inputs [0].myTimeStamp;
							lastRealRot = inputs [0].myRot;
						}
						//isGrounded is somehow buggy sometimes
						if (cc.isGrounded) {
							gravity = new Vector3 (0, -cc.skinWidth, 0);
							cc.Move (gravity);
						} else {
							Vector3 curGrav = Physics.gravity * inputs [0].myDT;
							cc.Move ((gravity + curGrav * 0.5f) * inputs [0].myDT);
							gravity += curGrav;
						}

						if (cc.isGrounded) {
							finalMove += inputs [0].myDir * spd * inputs [0].myDT;
						}

						totalDT += inputs [0].myDT;
						processedDT += inputs [0].myDT;

						cc.Move (finalMove);
						tempList.Add (inputs [0].myTimeStamp, new TempOutput (transform.position, inputs [0].myRot, inputs [0].myDT, inputs [0].myTimeStamp));
						tss.Add (inputs [0].myTimeStamp);
						inputs.RemoveAt (0);
					}
					resPos = transform.position;

					//tss.Sort ();

					int lastTimeStamp = tempTS;
					if (dumTS.Count > 0) {
						lastTimeStamp = dumTS [dumTS.Count - 1];
					}
					for (int a = 0; a < tss.Count; a++) {
						int b = tss [a];

						if (dumOutputs.Count == 0) {
							dumOutputs.Add (tempList [b].myTimeStamp, new DummyOutput (tempList [b].myPos, tempList [b].myRot, tempList [b].myDT, tempList [b].myDT));
						} else {
							dumOutputs.Add (tempList [b].myTimeStamp, new DummyOutput (tempList [b].myPos, tempList [b].myRot, tempList [b].myDT, tempList [b].myDT + dumOutputs [lastTimeStamp].totalDT));
						}
						dumTS.Add (tempList [b].myTimeStamp);

						lastTimeStamp = tempList [b].myTimeStamp;
					}
					dummySpd2 = dumOutputs [dumTS [dumTS.Count - 1]].totalDT / dummyTime;
					DummyAccelUpdate ();
				}
			}
			dummySpd = Mathf.MoveTowards (dummySpd, dummySpd2, dummyAccel * actualDT);
		}


	}

	void DummyAccelUpdate(){
		dummySpd2 = Mathf.Clamp (dummySpd2, 0.1f, maxDummySpd);
		if (dummySpd2 < dummySpd) {
			dummyAccel = (dummySpd - dummySpd2) / dummySpdDecreaseTime;
		} else {
			dummyAccel = (dummySpd2 - dummySpd) / dummySpdIncreaseTime;
		}
	}


	void LateUpdate () {
		actualDT = Time.deltaTime;
		if (isLocalPlayer) {
			Vector3 finalMove = Vector3.zero;

			Vector3 dir = Vector3.ClampMagnitude (new Vector3 (Joystick.padAxis.x + inputAxis.x, 0, Joystick.padAxis.y + inputAxis.y), 1);


			if (cc.isGrounded) {
				gravity = new Vector3 (0, -cc.skinWidth, 0);
				cc.Move(gravity);
			} else {
				Vector3 curGrav = Physics.gravity * actualDT;
				cc.Move((gravity + curGrav * 0.5f) * actualDT);
				gravity += curGrav;
			}


			if (dir != Vector3.zero) {
				if (cc.isGrounded) {
					finalMove += dir * spd * actualDT;
				} 
				transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation (dir.normalized, transform.up), actualDT * angularSpeed);
			}
			Vector3 asdfasdf = transform.position;

			cc.Move (finalMove);

			if (isServer) {
				if (dir != Vector3.zero) {
					totalDT += actualDT;
					playerupdated = true;
				}
			}else{
				if (dir != Vector3.zero) {
					bool sameInput = true;

					if (inputs.Count == 0) {
						sameInput = false;
					} else if (dir != prevDir) {
						sameInput = false;
					}

					if (sameInput) {
						inputs [inputs.Count - 1] = new MyInput (inputs [inputs.Count - 1].myDir, inputs [inputs.Count - 1].myDT + actualDT, timeStamp, transform.rotation);
						results [timeStamp] = transform.position;
					} else {
						if (timeStamp == int.MaxValue) {
							timeStamp = int.MinValue;
						} else {
							timeStamp++;
						}
						inputs.Add (new MyInput (dir, actualDT, timeStamp, transform.rotation));
						results.Add (timeStamp, transform.position);
					}

					if (recResults.Count > 0) {
						List<int> keys = new List<int> (results.Keys);
						Vector3 latestResult = results[keys[0]];
						while (recResults.Count > 0) {
							if (recTimeStamp < recResults [0].myTimeStamp) {
								recTimeStamp = recResults [0].myTimeStamp;
								latestResult = recResults [0].myResult;
							}
							recResults.RemoveAt (0);

						}
						posCorrection = latestResult - results [recTimeStamp];
						for (int a = 0; a < keys.Count; a++) {
							if (keys [a] < recTimeStamp) {
								results.Remove (keys [a]);
							}
						}
					}
					if (posCorrection.sqrMagnitude >= sqMinMove) {
						Vector3 tempPos2 = transform.position;
						Vector3 myLerp = Vector3.Lerp (Vector3.zero, posCorrection, 2 * actualDT);

						cc.Move (myLerp);
						Vector3 doneCorrection = transform.position - tempPos2;

						posCorrection -= doneCorrection;
						List<int> keys2 = new List<int> (results.Keys);
						for (int b = 0; b < keys2.Count; b++) {
							results [keys2 [b]] = results [keys2 [b]] + doneCorrection;
						}
					}
				}

				prevDir = dir;
			}

		} else {
			if (isServer && serverupdated) {
				transform.position = tempPos;
				serverupdated = false;
			}

			/*Vector3 lerp = Vector3.Lerp (Vector3.zero, resPos - transform.position, 8 * actualDT);
			cc.Move (lerp);
			transform.rotation = Quaternion.Slerp (transform.rotation, lastRealRot, 8 * actualDT);*/


			if (dumTS.Count > 0) {
				float lerpDT = Mathf.MoveTowards (0, dumOutputs [dumTS[dumTS.Count - 1]].totalDT, dummySpd * actualDT);

				if (dumTS.Count == 1) {
					int ts = dumTS [0];
					float doDT2 = lerpDT / dumOutputs [ts].totalDT;
					//cc.Move (Vector3.Lerp (Vector3.zero, dumOutputs [ts].myPos - transform.position, doDT2));
					dummyPos = Vector3.Lerp(transform.position, dumOutputs[ts].myPos, doDT2);
					dummyRot = Quaternion.Slerp (transform.rotation, dumOutputs [ts].myRot, doDT2);
					if (dumOutputs [ts].totalDT > lerpDT) {
						dumOutputs [ts].totalDT -= lerpDT;
						dumOutputs [ts].myDT -= lerpDT;
					} else {
						dumOutputs.Remove (ts);
						dumTS.RemoveAt (0);
					}
						
				} else {
					int b = 0;
					for (int a = 0; a < dumTS.Count; a++) {
						b = a;
						if (dumOutputs [dumTS [a]].totalDT > lerpDT) {
							break;
						}
					}
					for (int a = b - 2; a >= 0; a -= 1) {
						dumOutputs.Remove (dumTS [a]);
						dumTS.RemoveAt (a);
					}
						
					float doDT = lerpDT - dumOutputs [dumTS [0]].totalDT;
					float doDT2 = doDT / dumOutputs [dumTS [1]].myDT;

					dummyPos = Vector3.Lerp (dumOutputs [dumTS [0]].myPos, dumOutputs [dumTS [1]].myPos, doDT2);
					//cc.Move (dummyPos - transform.position);
					dummyRot = Quaternion.Slerp (dumOutputs [dumTS [0]].myRot, dumOutputs [dumTS [1]].myRot, doDT2);
					dumOutputs [dumTS [1]].myDT -= doDT;
					dumOutputs.Remove(dumTS[0]);
					dumTS.RemoveAt(0);

					for (int a = dumTS.Count - 1; a >= 0; a -= 1) {
						if (dumOutputs [dumTS [a]].totalDT > lerpDT){
							dumOutputs [dumTS [a]].totalDT -= lerpDT;
						} else {
							dumOutputs.Remove (dumTS [a]);
							dumTS.RemoveAt (a);
						}
					}
				}
			}

			//transform.position = Vector3.MoveTowards (transform.position, dummyPos, spd * actualDT);
			cc.Move(Vector3.ClampMagnitude(dummyPos - transform.position, spd * actualDT));
			transform.rotation = Quaternion.RotateTowards (transform.rotation, dummyRot, angularSpeed * actualDT);

			tempPos = transform.position;

		} 


	}

	[Command]
	public void CmdSendInput(Vector3 leDir, float leDT, int leTS, Quaternion leRot){
		inputs.Add (new MyInput(leDir, leDT, leTS, leRot));
	}

	[ClientRpc]
	public void RpcSendResult(Vector3 pos, Quaternion rot, int leTimeStamp, float leAccDT){
		if (!isServer){
			if (isLocalPlayer) {
				if (results.ContainsKey (leTimeStamp)) {
					recResults.Add (new MyOutput (pos, leTimeStamp));
				}
			} else {
				if (dumOutputs.Count == 0) {
					if (leTimeStamp > recTimeStamp) {
						dumOutputs.Add (leTimeStamp, new DummyOutput (pos, rot, leAccDT, leAccDT));
						dumTS.Add (leTimeStamp);
					}
				} else {
					if (dumTS[0] < leTimeStamp) {
						dumOutputs.Add (leTimeStamp, new DummyOutput (pos, rot, leAccDT, dumOutputs [dumTS [dumTS.Count - 1]].totalDT + leAccDT));
						dumTS.Add (leTimeStamp);
					}
				}
				if (leTimeStamp > recTimeStamp) {
					recTimeStamp = leTimeStamp;
					lastRealRot = rot;

					dummySpd2 = dumOutputs [recTimeStamp].totalDT / dummyTime;
					DummyAccelUpdate ();
				} else {
					for (int a = 0; a < dumTS.Count; a++) {
						if (dumTS [a] > leTimeStamp) {
							dumOutputs [dumTS [a]].totalDT += leAccDT;
						}
					}
					dummySpd2 = dumOutputs [recTimeStamp].totalDT / dummyTime;
					DummyAccelUpdate ();
				}
			}
		}
	}
}
