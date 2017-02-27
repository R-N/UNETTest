using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

public class Controller_3 : NetworkBehaviour {
	public class PlyMoveInput{
		public Vector3 dir;
		public sbyte jumpBt;
		public sbyte dashBt;
		public sbyte skillBt;
		public float dt;
		public Vector3 accResDeltaPos;
		public Quaternion rot;
		public Vector3 deltaGrav;
		public int impulseId;
		public int gravId;
		public float accTimeSinceUngrounded;
		public Vector3 curPos;
		public int state;

		public PlyMoveInput(){
			dir = Vector3.zero;
			jumpBt = 0;
			dashBt = 0;
			skillBt = 0;
			dt = 0;
			accResDeltaPos = Vector3.zero;
			rot = Quaternion.identity;
			deltaGrav = Vector3.zero;
			impulseId = int.MinValue;
			gravId = int.MinValue;
			accTimeSinceUngrounded = 0;
			curPos = Vector3.zero;
			state = 0;
		}

		public PlyMoveInput(Vector3 newDir, sbyte newJBt, sbyte newDBt, sbyte newSBt, float newDt, Vector3 newRDP, Quaternion newRot, Vector3 newDeltaGrav, int newImp, int newGravId, float newTime, Vector3 newPos, int newState){
			dir = newDir;
			jumpBt = newJBt;
			dashBt = newDBt;
			skillBt = newSBt;
			dt = newDt;
			accResDeltaPos = newRDP;
			rot = newRot;
			deltaGrav = newDeltaGrav;
			impulseId = newImp;
			gravId = newGravId;
			accTimeSinceUngrounded = newTime;
			curPos = newPos;
			state = newState;
		}

		public static PlyMoveInput CopyFromInput(PlyMoveInput parent){
			return new PlyMoveInput (parent.dir, parent.jumpBt, parent.dashBt, parent.skillBt, parent.dt, parent.accResDeltaPos, parent.rot, parent.deltaGrav, parent.impulseId, parent.gravId, parent.accTimeSinceUngrounded, parent.curPos, parent.state);
		}
	}

	public class PlyGravity{
		public static float gravMag;
		public static Vector3 gravNorm;
		public static Vector3 gravGround;

		public Vector3 velocity;
		public Vector3 accMove;

		public float applyTimeSinceUngrounded;
		public float timeDone;

		public PlyGravity(){
			velocity = Vector3.zero;
			accMove = Vector3.zero;
			applyTimeSinceUngrounded = 0;
			timeDone = 0;
		}

		public PlyGravity(Vector3 vel, Vector3 newAccMove, float applyTime){
			velocity = vel;
			accMove = newAccMove;
			applyTimeSinceUngrounded = applyTime;
			timeDone = Time.fixedDeltaTime;
		}
	}


	public class Impulse{
		public Vector3 impulseStart;
		public Vector3 impulseLeft;
		public float impulseDeccel;
		public float time;
		public Vector3 corrections;
		public Vector3 impulseMoveDone;
		public Vector3 serverImpulseMoveDone;
		public bool done;
		public bool serverDone;
		public float endTime;
		public float timeLeft;
		public Impulse(Vector3 newImp, float newDeccel){
			impulseStart = newImp;
			impulseLeft = newImp;
			impulseDeccel = newDeccel;
			time = 0;
			impulseMoveDone = Vector3.zero;
			serverImpulseMoveDone = Vector3.zero;
			done = false;
			serverDone = false;
			corrections = Vector3.zero;
			endTime = newImp.magnitude / newDeccel;
			timeLeft = endTime;
		}
		public Impulse(Vector3 newImp, float newDeccel, Vector3 impulseDone, float newTime){
			impulseStart = newImp;
			impulseLeft = newImp;
			impulseDeccel = newDeccel;
			time = newTime;
			impulseMoveDone = impulseDone;
			serverImpulseMoveDone = impulseDone;
			done = false;
			serverDone = false;
			corrections = Vector3.zero;
			timeLeft = newImp.magnitude / newDeccel;
			endTime = time + timeLeft;
		}
	}

	public class DummyOutput{
		public Vector3 pos;
		public Quaternion rot;
		public float dt;
		public float accDt;
	}

	//links
	public static Controller_3 player = null;
	public CharacterController cc = null;
	public ObjInfo info = null;
	public GameObject camHold = null;

	//player attr
	float mvSpd = 5;
	float angularSpeed = 270;
	float sqMinMove = 0.000001f;
	float capsuleHalfHeight = 0.5f;
	int state = 0;

	//player move
	PlyMoveInput myInput = new PlyMoveInput();
	PlyMoveInput prevInput = new PlyMoveInput();
	bool sameInput = false;
	List<PlyMoveInput> inputs = new List<PlyMoveInput>();

