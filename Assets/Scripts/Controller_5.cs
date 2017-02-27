using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

public class Controller_5 : NetworkBehaviour {
	public const int JumpBt = 0;
	public const int DashBt = 1;

	public const byte ButtonUnpressed = 0;
	public const byte ButtonDown = 1;
	public const byte ButtonHold = 2;
	public const byte ButtonUp = 3;
	public const byte ButtonClick = 4;
	public const byte ButtonDoubleClickHold = 5;
	public const byte ButtonDoubleClickUp = 6;
	public const byte ButtonHoldWaitUp = 7;
	public const byte ButtonUpWaitDown = 8;
	public const byte ButtonUpWaitClick = 9;

	const float DummySyncInterval = 0.12f;

	public class PlyMoveInput{
		public  const float inputInterval = 0.12f;
		public  const int btLength = 2;
		public  const int updates = 3;
		public  static string[] btString = new string[]{"JumpBt", "DashBt"};
		public byte timeStamp;
		public Vector3 dir;
		public byte[] bts;
		public int updatesDone;
		public Quaternion rot;

		public PlyMoveInput(){
			timeStamp = 0;
			dir = Vector3.zero;
			bts = new byte[btLength];
			for (int a = 0; a <btLength; a++){
				bts[a] = 0;
			}
			updatesDone = 0;
			rot = Quaternion.identity;
		}

		public void Reset(){
			dir = Vector3.zero;
			for (int a = 0; a <btLength; a++){
				bts[a] = 0;
			}
			rot = Quaternion.identity;
		}

		public void Next(){
			UpdateButtons ();
		}

		public PlyMoveInput(byte newTS, Vector3 newDir, byte newJBt, byte newDBt){
			timeStamp = newTS;
			dir = newDir;
			bts = new byte[]{newJBt, newDBt};
			rot = Quaternion.identity;
		}

		public void CopyFromInputToThis(PlyMoveInput parent){
			timeStamp = parent.timeStamp;
			dir = parent.dir;
			for (int a = 0; a < btLength; a++) {
				bts [a] = parent.bts [a];
			}
			updatesDone = parent.updatesDone;
			rot = parent.rot;
		}

		public bool CheckEqual(PlyMoveInput b){
			return PlyMoveInput.CheckEqual (this, b);
		}

		public static bool CheckEqual(PlyMoveInput a, PlyMoveInput b){
			if (a.dir != b.dir)
				return false;
			for (int c = 0; c < btLength; c++) {
				if (a.bts [c] != b.bts [c])
					return false;
			}
			if (a.rot != b.rot)
				return false;
			return true;
		}

		public void UpdateDone(){
			Next ();
			updatesDone++;
		}

		public void UpdateButtons(){
			for (int a = 0; a < PlyMoveInput.btLength; a++) {
				switch (bts [a]) {
				case ButtonDown:
					{
						bts [a] = ButtonHold;
						break;
					}
				case ButtonUp:
					{
						bts [a] = ButtonUnpressed;
						break;
					}
				case ButtonClick:
					{
						bts [a] = ButtonUp;
						break;
					}
				case ButtonDoubleClickHold:
					{
						bts [a] = ButtonUpWaitDown;
						break;
					}
				case ButtonDoubleClickUp:
					{
						bts [a] = ButtonUpWaitClick;
						break;
					}
				case ButtonUpWaitDown:{
						bts[a] = ButtonDown;
						break;
					}
				case ButtonUpWaitClick:{
						bts [a] = ButtonClick;
						break;
					}
				}

			}
		}

		public static PlyMoveInput CopyFromInput(PlyMoveInput parent){
			PlyMoveInput ret = new PlyMoveInput ();
			ret.CopyFromInputToThis (parent);
			return ret;
		}

		public void UnpackInputToThis(PackedInput pack){
			timeStamp = pack.timeStamp;
			dir = new Vector3 (pack.dir.x, 0, pack.dir.y);
			int btLeft = (int)pack.bts;
			int mod = 1;
			int mod2 = 1;
			for (int a = 0; a < btLength; a++) {
				mod *= 10;
				int bt = btLeft % mod;
				btLeft -= bt;
				bts[a] = (byte)(bt / mod2);
				mod2 *= 10;
			}
			rot = pack.rot;
		}

		public static PlyMoveInput UnpackInput(PackedInput pack){
			PlyMoveInput ret = new PlyMoveInput ();
			ret.UnpackInputToThis (pack);
			return ret;

		}
	}

	public struct PackedInput{
		public byte timeStamp;
		public Vector2 dir;
		public ushort bts;
		public Quaternion rot;

		public PackedInput(byte newTS, Vector2 newDir, ushort newBts, Quaternion newRot){
			timeStamp = newTS;
			dir = newDir;
			bts = newBts;
			rot = newRot;
		}

		public static PackedInput PackInput(PlyMoveInput parent){
			ushort newBts = 0;
			for (int a = 0; a < parent.bts.Length; a++){
				ushort bt = (ushort)parent.bts [a];
				for (int b = 0; b < a; b++){
					bt *= 10;
				}
				newBts = (ushort)(newBts + bt);
			}

			return new PackedInput (parent.timeStamp, new Vector2 (parent.dir.x, parent.dir.z), newBts, parent.rot);
		}
	}

	public class PlyMoveOutput{
		public const int updates = 3;
		public Vector3 accResDeltaPos;
		public Quaternion rot;
		public byte[] gravIds; 
		public byte[] impulseIds;
		public byte[] tMoveIds;

		public PlyMoveOutput(){
			accResDeltaPos = Vector3.zero;
			rot = Quaternion.identity;
			gravIds = new byte[updates];
			impulseIds = new byte[updates];
			tMoveIds = new byte[updates];
			for (int a = 0; a < updates; a++){
				gravIds[a] = 0;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
		}

		public void CopyFromOutputToThis(PlyMoveOutput parent){
			accResDeltaPos = parent.accResDeltaPos;
			rot = parent.rot;
			for (int a = 0; a < updates; a++){
				gravIds[a] = parent.gravIds[a];
				impulseIds[a] = parent.impulseIds[a];
				tMoveIds [a] = parent.tMoveIds [a];
			}
		}

		public static PlyMoveOutput CopyFromOutput(PlyMoveOutput parent){
			PlyMoveOutput ret = new PlyMoveOutput ();
			ret.CopyFromOutputToThis(parent);
			return ret;
		}

		public void Clean(){
			accResDeltaPos = Vector3.zero;
			for (int a = 0; a < updates; a++){
				gravIds[a] = 0;
				impulseIds[a] = 0;
				tMoveIds [a] = 0;
			}
		}

		public bool CheckEqual(PlyMoveOutput b){
			return PlyMoveOutput.CheckEqual(this, b);
		}

		public static bool CheckEqual(PlyMoveOutput a, PlyMoveOutput b){
			if (a.accResDeltaPos != b.accResDeltaPos)
				return false;
			if (a.rot != b.rot)
				return false;
			for (int c = 0; c < updates; c++) {
				if (a.gravIds [c] != b.gravIds [c])
					return false;
				if (a.impulseIds [c] != b.impulseIds [c])
					return false;
				if (a.tMoveIds [c] != b.tMoveIds [c])
					return false;
			}
			return true;
		}

		public void UnpackOutputToThis(PackedOutput pack){
			accResDeltaPos = pack.accResDeltaPos;
			rot = pack.rot;
			string[] newIds = pack.ids.Split ('@');
			string[] newGrav = newIds[0].Split('#');
			string[] newImp = newIds[1].Split('#');
			string[] newTM = newIds[2].Split ('#');
			for (int a = 0; a < updates; a++){
				gravIds[a] = byte.Parse(newGrav[a]);
				impulseIds[a] = byte.Parse(newImp[a]);
				tMoveIds [a] = byte.Parse (newTM [a]);
			}
		}

		public static PlyMoveOutput UnpackOutput(PackedOutput pack){
			PlyMoveOutput ret = new PlyMoveOutput ();
			ret.UnpackOutputToThis (pack);
			return ret;
		}
	}

	public struct PackedOutput{
		public Vector3 accResDeltaPos;
		public Quaternion rot;
		public string ids;

		public static PackedOutput PackOutput(PlyMoveOutput parent){
			PackedOutput ret = new PackedOutput ();
			ret.accResDeltaPos = parent.accResDeltaPos;
			ret.rot = parent.rot;
			for (int a = 0; a < PlyMoveInput.updates; a++) {
				if (a != 0) {
					ret.ids += "#";
				}
				ret.ids += parent.gravIds [a].ToString ();
			}
			ret.ids += "@";
			for (int a = 0; a < PlyMoveInput.updates; a++) {
				if (a != 0) {
					ret.ids += "#";
				}
				ret.ids += parent.impulseIds [a].ToString ();
			}
			ret.ids += "@";
			for (int a = 0; a < PlyMoveInput.updates; a++) {
				if (a != 0) {
					ret.ids += "#";
				}
				ret.ids += parent.tMoveIds [a].ToString ();
			}
			return ret;
		}
	}

	public class ServerOutput{
		public const int updates = 3;
		public bool done;
		public Vector3 accResDeltaPos;
		public Quaternion rot;
		public PlyGravity[] gravs;
		public byte[] impulseIds;
		public byte[] tMoveIds;

