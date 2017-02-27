using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine.UI;

public class NATNetworkManager_PHP : UnityEngine.Networking.NetworkManager
{
	public class NetMsgId
	{
		public const short playerSpawnReq = 9001;
		public const short displayError = 9002;
	}


	public static NATNetworkManager_PHP singleton = null;
	public int myPort = 2297;
	NATHelper_PHP natHelper;
	FieldInfo clientIDField;
	NetworkMatch networkMatch;
	public List<NetworkServerSimple> natServers = new List<NetworkServerSimple>();
	public static bool connectedToFac = false;

	public string phpUrl;

	public string secretKey;

	public class MyRoomInfo{
		public string gameID;
		public string hashPass;
		public string gameName;
		public string roomPass;
		public string extIP;
		public string intIP;
		public int curPlayers;
		public int maxPlayers;
		public bool isNetGame;

		public MyRoomInfo(){
			Clear();
			ClearNet();
		}

		public void Clear(){
			gameID = string.Empty;
			hashPass = string.Empty;
			gameName = string.Empty;
			roomPass = string.Empty;
			curPlayers = 0;
			maxPlayers = 4;
			isNetGame = false;
		}

		public void ClearNet(){
			extIP = string.Empty;
			intIP = string.Empty;
		}
	}

	public struct NetRoomInfo {
		public string gameID;
		public string gameName;
		public bool hasPass;
		public int curPlayers;
		public int maxPlayers;

		public NetRoomInfo(string newID, string newgm, bool newp, int newcp, int newmp){
			gameID = newID;
			gameName = newgm;
			hasPass = newp;
			curPlayers = newcp;
			maxPlayers = newmp;
		}
	}

	public class NullMsg : MessageBase{
		public NullMsg(){
		}
	}

	public class IntMsg : MessageBase{
		public int value;
		public IntMsg(){
		}
		public IntMsg(int newVal){
			value = newVal;
		}
	}

	public class StringMsg : MessageBase{
		public string value;
		public StringMsg(){
		}
		public StringMsg(string newVal){
			value = newVal;
		}
	}

	public class BoolMsg : MessageBase{
		public bool value;
		public BoolMsg(){
		}
		public BoolMsg(bool newVal){
			value = newVal;
		}
	}


	public Dictionary<string, NetRoomInfo> roomsClient = new Dictionary<string, NetRoomInfo> ();
	public MyRoomInfo myRoom = new MyRoomInfo();

	public int passPort = 2299;

	public Transform lanListParent = null;
	public Transform netListParent = null;

	bool waitingForRefresh = false;

	public GameObject roomEntry = null;

	public InputField roomNameField = null;
	public InputField roomMaxPlayersField = null;
	public InputField roomPassField = null;

	public GameObject playerUI = null;

	public GameObject errorDialog = null;
	public Text errorText = null;

	void Awake()
	{
		// We abuse unity's matchmaking system to pass around connection info but no match is ever actually joined
		networkMatch = gameObject.AddComponent<NetworkMatch>();

		// Use reflection to get a reference to the private m_ClientId field of NetworkClient
		// We're going to need this later to set the port that client connects from.
		// Even though it's called clientId it is actually the transport level host id that the NetworkClient
		// uses to connect
		clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);

		natHelper = GetComponent<NATHelper_PHP>();
		singleton = this;

		scriptCRCCheck = false;
		NetworkCRC.scriptCRCCheck = false;

		myPort = networkPort;

		System.Type type = typeof(UnityEngine.Networking.NetworkManager);
		var baseMethod = type.GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (baseMethod != null) baseMethod.Invoke(this, null);

