using UnityEngine;
using UnityEngine.UI;
using RakNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Open;
using Open.Nat;
using System.Threading;
using System.Threading.Tasks;

/**
 * Uses RakNet to perform NAT Punchthrough using an externally hosted Facilitator.
 * Once a hole is punched a connection can be made via UNet HLAPI.
 * Requires RakNet.dll and c# wrappers. Also requires the NATCompleteServer from 
 * the RakNet samples to be running on an external server somewhere.
 */



public class NATHelper_PHP : MonoBehaviour
{
	// IP Addres and port of the server running NATCompleteServer
	public string facilitatorIP = "50.57.111.104"; 
	public ushort facilitatorPort = 61111;

	public class NATCapabilityIndex
	{
		public const int undetermined = 0;
		public const int publicIPPortOpen = 1;
		public const int upnp = 2;
		public const int natFullCone = 3;
		public const int natAddressRestricted = 4;
		public const int natPortRestricted = 5;
		public const int natSymmetrical = 6;

		/*
		 * 
		 * don't give out room if sum of the natcapindex > 9
		 */
	}

	[NonSerialized]
	public string externalIP;
	[NonSerialized]
	public bool isReady;
	[NonSerialized]
	public string guid;

	NatPunchthroughClient natPunchthroughClient = null;
	SystemAddress facilitatorSystemAddress = null;
	bool firstTimeConnect = true;
	Action<int> onHolePunchedServer;
	Action<int, int> onHolePunchedClient;
	public RakPeerInterface rakPeer = null;

	[NonSerialized]
	public bool isReadyTest;
	[NonSerialized]
	public string guidTest;
	NatPunchthroughClient natPunchthroughClientTest;
	SystemAddress facilitatorSystemAddressTest;
	bool firstTimeConnectTest = true;
	public RakPeerInterface rakPeerTest;
	bool connectedToFacTest = false;

	public bool canPunch = false;

	public GameObject canvas = null;

	ConnectionTesterStatus ipStatus = ConnectionTesterStatus.Undetermined;
	ConnectionTesterStatus natStatus = ConnectionTesterStatus.Undetermined;

	public int natCapability = 0;

	RakNetGUID serverGuid;

	public GameObject netButton = null;


	NatDevice device = null;
	bool hasUpnp = false;

	public float joinTime = 0;
	public float joinTimeout = 10;

	void Start()
	{
		StartCoroutine ("WaitForNetwork");

	}

	IEnumerator WaitDiscover(){
		NatDiscoverer disc = new NatDiscoverer();
		CancellationTokenSource cts = new CancellationTokenSource ();
		cts.CancelAfter (10000);
		int gamePort = NATNetworkManager_PHP.singleton.myPort;
		Task<NatDevice> deviceTask = disc.DiscoverDeviceAsync(PortMapper.Upnp, cts);
		//deviceTask.RunSynchronously ()
		while (!deviceTask.IsCompleted && !deviceTask.IsCanceled) {
			yield return new WaitForEndOfFrame ();
		}
		if (deviceTask.IsCompleted) {
			bool hasException = false;
			try {
				device = deviceTask.Result;
			} catch (Exception ex) {
				Debug.Log ("No Upnp. FurtherInfo :" + ex.ToString());
				hasException = true;
			}finally{
				if (!hasException && device != null) {
					NATNetworkManager_PHP.DisplayLog("NAT DEVICE FOUND");
					Task portMapTask = device.CreatePortMapAsync (new Mapping (Protocol.Udp, gamePort, gamePort));
					//portMapTask.RunSynchronously ();
					StartCoroutine ("WaitUpnp", portMapTask);
				}
			}
		}
		yield return new WaitForEndOfFrame ();
	}

	IEnumerator WaitUpnp(Task portMapTask){
		float cancelTime = Time.realtimeSinceStartup + 10;
		while (!portMapTask.IsCompleted && !portMapTask.IsCanceled && cancelTime > Time.realtimeSinceStartup) {
			yield return new WaitForEndOfFrame ();
		}
		hasUpnp = portMapTask.IsCompleted;
		NATNetworkManager_PHP.DisplayLog ("HAS UPNP " + hasUpnp);
		if (!hasUpnp && !portMapTask.IsCanceled)
			portMapTask.Dispose ();
	}