		public ServerOutput(){
			done = false;
			accResDeltaPos = Vector3.zero;
			rot = Quaternion.identity;
			gravs = new PlyGravity[updates];
			impulseIds = new byte[updates];
			tMoveIds = new byte[updates];
			for (int a = 0; a < updates; a++){
				gravs[a] = null;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
		}

		public ServerOutput(Quaternion newRot){
			done = false;
			accResDeltaPos = Vector3.zero;
			rot = newRot;
			gravs = new PlyGravity[updates];
			impulseIds = new byte[updates];
			tMoveIds = new byte[updates];
			for (int a = 0; a < updates; a++){
				gravs[a] = null;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
		}

		public ServerOutput(bool newDone, Quaternion newRot){
			done = newDone;
			accResDeltaPos = Vector3.zero;
			rot = newRot;
			gravs = new PlyGravity[updates];
			impulseIds = new byte[updates];
			tMoveIds = new byte[updates];
			for (int a = 0; a < updates; a++){
				gravs[a] = null;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
		}

		public ServerOutput(bool newDone){
			done = newDone;
			accResDeltaPos = Vector3.zero;
			rot = Quaternion.identity;
			gravs = new PlyGravity[updates];
			impulseIds = new byte[updates];
			tMoveIds = new byte[updates];
			for (int a = 0; a < updates; a++){
				gravs[a] = null;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
		}

		public void Clean(){
			done = false;
			accResDeltaPos = Vector3.zero;
			for (int a = 0; a < updates; a++){
				gravs[a] = null;
				impulseIds[a] = 0;
				tMoveIds[a] = 0;
			}
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
		public bool done;
		public float endTime;
		public float timeLeft;
		public Impulse(Vector3 newImp, float newDeccel){
			impulseStart = newImp;
			impulseLeft = newImp;
			impulseDeccel = newDeccel;
			time = 0;
			impulseMoveDone = Vector3.zero;
			done = false;
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
			done = false;
			corrections = Vector3.zero;
			timeLeft = newImp.magnitude / newDeccel;
			endTime = time + timeLeft;
		}
	}

	public class TimedMove{
		public Vector3 velocity;
		public float timeLeft;
		public Vector3 accMove;
		public float timeDone;
		public Vector3 corrections;
		public bool done;

		public TimedMove(Vector3 newVel, float newTime){
			velocity = newVel;
			accMove = Vector3.zero;
			timeDone = 0;
			timeLeft = newTime;
			corrections = Vector3.zero;
			done = false;
		}

		public TimedMove(Vector3 newVel, float newTime, Vector3 newAcc, float newTD){
			velocity = newVel;
			timeLeft = newTime;
			accMove = newAcc;
			timeDone = newTD;
			corrections = Vector3.zero;
			done = false;
		}
	}

	public class MyState
	{
		public Vector3 pos;
		public Quaternion rot;
		public byte state;
		public bool grounded;

		public MyState(){
			pos = Vector3.zero;
			rot = Quaternion.identity;
			state = 0;
			grounded = false;
		}

		public MyState( Vector3 newPos, Quaternion newRot, byte newState, bool newGrounded){
			pos = newPos;
			rot = newRot;
			state = newState;
			grounded = newGrounded;
		}

		public bool CheckEqual(MyState b){
			return CheckEqual (this, b);
		}

		public static bool CheckEqual(MyState a, MyState b){
			if (a.grounded != b.grounded)
				return false;
			if (a.state != b.state)
				return false;
			if (a.pos != b.pos)
				return false;
			if (a.rot != b.rot)
				return false;
			return true;
		}

		public void CopyFromStateToThis(MyState parent){
			pos = parent.pos;
			rot = parent.rot;
			state = parent.state;
			grounded = parent.grounded;
		}

		public static MyState CopyFromState(MyState parent){
			MyState ret = new MyState ();
			ret.CopyFromStateToThis (parent);
			return ret;
		}
	}

	//links
	public static Controller_5 player = null;
	[SerializeField]
	public CharacterController cc = null;
	[SerializeField]
	public ObjInfo info = null;
	[SerializeField]
	public GameObject camHold = null;

	//player attr
	public float mvSpd = 5;
	public float angularSpeed = 270;
	float sqMinMove = 0.000001f;
	float capsuleHalfHeight = 0.5f;
	public byte state = 0;

	//player move
	byte myTimeStamp = 0;
	int skipFU = 0;
	bool inited = false;
	PlyMoveInput myInput = new PlyMoveInput();
	PlyMoveInput pendingInput = new PlyMoveInput();
	PlyMoveInput lastSentRecInput = new PlyMoveInput();
	PlyMoveOutput myPlyOutput = new PlyMoveOutput();
	bool waitingForInput = false;
	List<PlyMoveInput> inputs = new List<PlyMoveInput>();
	Vector3 plyMoveCorrection = Vector3.zero;


	//player gravity

	public bool grounded = false;
	Vector3 gravity = Vector3.zero;
	Vector3 gravAccMove = Vector3.zero;
	float timeSinceUngrounded = 0;

	byte lastGravityId = 0;

	Vector3 tempGrav = Vector3.zero;

	List<byte> gravIds = new List<byte>();
	Dictionary<byte, PlyGravity> pendingGrav = new Dictionary<byte, PlyGravity> (); 
	List<Vector3> gravAccs = new List<Vector3>();
	Vector3 gravCorrections = Vector3.zero;

	//impulses
	Dictionary<byte, Impulse> playerImpulse = new Dictionary<byte, Impulse>();
	Dictionary<byte, Impulse> serverImpulse = new Dictionary<byte, Impulse>();
	Vector3 impulseCorrections = Vector3.zero;
	byte lastClientImpulseId = 0;
	byte lastServerImpulseId = 0;
	List<byte> playerImpulseIds = new List<byte>();
	List<byte> serverImpulseIds = new List<byte>();

	//timedmoves
	Dictionary<byte, TimedMove> playerTMove = new Dictionary<byte, TimedMove>();
	Dictionary<byte, TimedMove> serverTMove = new Dictionary<byte, TimedMove>();
	Vector3 tMoveCorrections = Vector3.zero;
	byte lastClientTMoveId = 0;
	byte lastServerTMoveId = 0;
	List<byte> playerTMoveIds = new List<byte>();
	List<byte> serverTMoveIds = new List<byte>();

	//Server process
	const float maxSavedDt = 0.2f;
	float savedDt = 0;
	float accFixedDt = 0;
	float accUpdateDt = 0;
	float accInputDt = 0;
	bool addInputs = false;
	bool sendInputs = false;
	/// Stuff are done in FixedUpdate, 
	/// results are stored and then lerped by time to fake smoothness.
	MyState curState = new MyState();
	List<MyState> pendingState = new List<MyState> ();
	MyState lastSentRecState = new MyState();
	Vector3 clientPos = Vector3.zero;
	bool lostSync = false;
	float forceSyncTimeout = 0;
	const float SyncCheckInterval = 0.5f;
	Dictionary<byte, ServerOutput> serverOutputs = new Dictionary<byte, ServerOutput>();
	byte forceSyncId = 0;

	//Network
	bool waitingForNetwork = false;

	//Dummy 
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

		curState.pos = transform.position;
		curState.rot = transform.rotation;
		lastSentRecState.pos = transform.position;
		lastSentRecState.rot = transform.rotation;

		myInput.updatesDone = 3;
		myInput.rot = transform.rotation;
		lastSentRecInput.rot = transform.rotation;
		pendingInput.rot = transform.rotation;
		clientPos = transform.position;


		inputs.Add (new PlyMoveInput ());

		PlyGravity.gravMag = Physics.gravity.magnitude;
		PlyGravity.gravNorm = Physics.gravity / PlyGravity.gravMag;
		PlyGravity.gravGround = PlyGravity.gravNorm * 1;

		gravity = PlyGravity.gravGround;

		if (!CheckNetwork ()) {
			waitingForNetwork = true;
			StartCoroutine (WaitForNetwork ());
		}
	}



	IEnumerator WaitForNetwork(){
		for (int a = 0; a < 108000 && waitingForNetwork; a++){
			if (isServer) {
				if (isLocalPlayer) {
					//StartCoroutine (AddInputs (PlyMoveInput.inputInterval));
					addInputs = true;
					//AddInput ();
					inited = true;
					StartCoroutine (SendDummyCoroutine (DummySyncInterval));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					PlayerStart ();
					yield break;
				} else {
					//StartCoroutine (SendResultsClient (0.1f));
					StartCoroutine (SendDummyCoroutine (DummySyncInterval));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					PlayerStart ();
					yield break;
				}
			}else{
				if (isLocalPlayer) {
					//StartCoroutine (SendInputs (PlyMoveInput.inputInterval));
					sendInputs = true;
					//SendInput ();
					inited = true;
					StartCoroutine (CheckSync (0.5f));
					waitingForNetwork = false;
					StopCoroutine (WaitForNetwork ());
					PlayerStart ();
					yield break;
				}
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
		PlayerStart ();
	}

	bool CheckNetwork(){
		if (isServer) {
			if (isLocalPlayer) {
				//StartCoroutine (AddInputs (PlyMoveInput.inputInterval));
				addInputs = true;
				//AddInput ();
				StartCoroutine (SendDummyCoroutine (DummySyncInterval));
				waitingForNetwork = false;
				StopCoroutine (WaitForNetwork ());
				PlayerStart ();
				return true;
			} else {
				//StartCoroutine (SendResultsClient (0.1f));
				StartCoroutine (SendDummyCoroutine (DummySyncInterval));
				waitingForNetwork = false;
				StopCoroutine (WaitForNetwork ());
				PlayerStart ();
				return true;
			}
		}else{
			if (isLocalPlayer) {
				//StartCoroutine (SendInputs (PlyMoveInput.inputInterval));
				sendInputs = true;
				//SendInput ();
				StartCoroutine (CheckSync (0.5f));
				waitingForNetwork = false;
				StopCoroutine (WaitForNetwork ());
				PlayerStart ();
				return true;
			}
		}
		return false;
	}


	IEnumerator CheckSync(float interval){
		while (true) {
			if (pendingState.Count > 0) {
				CmdCheckSync (pendingState [pendingState.Count - 1].pos, pendingState [pendingState.Count - 1].state, forceSyncId);
			} else {
				CmdCheckSync (transform.position, state, forceSyncId);
			}
			yield return new WaitForSecondsRealtime (interval);
		}
	}


	IEnumerator SendDummyCoroutine(float interval){
		while (true) {
			MyState leState = curState;
			if (pendingState.Count > 0) {
				leState = pendingState [pendingState.Count - 1];
			} 
			if (leState.CheckEqual (lastSentRecState)) {
				//RpcSameDummy (myInput.timeStamp);
			} else {
				SendDummy (leState);
			}
			yield return new WaitForSecondsRealtime (interval);
		}
	}

	void SendDummy(MyState leState){
		RpcSendDummy (myInput.timeStamp, leState.pos, leState.rot, leState.state, leState.grounded);
	}

	[ClientRpc]
	void RpcSendDummy(byte leTimeStamp, Vector3 lePos, Quaternion leRot, byte leState, bool leGrounded){
		if (!isLocalPlayer && !isServer) {
			if (!inited) {
				inited = true;
				InitAccDt ();
				pendingState.Clear ();
			}
			MyState leMyState = new MyState (lePos, leRot, leState, leGrounded);
			pendingState.Add (leMyState);
			lastSentRecState.CopyFromStateToThis(leMyState);
		}
	}

	[ClientRpc]
	void RpcSameDummy(byte ts){
		if (!isLocalPlayer && !isServer) {
			pendingState.Add (MyState.CopyFromState (lastSentRecState));
		}
	}

	IEnumerator SendResultsClient(float interval){
		while (true) {
			if (plyMoveCorrection != Vector3.zero) {
				RpcSendPlyCorrection (plyMoveCorrection, forceSyncId);
				plyMoveCorrection = Vector3.zero;
				yield return new WaitForSeconds (interval);
			} else {
				yield return new WaitForEndOfFrame ();
			}
		}
	}

	IEnumerator SendInputs(float interval){
		while (true) {
			SendInput ();
			yield return new WaitForSecondsRealtime (interval);
		}
	}

	void SendInput(){
		if (myTimeStamp == byte.MaxValue)
			myTimeStamp = 0;
		else
			myTimeStamp++;
		pendingInput.timeStamp = myTimeStamp;
		if (pendingInput.CheckEqual (lastSentRecInput)) {
			inputs.Add (PlyMoveInput.CopyFromInput (lastSentRecInput));
			inputs [inputs.Count - 1].timeStamp = myTimeStamp;
			CmdSameInput (myTimeStamp, forceSyncId);
		} else {
			lastSentRecInput.CopyFromInputToThis (pendingInput);
			inputs.Add (PlyMoveInput.CopyFromInput (pendingInput));
			CmdSendInput (PackedInput.PackInput (pendingInput), forceSyncId);
		}
		pendingInput.Next ();
	}

	IEnumerator AddInputs(float interval){
		while(true){
			AddInput ();
			yield return new WaitForSecondsRealtime (interval);
		}
	}

	void AddInput(){
		if (pendingInput.CheckEqual (lastSentRecInput)) {
			inputs.Add (PlyMoveInput.CopyFromInput (lastSentRecInput));
		} else {
			lastSentRecInput.CopyFromInputToThis (pendingInput);
			inputs.Add (PlyMoveInput.CopyFromInput (pendingInput));
		}
		pendingInput.Next ();
	}

	void FixedUpdate(){
		if (skipFU > 0){
			skipFU -= 1;
		}else{
			ActualFixedUpdate();
		}
	}

	void ActualFixedUpdate(){


		accInputDt += Time.fixedDeltaTime;
		if (accInputDt >= PlyMoveInput.inputInterval) {
			accInputDt -= PlyMoveInput.inputInterval;
			if (addInputs) {
				AddInput ();
			} else if (sendInputs) {
				SendInput ();
			}
		}

		MyState lastState = (pendingState.Count == 0) ? curState : pendingState [pendingState.Count - 1];
		if (isLocalPlayer || isServer) {
			accFixedDt += Time.fixedDeltaTime;
			savedDt = Mathf.Clamp (savedDt + Time.fixedDeltaTime, -0.24f, maxSavedDt);
			transform.position = lastState.pos;
			transform.rotation = lastState.rot;
			state = lastState.state;
			grounded = lastState.grounded;
		}
		if (isLocalPlayer && !isServer)
			PlyMoveCorUpdate (Time.fixedDeltaTime);
		UpdateImpulse (Time.fixedDeltaTime);
		UpdateTMove (Time.fixedDeltaTime);
		UpdateGravity (Time.fixedDeltaTime); 

		if (isLocalPlayer || isServer) {
			TryGetInput ();
			int doCount = Mathf.FloorToInt (savedDt / Time.fixedDeltaTime);
			int curCount = -1;
			if (savedDt >= Time.fixedDeltaTime && !waitingForInput) {
				TryGetInput ();
				if (myInput.updatesDone < 3) {


					savedDt -= Time.fixedDeltaTime;
					if (isLocalPlayer) {
						LocalUpdate (Time.fixedDeltaTime);
					} else if (isServer) {
						ServerUpdate (Time.fixedDeltaTime, -1, 1);
						//ServerUpdate (Time.fixedDeltaTime, curCount, doCount);
					}
					curCount++;
				}/* else {
					break;
				}*/
			}
			if (savedDt >= Time.fixedDeltaTime && !waitingForInput) {
				TryGetInput ();
				if (myInput.updatesDone < 3) {


					savedDt -= Time.fixedDeltaTime;
					if (isLocalPlayer) {
						LocalUpdate (Time.fixedDeltaTime);
					} else if (isServer) {
						ServerUpdate (Time.fixedDeltaTime, -1, 1);
						//ServerUpdate (Time.fixedDeltaTime, curCount, doCount);
					}
					curCount++;
				} /*else {
					break;
				}*/
			}
			if (savedDt < Time.fixedDeltaTime){
				int b = 1;
				if (myInput.updatesDone >= 3)
					b++;
				while(inputs.Count > b + 1){
					if (!serverOutputs.ContainsKey(inputs[b].timeStamp))
						serverOutputs.Add(inputs[b].timeStamp, new ServerOutput(true, transform.rotation));
					inputs.RemoveAt(b);
				}
			}
			TryGetInput ();
			pendingState.Add (new MyState (transform.position, transform.rotation, state, grounded));
		}

	}

	void TryGetInput(){
		if (myInput.updatesDone >= 3) {
			if (isServer && !isLocalPlayer && serverOutputs.ContainsKey (myInput.timeStamp)) 
				serverOutputs [myInput.timeStamp].done = true;
			if (inputs.Count > 0)
				GetInput ();
			else if (!waitingForInput) {
				waitingForInput = true;
				StartCoroutine (WaitForInput ());
			}
		}
	}

	public void Update(){
		if (isLocalPlayer) {
			UpdateInput ();
		} 
		if (isLocalPlayer || isServer || pendingState.Count > 0)
			accUpdateDt += Time.deltaTime;


		if (isLocalPlayer || isServer) {


			while (accFixedDt < accUpdateDt){
				ActualFixedUpdate();
				skipFU++;
			}
			LerpState (Time.fixedDeltaTime);
		} else {
			LerpDummyState (DummySyncInterval);
		}
	}

	void LerpState(float dtPerState){
		while (accUpdateDt >= dtPerState && pendingState.Count > 0) {
			curState = pendingState [0];
			pendingState.RemoveAt (0);
			accUpdateDt -= dtPerState;
			accFixedDt -= dtPerState;
		}
		state = curState.state;
		grounded = curState.grounded;
		if (pendingState.Count > 0) {
			float lerpT = accUpdateDt / dtPerState;
			transform.position = Vector3.Lerp (curState.pos, pendingState [0].pos, lerpT);
			transform.rotation = Quaternion.Slerp (curState.rot, pendingState [0].rot, lerpT);
		} else {
			transform.position = curState.pos;
			transform.rotation = curState.rot;
		}
	}

	void LerpDummyState(float dtPerState){
		if (pendingState.Count > 0){
			float dt = dtPerState / Mathf.Clamp(pendingState.Count - 1, 1, pendingState.Count);
			while (accUpdateDt >= dt && pendingState.Count > 0) {
				curState = pendingState [0];
				pendingState.RemoveAt (0);
				accUpdateDt -= dt;
				dt = dtPerState / Mathf.Clamp(pendingState.Count - 1, 1, pendingState.Count);
			}
			state = curState.state;
			grounded = curState.grounded;
			if (pendingState.Count > 0) {
				float lerpT = accUpdateDt / dt;
				transform.position = Vector3.LerpUnclamped (curState.pos, pendingState [0].pos, lerpT);
				transform.rotation = Quaternion.SlerpUnclamped (curState.rot, pendingState [0].rot, lerpT);
			} else {
				transform.position = curState.pos;
				transform.rotation = curState.rot;
			}
		}
	}

	[Command]
	void CmdCheckSync(Vector3 newClientPos, byte clientState, byte fsid){
		if (fsid == forceSyncId) {
			float dist = 0;
			if (pendingState.Count > 0) {
				dist = Vector3.Distance (newClientPos, pendingState [pendingState.Count - 1].pos);
			} else {
				dist = Vector3.Distance (newClientPos, transform.position);
			}
			if (state == clientState) {
				if (playerImpulse.Count == 0 && serverImpulse.Count == 0) {
					float distToPrev = Vector3.Distance (clientPos, newClientPos);
					if (distToPrev < 0.5f || distToPrev / SyncCheckInterval < 2) {
						if (grounded) {
							if (dist < 0.5f) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += SyncCheckInterval;
							}
						} else {
							if (dist < 1) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += SyncCheckInterval;
							}
						}
					} else {
						if (grounded) {
							if (dist < 1) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += SyncCheckInterval;
							}
						} else {
							if (dist < 3) {
								forceSyncTimeout = 0;
							} else {
								forceSyncTimeout += SyncCheckInterval;
							}
						}
					}
				} else {
					if (grounded) {
						if (dist < 2) {
							forceSyncTimeout = 0;
						} else {
							forceSyncTimeout += SyncCheckInterval;
						}
					} else {
						if (dist < 5) {
							forceSyncTimeout = 0;
						} else {
							forceSyncTimeout += SyncCheckInterval;
						}
					}
				}
			} else {
				forceSyncTimeout += SyncCheckInterval;
			}
			if (forceSyncTimeout > 2) {
				ForceSync ();
			}
			clientPos = newClientPos;
		}
	}

	void ForceSync(){
		if (forceSyncId == byte.MaxValue) {
			forceSyncId = 0;
		} else {
			forceSyncId++;
		}


		forceSyncTimeout = 0;
		plyMoveCorrection = Vector3.zero;
		UpdateGround ();
		if (pendingState.Count > 0) {
			RpcForceSync (pendingState[pendingState.Count - 1].pos, pendingState[pendingState.Count - 1].rot, gravity, gravAccMove, timeSinceUngrounded, pendingState[pendingState.Count - 1].grounded, pendingState[pendingState.Count - 1].state, forceSyncId);
		} else {
			RpcForceSync (transform.position, transform.rotation, gravity, gravAccMove, timeSinceUngrounded, grounded, state, forceSyncId);
		}
		Debug.Log ("Forcing sync");
		inited = false;
		//inputs.Clear ();
		//myInput.updatesDone = 3;
		//serverOutputs.Clear ();

		if (myInput.updatesDone >= 3) {
			serverOutputs.Clear ();
		} else {
			List<byte> outputids = new List<byte> (serverOutputs.Keys);
			for (int a = 0; a < outputids.Count; a++) {
				byte id = outputids [a];
				if (id != myInput.timeStamp) {
					serverOutputs.Remove (id);
				}
			}
		}

		for (int a = playerImpulseIds.Count - 1; a >= 0 ; a -= 1) {
			if (lastServerImpulseId == byte.MaxValue)
				lastServerImpulseId = 1;
			else
				lastServerImpulseId++;

			while (serverImpulse.ContainsKey (lastServerImpulseId)) {
				if (lastServerImpulseId == byte.MaxValue)
					lastServerImpulseId = 1;
				else
					lastServerImpulseId++;
			}

			serverImpulse.Add (lastServerImpulseId, playerImpulse [playerImpulseIds [a]]);
			serverImpulseIds.Add (lastServerImpulseId);
			byte id = playerImpulseIds [a];
			playerImpulseIds.RemoveAt (a);
			playerImpulse.Remove (id);
		}
		serverImpulseIds.Sort ();
		playerImpulseIds.Sort ();
		for (int a = 0; a < serverImpulseIds.Count; a++) {
			RpcForceImpulse (serverImpulse [serverImpulseIds [a]].impulseLeft, serverImpulse [serverImpulseIds [a]].impulseDeccel, serverImpulse [serverImpulseIds [a]].impulseMoveDone, serverImpulse [serverImpulseIds [a]].time, serverImpulseIds[a], forceSyncId);
		}

		for (int a = playerTMoveIds.Count - 1; a >= 0 ; a -= 1) {
			if (lastServerTMoveId == byte.MaxValue)
				lastServerTMoveId = 1;
			else
				lastServerTMoveId++;

			while (serverTMove.ContainsKey (lastServerTMoveId)) {
				if (lastServerTMoveId == byte.MaxValue)
					lastServerTMoveId = 1;
				else
					lastServerTMoveId++;
			}

			serverTMove.Add (lastServerTMoveId, playerTMove [playerTMoveIds [a]]);
			serverTMoveIds.Add (lastServerTMoveId);
			byte id = playerImpulseIds [a];
			playerTMoveIds.RemoveAt (a);
			playerTMove.Remove (id);
		}
		serverTMoveIds.Sort ();
		playerTMoveIds.Sort ();
		for (int a = 0; a < serverTMoveIds.Count; a++) {
			RpcForceTMove (serverTMove [serverTMoveIds [a]].velocity, serverTMove [serverTMoveIds [a]].accMove, serverTMove [serverTMoveIds [a]].timeLeft, serverTMoveIds[a], forceSyncId);
		}
	}

	void LocalUpdate(float dt){
		CleanTemp ();
		Vector3 curPos = transform.position;
		UpdateGround ();
		PlayerUpdate (myInput, dt);
		myPlyOutput.accResDeltaPos += transform.position - curPos;
		myPlyOutput.rot = transform.rotation;
		if (AddGravitySelfReal (tempGrav))
			myPlyOutput.gravIds [myInput.updatesDone] = lastGravityId;
		else
			myPlyOutput.gravIds [myInput.updatesDone] = 0;

		myInput.UpdateDone ();
		if (myInput.updatesDone >= 3){
			if (!isServer){
				CmdSendOutput (myInput.timeStamp, PackedOutput.PackOutput (myPlyOutput), forceSyncId);
			}
			TryGetInput ();
			myPlyOutput.Clean ();
		}
	}

	void ServerUpdate(float dt, int curCount, int doCount){
		Vector3 curPos = transform.position;
		Vector3 curPos2 = transform.position;
		CleanTemp ();
		UpdateGround ();
		PlayerUpdate (myInput, dt);

		if (!serverOutputs.ContainsKey (myInput.timeStamp)) {
			serverOutputs.Add (myInput.timeStamp, new ServerOutput (transform.rotation));
		}
		serverOutputs [myInput.timeStamp].accResDeltaPos += transform.position - curPos;
		serverOutputs [myInput.timeStamp].rot = transform.rotation;

		curPos = transform.position;
		if (AddGravitySelfReal (tempGrav)) {
			UpdateGround ();
			serverOutputs[myInput.timeStamp].gravs[myInput.updatesDone] = new PlyGravity (tempGrav, transform.position - curPos, timeSinceUngrounded);
			//curPos = transform.position;
		} else {
			serverOutputs [myInput.timeStamp].gravs [myInput.updatesDone] = null;
		}

		Vector3 deltaPos = transform.position - curPos2;

		if (doCount > 1 && curCount < doCount - 1 && pendingState.Count > curCount) {
			UpdateStatePosSinceIndex (curCount, deltaPos);
		}

		myInput.UpdateDone ();

		if (myInput.updatesDone >= 3)
			serverOutputs [myInput.timeStamp].rot = transform.rotation;

		TryGetInput ();
	}

	void UpdateStatePosSinceIndex(int startIndex, Vector3 dp){
		if (startIndex < 0) {
			UpdatePendingStatePosSinceIndex (0, dp);
			curState.pos += dp;
		} else {
			UpdatePendingStatePosSinceIndex (startIndex, dp);
		}
	}


	void UpdatePendingStatePosSinceIndex(int startIndex, Vector3 dp){
		for (int a = startIndex; a < pendingState.Count; a++)
			pendingState[a].pos += dp;
	}

	IEnumerator WaitForInput(){
		while (waitingForInput) {
			if (inputs.Count > 0) {
				GetInput ();
				yield break;
			}
			yield return new WaitForEndOfFrame();
		}
	}

	void GetInput(){
		myInput.CopyFromInputToThis (inputs [0]);
		if (!isLocalPlayer && isServer){
			if (serverOutputs.ContainsKey (myInput.timeStamp)) {
				serverOutputs [myInput.timeStamp].Clean ();
				serverOutputs [myInput.timeStamp].rot = transform.rotation;
			}else{
				serverOutputs.Add (myInput.timeStamp, new ServerOutput (transform.rotation));
			}
		}
		inputs.RemoveAt (0);
		waitingForInput = false;
	}

	[Command]
	void CmdSendOutput(byte timeStamp, PackedOutput pack, byte fsid){
		if (fsid == forceSyncId)
			StartCoroutine(WaitToCheckIds(timeStamp, PlyMoveOutput.UnpackOutput(pack)));
	}

	IEnumerator WaitToCheckIds(byte timeStamp, PlyMoveOutput output){
		for (int b = 0; b < 1200; b++){
			if (serverOutputs.ContainsKey(timeStamp) && serverOutputs[timeStamp].done){
				ServerOutput curOutput = serverOutputs [timeStamp];
				RpcSendPlyCorrection (curOutput.accResDeltaPos - output.accResDeltaPos, forceSyncId);
				PlyGravity curGrav = null;
				Impulse curImp = null;
				TimedMove curTMove = null;
				for (int a = 0; a < PlyMoveInput.updates; a++) {
					curGrav = curOutput.gravs [a];
					if (output.gravIds[a] == 0) {
						if (curGrav != null) {
							RpcSendAddGrav (tempGrav, timeSinceUngrounded, forceSyncId);
						} 
					} else {
						if (curGrav == null) {
							RpcSendGravDecline (output.gravIds[a], forceSyncId);
						} else {
							RpcSendGravCorrection (output.gravIds[a], curGrav.velocity, curGrav.applyTimeSinceUngrounded, forceSyncId);
						}
					}

					byte curImpId = curOutput.impulseIds [a];
					byte plyImpId = output.impulseIds [a];
					if (plyImpId == 0) {
						if (curImpId != 0 && serverImpulse.ContainsKey(curImpId)) {
							curImp = serverImpulse [curImpId];
							RpcSendImpulse (curImp.impulseStart, curImp.impulseDeccel, curImpId, forceSyncId);
						}
					} else {
						if (curImpId == 0) {
							RpcSendImpulseDecline (output.impulseIds[a], forceSyncId);
						} else if (serverImpulse.ContainsKey(curImpId)){
							curImp = serverImpulse [curImpId];
							RpcSendImpulseCorrection (curImp.impulseStart, curImp.impulseDeccel, plyImpId, curImpId, forceSyncId);
							//playerImpulse.Add (plyImpId, serverImpulse [curImpId]);
							//serverImpulse.Remove (curImpId);
						}
					}


					byte curTMoveId = curOutput.tMoveIds [a];
					byte plyTMoveId = output.tMoveIds [a];
					if (plyTMoveId == 0) {
						if (curTMoveId != 0 && serverTMove.ContainsKey(curTMoveId)) {
							curTMove = serverTMove [curTMoveId];
							RpcSendTMove (curImp.impulseStart, curImp.impulseDeccel, curTMoveId, forceSyncId);
						}
					} else {
						if (curTMoveId == 0) {
							RpcSendTMoveDecline (output.tMoveIds[a], forceSyncId);
						} else if (serverTMove.ContainsKey(curTMoveId)){
							curTMove = serverTMove [curTMoveId];
							RpcSendTMoveCorrection (curTMove.velocity, curTMove.timeLeft + curTMove.timeDone, plyTMoveId, curTMoveId, forceSyncId);
						}
					}
				}
				serverOutputs.Remove (timeStamp);
				yield break;
			}
			yield return new WaitForSecondsRealtime(0.1f);
		}
	}

	void CleanTemp(){
		tempGrav = Vector3.zero;
	}

	void LateUpdate(){
	}

	public void Move(Vector3 delta){
		cc.Move (delta);
	}

	void PlyMoveCorUpdate(float dt){
		if (!isServer) {
			if (plyMoveCorrection != Vector3.zero) {

				Vector3 tempPos2 = transform.position;
				Vector3 lerpPos = Vector3.Lerp (Vector3.zero, plyMoveCorrection, 2 * dt);
				cc.Move (lerpPos);
				Vector3 deltaPos = transform.position - tempPos2;
				UpdateStatePos (deltaPos);
				plyMoveCorrection -= deltaPos;
			}
		}
	}

	void UpdateStatePos(Vector3 deltaPos){
		for (int a = 0; a < pendingState.Count; a++) {
			pendingState [a].pos += deltaPos;
		}
		curState.pos += deltaPos;
	}

	void UpdateGravity(float dt){
		//corrections
		if (!isServer) {
			if (gravCorrections != Vector3.zero) {
				if (CheckIfCorrectionPossibleOverTime (gravCorrections)) {
					Vector3 curPos = transform.position;
					Vector3 lerp = Vector3.Lerp (Vector3.zero, gravCorrections, 4 * dt);
					cc.Move (lerp);
					Vector3 deltaPos = transform.position - curPos;
					UpdateStatePos (deltaPos);
					gravCorrections -= deltaPos;
				} else {
					gravCorrections = Vector3.zero;
					Debug.Log ("teleport gravcor");
				}
			}
		}

		for (int a = 0; a < gravIds.Count; a++) {
			Vector3 curPos = transform.position;
			cc.Move (pendingGrav [gravIds [a]].velocity * dt);
			pendingGrav [gravIds [a]].accMove += transform.position - curPos;
			pendingGrav [gravIds [a]].timeDone += dt;
		}
		UpdateGround ();
		if (grounded) {
			if (gravity == PlyGravity.gravGround) {
				cc.Move (PlyGravity.gravGround);
			} else {
				Vector3 curPos2 = transform.position;
				cc.Move (gravity * dt);
				UpdateGround ();
				if (!grounded) {
					timeSinceUngrounded += dt;
					Vector3 curAccGrav = Physics.gravity * dt;
					cc.Move ((gravity + 0.5f * curAccGrav) * dt);
					gravAccMove += transform.position - curPos2;
					timeSinceUngrounded += dt;
					gravity += curAccGrav;
				}
			}
		} else {
			Vector3 curPos2 = transform.position;
			Vector3 curAccGrav = Physics.gravity * dt;
			cc.Move ((gravity + 0.5f * curAccGrav) * dt);
			gravAccMove += transform.position - curPos2;
			timeSinceUngrounded += dt;
			gravity += curAccGrav;
		}
		UpdateGround ();
	}

	void UpdateImpulse(float dt){
		//corrections
		if (!isServer) {
			if (impulseCorrections != Vector3.zero) {
				if (CheckIfCorrectionPossibleOverTime (impulseCorrections)) {
					Vector3 curPos = transform.position;
					Vector3 lerp = Vector3.Lerp (Vector3.zero, impulseCorrections, 4 * dt);
					cc.Move (lerp);
					Vector3 deltaPos = transform.position - curPos;
					UpdateStatePos (deltaPos);
					impulseCorrections -= deltaPos;
				} else {
					impulseCorrections = Vector3.zero;
					Debug.Log ("teleport impulsecor");
				}
			}
		}


		UpdateImpulseReal (serverImpulse, serverImpulseIds, dt, false);
		UpdateImpulseReal (playerImpulse, playerImpulseIds, dt, true);
	}

	void UpdateImpulseReal(Dictionary<byte, Impulse> imps, List<byte> impIds, float leDt, bool player){

		Impulse curImp;
		for (int a = impIds.Count - 1; a >= 0; a -= 1) {
			curImp = imps [impIds[a]];
			if (!isServer && curImp.corrections != Vector3.zero) {
				Vector3 curPos = transform.position;
				Vector3 lerp = Vector3.Lerp (Vector3.zero, curImp.corrections, 4 * leDt);
				cc.Move (lerp);
				Vector3 deltaPos = transform.position - curPos;
				UpdateStatePos (deltaPos);
				curImp.corrections -= deltaPos;
				curImp.impulseMoveDone += deltaPos;
			}
			if (!imps [impIds [a]].done) {
				Vector3 curPos = transform.position;
				float dt = leDt;
				if (dt > curImp.timeLeft) {
					dt = curImp.timeLeft;
					curImp.timeLeft = 0;
					curImp.done = true;
				} else {
					curImp.timeLeft -= dt;
				}
				Vector3 newImpleft = Vector3.MoveTowards (curImp.impulseLeft, Vector3.zero, curImp.impulseDeccel * dt);
				cc.Move ((curImp.impulseLeft + newImpleft) * 0.5f * dt);
				curImp.impulseMoveDone += transform.position - curPos;
				curImp.time = Mathf.Clamp (curImp.time + dt, 0, curImp.endTime);
				if (newImpleft == Vector3.zero) {
					curImp.done = true;
				}
				if (curImp.done && isServer) {
					byte id = impIds [a];
					if (!isLocalPlayer) {
						if (player) {
							RpcSendImpulseDonePlayer (id, curImp.impulseMoveDone, forceSyncId);
						} else {
							RpcSendImpulseDoneServer (id, curImp.impulseMoveDone, forceSyncId);
						}
					}
					impIds.RemoveAt (a);
					imps.Remove (id);
				}
			}
		}
	}





	void UpdateTMove(float dt){
		//corrections
		if (!isServer) {
			if (tMoveCorrections != Vector3.zero) {
				if (CheckIfCorrectionPossibleOverTime (tMoveCorrections)) {
					Vector3 curPos = transform.position;
					Vector3 lerp = Vector3.Lerp (Vector3.zero, tMoveCorrections, 4 * dt);
					cc.Move (lerp);
					Vector3 deltaPos = transform.position - curPos;
					UpdateStatePos (deltaPos);
					tMoveCorrections -= deltaPos;
				} else {
					tMoveCorrections = Vector3.zero;
					Debug.Log ("teleport tMovescor");
				}
			}
		}


		UpdateTMoveReal (serverTMove, serverTMoveIds, dt, false);
		UpdateTMoveReal (playerTMove, playerTMoveIds, dt, true);
	}

	void UpdateTMoveReal(Dictionary<byte, TimedMove> tMoves, List<byte> tMoveIds, float leDt, bool player){

		TimedMove curTMove;
		for (int a = tMoveIds.Count - 1; a >= 0; a -= 1) {
			curTMove = tMoves [tMoveIds[a]];
			if (!isServer && curTMove.corrections != Vector3.zero) {
				Vector3 curPos = transform.position;
				Vector3 lerp = Vector3.Lerp (Vector3.zero, curTMove.corrections, 4 * leDt);
				cc.Move (lerp);
				Vector3 deltaPos = transform.position - curPos;
				UpdateStatePos (deltaPos);
				curTMove.corrections -= deltaPos;
				curTMove.accMove += deltaPos;
			}
			if (!tMoves [tMoveIds [a]].done) {
				Vector3 curPos = transform.position;
				float dt = leDt;
				if (dt > curTMove.timeLeft) {
					dt = curTMove.timeLeft;
					curTMove.timeLeft = 0;
					curTMove.done = true;
				} else {
					curTMove.timeLeft -= dt;
				}
				cc.Move (curTMove.velocity * dt);
				curTMove.accMove += transform.position - curPos;
				if (curTMove.done && isServer) {
					byte id = tMoveIds [a];
					if (!isLocalPlayer) {
						if (player) {
							RpcSendTMoveDonePlayer (id, curTMove.accMove, forceSyncId);
						} else {
							RpcSendTMoveDoneServer (id, curTMove.accMove, forceSyncId);
						}
					}
					tMoveIds.RemoveAt (a);
					tMoves.Remove (id);
				}
			}
		}
	}






	public bool AddGravitySelf(Vector3 grav){
		if (grav == Vector3.zero)
			return false;

		tempGrav += grav;
		return true;
	}


	public bool AddImpulseExt(Vector3 addImp, float addDeccel){
		if (addImp == Vector3.zero) 
			return false;

		if (addDeccel <= 0) 
			return false;

		if (!isServer) 
			return false;

		if (lastServerImpulseId == byte.MaxValue) 
			lastServerImpulseId = 1;
		else
			lastServerImpulseId++;

		while (serverImpulse.ContainsKey (lastServerImpulseId)) {
			if (lastServerImpulseId == byte.MaxValue)
				lastServerImpulseId = 1;
			else
				lastServerImpulseId++;
		}

		serverImpulseIds.Add (lastServerImpulseId);
		serverImpulseIds.Sort ();
		serverImpulse.Add (lastServerImpulseId, new Impulse (addImp, addDeccel));
		if (!isLocalPlayer)
			RpcSendImpulse (addImp, addDeccel, lastServerImpulseId, forceSyncId);
		return true;
	}

	public bool AddImpulseSelf(Vector3 addImp, float addDeccel, bool fromPlayer = false){
		if (addImp == Vector3.zero) {
			return false;
		}

		if (addDeccel <= 0) {
			return false;
		}

		if (myPlyOutput.impulseIds [myInput.updatesDone] != 0) {
			return false;
		}

		if (!isServer || fromPlayer) {
			if (lastClientImpulseId == byte.MaxValue) 
				lastClientImpulseId = 1;
			else
				lastClientImpulseId++;

			while (playerImpulse.ContainsKey (lastClientImpulseId)) {
				if (lastClientImpulseId == byte.MaxValue)
					lastClientImpulseId = 1;
				else
					lastClientImpulseId++;
			}

			playerImpulseIds.Add (lastClientImpulseId);
			playerImpulseIds.Sort ();
			playerImpulse.Add (lastClientImpulseId, new Impulse (addImp, addDeccel));
			myPlyOutput.impulseIds[myInput.updatesDone] = lastClientImpulseId;
		}else{
			if (lastServerImpulseId == byte.MaxValue) 
				lastServerImpulseId = 1;
			else
				lastServerImpulseId++;

			while (serverImpulse.ContainsKey (lastServerImpulseId)) {
				if (lastServerImpulseId == byte.MaxValue)
					lastServerImpulseId = 1;
				else
					lastServerImpulseId++;
			}
			serverImpulseIds.Add (lastServerImpulseId);
			serverImpulseIds.Sort ();
			serverImpulse.Add (lastServerImpulseId, new Impulse (addImp, addDeccel));
			if (!isLocalPlayer)
				serverOutputs [myInput.timeStamp].impulseIds [myInput.updatesDone] = lastServerImpulseId;
		}
		return true;

	}


	public bool AddTMoveExt(Vector3 addMove, float addTime, bool fromPlayer = false){
		if (addMove == Vector3.zero) {
			return false;
		}

		if (addTime <= 0) {
			return false;
		}

		if (lastServerTMoveId == byte.MaxValue) 
			lastServerTMoveId = 1;
		else
			lastServerTMoveId++;

		while (serverTMove.ContainsKey (lastServerTMoveId)) {
			if (lastServerTMoveId == byte.MaxValue)
				lastServerTMoveId = 1;
			else
				lastServerTMoveId++;
		}
		serverTMoveIds.Add (lastServerTMoveId);
		serverTMoveIds.Sort ();
		serverTMove.Add (lastServerTMoveId, new TimedMove (addMove, addTime));
		if (!isLocalPlayer)
			RpcSendTMove (addMove, addTime, lastServerTMoveId, forceSyncId);
		return true;

	}

	public bool AddTMoveSelf(Vector3 addMove, float addTime, bool fromPlayer = false){
		if (addMove == Vector3.zero) {
			return false;
		}

		if (addTime <= 0) {
			return false;
		}

		if (myPlyOutput.tMoveIds [myInput.updatesDone] != 0)
			return false;

		if (!isServer || fromPlayer) {
			if (lastClientTMoveId == byte.MaxValue) 
				lastClientTMoveId = 1;
			else
				lastClientTMoveId++;

			while (playerTMove.ContainsKey (lastClientTMoveId)) {
				if (lastClientTMoveId == byte.MaxValue)
					lastClientTMoveId = 1;
				else
					lastClientTMoveId++;
			}

			playerTMoveIds.Add (lastClientTMoveId);
			playerTMoveIds.Sort ();
			playerTMove.Add (lastClientTMoveId, new TimedMove (addMove, addTime));
			myPlyOutput.tMoveIds[myInput.updatesDone] = lastClientTMoveId;
		}else{
			if (lastServerTMoveId == byte.MaxValue) 
				lastServerTMoveId = 1;
			else
				lastServerTMoveId++;

			while (serverTMove.ContainsKey (lastServerTMoveId)) {
				if (lastServerTMoveId == byte.MaxValue)
					lastServerTMoveId = 1;
				else
					lastServerTMoveId++;
			}
			serverTMoveIds.Add (lastServerTMoveId);
			serverTMoveIds.Sort ();
			serverTMove.Add (lastServerTMoveId, new TimedMove (addMove, addTime));
		}
		return true;

	}

	public bool AddGravityExt(Vector3 addGrav){
		if (addGrav == Vector3.zero) {
			return false;
		}
		if (!isServer) {
			return false;
		}
		Vector3 curPos = transform.position;
		cc.Move (addGrav * Time.fixedDeltaTime);
		Vector3 delta = transform.position - curPos;
		UpdateGround ();
		gravity += addGrav;
		gravAccMove += delta;
		if (!isLocalPlayer)
			RpcSendAddGrav (addGrav, timeSinceUngrounded, forceSyncId);
		return true;
	}

	public bool AddGravitySelfReal(Vector3 addGrav){
		if (addGrav == Vector3.zero) {
			return false;
		}
		Vector3 curPos = transform.position;
		cc.Move (addGrav * Time.fixedDeltaTime);
		Vector3 delta = transform.position - curPos;
		UpdateGround ();
		if (isServer) {
			gravity += addGrav;
			gravAccMove += delta;
		} else {
			if (lastGravityId == byte.MaxValue) 
				lastGravityId = 1;
			else
				lastGravityId++;
			while (pendingGrav.ContainsKey (lastGravityId)) {
				if (lastGravityId == byte.MaxValue) 
					lastGravityId = 1;
				else
					lastGravityId++;
			}
			gravIds.Add (lastGravityId);
			gravIds.Sort ();
			pendingGrav.Add (lastGravityId, new PlyGravity (addGrav, delta, timeSinceUngrounded));
			myPlyOutput.gravIds[myInput.updatesDone] = lastGravityId;
		}
		return true;
	}

	bool UpdateGround(){
		return UpdateGround (Vector3.zero);
	}

	bool UpdateGround(Vector3 deltaMoveByGrav){
		if (grounded && gravity == PlyGravity.gravGround && pendingGrav.Count == 0) {
			cc.Move (PlyGravity.gravGround);
		}
		if (cc.isGrounded != grounded) {
			if (grounded) {
				timeSinceUngrounded = Time.fixedDeltaTime;
				gravAccMove = deltaMoveByGrav;
			}else{
				for (int a = gravIds.Count - 1; a >= 0; a -= 1) {
					gravAccMove += pendingGrav [gravIds[a]].accMove;
					byte id = gravIds [a];
					gravIds.RemoveAt (a);
					pendingGrav.Remove (id);
				}
				gravity = PlyGravity.gravGround;
				if (isServer) {
					if (!isLocalPlayer) {
						RpcSendGravDone (gravAccMove, forceSyncId);
					} 
				}else if (isLocalPlayer) {
					gravAccs.Add (gravAccMove);
					gravAccMove = Vector3.zero;
				}

			}
			grounded = cc.isGrounded;
			return true;
		}
		return false;
	}


	void UpdateInput(){
		pendingInput.dir = Vector3.ClampMagnitude (new Vector3 (Joystick.padAxis.x + Input.GetAxis ("Horizontal"), 0, Joystick.padAxis.y + Input.GetAxis ("Vertical")), 1);

		for (int a = 0; a < PlyMoveInput.btLength; a++) {
			byte bt = pendingInput.bts [a];
			if (CrossPlatformInputManager.GetButtonDown (PlyMoveInput.btString [a])) {
				if (bt == ButtonDown || bt == ButtonHold)
					pendingInput.bts [a] = ButtonDoubleClickHold;
				else if (bt == ButtonClick)
					pendingInput.bts [a] = ButtonDoubleClickHold;
				else if (bt == ButtonUp)
					pendingInput.bts [a] = ButtonUpWaitDown;
				else
					pendingInput.bts [a] = ButtonDown;
				/*} else if (CrossPlatformInputManager.GetButton (PlyMoveInput.btString [a])) {
				if (bt != ButtonDoubleClickHold && bt != ButtonDown && bt != ButtonClick && bt != ButtonDoubleClickUp)
					pendingInput.bts [a] = ButtonHold;*/
			} else if (CrossPlatformInputManager.GetButtonUp (PlyMoveInput.btString [a])) {
				if (bt == ButtonDown)
					pendingInput.bts [a] = ButtonClick;
				else if (bt == ButtonDoubleClickHold)
					pendingInput.bts [a] = ButtonDoubleClickUp;
				else
					pendingInput.bts [a] = ButtonUp;
			}/* else {
				pendingInput.bts [a] = ButtonUnpressed;
			}*/
		}

		CleanTemp ();
	}

	public virtual void PlayerStart(){
	}

	public virtual void PlayerUpdate(PlyMoveInput input, float dt){
	}

	public bool GetButtonDown(byte bt){
		if (bt == ButtonDown || bt == ButtonClick || bt == ButtonDoubleClickHold || bt == ButtonDoubleClickUp)
			return true;
		return false;
	}

	public bool GetButton(byte bt){
		if (bt == ButtonDown || bt == ButtonHold || bt == ButtonClick || bt == ButtonDoubleClickHold || bt == ButtonDoubleClickUp || bt == ButtonHoldWaitUp)
			return true;
		return false;
	}

	public bool GetButtonUp(byte bt){
		if (bt == ButtonUp || bt ==ButtonUpWaitDown || bt == ButtonUpWaitClick)
			return true;
		return false;
	}

	bool GetClick(byte bt){
		if (bt == ButtonClick || bt == ButtonDoubleClickHold || bt == ButtonDoubleClickUp)
			return true;
		return false;
	}

	bool GetDoubleClick(byte bt){
		if (bt == ButtonDoubleClickHold || bt == ButtonDoubleClickUp || bt == ButtonUpWaitDown || bt == ButtonUpWaitClick)
			return true;
		return false;
	}


	[Command]
	void CmdSendInput(PackedInput pack, byte fsid){
		//if (fsid == forceSyncId) {
			if (!inited) {
				inited = true;
				InitAccDt ();
				pendingState.Clear ();
			}
			PlyMoveInput parsed = PlyMoveInput.UnpackInput (pack);
			if (serverOutputs.ContainsKey (parsed.timeStamp)) {
				serverOutputs [parsed.timeStamp].Clean ();
			} else
				serverOutputs.Add (parsed.timeStamp, new ServerOutput (transform.rotation));
			inputs.Add (parsed);
			lastSentRecInput.CopyFromInputToThis (parsed);
		//}
	}

	void InitAccDt(){
		accFixedDt = 0;
		accInputDt = 0;
		accUpdateDt = 0;
		//savedDt = 0;
		/*Mathf.Clamp (accFixedDt, -0.24f, PlyMoveInput.inputInterval);
		Mathf.Clamp (accInputDt, -0.24f, PlyMoveInput.inputInterval);
		Mathf.Clamp (accUpdateDt, -0.24f, PlyMoveInput.inputInterval);
		Mathf.Clamp (savedDt, -0.24f, PlyMoveInput.inputInterval);*/
	}

	[Command]
	void CmdSameInput(byte ts, byte fsid){
		//if (fsid == forceSyncId) {
			if (!inited) {
				inited = true;
				InitAccDt ();
				pendingState.Clear ();
			}
			if (serverOutputs.ContainsKey (ts)) {
				serverOutputs [ts].Clean ();
			} else
				serverOutputs.Add (ts, new ServerOutput (transform.rotation));
			inputs.Add (PlyMoveInput.CopyFromInput (lastSentRecInput));
			inputs [inputs.Count - 1].timeStamp = ts;
		//}
	}

	[ClientRpc]
	void RpcSendPlyCorrection(Vector3 cor, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			plyMoveCorrection += cor;
			if (!CheckIfCorrectionPossibleOverTime (plyMoveCorrection)) {
				plyMoveCorrection = Vector3.zero;
				Debug.Log ("teleport plymovecor");
			}
		}
	}

	[ClientRpc]
	void RpcSendGravCorrection(byte id, Vector3 grav, float time, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (pendingGrav.ContainsKey (id)) {
				Vector3 cor = grav - pendingGrav [id].velocity;
				gravAccMove += pendingGrav [id].accMove;
				if (cor == Vector3.zero) {
					gravity += pendingGrav [id].velocity;
					if (time != pendingGrav [id].applyTimeSinceUngrounded) {

					}
				} else {
					Vector3 correctedGrav = pendingGrav [id].velocity + cor;
					gravity += correctedGrav;
					Vector3 corMove = correctedGrav * pendingGrav [id].timeDone;
					//Vector3 shouldMove = pendingGrav [id].velocity * pendingGrav [id].timeDone;
					if (pendingGrav [id].accMove.sqrMagnitude >= corMove.sqrMagnitude || Vector3.Dot (corMove, pendingGrav [id].accMove) <= 0) {
						Vector3 correction = cor * pendingGrav [id].timeDone;
						if (CheckIfCorrectionPossibleOverTime (correction)) {
							gravCorrections += correction;
						} else {
							Debug.Log ("teleport gravcor piece 1");
						}
					} else {
						Vector3 correction = -pendingGrav [id].accMove;
						if (CheckIfCorrectionPossibleOverTime (correction)) {
							gravCorrections += correction;
						} else{
							Debug.Log("teleport gravcor piece 2");
						}
					}
				}
				gravIds.Remove (id);
				pendingGrav.Remove (id);

			}
		}
	}