		StartCoroutine ("PHPTest");
	}

	IEnumerator PHPTest(){
		WWWForm form = new WWWForm ();
		form.AddField ("phppass", secretKey);
		form.AddField ("action", "test");
		//Dictionary<string, string> headers = form.headers;
		//headers.Add("User-Agent", "UNETTEST");
		//WWW www = new WWW (phpUrl, form.data, headers);

		form.headers.Add ("User-Agent", "UNETTEST");
		WWW www = new WWW (phpUrl, form);

		yield return www;
		Debug.Log ("WWWtest " + www.text);
	}

	public void RefreshRooms(){
		if (!waitingForRefresh) {
			waitingForRefresh = true;
			for (int a = netListParent.childCount - 1; a >= 0; a -= 1) {
				Destroy (netListParent.GetChild (a).gameObject);
			}
			roomsClient.Clear ();
			StartCoroutine ("WaitForRoomRefresh");
		}
	}

	IEnumerator WaitForRoomRefresh(){
		WWWForm form = new WWWForm();
		form.AddField ("action", "getrooms");
		form.AddField ("phppass", secretKey);
		form.AddField ("natcap", natHelper.natCapability);
		form.AddField ("externalip", Network.player.ipAddress);

		WWW www = new WWW (phpUrl, form);
		yield return www;
		string ret = www.text;
		Debug.Log (ret);
		if (!string.IsNullOrEmpty (ret) && ret != "-5") {
			string[] parsed = ret.Split ('#');
			foreach (string a in parsed) {
				string[] parsed2 = a.Split ('@');
				GameObject curEntry = (GameObject)Instantiate (roomEntry);

				RoomEntry entry = curEntry.GetComponent<RoomEntry> ();
				bool locked = int.Parse (parsed2 [2]) == 1;
				roomsClient.Add(parsed2[0], new NetRoomInfo(parsed2[0], parsed2[1], locked, int.Parse(parsed2[3]), int.Parse(parsed2[4])));
				entry.isNetGame = true;
				entry.gameID = parsed2 [0];
				entry.SetName (parsed2 [1]);
				entry.SetLock (locked);
				entry.SetPlayerCount(parsed2[3] + "/" + parsed2[4]);
				entry.SetPlaying (false); // change this later

				curEntry.transform.SetParent (netListParent, false);

			}
		}
		waitingForRefresh = false;
	}

	public override void OnServerDisconnect(NetworkConnection conn){
		if (conn.playerControllers.Count > 0) {
			myRoom.curPlayers = myRoom.curPlayers - 1;
			if (myRoom.isNetGame) {
				StopCoroutine ("updatePlayerCount");
				StartCoroutine ("updatePlayerCount");
			}
		}
		base.OnServerDisconnect (conn);
	}

	public override void OnClientConnect(NetworkConnection conn){
		playerUI.SetActive (true);
		ClientScene.Ready(conn);
		ClientScene.AddPlayer (conn, 0, new StringMsg (myRoom.roomPass));
		//base.OnClientConnect (conn);
	}

	public bool PassCheck (string pass){
		if (string.IsNullOrEmpty (myRoom.roomPass))
			return true;
		if (myRoom.roomPass == pass)
			return true;
		return false;
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
	{
		bool accepted = true;
		string pass = extraMessageReader.ReadMessage<StringMsg> ().value;
		if (myRoom.curPlayers >= myRoom.maxPlayers) {
			accepted = false;
			conn.Send (NetMsgId.displayError, new StringMsg("Room full. Please refresh."));
		} else if (!PassCheck(pass)){
			Debug.Log ("UNACCEPTED ");
			accepted = false;
			conn.Send (NetMsgId.displayError, new StringMsg("Wrong pass"));
		}
		if (accepted) {
			GameObject ply = (GameObject)Instantiate (playerPrefab, Vector3.zero, Quaternion.identity);
			NetworkServer.AddPlayerForConnection (conn, ply, playerControllerId);
			myRoom.curPlayers = myRoom.curPlayers + 1;
			if (myRoom.isNetGame) {
				StopCoroutine ("updatePlayerCount");
				StartCoroutine ("updatePlayerCount");
			}
		} else {
			conn.Disconnect ();
			conn.Dispose ();
		}
	}

	public void DisplayLogMessage(NetworkMessage msg){
		StringMsg parsed = msg.ReadMessage<StringMsg> ();
		DisplayLog (parsed.value);
	}

	public void GetRoomAddressToJoin(string id, string pass){
		StartCoroutine(WaitForAddress(id, pass));
	}

	IEnumerator WaitForAddress(string id, string pass){
		myRoom.roomPass = pass;
		WWWForm form = new WWWForm ();
		form.AddField ("action", "getroomaddress");
		form.AddField ("phppass", secretKey);
		form.AddField ("gameid", id);
		form.AddField ("roompass", pass);
		WWW www = new WWW (phpUrl, form);
		yield return www;
		string ret = www.text;
		Debug.Log (ret);
		if (!string.IsNullOrEmpty(ret)){
			string[] parsed = ret.Split ('#');
			if (int.Parse(parsed[0]) > 0 && parsed.Length > 2){
				myRoom.extIP = parsed[1];
				myRoom.intIP = parsed[2];

				if (myRoom.extIP == natHelper.externalIP) {
					if (myRoom.intIP == Network.player.ipAddress) {
						networkAddress = "127.0.0.1";
					} else {
						networkAddress = myRoom.intIP;
					}
					JoinLocal ();
				} else {
					networkAddress = myRoom.extIP;
				//}
					int hostNatCap = int.Parse(parsed[4]);
					if (hostNatCap == NATHelper_PHP.NATCapabilityIndex.publicIPPortOpen || (hostNatCap == NATHelper_PHP.NATCapabilityIndex.upnp && natHelper.natCapability == NATHelper_PHP.NATCapabilityIndex.upnp)) {
						JoinLocal ();
					} else {
						if (string.IsNullOrEmpty (parsed [3])) {
							DisplayLog ("GUID EMPTY OR NULL");
							StartCoroutine ("getNewGUID");
						} else {
							natHelper.punchThroughToServer (parsed [3], OnHolePunchedGameClient);
						}
					}
				}
			}else{
				DisplayLog (ret);
			}
		}
	}

	public IEnumerator setNewGUID(string newguid){
		if (NetworkServer.active && myRoom.isNetGame) {
			while (string.IsNullOrEmpty (myRoom.hashPass)) {
				yield return new WaitForEndOfFrame ();
			}
			WWWForm form = new WWWForm ();
			form.AddField ("action", "setnewguid");
			form.AddField ("phppass", secretKey);
			form.AddField ("gameid", myRoom.gameID);
			form.AddField ("hashpass", myRoom.hashPass);
			form.AddField ("guid", newguid);

			WWW www = new WWW (phpUrl, form);
			yield return www;
			Debug.Log ("setguid " + www.text);
		} 
		yield return new WaitForEndOfFrame ();

	}

	IEnumerator updatePlayerCount(){
		while (string.IsNullOrEmpty (myRoom.hashPass)) {
			yield return new WaitForEndOfFrame ();
		}
		WWWForm form = new WWWForm ();
		form.AddField ("action", "updateplayercount");
		form.AddField ("phppass", secretKey);
		form.AddField ("gameid", myRoom.gameID);
		form.AddField ("hashpass", myRoom.hashPass);
		form.AddField ("curplayers", myRoom.curPlayers);
		form.AddField ("maxplayers", myRoom.maxPlayers);

		WWW www = new WWW (phpUrl, form);
		yield return www;
		Debug.Log ("update player " + www.text);
	}

	public IEnumerator getNewGUID(){
		yield return new WaitForSecondsRealtime (1.5f);
		WWWForm form = new WWWForm ();
		form.AddField ("action", "getnewguid");
		form.AddField ("phppass", secretKey);
		form.AddField ("gameid", myRoom.gameID);
		form.AddField ("externalip", myRoom.extIP);
		form.AddField ("internalip", myRoom.intIP);
		form.AddField ("roompass", myRoom.roomPass);

		WWW www = new WWW (phpUrl, form);

		yield return www;
		string ret = www.text;
		Debug.Log (ret);
		if (!string.IsNullOrEmpty (ret)) {
			string[] parsed = ret.Split ('#');
			if (int.Parse (parsed [0]) > 0 && parsed.Length > 1 && !string.IsNullOrEmpty(parsed[1])) {
				natHelper.punchThroughToServer (parsed [1], OnHolePunchedGameClient);
			} else {
				DisplayLog ("getguid " + ret);
				StartCoroutine ("getNewGUID");
			}
		}
	}

	public void JoinLocal(){
		networkPort = myPort;
		InitConfig ();
		StartClient ();
	}


	public static void RefreshInfo(){
		/*singleton.guiInfo.text = "Facilitator status : " + (connectedToFac ? "Connected" : "Not connected") + "\n"
		+ "External IP 1 : " + singleton.natHelper.externalIP + "\n"
		+ "Internal IP : " + Network.player.ipAddress + "\n"
		+ "RakNet GUID 1 : " + singleton.natHelper.guid + "\n";*/
	}

	public static void DisplayLog(string err){
		Debug.Log (err);
		singleton.errorText.text = err;
		singleton.errorDialog.SetActive (true);
	}

	public void MyHostGame(bool net){
		if (net) {
			if (natHelper.natCapability < NATHelper_PHP.NATCapabilityIndex.natSymmetrical) {
				DoHost (net);
				if (MyNetworkDiscovery.singleton.passListening)
					MyNetworkDiscovery.singleton.passServer.Stop ();
				if (MyNetworkDiscovery.singleton.isServer || MyNetworkDiscovery.singleton.isClient)
					MyNetworkDiscovery.singleton.StopBroadcast ();
				StopCoroutine ("WaitForNatTestForHost");
				StartCoroutine ("WaitForNatTestForHost");
			} else {
				DisplayLog ("Please use wifi");
			}
		} else {
			DoHost (net);
			MyNetworkDiscovery.singleton.MyStartBroadcast ();
		}
	}

	void DoHost(bool net){
		networkPort = myPort;
		InitConfig ();
		StartHost ();
		myRoom.Clear ();
		myRoom.isNetGame = net;
		myRoom.curPlayers = 0;
		myRoom.maxPlayers = int.Parse(roomMaxPlayersField.text);
		maxConnections = myRoom.maxPlayers;
		myRoom.gameName = roomNameField.text;
		myRoom.roomPass = roomPassField.text;
	}

	IEnumerator WaitForNatTestForHost(){
		while (natHelper.natCapability == 0 || !natHelper.canPunch) {
			yield return new WaitForEndOfFrame ();
		}
		if (natHelper.natCapability < 5) {
			StartCoroutine ("CreateRoomPHP");
			natHelper.startListeningForPunchthrough (OnHolePunchedHost);
		}
	}

	IEnumerator CreateRoomPHP(){
		WWWForm form = new WWWForm ();
		form.AddField ("action", "createroom");
		form.AddField ("phppass", secretKey);
		form.AddField ("externalip", natHelper.externalIP);
		form.AddField ("internalip", Network.player.ipAddress);
		form.AddField ("port", myPort);
		form.AddField ("guid", natHelper.guid);
		form.AddField ("natcap", natHelper.natCapability);
		form.AddField ("gamename", myRoom.gameName);
		form.AddField ("maxplayers", myRoom.maxPlayers);
		form.AddField ("roompass", myRoom.roomPass);
		WWW www = new WWW (phpUrl, form);
		yield return www;
		string ret = www.text;
		Debug.Log ("createroom " + ret);
		string[] parsed = ret.Split('#');
		if (int.Parse(parsed[0]) == 1){
			myRoom.gameID = parsed[1];
			myRoom.hashPass = parsed[2];
			StartCoroutine("PingRoom", 25f);
		}
	}

	IEnumerator PingRoom(float delay){
		while (string.IsNullOrEmpty (myRoom.gameID)) {
			yield return new WaitForEndOfFrame ();
		}
		WWWForm form = new WWWForm();
		form.AddField ("action", "pingroom");
		form.AddField ("phppass", secretKey);
		form.AddField ("gameid", myRoom.gameID);
		form.AddField ("hashpass", myRoom.hashPass);
		while (!string.IsNullOrEmpty (myRoom.hashPass) && NetworkServer.active) {
			yield return new WaitForSecondsRealtime(delay);
			StartCoroutine ("ActualPingRoom", form);
		}
	}

	IEnumerator ActualPingRoom(WWWForm form){
		WWW www = new WWW (phpUrl, form);
		yield return www;
	}



	/**
     * Punches a hole through to the host of the first match in the list
     */
	void OnHolePunchedHost(int natListenPort)
	{
		NATServer newServer = new NATServer();

		bool isListening = newServer.Listen(natListenPort, NetworkServer.hostTopology);
		if (isListening)
		{
			natServers.Add(newServer);
		}
	}


	public void OnHolePunchedGameClient(int natListenPort, int natConnectPort)
	{
		networkPort = natConnectPort;

		Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

		InitConfig ();

		HostTopology topo = new HostTopology(connectionConfig, maxConnections);

		int natListenSocketID = NetworkTransport.AddHost(topo, natListenPort);

		NetworkClient gameClient = new NetworkClient();
		gameClient.Configure(topo);

		// Connect to the port on the server that we punched through to
		gameClient.Connect(networkAddress, networkPort);

		clientIDField.SetValue(gameClient, natListenSocketID);

		UseExternalClient(gameClient);

	}

	public void InitConfig(){
		if (connectionConfig.Channels.Count == 0) {
			NetworkTransport.Init(globalConfig);
			if (customConfig) {
				foreach (QosType type in base.channels) {
					connectionConfig.AddChannel (type);
				}
			} else {
				connectionConfig.AddChannel (QosType.ReliableSequenced);
				connectionConfig.AddChannel (QosType.Unreliable);
			}
		}
	}

	void Update(){
		natServers.ForEach(server => server.Update());
	}

	void OnApplicationQuit(){
		StartCoroutine ("RemoveRoom");
	}

	IEnumerator RemoveRoom(){
		if (!string.IsNullOrEmpty (myRoom.hashPass)) {
			Debug.Log ("removing room");
			WWWForm form = new WWWForm ();
			form.AddField ("phppass", secretKey);
			form.AddField ("action", "removeroom");
			form.AddField ("gameid", myRoom.gameID);
			form.AddField ("hashpass", myRoom.hashPass);
			WWW www = new WWW (phpUrl, form);
			yield return www;
			Debug.Log ("Removeroom " + www.text);
		}
	}

}