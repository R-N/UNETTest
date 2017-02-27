using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public class MyNetworkDiscovery : NetworkDiscovery {
	public struct RoomInfo{
		public string address;
		public string gameName;
		public bool needPass;
		public int curPlayers;
		public int maxPlayers;

		public RoomInfo(string newAdd, string newName, bool newP, int newCP, int newMP){
			address = newAdd;
			gameName = newName;
			needPass = newP;
			curPlayers = newCP;
			maxPlayers = newMP;
		}
	}
	Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();


	bool canRefresh = true;
	bool waitingRefresh = false;

	public bool passListening = false;
	public NetworkServerSimple passServer = new NetworkServerSimple();
	NetworkClient passClient = new NetworkClient();

	public NATNetworkManager_PHP netMan = null;

	public static MyNetworkDiscovery singleton = null;

	public bool didInit = false;

	const short PassCheckId = 9000;

	public class PassCheckReplyMsg : MessageBase{
		public string address;
		public string key;
		public PassCheckReplyMsg(){
		}
		public PassCheckReplyMsg(string newA, string newK){
			address = newA;
			key = newK;
		}
	}

	void Start(){
		StartCoroutine ("WaitForNetworkManager");
		StartCoroutine ("WaitForNetwork");
		singleton = this;
		didInit = Initialize ();
		isServer = false;
		isClient = false;
	}

	void LateUpdate(){
		if (passListening)
			passServer.Update ();
		
	}

	IEnumerator WaitForNetworkManager(){
		while ((!netMan || netMan == null) && (!NATNetworkManager_PHP.singleton || NATNetworkManager_PHP.singleton == null)) {
			yield return new WaitForEndOfFrame ();
		}
		netMan = NATNetworkManager_PHP.singleton;
	}

	IEnumerator WaitForNetwork(){
		while (Application.internetReachability == NetworkReachability.NotReachable) {
			yield return new WaitForEndOfFrame ();
		}
		passServer.listenPort = netMan.passPort;
		passServer.Initialize ();
		passServer.RegisterHandler (PassCheckId, PasswordCheckServer);
		passClient.RegisterHandler (PassCheckId, PasswordCheckClientReply);
	}

	public void PasswordCheckClientReply(NetworkMessage msg){
		NATNetworkManager_PHP.StringMsg parsed = msg.ReadMessage<NATNetworkManager_PHP.StringMsg> ();
		msg.conn.Disconnect ();
		msg.conn.Dispose ();
		if (!string.IsNullOrEmpty(parsed.value)) {
			if (parsed.value == Network.player.ipAddress) {
				NATNetworkManager_PHP.singleton.networkAddress = "127.0.0.1";
			} else {
				NATNetworkManager_PHP.singleton.networkAddress = parsed.value;
			}
			NATNetworkManager_PHP.singleton.JoinLocal ();
		} else {
			NATNetworkManager_PHP.DisplayLog ("Wrong Password");
		}
	}

	public void PasswordCheckClient(string address, string pass){
		passClient.Connect (address, netMan.passPort);
		StartCoroutine ("WaitForPassClientConnect", pass);
	}

	IEnumerator WaitForPassClientConnect(string pass){
		float time = 0;
		while (!passClient.isConnected && time < 5) {
			yield return new WaitForEndOfFrame ();
			time += Time.deltaTime;
		}
		if (passClient.isConnected) {
			if (!passClient.handlers.ContainsKey (PassCheckId))
				passClient.RegisterHandler (PassCheckId, PasswordCheckClientReply);
			passClient.Send(PassCheckId, new NATNetworkManager_PHP.StringMsg(pass));
			NATNetworkManager_PHP.singleton.myRoom.roomPass = pass;
		} else {
			NATNetworkManager_PHP.DisplayLog ("Connection Timed Out");
		}
	}

	public void PasswordCheckServer(NetworkMessage msg){
		if (NATNetworkManager_PHP.singleton.PassCheck (msg.ReadMessage<NATNetworkManager_PHP.StringMsg> ().value)) {
			msg.conn.Send (PassCheckId, new NATNetworkManager_PHP.StringMsg(Network.player.ipAddress));
		} else {
			msg.conn.Send (PassCheckId, new NATNetworkManager_PHP.StringMsg (string.Empty));
		}
	}

	public void MyStartBroadcast(){
		if (isClient)
			StopBroadcast ();
		if (!didInit)
			didInit = Initialize ();
		if (!passListening)
			passListening = passServer.Listen (NATNetworkManager_PHP.singleton.passPort);
		if (!passServer.handlers.ContainsKey(PassCheckId))
			passServer.RegisterHandler (PassCheckId, PasswordCheckServer);
		UpdateBroadcastData ();
		if (!isServer)
			isServer = StartAsServer ();
	}
		

	void UpdateBroadcastData(){
		NATNetworkManager_PHP.MyRoomInfo myRoom = NATNetworkManager_PHP.singleton.myRoom;
		broadcastData = myRoom.gameName + '@' + (!string.IsNullOrEmpty(NATNetworkManager_PHP.singleton.myRoom.roomPass) ? 1 : 0).ToString() + '@' + myRoom.curPlayers.ToString() + '@' + myRoom.maxPlayers.ToString();
	}

	public void RefreshRooms(){
		if (!waitingRefresh) {
			waitingRefresh = true;
			StartCoroutine ("WaitingRefresh");
		}
	}

	IEnumerator WaitingRefresh(){
		while (!canRefresh) {
			yield return new WaitForEndOfFrame ();
		}
		RefreshRoomsReal ();
	}

	IEnumerator RefreshCooldown(){
		yield return new WaitForSecondsRealtime (1);
		canRefresh = true;
	}

	void RefreshRoomsReal(){
		for (int a = netMan.lanListParent.childCount - 1; a >= 0; a -= 1) {
			Destroy(netMan.lanListParent.GetChild(a).gameObject);
		}
		rooms.Clear ();
		StopCoroutine ("WaitListen");
		StartCoroutine ("WaitListen");
		canRefresh = false;
		waitingRefresh = false;
		StopCoroutine ("RefreshCooldown");
		StartCoroutine ("RefreshCooldown");
		if (!didInit)
			didInit = Initialize ();
		if (!isClient)
		isClient = StartAsClient ();
	}

	IEnumerator WaitListen(){
		yield return new WaitForSecondsRealtime (5);
		StopBroadcast ();
		isClient = false;
	}

	public override void OnReceivedBroadcast (string fromAddress, string data)
	{
		if (isClient && !rooms.ContainsKey (fromAddress)) {
			string[] parsed = data.Split ('@');
			bool locked = int.Parse (parsed [1]) == 1;
			int maxPlyr = int.Parse (parsed [3]);
			rooms.Add (fromAddress, new RoomInfo (fromAddress, parsed [0], locked, int.Parse(parsed [2]), maxPlyr));
			GameObject curEntry = (GameObject)Instantiate (netMan.roomEntry);

			RoomEntry entry = curEntry.GetComponent<RoomEntry> ();
			entry.address = fromAddress;
			entry.isNetGame = false;
			entry.SetLock(locked);
			entry.SetPlayerCount (parsed [2] + "/" + parsed [3]);
			entry.SetName (parsed [0]);
			entry.maxPlyr = maxPlyr;

			curEntry.transform.SetParent (netMan.lanListParent, false);
		}
	}
}