	Vector3 plyMoveCorrection = Vector3.zero;

	Vector3 lastDir = Vector3.zero;

	//player gravity

	public bool grounded = false;
	Vector3 gravity = Vector3.zero;
	Vector3 gravAccMove = Vector3.zero;
	float timeSinceUngrounded = 0;

	int lastGravityId = int.MinValue;

	Vector3 tempGrav = Vector3.zero;

	List<int> gravIds = new List<int>();
	Dictionary<int, PlyGravity> pendingGrav = new Dictionary<int, PlyGravity> ();

	Vector3 gravCorrections = Vector3.zero;

	//impulses
	Dictionary<int, Impulse> playerImpulse = new Dictionary<int, Impulse>();
	Dictionary<int, Impulse> serverImpulse = new Dictionary<int, Impulse>();
	Vector3 impulseCorrections = Vector3.zero;
	int lastClientImpulseId = int.MinValue;
	int lastServerImpulseId = int.MinValue;
	List<int> playerImpulseIds = new List<int>();
	List<int> serverImpulseIds = new List<int>();
	Vector3 tempImpulse = Vector3.zero;
	float tempImpulseDeccel = 0;

	//Server process
	float frameDTThreshold = 0.12f;
	float pendingDTThreshold = 0.21f;
	Vector3 tempPos = Vector3.zero; //moves are done in Update, but for smoothness, it will be rewind on LateUpdate and moved smoothly
	Vector3 lastSentDumPos = Vector3.zero;
	Quaternion lastSentDumRot = Quaternion.identity;
	Vector3 clientPos = Vector3.zero;
	float accDt = 0;
	float forceSyncTimeout = 0;
	int clientState = 0;

	//Network
	bool waitingForNetwork = false;
	List<PlyMoveInput> pendingInputs = new List<PlyMoveInput> ();

	//Dummy 
	Vector3 dumPos = Vector3.zero;
	Quaternion dumRot = Quaternion.identity;
	float dummyTimeSpd = 1;
	float maxSpd = 5;

	//Correction 
	public LayerMask staticCols;

	void Start(){
		if (isLocalPlayer) {
			if (Camera.main) {
				Camera.main.enabled = false;
			}
			camHold.SetActive (true);
		}
		capsuleHalfHeight = cc.height * 0.5f;

		tempPos = transform.position;
		dumPos = transform.position;
		dumRot = transform.rotation;

		myInput.rot = transform.rotation;
		myInput.curPos = transform.position;
		prevInput.rot = transform.rotation;
		prevInput.curPos = transform.position;

		lastSentDumPos = transform.position;
		lastSentDumRot = transform.rotation;

		PlyGravity.gravMag = Physics.gravity.magnitude;
		PlyGravity.gravNorm = Physics.gravity / PlyGravity.gravMag;
		PlyGravity.gravGround = PlyGravity.gravNorm * cc.skinWidth;

		gravity = PlyGravity.gravGround;

		waitingForNetwork = true;
		StartCoroutine (WaitForNetwork ());
	}