	IEnumerator WaitForNetwork(){
		for (int a = 0; a < 600; a++){
			if (Application.internetReachability != NetworkReachability.NotReachable) {
				StartCoroutine ("TestNAT");
				StartCoroutine ("TestIP");
				StartCoroutine("WaitDiscover");
				//StartCoroutine ("WaitForTest");
				//joinNet.SetActive (true);
				TryCoroutine ();
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
		if (Application.internetReachability == NetworkReachability.NotReachable) {
			NATNetworkManager_PHP.DisplayLog ("Not connected to any network.");
			StartCoroutine ("WaitForNetwork");
		}
	}

	IEnumerator TestIP(){
		while (Network.TestConnection () == ConnectionTesterStatus.Undetermined) {
			yield return new WaitForEndOfFrame ();
		}
		ipStatus = Network.TestConnection ();
		UnityEngine.Debug.Log (Network.TestConnection ());

	}

	IEnumerator TestNAT(){
		while (Network.TestConnectionNAT () == ConnectionTesterStatus.Undetermined) {
			yield return new WaitForEndOfFrame ();
		}
		natStatus = Network.TestConnectionNAT ();
		UnityEngine.Debug.Log (Network.TestConnectionNAT ());
	}

	void TryCoroutine(){
		ConnectFac ();
		ConnectFacTest ();
		StartCoroutine ("WaitForRaknet");
	}

	IEnumerator WaitForRaknet(){
		for (int a = 0; a < 600; a++){
			bool testDone = false;
			if (ipStatus != ConnectionTesterStatus.Undetermined && natStatus != ConnectionTesterStatus.Undetermined) {
				testDone = true;
			}
			if (ipStatus == ConnectionTesterStatus.Error) {
				ipStatus = ConnectionTesterStatus.Undetermined;
				testDone = false;
				Network.TestConnection (true);
				StartCoroutine ("TestIP");
			}
			if (natStatus == ConnectionTesterStatus.Error) {
				natStatus = ConnectionTesterStatus.Undetermined;
				testDone = false;
				Network.TestConnectionNAT (true);
				StartCoroutine ("TestNAT");
			}
			if (NATNetworkManager_PHP.connectedToFac && testDone) {
				if (ipStatus == ConnectionTesterStatus.PublicIPIsConnectable || ipStatus == ConnectionTesterStatus.PublicIPNoServerStarted) {
					natCapability = NATCapabilityIndex.publicIPPortOpen;
				} else if (hasUpnp) {
					natCapability = NATCapabilityIndex.upnp;
				} else {
					switch (ipStatus) { 
					case ConnectionTesterStatus.NATpunchthroughFullCone:
						{
							natCapability = NATCapabilityIndex.natFullCone;
							break;
						}
					case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
						{
							natCapability = NATCapabilityIndex.natAddressRestricted;
							break;
						}
					case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
						{
							natCapability = NATCapabilityIndex.natPortRestricted;
							break;
						}
					case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
						{
							natCapability = NATCapabilityIndex.natSymmetrical;
							break;
						}
					}
				}
				if (!connectedToFacTest)
					ConnectFacTest ();
				StartCoroutine ("WaitForRaknetTest");
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
		if (!NATNetworkManager_PHP.connectedToFac) {
			NATNetworkManager_PHP.DisplayLog ("ConnectFac failed");
			if (Application.internetReachability == NetworkReachability.NotReachable) {
				StartCoroutine ("WaitForNetwork");
			} else {
				TryCoroutine ();
			}
		}
	}

	IEnumerator WaitForRaknetTest(){
		for (int a = 0; a < 600; a++){
			if (connectedToFacTest) {
				yield return new WaitForSecondsRealtime (1);
				StopCoroutine("waitForIncomingNATPunchThroughOnServer");
				onHolePunchedServer = EmptyMethod;
				StartCoroutine("waitForIncomingNATPunchThroughOnServer");
				punchThroughToServerTest (guid);
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
	}

	void EmptyMethod(int natListenPort){
	}

	public void onHolePunchedTest(int natListenPort, int natConnectPort){
		canPunch = true;
		netButton.SetActive (true);
	}


	public void ConnectFac(){
		if (!NATNetworkManager_PHP.connectedToFac) {
			rakPeer = RakPeerInterface.GetInstance ();
			//rakPeer = natPunchthroughClient.GetRakPeerInterface();
			//natPunchthroughClient = null;
			//facilitatorSystemAddress = null;
			StopCoroutine ("connectToNATFacilitator");
			StartCoroutine ("connectToNATFacilitator");
		}
	}

	public void ConnectFacTest(){
		if (!connectedToFacTest) {
			rakPeerTest = RakPeerInterface.GetInstance ();
			//natPunchthroughClientTest = null;
			//facilitatorSystemAddressTest = null;
			StopCoroutine ("connectToNATFacilitatorTest");
			StartCoroutine ("connectToNATFacilitatorTest");
		}
	}


	IEnumerator connectToNATFacilitator()
	{
		StartupResult startResult = rakPeer.Startup(2, new SocketDescriptor(), 1);
		if (startResult != StartupResult.RAKNET_STARTED)
		{
			NATNetworkManager_PHP.DisplayLog("Failed to initialize network interface: " + startResult.ToString());
			yield break;
		}
		ConnectionAttemptResult connectResult = rakPeer.Connect(facilitatorIP, (ushort)facilitatorPort, null, 0);
		if (connectResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
		{
			NATNetworkManager_PHP.DisplayLog("Failed to initialize connection to NAT Facilitator: " + connectResult.ToString());
			yield break;
		}

		Packet packet;
		while ((packet = rakPeer.Receive()) == null) yield return new WaitForEndOfFrame();

		if (packet.data[0] != (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED)
		{
			NATNetworkManager_PHP.DisplayLog("Failed to connect to NAT Facilitator: " + ((DefaultMessageIDTypes)packet.data[0]));
			NATNetworkManager_PHP.RefreshInfo ();
			yield break;
		}

		guid = rakPeer.GetMyGUID().g.ToString();
		Debug.Log("Connected: " + guid);


		externalIP = rakPeer.GetExternalID(packet.systemAddress).ToString(false);

		NATNetworkManager_PHP.RefreshInfo ();



		//if (firstTimeConnect) {
		//	firstTimeConnect = false;
		natPunchthroughClient = new NatPunchthroughClient ();
		rakPeer.AttachPlugin (natPunchthroughClient);

		natPunchthroughClient.FindRouterPortStride (packet.systemAddress);
			facilitatorSystemAddress = packet.systemAddress;
		/*} else {
			rakPeer.AttachPlugin (natPunchthroughClient);
			//if (!facilitatorSystemAddress.EqualsExcludingPort(packet.systemAddress)) {
				natPunchthroughClient.FindRouterPortStride (packet.systemAddress);
			//}
			facilitatorSystemAddress = packet.systemAddress;
		}*/
		if (NetworkServer.active && NATNetworkManager_PHP.singleton.myRoom.isNetGame)
			StartCoroutine ("waitForIncomingNATPunchThroughOnServer");

		if (string.IsNullOrEmpty (guid)) {
			NATNetworkManager_PHP.DisplayLog ("GUID IS NULL OR EMPTY");
		} else {
			NATNetworkManager_PHP.singleton.StartCoroutine ("setNewGUID", guid);
		}

		NATNetworkManager_PHP.connectedToFac = true;
		rakPeer.SetMaximumIncomingConnections(2);
		isReady= true;
	}


	IEnumerator connectToNATFacilitatorTest()
	{
		StartupResult startResult = rakPeerTest.Startup(2, new SocketDescriptor(), 1);
		if (startResult != StartupResult.RAKNET_STARTED)
		{
			Debug.Log("Failed to initialize network interface 2: " + startResult.ToString());
			yield break;
		}
		ConnectionAttemptResult connectResult = rakPeerTest.Connect(facilitatorIP, (ushort)facilitatorPort, null, 0);
		if (connectResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
		{
			Debug.Log("Failed to initialize connection to NAT Facilitator 2: " + connectResult.ToString());
			yield break;
		}

		Packet packet;
		while ((packet = rakPeerTest.Receive()) == null) yield return new WaitForEndOfFrame();

		if (packet.data[0] != (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED)
		{
			Debug.Log("Failed to connect to NAT Facilitator 2: " + ((DefaultMessageIDTypes)packet.data[0]));
			yield break;
		}

		guidTest = rakPeerTest.GetMyGUID().g.ToString();
		Debug.Log("Connected 2: " + guidTest);

		facilitatorSystemAddressTest = packet.systemAddress;

		externalIP = rakPeerTest.GetExternalID(packet.systemAddress).ToString(false);

		rakPeerTest.SetMaximumIncomingConnections(2);

		if (firstTimeConnectTest) {
			firstTimeConnectTest = false;

			natPunchthroughClientTest = new NatPunchthroughClient ();
			rakPeerTest.AttachPlugin (natPunchthroughClientTest);

			natPunchthroughClientTest.FindRouterPortStride (facilitatorSystemAddressTest);
		} else {
			StartCoroutine ("waitForIncomingNATPunchThroughOnServerTest");
		}
		connectedToFacTest = true;

		isReadyTest= true;
	}








	/**
     * Called on the server to start listening for clients trying to punch through
     */
	public void startListeningForPunchthrough(Action<int> leonHolePunched)
	{
		this.onHolePunchedServer = leonHolePunched;
		StopCoroutine ("waitForIncomingNATPunchThroughOnServer");
		StartCoroutine("waitForIncomingNATPunchThroughOnServer");
	}


	IEnumerator waitForIncomingNATPunchThroughOnServer()
	{
		ushort natListenPort = 0;
		NatPunchthroughClient cli = natPunchthroughClient;
		RakPeerInterface peer = rakPeer;
		SystemAddress facAddr = facilitatorSystemAddress;
		Action<int> onHolePunched = onHolePunchedServer;
		while (true)
		{
			yield return new WaitForEndOfFrame();

			// Check for incoming packet
			Packet packet = peer.Receive();

			// No packet, maybe next time
			if (packet == null) continue;

			// Got a packet, see what it is
			RakNet.DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];
			switch (messageType)
			{
			case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_SUCCEEDED:
				bool weAreTheSender = packet.data[1] == 1;
				if (!weAreTheSender)
				{
					// Someone successfully punched through to us
					// natListenPort is the port that the server should listen on
					natListenPort = peer.GetInternalID().GetPort();
					// Now we're waiting for the client to try and connect to us
					Debug.Log("Received punch through from " + packet.guid + " " + packet.systemAddress.ToString());


					// Reconnect to Facilitator for next punchthrough
					NATNetworkManager_PHP.connectedToFac = false;
					//firstTimeConnect = true;
					//rakPeer = null;
					ConnectFac();

				}
				break;

			case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
				// Cool we've got a connection. The client now knows which port to connect from / to
				Debug.Log("Received incoming RakNet connection.");
				// Close the connection to the client
				peer.CloseConnection(packet.guid, true);
				// And also to the Facilitator
				peer.CloseConnection(facAddr, true);
				break;

			case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
				// Once the Facilitator has disconnected we shut down raknet
				// so that UNet can listen on the port that RakNet is currently listening on
				// At this point the hole is succesfully punched
				// RakNet is then reconnected to the facilitator on a new random port.
				if (packet.systemAddress == facAddr)
				{
					peer.Shutdown(0);

					// Hole is punched, UNet can start listening
					onHolePunched(natListenPort);

					yield break; // Totally done
				}
				break;

			default:
				Debug.Log(((DefaultMessageIDTypes)packet.data[0]).ToString());
				break;
			}
		}
	}





	public void punchThroughToServer(string hostGUIDString, Action<int, int> leonHolePunched)
	{
		RakNetGUID hostGUID = new RakNetGUID(ulong.Parse(hostGUIDString));
		natPunchthroughClient.OpenNAT (hostGUID, facilitatorSystemAddress);
		onHolePunchedClient = leonHolePunched;
		StartCoroutine ("waitForResponseFromServer", hostGUID);
	}

	public void punchThroughToServerTest(string hostGUIDString)
	{
		RakNetGUID hostGUID = new RakNetGUID(ulong.Parse(hostGUIDString));
		natPunchthroughClientTest.OpenNAT (hostGUID, facilitatorSystemAddressTest);
		StartCoroutine ("waitForResponseFromServerTest", hostGUID);
	}







	IEnumerator waitForResponseFromServer(RakNetGUID hostGUID)
	{
		ushort natListenPort = 0, natConnectPort = 0;
		while (true)
		{
			Packet packet = rakPeer.Receive();
			if (packet != null)
			{
				DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];

				switch (messageType)
				{
				case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_SUCCEEDED:
					// A hole has been punched but we're not done yet. We need to actually connect (via RakNet)
					// to the server so that we can get the port to connect from
					// natConnectPort is the external port of the server to connect to
					natConnectPort = packet.systemAddress.GetPort ();
					rakPeer.Connect(packet.systemAddress.ToString(false), natConnectPort, "", 0);
					Debug.Log("Hole punched!" + packet.systemAddress.GetPort());
					break;

				case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
					// Ok, we connected, we can now use GetExternalID to get the port to connect from.
					natListenPort = rakPeer.GetExternalID (packet.systemAddress).GetPort ();
					Debug.Log("Connected");
					// Now we wait for the server to disconnect us so that we know it is ready for the UNet connection
					break;

				case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
					// Server has disconnected us. We are ready to connect via UNet

					// Shut down RakNet
					rakPeer.Shutdown (0);
					// Hole is punched!
					onHolePunchedClient (natListenPort, natConnectPort);

					//StartCoroutine(connectToNATFacilitator());

					yield break; // Totally done
				case DefaultMessageIDTypes.ID_NAT_CONNECTION_TO_TARGET_LOST:
					{
						NATNetworkManager_PHP.DisplayLog ("target connection lost"); //when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_TARGET_UNRESPONSIVE:
					{
						NATNetworkManager_PHP.DisplayLog ("unresponsive");//when three players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_FAILED:
					{
						NATNetworkManager_PHP.DisplayLog ("fail"); //when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_TARGET_NOT_CONNECTED:
					{
						NATNetworkManager_PHP.DisplayLog ("target not connected");//when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NO_FREE_INCOMING_CONNECTIONS:
					{
						NATNetworkManager_PHP.DisplayLog ("no free incoming connections");//when three players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				default:
					NATNetworkManager_PHP.DisplayLog("RakNet Client received unexpected message type: " + ((DefaultMessageIDTypes)packet.data[0]).ToString());
					yield break;
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}

	void TryRepunch(RakNetGUID hostGUID, SystemAddress address){
		rakPeer.CancelConnectionAttempt (address);
		rakPeer.CloseConnection (hostGUID, false);
		StopCoroutine ("waitForResponseFromServer");
		NATNetworkManager_PHP.singleton.StartCoroutine ("getNewGUID");
	}

	void TryRepunchTest(RakNetGUID hostGUID, SystemAddress address){
		rakPeerTest.CancelConnectionAttempt (address);
		rakPeerTest.CloseConnection (hostGUID, false);
		StopCoroutine ("waitForResponseFromServerTest");
		natPunchthroughClientTest.OpenNAT (hostGUID, facilitatorSystemAddressTest);
		StartCoroutine ("waitForResponseFromServerTest", hostGUID);
	}

	IEnumerator waitForResponseFromServerTest(RakNetGUID hostGUID)
	{
		ushort natListenPort = 0, natConnectPort = 0;
		while (true)
		{
			Packet packet = rakPeerTest.Receive();
			if (packet != null)
			{
				DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];

				switch (messageType)
				{
				case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_SUCCEEDED:
					// A hole has been punched but we're not done yet. We need to actually connect (via RakNet)
					// to the server so that we can get the port to connect from
					// natConnectPort is the external port of the server to connect to
					natConnectPort = packet.systemAddress.GetPort ();
					rakPeerTest.Connect(packet.systemAddress.ToString(false), natConnectPort, "", 0);
					Debug.Log("Hole punched!" + packet.systemAddress.GetPort());
					break;

				case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
					// Ok, we connected, we can now use GetExternalID to get the port to connect from.
					natListenPort = rakPeerTest.GetExternalID (packet.systemAddress).GetPort ();
					Debug.Log("Connected");
					// Now we wait for the server to disconnect us so that we know it is ready for the UNet connection
					break;

				case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
					// Server has disconnected us. We are ready to connect via UNet

					// Shut down RakNet
					rakPeerTest.Shutdown(0);
					connectedToFacTest = false;
					// Hole is punched!
					onHolePunchedTest (natListenPort, natConnectPort);

					//ConnectFacTest (); //reconnect for server
					//StartCoroutine(connectToNATFacilitator());

					yield break; // Totally done

				case DefaultMessageIDTypes.ID_NAT_CONNECTION_TO_TARGET_LOST:
					{
						Debug.Log ("target connection lost"); //when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_TARGET_UNRESPONSIVE:
					{
						Debug.Log ("unresponsive");//when three players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_FAILED:
					{
						Debug.Log ("fail"); //when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NAT_TARGET_NOT_CONNECTED:
					{
						Debug.Log ("target not connected");//when two players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				case DefaultMessageIDTypes.ID_NO_FREE_INCOMING_CONNECTIONS:
					{
						Debug.Log ("no free incoming connections");//when three players conecting at same time
						TryRepunch(hostGUID, packet.systemAddress );
						yield break;
					}
				default:
					Debug.Log("RakNet Client received unexpected message type: " + ((DefaultMessageIDTypes)packet.data[0]).ToString());
					yield break;
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}




	void OnApplicationQuit()
	{
		rakPeer.Shutdown(0);
		//rakPeerTest.Shutdown(0);
	}
}
