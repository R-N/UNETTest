using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Reflection;
using UnityEngine.UI;

public class NATNetworkManager : UnityEngine.Networking.NetworkManager
{
	public static NATNetworkManager singleton = null;
	NATHelper natHelper;
	FieldInfo clientIDField;
	NetworkMatch networkMatch;
	string relayExternalIP, relayInternalIP, hostExternalIP, hostInternalIP;
	List<NetworkServerSimple> natServers = new List<NetworkServerSimple>();
	public Text guiInfo = null;
	public static bool connectedToFac = false;
	public static bool connectedToFac2 = false;
	public static int relayStatus = 0; //0 not connected 1 connected 2 hosting

	public InputField localIPField = null;

	public static int runMode = 0; 
	/// 0 -- not running
	/// 1 -- relay
	/// 2 -- host
	/// 3 -- client

	public class RoomInfo : MessageBase{
		public string externalIP;
		public string internalIP;
		public string guid;
		public string info;
		public RoomInfo(string newEIP, string newIIP, string newGUID, string newInfo){
			externalIP = newEIP;
			internalIP = newIIP;
			guid = newGUID;
			info = newInfo;
		}
		public RoomInfo(){
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

	public static class NetMsg
	{
		public const short SendRoom = 500;
		public const short CreateRoom = 501;
		public const short UpdateRoom = 502;
		public const short JoinRoom = 503;
		public const short RMPunched = 504;
	}

	public class PunchMsg : MessageBase{
		public int natListenPort;
		public int natConnectPort;
		public string clientGUID;
		public PunchMsg(){
		}

		public PunchMsg(int newLP, int newCP, string newGUID){
			natListenPort = newLP;
			natConnectPort = newCP;
			clientGUID = newGUID;
		}
	}

	public Dictionary<NetworkConnection, RoomInfo> rooms = new Dictionary<NetworkConnection, RoomInfo>();
	public List<NetworkConnection> connections = new List<NetworkConnection> ();
	public List<NetworkConnection> roomMasters = new List<NetworkConnection>();
	public List<RoomInfo> roomsClient = new List<RoomInfo>();
	public Dictionary<string, NetworkConnection> waitJoin = new Dictionary<string, NetworkConnection>();
	public NetworkClient relayClient = null;


	void Awake()
	{
		// We abuse unity's matchmaking system to pass around connection info but no match is ever actually joined
		networkMatch = gameObject.AddComponent<NetworkMatch>();

		// Use reflection to get a reference to the private m_ClientId field of NetworkClient
		// We're going to need this later to set the port that client connects from.
		// Even though it's called clientId it is actually the transport level host id that the NetworkClient
		// uses to connect
		clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);

		natHelper = GetComponent<NATHelper>();
		singleton = this;

		scriptCRCCheck = false;
		NetworkCRC.scriptCRCCheck = false;

		System.Type type = typeof(UnityEngine.Networking.NetworkManager);
		var baseMethod = type.GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (baseMethod != null) baseMethod.Invoke(this, null);
	}

	IEnumerator WaitForRelayConnection(){
		while (relayStatus == 0) {
			if (relayClient.isConnected) {
				Debug.Log ("RELAY CONNECTED");
				relayStatus = 1;
				StartCoroutine (CheckRelayConnection ());
				RefreshInfo ();
				RefreshRooms ();
				StopCoroutine (WaitForRelayConnection ());
				break;
			} 
			yield return new WaitForSecondsRealtime (0.1f);
		}
	}