	[ClientRpc]
	void RpcSendGravDecline(byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (pendingGrav.ContainsKey (id)) {
				Vector3 correction = -pendingGrav [id].accMove;

				if (pendingGrav [id].applyTimeSinceUngrounded == 0) {
					correction += -gravAccMove;
				}

				if (CheckIfCorrectionPossibleOverTime (correction)) {
					gravCorrections += correction;
				} else {
					Debug.Log ("teleport grav decline");
				}
				gravIds.Remove (id);
				pendingGrav.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendAddGrav(Vector3 grav, float time, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
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
				yield break;
			}
			yield return new WaitForEndOfFrame ();
		}
	}

	[ClientRpc]
	void RpcSendGravDone(Vector3 accMove, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (grounded && gravAccs.Count == 0) {
				gravCorrections += accMove - gravAccMove;
			} else {
				StartCoroutine (WaitForGroundedForCheck (accMove));
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulseCorrection(Vector3 imp, float deccel, byte id, byte serverId, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
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

				serverImpulseIds.Add (serverId);
				serverImpulseIds.Sort ();
				serverImpulse.Add (serverId, playerImpulse [id]);
				playerImpulseIds.Remove (id);
				playerImpulse.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulseDecline(byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (playerImpulse.ContainsKey (id)) {
				impulseCorrections += -playerImpulse [id].impulseMoveDone;
				playerImpulseIds.Remove (id);
				playerImpulse.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulseDonePlayer(byte id, Vector3 accMove, byte fsid){
		if (playerImpulse.ContainsKey (id) && forceSyncId == fsid  && forceSyncId == fsid) {
			StartCoroutine (WaitForImpulseDonePlayer (id, accMove));
		} /*else {
			impulseCorrections += accMove;
		}*/
	}

	[ClientRpc]
	void RpcSendImpulseDoneServer(byte id, Vector3 accMove, byte fsid){
		if (serverImpulse.ContainsKey (id) && forceSyncId == fsid) {
			StartCoroutine (WaitForImpulseDoneServer (id, accMove));
		} /*else {
			impulseCorrections += accMove;
		}*/
	}

	IEnumerator WaitForImpulseDonePlayer(byte id, Vector3 accMove){
		while (true) {
			if (playerImpulse.ContainsKey (id)) {
				if (playerImpulse [id].done) {
					impulseCorrections += playerImpulse [id].impulseMoveDone - accMove;
					playerImpulseIds.Remove (id);
					playerImpulse.Remove (id);
					yield break;
				}
			}/* else {
				impulseCorrections += accMove;
				yield break;
			}*/
			yield return new WaitForEndOfFrame ();
		}
	}

	IEnumerator WaitForImpulseDoneServer(byte id, Vector3 accMove){
		while (true) {
			if (serverImpulse.ContainsKey (id)) {
				if (serverImpulse [id].done) {
					impulseCorrections += serverImpulse [id].impulseMoveDone - accMove;
					serverImpulseIds.Remove (id);
					serverImpulse.Remove (id);
					yield break;
				}
			} /*else {
				impulseCorrections += accMove;
				yield break;
			}*/
			yield return new WaitForEndOfFrame ();
		}
	}

	[ClientRpc]
	void RpcSendTMoveCorrection(Vector3 vel, float newTime, byte id, byte serverId, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (playerTMove.ContainsKey (id)) {
				if (playerTMove [id].velocity != vel || playerTMove [id].timeDone + playerTMove[id].timeLeft != newTime) {
					float playerTime = playerTMove [id].timeDone;
					float time = playerTime;
					if (time > newTime) {
						time = newTime;
					}
					Vector3 trueShouldDone = vel * time;
					Vector3 playerDone = playerTMove [id].accMove;
					Vector3 playerShouldDone = playerTMove [id].velocity * playerTime;
					float sqPlayerDoneMag = playerDone.sqrMagnitude;

					if (Vector3.Dot (playerDone, trueShouldDone) > 0) {
						if (sqPlayerDoneMag > trueShouldDone.sqrMagnitude) {
							playerTMove [id].corrections += trueShouldDone - playerDone;
						} else if (sqPlayerDoneMag >= playerShouldDone.sqrMagnitude) {
							playerTMove [id].corrections += trueShouldDone - playerDone;
						}
					}

					playerTMove [id].velocity = vel;
					playerTMove [id].timeDone = Mathf.Clamp(playerTMove [id].timeDone, 0, newTime);
					playerTMove [id].timeLeft = newTime - playerTMove [id].timeDone;

					playerTMove [id].done = (playerTMove [id].timeLeft <= 0);
				}

				serverTMoveIds.Add (serverId);
				serverTMoveIds.Sort ();
				serverTMove.Add (serverId, playerTMove [id]);
				playerTMoveIds.Remove (id);
				playerTMove.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendTMoveDecline(byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (playerTMove.ContainsKey (id)) {
				tMoveCorrections += -playerTMove [id].accMove;
				playerTMoveIds.Remove (id);
				playerTMove.Remove (id);
			}
		}
	}

	[ClientRpc]
	void RpcSendTMoveDonePlayer(byte id, Vector3 accMove, byte fsid){
		if (playerTMove.ContainsKey (id) && forceSyncId == fsid) {
			StartCoroutine (WaitForTMoveDonePlayer (id, accMove));
		} /*else {
			tMoveCorrections += accMove;
		}*/
	}

	[ClientRpc]
	void RpcSendTMoveDoneServer(byte id, Vector3 accMove, byte fsid){
		if (serverTMove.ContainsKey (id) && forceSyncId == fsid) {
			StartCoroutine (WaitForTMoveDoneServer (id, accMove));
		} /*else {
			tMoveCorrections += accMove;
		}*/
	}

	IEnumerator WaitForTMoveDonePlayer(byte id, Vector3 accMove){
		while (true) {
			if (playerTMove.ContainsKey (id)) {
				if (playerTMove [id].done) {
					tMoveCorrections += playerTMove [id].accMove - accMove;
					playerTMoveIds.Remove (id);
					playerTMove.Remove (id);
					yield break;
				}
			} /*else {
				tMoveCorrections += accMove;
				yield break;
			}*/
			yield return new WaitForEndOfFrame ();
		}
	}

	IEnumerator WaitForTMoveDoneServer(byte id, Vector3 accMove){
		while (true) {
			if (serverTMove.ContainsKey (id)) {
				if (serverTMove [id].done) {
					impulseCorrections += serverTMove [id].accMove - accMove;
					serverTMoveIds.Remove (id);
					serverTMove.Remove (id);
					yield break;
				}
			} /*else {
				impulseCorrections += accMove;
				yield break;
			}*/
			yield return new WaitForEndOfFrame ();
		}
	}


	[ClientRpc]
	void RpcForceTMove(Vector3 vel, Vector3 accMove, float timeLeft, byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (serverTMove.ContainsKey (id)) {
				serverTMove [id].velocity = vel;
				serverTMove [id].timeLeft = timeLeft;
				serverTMove [id].accMove = accMove;
			} else {
				serverTMoveIds.Add (id);
				serverTMoveIds.Sort ();
				serverTMove.Add (id, new TimedMove (vel, timeLeft, accMove, 0));
			}
		}
	}

	[ClientRpc]
	void RpcSendTMove(Vector3 impulse, float deccel, byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			serverTMoveIds.Add (id);
			serverTMoveIds.Sort ();
			serverTMove.Add (id, new TimedMove (impulse, deccel));
		}
	}

	[ClientRpc]
	void RpcForceSync(Vector3 pos, Quaternion rot, Vector3 grav, Vector3 gravAcc, float timeUnground, bool newGrounded, byte newState, byte newId){
		if (isLocalPlayer && forceSyncId != newId) {
			Debug.Log ("Forced sync");

			accFixedDt = 0;
			accInputDt = Mathf.Clamp (accInputDt, PlyMoveInput.inputInterval, accInputDt);
			accUpdateDt = 0;

			transform.position = pos;
			transform.rotation = rot;
			curState.pos = pos;
			curState.rot = rot;
			//inputs.Clear ();
			//myInput.updatesDone = 3;
			//TryGetInput ();
			pendingState.Clear ();
			playerImpulseIds.Clear ();
			playerImpulse.Clear ();
			serverImpulseIds.Clear ();
			serverImpulse.Clear ();
			playerTMoveIds.Clear ();
			playerTMove.Clear ();
			serverTMoveIds.Clear ();
			serverTMove.Clear ();
			pendingGrav.Clear ();
			gravIds.Clear ();
			gravAccs.Clear ();

			StopCoroutine ("WaitForGroundedForCheck");
			StopCoroutine ("WaitForGravTime");
			StopCoroutine ("WaitForImpulseDonePlayer");
			StopCoroutine ("WaitForImpulseDoneServer");
			StopCoroutine ("WaitToCheckIds");
			UpdateGround ();
			gravity = grav;
			gravAccMove = gravAcc;
			plyMoveCorrection = Vector3.zero;
			gravCorrections = Vector3.zero;
			impulseCorrections = Vector3.zero;
			tMoveCorrections = Vector3.zero;
			timeSinceUngrounded = timeUnground;
			curState.grounded = newGrounded;
			curState.state = newState;
			grounded = newGrounded;
			state = newState;
			forceSyncId = newId;
		}
	}

	[ClientRpc]
	void RpcForceImpulse(Vector3 impulseLeft, float deccel, Vector3 impulseDone, float impTime, byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			if (serverImpulse.ContainsKey (id)) {
				serverImpulse [id].impulseLeft = impulseLeft;
				serverImpulse [id].impulseDeccel = deccel;
				serverImpulse [id].time = impTime;
				serverImpulse [id].impulseMoveDone = impulseDone;
			} else {
				serverImpulseIds.Add (id);
				serverImpulseIds.Sort ();
				serverImpulse.Add (id, new Impulse (impulseLeft, deccel, impulseDone, impTime));
			}
		}
	}

	[ClientRpc]
	void RpcSendImpulse(Vector3 impulse, float deccel, byte id, byte fsid){
		if (isLocalPlayer && forceSyncId == fsid) {
			serverImpulseIds.Add (id);
			serverImpulseIds.Sort ();
			serverImpulse.Add (id, new Impulse (impulse, deccel));
		}
	}

	IEnumerator WaitForGroundedForCheck(Vector3 accMove){
		for (int a = 0; a < 600; a++) {
			if (gravAccs.Count > 0) {
				gravCorrections += accMove - gravAccs [0];
				gravAccs.RemoveAt (0);
				yield break;
			} 
			/*if (a == 599) {
				gravCorrections += accMove;
			}*/
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
			transform.position = transform.position + cor;
			UpdateGround ();
			return false;
		}
		return true;
	}

}