	IEnumerator WaitForNetwork(){
		for (int a = 0; a < 108000 && waitingForNetwork; a++){
			if (isServer) {
				if (isLocalPlayer) {
					//StartCoroutine (SendResultsLP (0.1f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					yield break;
				} else {
					StartCoroutine (SendResultsClient (0.1f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					yield break;
				}
			}else{
				if (isLocalPlayer) {
					StartCoroutine (SendInputs (0.05f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					yield break;
				}
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
	}


	IEnumerator SendResultsLP(float interval){
		while (true) {
			/*if (playerupdated) {
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
			}*/
			yield return new WaitForEndOfFrame ();
		}
	}

	IEnumerator SendResultsClient(float interval){
		while (true) {
			if (plyMoveCorrection != Vector3.zero) {
				RpcSendPlyCorrection (plyMoveCorrection);
				accDt = 0;
				plyMoveCorrection = Vector3.zero;
				lastSentDumPos = transform.position;
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
					CmdSendInput (inputs [0].dir, inputs [0].jumpBt, inputs [0].dashBt, 
						inputs [0].skillBt, inputs [0].dt, inputs [0].accResDeltaPos, 
						inputs [0].rot, inputs [0].deltaGrav, inputs [0].impulseId,
						inputs[0].gravId, inputs[0].accTimeSinceUngrounded, inputs[0].curPos, inputs[0].state);
					inputs.RemoveAt (0);
				}
				yield return new WaitForSeconds (interval);
			} else {
				/*CmdSendInput (myInput.dir, myInput.jumpBt, myInput.dashBt, 
					myInput.skillBt, myInput.dt, myInput.accResDeltaPos, 
					myInput.rot, myInput.deltaGrav, myInput.impulseId,
					myInput.gravId, myInput.accTimeSinceUngrounded);*/
				yield return new WaitForEndOfFrame();
			}
		}
	}

	void Update(){


		if (isLocalPlayer) {
			sameInput = UpdateInput ();
		} else if (isServer) {
			Vector3 prevClientPos = clientPos;
			float curAccDt = 0;
			if (inputs.Count > 0) {
				tempPos = transform.position;

				while (inputs.Count > 0) {
					Vector3 curPos = transform.position;
					tempGrav = Vector3.zero;
					tempImpulse = Vector3.zero;
					tempImpulseDeccel = 0;
					PlayerUpdate (inputs [0]);

					if (AddGravity (tempGrav)) {
						Vector3 gravCor = tempGrav - inputs [0].deltaGrav;
						if (inputs [0].gravId == int.MinValue) {
							if (gravCor != Vector3.zero) {
								RpcSendAddGrav (gravCor, timeSinceUngrounded);
							} 
						} else {
							if (tempGrav == Vector3.zero) {
								RpcSendGravDecline (inputs [0].gravId);
							} else {
								RpcSendGravCorrection (inputs [0].gravId, gravCor);
							}
						}
					}

					if (inputs [0].impulseId == int.MinValue) {
						if (AddImpulse (tempImpulse, tempImpulseDeccel)) {
							RpcSendImpulse (tempImpulse, tempImpulseDeccel, lastServerImpulseId);
						}
					} else {
						if (AddImpulse (tempImpulse, tempImpulseDeccel, inputs[0].impulseId, true)) {
							RpcSendImpulseCorrection (tempImpulse, tempImpulseDeccel, inputs [0].impulseId);
						} else {
							RpcSendImpulseDecline (inputs [0].impulseId);
							Debug.Log ("declined");
						}
					}


					curAccDt += inputs [0].dt;
					plyMoveCorrection += transform.position - curPos - inputs [0].accResDeltaPos;
					clientPos = inputs [0].curPos;
					inputs.RemoveAt (0);
				}
			}


			tempGrav = Vector3.zero;
			tempImpulse = Vector3.zero;
			tempImpulseDeccel = 0;

			accDt += curAccDt;

			// if clientPos differs too much, force pos sync
			float dist = Vector3.Distance (clientPos, transform.position);
			if (state == clientState) {
				if (playerImpulse.Count == 0 && serverImpulse.Count == 0) {
					float distToPrev = Vector3.Distance (prevClientPos, clientPos);
					if (distToPrev < 0.5f || distToPrev / curAccDt < 2) {
						if (grounded) {
							if (dist < 0.5f) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += curAccDt;
							}
						} else {
							if (dist < 1) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += curAccDt;
							}
						}
					} else {
						if (grounded) {
							if (dist < 1) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += curAccDt;
							}
						} else {
							if (dist < 3) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += curAccDt;
							}
						}
					}
				} else {
					if (grounded) {
						if (dist < 2) {
							forceSyncTimeout = 0;
						} else {
							forceSyncTimeout += curAccDt;
						}
					} else {
						if (dist < 5) {
							forceSyncTimeout = 0;
						} else {
							forceSyncTimeout += curAccDt;
						}
					}
				}
			} else {
				forceSyncTimeout += curAccDt;
			}
			if (forceSyncTimeout > 2){
				forceSyncTimeout = 0;
				clientPos = transform.position;
				plyMoveCorrection = Vector3.zero;
				UpdateGround ();
				RpcForceSync (transform.position, gravity, gravAccMove, timeSinceUngrounded, grounded, state);
				Debug.Log ("Forcing sync");
				for (int a = playerImpulseIds.Count - 1; a >= 0 ; a -= 1) {
					lastServerImpulseId++;
					serverImpulse.Add (lastServerImpulseId, playerImpulse [playerImpulseIds [a]]);
					serverImpulseIds.Add (lastServerImpulseId);
					playerImpulse.Remove (playerImpulseIds [a]);
					playerImpulseIds.RemoveAt (a);
				}
				for (int a = 0; a < serverImpulseIds.Count; a++) {
					RpcForceImpulse (serverImpulse [serverImpulseIds [a]].impulseLeft, serverImpulse [serverImpulseIds [a]].impulseDeccel, serverImpulse [serverImpulseIds [a]].impulseMoveDone, serverImpulse [serverImpulseIds [a]].time, serverImpulseIds[a]);
				}
			}
		}

	}

	void LateUpdate(){

		if (isLocalPlayer) {
			if (!isServer) {
				if (plyMoveCorrection != Vector3.zero) {

					Vector3 tempPos2 = transform.position;
					Vector3 lerpPos = Vector3.Lerp (Vector3.zero, plyMoveCorrection, 2 * Time.deltaTime);
					cc.Move (lerpPos);
					plyMoveCorrection -= transform.position - tempPos2;
				}
			}
			Vector3 curPos = transform.position;
			PlayerUpdate (myInput, Time.deltaTime);
			myInput.deltaGrav = tempGrav;
			AddGravity (tempGrav);
			AddImpulse (tempImpulse, tempImpulseDeccel, true);
			myInput.accResDeltaPos = transform.position - curPos;
			myInput.rot = transform.rotation;
			myInput.curPos = transform.position;
			myInput.state = state;
			if (isServer) {
			} else {
				if (sameInput) {
					if (CheckInput2 ()) {
						int last = inputs.Count - 1;
						inputs [last].dt += Time.deltaTime;
						inputs [last].accResDeltaPos += myInput.accResDeltaPos;
						inputs [last].rot = myInput.rot;
						inputs [last].curPos = myInput.curPos;
					} else {
						sameInput = false;
					}
				}
				if (!sameInput) {
					inputs.Add (PlyMoveInput.CopyFromInput (myInput));
					SetPrevInput ();
					SetPrevInput2 ();
				}

			}
		} else {
			if (isServer) {
			}
		}


		UpdateImpulse ();
		UpdateGravity (); //gravity update done very last to respect latency
	}

	void UpdateGravity(){
		//corrections
		if (!isServer) {
			if (gravCorrections != Vector3.zero) {
				if (CheckIfCorrectionPossibleOverTime (gravCorrections)) {
					Vector3 curPos = transform.position;
					Vector3 lerp = Vector3.Lerp (Vector3.zero, gravCorrections, 4 * Time.deltaTime);
					cc.Move (lerp);
					gravCorrections -= transform.position - curPos;
				} else {
					gravCorrections = Vector3.zero;
				}
			}
		}

		for (int a = 0; a < gravIds.Count; a++) {
			Vector3 curPos = transform.position;
			cc.Move (pendingGrav [gravIds [a]].velocity * Time.deltaTime);
			pendingGrav [gravIds [a]].accMove += transform.position - curPos;
			pendingGrav [gravIds [a]].timeDone += Time.deltaTime;
		}
		UpdateGround ();
		if (grounded) {
			if (gravity == PlyGravity.gravGround) {
				cc.Move (PlyGravity.gravGround);
			} else {
				Vector3 curPos2 = transform.position;
				cc.Move (gravity * Time.deltaTime);
				UpdateGround ();
				if (!grounded) {
					timeSinceUngrounded += Time.deltaTime;
					Vector3 curAccGrav = Physics.gravity * Time.deltaTime;
					cc.Move ((gravity + 0.5f * curAccGrav) * Time.deltaTime);
					gravAccMove += transform.position - curPos2;
					timeSinceUngrounded += Time.deltaTime;
					gravity += curAccGrav;
				}
			}
		} else {
			Vector3 curPos2 = transform.position;
			Vector3 curAccGrav = Physics.gravity * Time.deltaTime;
			cc.Move ((gravity + 0.5f * curAccGrav) * Time.deltaTime);
			gravAccMove += transform.position - curPos2;
			timeSinceUngrounded += Time.deltaTime;
			gravity += curAccGrav;
		}
		UpdateGround ();
	}

	void UpdateImpulse(){
		//corrections
		if (!isServer) {
			if (impulseCorrections != Vector3.zero) {
				if (CheckIfCorrectionPossibleOverTime (impulseCorrections)) {
					Vector3 curPos = transform.position;
					Vector3 lerp = Vector3.Lerp (Vector3.zero, impulseCorrections, 4 * Time.deltaTime);
					cc.Move (lerp);
					impulseCorrections -= transform.position - curPos;
				} else {
					impulseCorrections = Vector3.zero;
				}
			}
		}


		UpdateImpulseReal (serverImpulse, serverImpulseIds, false);
		UpdateImpulseReal (playerImpulse, playerImpulseIds, true);
	}

	void UpdateImpulseReal(Dictionary<int, Impulse> imps, List<int> impIds, bool player){
		
		for (int a = imps.Count - 1; a >= 0; a -= 1) {
			if (!isServer && imps [impIds [a]].corrections != Vector3.zero) {
				Vector3 curPos = transform.position;
				Vector3 lerp = Vector3.Lerp (Vector3.zero, imps [impIds [a]].corrections, 4 * Time.deltaTime);
				cc.Move (lerp);
				Vector3 deltaPos = transform.position - curPos;
				imps [impIds [a]].corrections -= deltaPos;
				imps [impIds [a]].impulseMoveDone += deltaPos;
			}
			if (!imps [impIds [a]].done) {
				Vector3 curPos = transform.position;
				float dt = Time.deltaTime;
				if (dt > imps [impIds [a]].timeLeft) {
					dt = imps [impIds [a]].timeLeft;
					imps [impIds [a]].timeLeft = 0;
					imps [impIds [a]].done = true;
				} else {
					imps [impIds [a]].timeLeft -= dt;
				}
				Vector3 newImpleft = Vector3.MoveTowards (imps [impIds [a]].impulseLeft, Vector3.zero, imps [impIds [a]].impulseDeccel * dt);
				cc.Move ((imps [impIds [a]].impulseLeft + newImpleft) * 0.5f * dt);
				imps [impIds [a]].impulseMoveDone += transform.position - curPos;
				imps [impIds [a]].time = Mathf.Clamp (imps [impIds [a]].time + dt, 0, imps [impIds [a]].endTime);
				if (newImpleft == Vector3.zero) {
					imps [impIds [a]].done = true;
				}
				if (imps [impIds [a]].done && isServer) {
					if (!isLocalPlayer) {
						if (player) {
							RpcSendImpulseDonePlayer (impIds [a], imps [impIds [a]].impulseMoveDone);
						} else {
							RpcSendImpulseDoneServer (impIds [a], imps [impIds [a]].impulseMoveDone);
						}
					}

					imps.Remove (impIds [a]);
					impIds.RemoveAt (a);
				}
			}
		}
	}

	bool AddImpulse(Vector3 addImp, float addDeccel, bool fromPlayer = false){
		if (addImp == Vector3.zero) {
			return false;
		}

		if (addDeccel <= 0) {
			return false;
		}

		if (myInput.impulseId != int.MinValue) {
			return false;
		}

		if (!isServer || fromPlayer) {
			lastClientImpulseId++;
			playerImpulse.Add (lastClientImpulseId, new Impulse (addImp, addDeccel));
			playerImpulseIds.Add (lastClientImpulseId);
			myInput.impulseId = lastClientImpulseId;
		}else{
			lastServerImpulseId++;
			serverImpulse.Add (lastServerImpulseId, new Impulse (addImp, addDeccel));
			serverImpulseIds.Add (lastServerImpulseId);
			RpcSendImpulse (addImp, addDeccel, lastServerImpulseId);
		}
		return true;

	}

	bool AddImpulse(Vector3 addImp, float addDeccel, int id, bool fromPlayer = false){
		if (addImp == Vector3.zero) {
			return false;
		}

		if (addDeccel <= 0) {
			return false;
		}

		if (isLocalPlayer && myInput.impulseId != int.MinValue) {
			return false;
		}

		if (!isServer || fromPlayer) {
			lastClientImpulseId = id;
			playerImpulse.Add (id, new Impulse (addImp, addDeccel));
			playerImpulseIds.Add (id);
			myInput.impulseId = id;
		}else{
			lastServerImpulseId = id;
			serverImpulse.Add (id, new Impulse (addImp, addDeccel));
			serverImpulseIds.Add (id);
			RpcSendImpulse (addImp, addDeccel, id);
		}
		return true;

	}

	bool AddGravity(Vector3 addGrav){
		if (addGrav == Vector3.zero) {
			return false;
		}
		if (isLocalPlayer && myInput.gravId != int.MinValue) {
			return false;
		}
		Vector3 curPos = transform.position;
		cc.Move (addGrav * Time.fixedDeltaTime);
		UpdateGround ();
		if (isServer) {
			gravity += addGrav;
			gravAccMove += transform.position - curPos;
		} else {
			lastGravityId++;
			pendingGrav.Add (lastGravityId, new PlyGravity (addGrav, transform.position - curPos, timeSinceUngrounded));
			gravIds.Add (lastGravityId);
			myInput.gravId = lastGravityId;
		}
		return true;
	}

	bool UpdateGround(){
		if (grounded && gravity == PlyGravity.gravGround && pendingGrav.Count == 0) {
			cc.Move (PlyGravity.gravGround);
		}
		if (cc.isGrounded != grounded) {
			if (grounded) {
				timeSinceUngrounded = 0;
				gravAccMove = Vector3.zero;
			}else{
				for (int a = gravIds.Count - 1; a >= 0; a -= 1) {
					gravAccMove += pendingGrav [gravIds[a]].accMove;
					pendingGrav.Remove (gravIds [a]);
					gravIds.RemoveAt (a);
				}
				gravity = PlyGravity.gravGround;
				lastDir = Vector3.zero;
				if (isServer && !isLocalPlayer) {
					RpcSendGravDone (gravAccMove);
				}
			}
			grounded = cc.isGrounded;
		}
		return grounded;
	}


	bool UpdateInput(){
		myInput.dir = Vector3.ClampMagnitude (new Vector3 (Joystick.padAxis.x + Input.GetAxis ("Horizontal"), 0, Joystick.padAxis.y + Input.GetAxis ("Vertical")), 1);

		if (CrossPlatformInputManager.GetButtonDown ("JumpBt")) 
			myInput.jumpBt = 1;
		else if (CrossPlatformInputManager.GetButton ("JumpBt"))
			myInput.jumpBt = 2;
		else if (CrossPlatformInputManager.GetButtonUp ("JumpBt"))
			myInput.jumpBt = 3;
		else
			myInput.jumpBt = 0;

		if (CrossPlatformInputManager.GetButtonDown ("DashBt"))
			myInput.dashBt = 1;
		else if (CrossPlatformInputManager.GetButton ("DashBt"))
			myInput.dashBt = 2;
		else if (CrossPlatformInputManager.GetButtonUp ("DashBt"))
			myInput.dashBt = 3;
		else
			myInput.dashBt = 0;

		if (CrossPlatformInputManager.GetButtonDown ("SkillBt"))
			myInput.skillBt = 1;
		else if (CrossPlatformInputManager.GetButton ("SkillBt"))
			myInput.skillBt = 2;
		else if (CrossPlatformInputManager.GetButtonUp ("SkillBt"))
			myInput.skillBt = 3;
		else
			myInput.skillBt = 0;

		tempGrav = Vector3.zero;
		tempImpulse = Vector3.zero;
		tempImpulseDeccel = 0;
		myInput.dt = Time.deltaTime;
		myInput.deltaGrav = Vector3.zero;
		myInput.impulseId = int.MinValue;
		myInput.gravId = int.MinValue;
		return CheckInput ();
	}

	bool CheckInputAll(){
		return (CheckInput () && CheckInput2 ());
	}

	bool CheckInput(){
		if (!isServer && inputs.Count == 0)
			return false;
		else if (myInput.dir != prevInput.dir) 
			return false;
		else if (myInput.dashBt != prevInput.dashBt) 
			return false;
		else if (myInput.jumpBt != prevInput.jumpBt) 
			return false;
		else if (myInput.skillBt != prevInput.skillBt) 
			return false;
		else
			return true;
	}

	bool CheckInput2(){
		if (!isServer && inputs.Count == 0)
			return false;
		else if (myInput.deltaGrav != prevInput.deltaGrav)
			return false;
		else if (myInput.gravId != prevInput.gravId)
			return false;
		else if (myInput.impulseId != prevInput.impulseId)
			return false;
		else if (myInput.state != prevInput.state)
			return false;
		else
			return true;
	}

	void SetPrevInput(){
		prevInput.dir = myInput.dir;
		prevInput.dashBt = myInput.dashBt;
		prevInput.jumpBt = myInput.jumpBt;
		prevInput.skillBt = myInput.skillBt;
	}

	void SetPrevInput2(){
		prevInput.deltaGrav = myInput.deltaGrav;
		prevInput.gravId = myInput.gravId;
		prevInput.impulseId = myInput.impulseId;
		prevInput.state = myInput.state;
	}

	void PlayerUpdate(PlyMoveInput input){
		PlayerUpdate (input, input.dt);
	}

	void PlayerUpdate(PlyMoveInput input, float dt){
		UpdateGround ();
		if (grounded) {
			if (input.jumpBt == 1) {
				tempGrav += -PlyGravity.gravNorm * 2 * mvSpd;
				lastDir = input.dir;
			} else if (input.dashBt == 1) {
				tempImpulse += input.dir * 4 * mvSpd;
				tempImpulseDeccel += 8 * mvSpd;
			}else{
				cc.Move (input.dir * mvSpd * dt);
			}
		} else {
			cc.Move (lastDir * mvSpd * dt);
		}
	}


	[Command]
	void CmdSendInput(Vector3 newDir, sbyte newJBt, sbyte newDBt, sbyte newSBt, float newDt, Vector3 newRDP, Quaternion newRot, Vector3 newDeltaGrav, int newImp, int newGravId, float newTime, Vector3 newCurPos, int newState){
		inputs.Add (new PlyMoveInput (newDir, newJBt, newDBt, newSBt, newDt, newRDP, newRot, newDeltaGrav, newImp, newGravId, newTime, newCurPos, newState));
	}

	[ClientRpc]
	void RpcSendPlyCorrection(Vector3 cor){
		if (isLocalPlayer) {
			plyMoveCorrection += cor;
			if (!CheckIfCorrectionPossibleOverTime (plyMoveCorrection)) {
				plyMoveCorrection = Vector3.zero;
			}
		}
	}

	[ClientRpc]
	void RpcSendGravCorrection(int id, Vector3 cor){
		if (isLocalPlayer) {
			if (pendingGrav.ContainsKey (id)) {
				gravAccMove += pendingGrav [id].accMove;
					if (cor == Vector3.zero) {
						gravity += pendingGrav [id].velocity;
						//gravAccMove += pendingGrav [id].accMove;
					} else {
						Vector3 correctedGrav = pendingGrav [id].velocity + cor;
						gravity += correctedGrav;
						Vector3 corMove = correctedGrav * pendingGrav [id].timeDone;
						//Vector3 shouldMove = pendingGrav [id].velocity * pendingGrav [id].timeDone;
						if (pendingGrav [id].accMove.sqrMagnitude >= corMove.sqrMagnitude || Vector3.Dot (corMove, pendingGrav [id].accMove) <= 0) {
							Vector3 correction = cor * pendingGrav [id].timeDone;
							if (CheckIfCorrectionPossibleOverTime (correction)) {
								gravCorrections += correction;
							}
						} else {
							Vector3 correction = -pendingGrav [id].accMove;
							if (CheckIfCorrectionPossibleOverTime (correction)) {
								gravCorrections += correction;
							}
						}
					}
				pendingGrav.Remove (id);
				gravIds.Remove (id);

			}
		}
	}

	[ClientRpc]
	void RpcSendGravDecline(int id){
		if (isLocalPlayer) {
			if (pendingGrav.ContainsKey (id)) {
				Vector3 correction = -pendingGrav [id].accMove;

				if (pendingGrav [id].applyTimeSinceUngrounded == 0) {
					correction += -gravAccMove;
				}

				if (CheckIfCorrectionPossibleOverTime (correction)) {
					gravCorrections += correction;
				}
				pendingGrav.Remove (id);
				gravIds.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendAddGrav(Vector3 grav, float time){
		if (isLocalPlayer) {
			StartCoroutine(WaitForGravTime(grav, time));
		}
	}
	
	IEnumerator WaitForGravTime(Vector3 grav, float time){
		while (true){
			if (timeSinceUngrounded >= time){
			Vector3 curPos = transform.position;
			gravity += grav;
			cc.Move (grav * Time.fixedDeltaTime);
			gravAccMove += transform.position - curPos;
			gravCorrections += grav * (timeSinceUngrounded - time);
			UpdateGround ();
			}
		}
	}

	[ClientRpc]
	void RpcSendGravDone(Vector3 accMove){
		if (isLocalPlayer) {
			StartCoroutine (WaitForGroundedForCheck (accMove));
		}
	}

	[ClientRpc]
	void RpcSendImpulseCorrection(Vector3 imp, float deccel, int id){
		if (isLocalPlayer) {
			if (playerImpulse.ContainsKey (id)) {
				if (playerImpulse [id].impulseStart != imp || playerImpulse [id].impulseDeccel != deccel) {
					float endTime = imp.magnitude / deccel;
					float playerTime = playerImpulse [id].time;
					float time = playerTime;
					if (time > endTime) {
						time = endTime;
					}
					Vector3 trueShouldDone = imp * time - imp.normalized * deccel * time * time;
					Vector3 playerDone = playerImpulse [id].impulseMoveDone;
					Vector3 playerShouldDone = playerImpulse [id].impulseStart * playerTime - playerImpulse [id].impulseStart.normalized * playerImpulse [id].impulseDeccel * playerTime * playerTime;
					float sqPlayerDoneMag = playerDone.sqrMagnitude;

					if (Vector3.Dot (playerDone, trueShouldDone) > 0) {
						if (sqPlayerDoneMag > trueShouldDone.sqrMagnitude) {
							playerImpulse [id].corrections += trueShouldDone - playerDone;
						} else if (sqPlayerDoneMag >= playerShouldDone.sqrMagnitude) {
							playerImpulse [id].corrections += trueShouldDone - playerDone;
						}
					}

					playerImpulse [id].impulseStart = imp;
					playerImpulse [id].impulseDeccel = deccel;
					playerImpulse [id].time = time;
					playerImpulse [id].endTime = endTime;
					if (playerTime > endTime) {
						playerImpulse [id].timeLeft = 0;
						playerImpulse [id].impulseLeft = Vector3.zero;
						playerImpulse [id].done = true;
					} else {
						playerImpulse [id].impulseLeft = Vector3.MoveTowards (imp, Vector3.zero, deccel * time);
						playerImpulse [id].timeLeft = Mathf.Clamp(endTime - time, 0, Mathf.Infinity);
						if (playerTime < endTime) {
							playerImpulse [id].done = false;
						}
					}
				}
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulseDecline(int id){
		if (isLocalPlayer) {
			if (playerImpulse.ContainsKey (id)) {
				impulseCorrections += -playerImpulse [id].impulseMoveDone;
				playerImpulse.Remove (id);
				playerImpulseIds.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulseDonePlayer(int id, Vector3 accMove){
		if (playerImpulse.ContainsKey (id)) {
			StartCoroutine (WaitForImpulseDonePlayer (id, accMove));
		} else {
			impulseCorrections += accMove;
		}
	}

	[ClientRpc]
	void RpcSendImpulseDoneServer(int id, Vector3 accMove){
		if (serverImpulse.ContainsKey (id)) {
			StartCoroutine (WaitForImpulseDoneServer (id, accMove));
		} else {
			impulseCorrections += accMove;
		}
	}

	IEnumerator WaitForImpulseDonePlayer(int id, Vector3 accMove){
		while (true) {
			if (playerImpulse.ContainsKey (id)) {
				if (playerImpulse [id].done) {
					impulseCorrections += playerImpulse [id].impulseMoveDone - accMove;
					playerImpulse.Remove (id);
					playerImpulseIds.Remove (id);
					yield break;
				}
			} else {
				impulseCorrections += accMove;
				yield break;
			}
			yield return new WaitForSeconds (0.1f);
		}
	}

	IEnumerator WaitForImpulseDoneServer(int id, Vector3 accMove){
		while (true) {
			if (serverImpulse.ContainsKey (id)) {
				if (serverImpulse [id].done) {
					impulseCorrections += playerImpulse [id].impulseMoveDone - accMove;
					serverImpulse.Remove (id);
					serverImpulseIds.Remove (id);
					yield break;
				}
			} else {
				impulseCorrections += accMove;
				yield break;
			}
			yield return new WaitForSeconds (0.1f);
		}
	}

	[ClientRpc]
	void RpcForceSync(Vector3 pos, Vector3 grav, Vector3 gravAcc, float timeUnground, bool newGrounded, int newState){
		if (isLocalPlayer) {
			Debug.Log ("Forced sync");
			transform.position = pos;
			playerImpulse.Clear ();
			playerImpulseIds.Clear ();
			serverImpulse.Clear ();
			serverImpulseIds.Clear ();
			UpdateGround ();
			gravity = grav;
			gravAccMove = gravAcc;
			plyMoveCorrection = Vector3.zero;
			gravCorrections = Vector3.zero;
			impulseCorrections = Vector3.zero;
			timeSinceUngrounded = timeUnground;
			grounded = newGrounded;
			state = newState;
		}
	}

	[ClientRpc]
	void RpcForceImpulse(Vector3 impulseLeft, float deccel, Vector3 impulseDone, float impTime, int id){
		if (isLocalPlayer) {
			if (serverImpulse.ContainsKey (id)) {
				serverImpulse [id].impulseLeft = impulseLeft;
				serverImpulse [id].impulseDeccel = deccel;
				serverImpulse [id].time = impTime;
				serverImpulse [id].serverImpulseMoveDone = impulseDone;
				serverImpulse [id].impulseMoveDone = impulseDone;
			} else {
				serverImpulseIds.Add (id);
				serverImpulse.Add (id, new Impulse (impulseLeft, deccel, impulseDone, impTime));
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulse(Vector3 impulse, float deccel, int id){
		if (isLocalPlayer) {
			serverImpulseIds.Add (id);
			serverImpulse.Add (id, new Impulse (impulse, deccel));
		}
	}

	IEnumerator WaitForGroundedForCheck(Vector3 accMove){
		for (int a = 0; a < 600; a++) {
			if (grounded) {
				gravCorrections += accMove - gravAccMove;
				Debug.Log ("Grav Correction " + a + " " + gravCorrections);
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
	}

	bool CheckIfCorrectionPossibleOverTime(Vector3 cor){
		if (Physics.CapsuleCast (transform.position + transform.up * capsuleHalfHeight, 
			transform.position - transform.up * capsuleHalfHeight, 
			cc.radius - cc.skinWidth, 
			cor.normalized, 
			cor.magnitude, 
			staticCols,
			QueryTriggerInteraction.Ignore)) {
			Debug.Log ("Teleport correction");
			transform.position = transform.position + cor;
			UpdateGround ();
			return false;
		}
		return true;
	}

}