	IEnumerator CheckRelayConnection(){
		while (true) {
			if (!relayClient.isConnected) {
				Debug.Log ("RELAY DISCONNECTED");
				relayStatus = 0;
				StopCoroutine (CheckRelayConnection ());
				break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
	}

	public void MyJoinLocal(){
		networkAddress = localIPField.text;
		networkPort = 2297;
		autoCreatePlayer = true;
		StartClient ();
	}

	public void OnRequestedRoom(NetworkMessage msg){
		Debug.Log ("REQUESTED ROOMS");
		Debug.Log ("CONNS " + connections.Count);
		if (!connections.Contains (msg.conn)) {
			connections.Add (msg.conn);
		}
			for (int a = 0; a < roomMasters.Count; a++) {
				if (rooms.ContainsKey (roomMasters [a])) {
					msg.conn.Send (NetMsg.SendRoom, rooms [roomMasters [a]]);
				}
			}
	}

	public void OnReceivedRoom(NetworkMessage msg){
		Debug.Log ("GOT ROOM");
		roomsClient.Add (msg.ReadMessage<RoomInfo> ());
	}

	public void OnRoomCreateRequest(NetworkMessage msg){
		Debug.Log ("CReATING ROOM");
		rooms.Add(msg.conn, msg.ReadMessage<RoomInfo>());
		roomMasters.Add (msg.conn);
		for (int a = 0; a < connections.Count; a++) {
			connections [a].Send (NetMsg.SendRoom, msg.ReadMessage<RoomInfo> ());
		}
	}

	public void OnRMPunchedRelay(NetworkMessage msg){
		PunchMsg parsed = msg.ReadMessage<PunchMsg> ();
		if (waitJoin.ContainsKey (parsed.clientGUID)) {
			Debug.Log ("RM PUNCHED NOW RELAYING PUNCHMSG TO CLIENT RIGHT AWAY");
			waitJoin [parsed.clientGUID].Send (NetMsg.RMPunched, parsed);
		} else {
			Debug.Log ("RM PUNCHED BUT GUID NOT REGISTERED, WAITING");
			StartCoroutine(WaitForJoinRequest(parsed));
		}
	}

	IEnumerator WaitForJoinRequest(PunchMsg msg){
		for (int a = 0; a < 600; a++) {
			if (waitJoin.ContainsKey (msg.clientGUID)) {
				Debug.Log ("WAITED, RELAYING PUNCHMSG TO CLIENT");
				waitJoin [msg.clientGUID].Send (NetMsg.RMPunched, msg);
				break;
			} else {
				yield return new WaitForSecondsRealtime (0.1f);
			}
		}
	}

	public void OnRMPunchedClient(NetworkMessage msg){
		Debug.Log ("YO FAM PUNCHMSG RECEIVED ON CLIENT");
		PunchMsg parsed = msg.ReadMessage<PunchMsg> ();
		//natHelper.rakPeer.Shutdown (0);
		OnHolePunchedGameClient (parsed.natListenPort, parsed.natConnectPort);
	}

	public void OnJoinRoomRequest(NetworkMessage msg){
		Debug.Log ("CLIENT REQUESTING JOIN ROOM");
		StringMsg parsed = msg.ReadMessage<StringMsg> ();
		if (!waitJoin.ContainsKey (parsed.value)) {
			waitJoin.Add (parsed.value, msg.conn);
		}
	}

	public void RefreshRooms(){
		roomsClient.Clear ();
		Debug.Log ("REQUESTING ROOM");
		relayClient.Send (NetMsg.SendRoom, new IntMsg(0));
	}

	public static void RefreshInfo(){
		string relayString = "Not connected";
		if (relayStatus == 1)
			relayString = "Connected";
		else if (relayStatus == 2)
			relayString = "Hosting";
		
		singleton.guiInfo.text = "Facilitator 1 status : " + (connectedToFac ? "Connected" : "Not connected") + "\n"
			+ "Facilitator 2 status : " + (connectedToFac2 ? "Connected" : "Not connected") + "\n"
			+ "External IP 1 : " + singleton.natHelper.externalIP + "\n"
			+ "External IP 2 : " + singleton.natHelper.externalIP2 + "\n"
			+ "Internal IP : " + Network.player.ipAddress + "\n"
			+ "RakNet GUID 1 : " + singleton.natHelper.guid + "\n"
			+ "RakNet GUID 2 : " + singleton.natHelper.guid2 + "\n"
			+ "Relay server status : " + relayString;
	}

	public override void OnServerDisconnect(NetworkConnection conn){
		if (runMode == 1) {
			if (rooms.ContainsKey (conn)) {
				rooms.Remove (conn);
			}
			if (connections.Contains (conn)) {
				connections.Remove (conn);
			}
			if (roomMasters.Contains (conn)) {
				roomMasters.Remove (conn);
			}
			base.OnServerDisconnect (conn);
		}
	}

	public void MyConnectToRelay(){
		autoCreatePlayer = false;
		networkMatch.ListMatches(0, 1, "", true, 0, 0, OnConnectRelay);
	}

	public void MyHostRelay(){
		// This is how RakNet identifies each peer in the network
		// Clients will use the guid of the server to punch a hole
		autoCreatePlayer = false;

		string guid = natHelper.guid;
		runMode = 1;
		// The easiest way I've found to get all the connection data to the clients is to just
		// mash it all together in the match name
		string name = string.Join(":", new string[] { natHelper.externalIP, Network.player.ipAddress, guid });
		networkMatch.CreateMatch(name, 2, true, "", natHelper.externalIP, Network.player.ipAddress, 0, 0, OnHostRelay);
	}

	public void MyHostGame(){
		networkPort = 2297;
		autoCreatePlayer = true;
		runMode = 2;
		StartHost ();
		if (relayStatus == 1){
			relayClient.Send (NetMsg.CreateRoom, new RoomInfo (natHelper.externalIP2, Network.player.ipAddress, natHelper.guid2, ""));
		}
		if (connectedToFac){
			natHelper.startListeningForPunchthrough2(OnHolePunchedHost);
		}
	}

	public void MyJoinGame(){
		if (roomsClient.Count > 0) {
			hostExternalIP = roomsClient [0].externalIP;
			hostInternalIP = roomsClient [0].internalIP;
			networkPort = 2297;
			autoCreatePlayer = true;
			runMode = 3;

			Debug.Log ("Joining " + hostExternalIP + " | " + hostInternalIP + " | " + roomsClient [0].guid);
			relayClient.Send (NetMsg.JoinRoom, new StringMsg (natHelper.guid2));
			natHelper.punchThroughToServer2 (roomsClient [0].guid, OnHolePunchedGameClient);
		} else {
			Debug.Log ("NO ROOM");
		}
	}


	#region Network events ------------------------------------------------------------------------

	/**
     * We start a host as normal but we also tell the NATHelper to start listening for incoming punchthroughs. 
     */
	public void OnHostRelay(bool success, string extendedInfo, MatchInfo matchInfo)
	{
		// It's important here that we host via NetworkServer.ListenRelay otherwise the match will
		// be delisted from the UNet matchmaking service after 30 seconds and clients will not be
		// able to find the game.
		// Luckily this is exactly what unity does by default in OnMatchCreate so it's all good
		base.OnMatchCreate(success, extendedInfo, matchInfo);

		NetworkServer.RegisterHandler (NetMsg.SendRoom, OnRequestedRoom);
		NetworkServer.RegisterHandler (NetMsg.CreateRoom, OnRoomCreateRequest);
		NetworkServer.RegisterHandler (NetMsg.JoinRoom, OnJoinRoomRequest);
		NetworkServer.RegisterHandler (NetMsg.RMPunched, OnRMPunchedRelay);
		natHelper.startListeningForPunchthrough(OnHolePunchedHost);
		relayStatus = 2;
		Debug.Log ("Hosting Relay Server");
		RefreshInfo ();
		natHelper.canvas.SetActive (false);
		Application.LoadLevel (1);
	}

	/**
     * Punches a hole through to the host of the first match in the list
     */
	public void OnConnectRelay(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
	{
		if (!success || matchList.Count == 0) {
			Debug.Log ("NO RELAY SERVER FOUND");
			natHelper.OnNoRelayUp ();
			return;
		}

		string[] data = matchList[0].name.Split(':');

		relayExternalIP = data[0];
		relayInternalIP = data[1];
		string relayGUID = data[2];

		natHelper.punchThroughToServer(relayGUID, OnHolePunchedRelayClient);

		// This would be a good time to try and make a direct connection without NAT Punchthrough
		// If the connection succeeds then the code in OnHolePunchedClient isn't necessary
	}

	/**
     * Server received a hole punch from a client
     * Start up a new NATServer listening on the newly punched hole
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

	void OnHolePunchedRelayClient(int natListenPort, int natConnectPort)
	{
		// The port on the server that we are connecting to
		networkPort = natConnectPort;

		// Make sure to connect to the correct IP or things won't work
		if (relayExternalIP == natHelper.externalIP)
		{
			if (relayInternalIP == Network.player.ipAddress)
			{
				// Host is running on the same computer as client, two separate builds
				networkAddress = "127.0.0.1";
			}
			else
			{
				// Host is on the same local network as client
				networkAddress = relayInternalIP;
			}
		}
		else
		{
			// Host is somewhere out on the internet
			networkAddress = relayExternalIP;
		}

		Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

		// Standard client setup stuff than the NetworkMager would normally take care of for us
		NetworkTransport.Init(globalConfig);
	
		if (customConfig)
		{
			foreach (QosType type in base.channels)
			{
				connectionConfig.AddChannel(type);
			}
		}
		else
		{
			connectionConfig.AddChannel(QosType.ReliableSequenced);
			connectionConfig.AddChannel(QosType.Unreliable);
		}

		Debug.Log("channel count " + connectionConfig.Channels.Count);

		// If we try to use maxConnection when the Advanced Configuration checkbox is not checked
		// we will get a crc mismatch because the host will have been started with the default
		// max players of 8 rather than the value in maxConnections
		int maxPlayers = 8;
		if (customConfig)
		{
			maxPlayers = maxConnections;
		}

		HostTopology topo = new HostTopology(connectionConfig, maxPlayers);

		// Start up a transport level host on the port that the hole was punched from
		int natListenSocketID = NetworkTransport.AddHost(topo, natListenPort);

		// Create and configure a new NetworkClient
		relayClient = new NetworkClient();
		relayClient.Configure(topo);

		// Connect to the port on the server that we punched through to
		relayClient.Connect(networkAddress, networkPort);
		relayClient.RegisterHandler (NetMsg.SendRoom, OnReceivedRoom);
		relayClient.RegisterHandler (NetMsg.RMPunched, OnRMPunchedClient);

		// Magic! Set the client's transport level host ID so that the client will use
		// the host we just started above instead of the one it creates internally when we call Connect.
		// This has to be done so that the connection will be made from the correct port, otherwise
		// Unity will use a random port to connect from and NAT Punchthrough will fail.
		// This is the shit that keeps me up at night.
		clientIDField.SetValue(relayClient, natListenSocketID);

		// Tell Unity to use the client we just created as _the_ client so that OnClientConnect will be called
		// and all the other HLAPI stuff just works. Oh god, so nice.
		//UseExternalClient(relayClient);
		StartCoroutine(WaitForRelayConnection());
	}


	void OnHolePunchedGameClient(int natListenPort, int natConnectPort)
	{
		networkPort = natConnectPort;

		if (hostExternalIP == natHelper.externalIP)
		{
			if (hostInternalIP == Network.player.ipAddress)
			{
				networkAddress = "127.0.0.1";
			}
			else
			{
				networkAddress = hostInternalIP;
			}
		}
		else
		{
			networkAddress = hostExternalIP;
		}

		Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

		NetworkTransport.Init(globalConfig);
		/*if (customConfig)
		{
			foreach (QosType type in base.channels)
			{
				connectionConfig.AddChannel(type);
			}
		}
		else
		{
			connectionConfig.AddChannel(QosType.ReliableSequenced);
			connectionConfig.AddChannel(QosType.Unreliable);
		}*/ //probably not needed anymore since it has been done before


		Debug.Log("channel count " + connectionConfig.Channels.Count);

		int maxPlayers = 8;
		if (customConfig)
		{
			maxPlayers = maxConnections;
		}

		HostTopology topo = new HostTopology(connectionConfig, maxPlayers);

		int natListenSocketID = NetworkTransport.AddHost(topo, natListenPort);

		NetworkClient gameClient = new NetworkClient();
		gameClient.Configure(topo);

		// Connect to the port on the server that we punched through to
		gameClient.Connect(networkAddress, networkPort);
		gameClient.RegisterHandler (NetMsg.SendRoom, OnReceivedRoom);

		clientIDField.SetValue(gameClient, natListenSocketID);

		UseExternalClient(gameClient);

	}

	#endregion Network events ---------------------------------------------------------------------

	void Update()
	{
		for (int a = connections.Count - 1; a >= 0; a -= 1) {
			if (!connections [a].isConnected) {
				if (rooms.ContainsKey (connections [a])) {
					rooms.Remove (connections [a]);
				}
				if (roomMasters.Contains (connections [a])) {
					roomMasters.Remove (connections [a]);
				}
				connections.RemoveAt (a);
			}
		}
		natServers.ForEach(server => server.Update());
	}

}