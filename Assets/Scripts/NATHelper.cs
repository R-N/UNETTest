using UnityEngine;
using UnityEngine.UI;
using RakNet;
using System;
using System.Collections;

/**
 * Uses RakNet to perform NAT Punchthrough using an externally hosted Facilitator.
 * Once a hole is punched a connection can be made via UNet HLAPI.
 * Requires RakNet.dll and c# wrappers. Also requires the NATCompleteServer from 
 * the RakNet samples to be running on an external server somewhere.
 */
public class NATHelper : MonoBehaviour
{
    // IP Addres and port of the server running NATCompleteServer
    public string facilitatorIP = "50.57.111.104"; 
    public ushort facilitatorPort = 61111;

    [NonSerialized]
    public string externalIP;
    [NonSerialized]
    public bool isReady;
    [NonSerialized]
    public string guid;

	NatPunchthroughClient natPunchthroughClient;
    SystemAddress facilitatorSystemAddress;
    bool firstTimeConnect = true;
    Action<int> onHolePunched;
	public RakPeerInterface rakPeer;

	public GameObject noRelay = null;



	[NonSerialized]
	public string externalIP2;
	[NonSerialized]
	public bool isReady2;
	[NonSerialized]
	public string guid2;

	NatPunchthroughClient natPunchthroughClient2;
	SystemAddress facilitatorSystemAddress2;
	bool firstTimeConnect2 = true;
	Action<int> onHolePunched2;
	public RakPeerInterface rakPeer2;

	public GameObject joinNet = null;

	public GameObject canvas = null;

    void Start()
    {
        rakPeer = RakPeerInterface.GetInstance();
		rakPeer2 = RakPeerInterface.GetInstance ();

		StartCoroutine ("WaitForNetwork");
    }

	IEnumerator WaitForNetwork(){
		for (int a = 0; a < 600; a++){
			if (Application.internetReachability != NetworkReachability.NotReachable) {
				TryCoroutine ();
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
		if (Application.internetReachability == NetworkReachability.NotReachable) {
			Debug.Log ("Not connected to any network. Please do so. If problem persists please try restarting the game.");
			StartCoroutine ("WaitForNetwork");
		}
	}

	void TryCoroutine(){
		ConnectFac ();
		ConnectFac2 ();
		StartCoroutine ("WaitForRaknet");
	}

	IEnumerator WaitForRaknet(){
		for (int a = 0; a < 600; a++){
			if (NATNetworkManager.connectedToFac) {
				NATNetworkManager.singleton.MyConnectToRelay ();
				StartCoroutine ("WaitForRelay");
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}
		if (!NATNetworkManager.connectedToFac) {
			Debug.Log ("ConnectFac failed");
			if (Application.internetReachability == NetworkReachability.NotReachable) {
				StartCoroutine ("WaitForNetwork");
			} else {
				TryCoroutine ();
			}
		}
	}

	public IEnumerator WaitForRelay(){
		for (int a = 0; a < 600; a++){
			if (NATNetworkManager.relayStatus == 1 && NATNetworkManager.connectedToFac2){
				joinNet.SetActive (true);
				if (!NATNetworkManager.connectedToFac2)
					ConnectFac2 ();
				yield break;
			}
			yield return new WaitForSecondsRealtime (0.1f);
		}

		if (Application.internetReachability == NetworkReachability.NotReachable) {
			StartCoroutine ("WaitForNetwork");
		} else {
			bool shouldConnect = true;
			if (!NATNetworkManager.connectedToFac) 
				shouldConnect = false;
			if (!NATNetworkManager.connectedToFac2) 
				shouldConnect = false;
			if (shouldConnect) {
				if (NATNetworkManager.relayStatus == 0) {
					Debug.Log ("Can't connect to relay somehow");
					OnNoRelayUp ();
				} else if (NATNetworkManager.relayStatus == 1) {
					joinNet.SetActive (true);
				}
			} else {
				TryCoroutine ();
			}
		}
	}

	public void OnNoRelayUp(){
		StopCoroutine ("WaitForRelay");
		noRelay.SetActive (true);
	}

	public void ConnectFac(){
		if (!NATNetworkManager.connectedToFac) {
			StopCoroutine ("connectToNATFacilitator");
			StartCoroutine ("connectToNATFacilitator");
		}
	}

	public void ConnectFac2(){
		if (!NATNetworkManager.connectedToFac2) {
			StopCoroutine ("connectToNATFacilitator2");
			StartCoroutine ("connectToNATFacilitator2");
		}
	}

	public void Reconnect(){
		NATNetworkManager.connectedToFac = rakPeer.GetConnectionState(facilitatorSystemAddress) == ConnectionState.IS_CONNECTED ? true : false;
		NATNetworkManager.connectedToFac2 = rakPeer2.GetConnectionState(facilitatorSystemAddress2) == ConnectionState.IS_CONNECTED ? true : false;
		NATNetworkManager.relayStatus = NATNetworkManager.relayStatus == 2 ? 2 : ((NATNetworkManager.singleton.relayClient != null && NATNetworkManager.singleton.relayClient.isConnected) ? 1 : 0);
		if (NATNetworkManager.connectedToFac) {
			NATNetworkManager.singleton.MyConnectToRelay ();
			if (!NATNetworkManager.connectedToFac2)
				ConnectFac2 ();
			StartCoroutine ("WaitForRelay");
		} else {
			StopAllCoroutines ();
			NATNetworkManager.RefreshInfo ();
			StartCoroutine ("WaitForNetwork");
		}
	}

    /**
     * Connect to the externally hosted NAT Facilitator (NATCompleteServer from the RakNet samples)
     * This is called initially and also after each succesfull punchthrough received on the server.
     */
    IEnumerator connectToNATFacilitator()
    {
        // Start the RakNet interface listening on a random port
        // We never need more than 2 connections, one to the Facilitator and one to either the server or the latest incoming client
        // Each time a client connects on the server RakNet is shut down and restarted with two fresh connections
        StartupResult startResult = rakPeer.Startup(2, new SocketDescriptor(), 1);
        if (startResult != StartupResult.RAKNET_STARTED)
        {
            Debug.Log("Failed to initialize network interface: " + startResult.ToString());
            yield break;
        }
        
        // Connect to the Facilitator
        ConnectionAttemptResult connectResult = rakPeer.Connect(facilitatorIP, (ushort)facilitatorPort, null, 0);
        if (connectResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
        {
            Debug.Log("Failed to initialize connection to NAT Facilitator: " + connectResult.ToString());
            yield break;
        }

        // Connecting, wait for response
        Packet packet;
        while ((packet = rakPeer.Receive()) == null) yield return new WaitForEndOfFrame();
        
        // Was the connection accepted?
        if (packet.data[0] != (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED)
        {
			Debug.Log("Failed to connect to NAT Facilitator: " + ((DefaultMessageIDTypes)packet.data[0]));
			NATNetworkManager.RefreshInfo ();
            yield break;
        }

        // Success, we are connected to the Facilitator
        guid = rakPeer.GetMyGUID().g.ToString();
        Debug.Log("Connected: " + guid);
		NATNetworkManager.connectedToFac = true;

        // We store this for later so that we can tell which incoming messages are coming from the facilitator
        facilitatorSystemAddress = packet.systemAddress;

        // Now that we have an external connection we can get the externalIP
		externalIP = rakPeer.GetExternalID(packet.systemAddress).ToString(false);

		NATNetworkManager.RefreshInfo ();

        if (firstTimeConnect)
        {
            firstTimeConnect = false;

            // Attach RakNet punchthrough client
            // This is really what does all the heavy lifting
            natPunchthroughClient = new NatPunchthroughClient();
            rakPeer.AttachPlugin(natPunchthroughClient);
            // Punchthrough can't happen until RakNet is done finding router port stride
            // so we start it asap. If we didn't call this here RakNet would handle it
            // when we actually try and punch through.
            natPunchthroughClient.FindRouterPortStride(facilitatorSystemAddress);
        }
        else
        {
            // If this is not the first time connecting to the facilitor it means the server just received
            // a successful punchthrough and it reconnecting to prepare for more punching. We can start
			// listening immediately.
            StartCoroutine(waitForIncomingNATPunchThroughOnServer(onHolePunched));
        }

        isReady = true;
    }





	IEnumerator connectToNATFacilitator2()
	{
		// Start the RakNet interface listening on a random port
		// We never need more than 2 connections, one to the Facilitator and one to either the server or the latest incoming client
		// Each time a client connects on the server RakNet is shut down and restarted with two fresh connections
		StartupResult startResult = rakPeer2.Startup(2, new SocketDescriptor(), 1);
		if (startResult != StartupResult.RAKNET_STARTED)
		{
			Debug.Log("Failed to initialize network interface: " + startResult.ToString());
			yield break;
		}

		// Connect to the Facilitator
		ConnectionAttemptResult connectResult = rakPeer2.Connect(facilitatorIP, (ushort)facilitatorPort, null, 0);
		if (connectResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
		{
			Debug.Log("Failed to initialize connection to NAT Facilitator: " + connectResult.ToString());
			yield break;
		}

		// Connecting, wait for response
		Packet packet;
		while ((packet = rakPeer2.Receive()) == null) yield return new WaitForEndOfFrame();

		// Was the connection accepted?
		if (packet.data[0] != (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED)
		{
			Debug.Log("Failed to connect to NAT Facilitator: " + ((DefaultMessageIDTypes)packet.data[0]));
			NATNetworkManager.RefreshInfo ();
			yield break;
		}

		// Success, we are connected to the Facilitator
		guid2 = rakPeer2.GetMyGUID().g.ToString();
		Debug.Log("Connected: " + guid2);
		NATNetworkManager.connectedToFac2 = true;

		// We store this for later so that we can tell which incoming messages are coming from the facilitator
		facilitatorSystemAddress2 = packet.systemAddress;

		// Now that we have an external connection we can get the externalIP
		externalIP2 = rakPeer2.GetExternalID(packet.systemAddress).ToString(false);

		NATNetworkManager.RefreshInfo ();

		if (firstTimeConnect2)
		{
			firstTimeConnect2 = false;

			// Attach RakNet punchthrough client
			// This is really what does all the heavy lifting
			natPunchthroughClient2 = new NatPunchthroughClient();
			rakPeer2.AttachPlugin(natPunchthroughClient2);
			// Punchthrough can't happen until RakNet is done finding router port stride
			// so we start it asap. If we didn't call this here RakNet would handle it
			// when we actually try and punch through.
			natPunchthroughClient2.FindRouterPortStride(facilitatorSystemAddress2);
		}
		else
		{
			// If this is not the first time connecting to the facilitor it means the server just received
			// a successful punchthrough and it reconnecting to prepare for more punching. We can start
			// listening immediately.
			StartCoroutine(waitForIncomingNATPunchThroughOnServer2(onHolePunched2));
		}

		isReady2 = true;
	}









    
    /**
     * Called on the server to start listening for clients trying to punch through
     */
    public void startListeningForPunchthrough(Action<int> onHolePunched)
    {
        this.onHolePunched = onHolePunched;
        rakPeer.SetMaximumIncomingConnections(2);
        StartCoroutine(waitForIncomingNATPunchThroughOnServer(onHolePunched));
    }


	public void startListeningForPunchthrough2(Action<int> onHolePunched2)
	{
		this.onHolePunched2 = onHolePunched2;
		rakPeer2.SetMaximumIncomingConnections(2);
		StartCoroutine(waitForIncomingNATPunchThroughOnServer2(onHolePunched2));
	}






    /**
     * Wait for an incoming punchthrough from a client. Once a hole is punched
     * RakNet is shut down so that the NetworkManager can create a new UNet Server
     * listening on the port that the puncthrough arrived on.
     */
    IEnumerator waitForIncomingNATPunchThroughOnServer(Action<int> onHolePunched)
    {
        ushort natListenPort = 0;
        while (true)
        {
            yield return new WaitForEndOfFrame();

            // Check for incoming packet
            Packet packet = rakPeer.Receive();

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
                        natListenPort = rakPeer.GetInternalID().GetPort();
					Debug.Log ("myport " + natListenPort);
                        // Now we're waiting for the client to try and connect to us
					Debug.Log("Received punch through from " + packet.guid + " " + packet.systemAddress.ToString());

					/*string[] parsed = packet.systemAddress.ToString ().Split ('|');

					if (NATNetworkManager.runMode == 2) {
						Debug.Log ("RM PUNCHED ON RM");
						NATNetworkManager.singleton.relayClient.Send (NATNetworkManager.NetMsg.RMPunched, new NATNetworkManager.PunchMsg (int.Parse(parsed [1]), (int)natListenPort, packet.guid.ToString()));
						rakPeer.CloseConnection(packet.guid, true);
						rakPeer.CloseConnection(facilitatorSystemAddress, true);
					}*/

                    }
                    break;

                case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                    // Cool we've got a connection. The client now knows which port to connect from / to
                    Debug.Log("Received incoming RakNet connection.");
                    // Close the connection to the client
                    rakPeer.CloseConnection(packet.guid, true);
                    // And also to the Facilitator
                    rakPeer.CloseConnection(facilitatorSystemAddress, true);
                    break;

                case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                    // Once the Facilitator has disconnected we shut down raknet
                    // so that UNet can listen on the port that RakNet is currently listening on
                    // At this point the hole is succesfully punched
                    // RakNet is then reconnected to the facilitator on a new random port.
                    if (packet.systemAddress == facilitatorSystemAddress)
                    {
                        rakPeer.Shutdown(0);
                            
                        // Hole is punched, UNet can start listening
                        onHolePunched(natListenPort);

                        // Reconnect to Facilitator for next punchthrough
                        StartCoroutine(connectToNATFacilitator());
                            
                        yield break; // Totally done
                    }
                    break;

                default:
                    Debug.Log(((DefaultMessageIDTypes)packet.data[0]).ToString());
                    break;
            }
        }
    }


	IEnumerator waitForIncomingNATPunchThroughOnServer2(Action<int> onHolePunched2)
	{
		ushort natListenPort = 0;
		while (true)
		{
			yield return new WaitForEndOfFrame();

			// Check for incoming packet
			Packet packet = rakPeer2.Receive();

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
					natListenPort = rakPeer2.GetInternalID().GetPort();
					Debug.Log ("myport " + natListenPort);
					// Now we're waiting for the client to try and connect to us
					Debug.Log("Received punch through from " + packet.guid + " " + packet.systemAddress.ToString());

					/*string[] parsed = packet.systemAddress.ToString ().Split ('|');

					if (NATNetworkManager.runMode == 2) {
						Debug.Log ("RM PUNCHED ON RM");
						NATNetworkManager.singleton.relayClient.Send (NATNetworkManager.NetMsg.RMPunched, new NATNetworkManager.PunchMsg (int.Parse(parsed [1]), (int)natListenPort, packet.guid.ToString()));
						rakPeer.CloseConnection(packet.guid, true);
						rakPeer.CloseConnection(facilitatorSystemAddress, true);
					}*/

				}
				break;

			case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
				// Cool we've got a connection. The client now knows which port to connect from / to
				Debug.Log("Received incoming RakNet connection.");
				// Close the connection to the client
				rakPeer2.CloseConnection(packet.guid, true);
				// And also to the Facilitator
				rakPeer2.CloseConnection(facilitatorSystemAddress2, true);
				break;

			case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
				// Once the Facilitator has disconnected we shut down raknet
				// so that UNet can listen on the port that RakNet is currently listening on
				// At this point the hole is succesfully punched
				// RakNet is then reconnected to the facilitator on a new random port.
				if (packet.systemAddress == facilitatorSystemAddress2)
				{
					rakPeer2.Shutdown(0);

					// Hole is punched, UNet can start listening
					onHolePunched2(natListenPort);

					// Reconnect to Facilitator for next punchthrough
					StartCoroutine(connectToNATFacilitator2());

					yield break; // Totally done
				}
				break;

			default:
				Debug.Log(((DefaultMessageIDTypes)packet.data[0]).ToString());
				break;
			}
		}
	}










    /**
     * Punch a hole form a client to the server identified by hostGUID
     * Once the hole is punched onHolePunched will be called with the ports to use to connect
     */
    public void punchThroughToServer(string hostGUIDString, Action<int, int> onHolePunched)
    {
        RakNetGUID hostGUID = new RakNetGUID(ulong.Parse(hostGUIDString));
		Debug.Log("opennat " + natPunchthroughClient.OpenNAT (hostGUID, facilitatorSystemAddress));
		//if (NATNetworkManager.runMode != 3) {
			StartCoroutine (waitForResponseFromServer (hostGUID, onHolePunched));
	//	}
	}//


	public void punchThroughToServer2(string hostGUIDString, Action<int, int> onHolePunched2)
	{
		RakNetGUID hostGUID = new RakNetGUID(ulong.Parse(hostGUIDString));
		Debug.Log("opennat2 " + natPunchthroughClient2.OpenNAT (hostGUID, facilitatorSystemAddress2));
		//if (NATNetworkManager.runMode != 3) {
		StartCoroutine (waitForResponseFromServer2 (hostGUID, onHolePunched2));
		//	}
	}//







    /**
     * Wait for a NAT Punchthrough responses from the server. Once a hole is punched
     * RakNet is shutdown so the NetworkManager can connect via UNet
     */
    IEnumerator waitForResponseFromServer(RakNetGUID hostGUID, Action<int, int> onHolePunched)
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
					Debug.Log ("conport " + natConnectPort);
                        rakPeer.Connect(packet.systemAddress.ToString(false), natConnectPort, "", 0);
                        Debug.Log("Hole punched!" + packet.systemAddress.GetPort());
                        break;

				case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
                        // Ok, we connected, we can now use GetExternalID to get the port to connect from.
					natListenPort = rakPeer.GetExternalID (packet.systemAddress).GetPort ();
					Debug.Log ("lisport " + natListenPort);
                        Debug.Log("Connected");
                        // Now we wait for the server to disconnect us so that we know it is ready for the UNet connection
                        break;

                    case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                        // Server has disconnected us. We are ready to connect via UNet

                        // Shut down RakNet
                        rakPeer.Shutdown(0);

                        // Hole is punched!
					onHolePunched(natListenPort, natConnectPort);

					//StartCoroutine(connectToNATFacilitator());

                        yield break; // Totally done
				case DefaultMessageIDTypes.ID_NAT_CONNECTION_TO_TARGET_LOST:
					{
						//OnNoRelayUp ();
						StopCoroutine("WaitForRelay");
						Debug.Log ("ID_NAT_CONNECTION_TO_TARGET_LOST");
						NATNetworkManager.singleton.MyConnectToRelay ();
						StartCoroutine ("WaitForRelay");
						yield break;
					}
                    default:
                        Debug.Log("RakNet Client received unexpected message type: " + ((DefaultMessageIDTypes)packet.data[0]).ToString());
                        break;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }



	IEnumerator waitForResponseFromServer2(RakNetGUID hostGUID, Action<int, int> onHolePunched2)
	{
		ushort natListenPort = 0, natConnectPort = 0;
		while (true)
		{
			Packet packet = rakPeer2.Receive();
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
					Debug.Log ("conport " + natConnectPort);
					rakPeer2.Connect(packet.systemAddress.ToString(false), natConnectPort, "", 0);
					Debug.Log("Hole punched!" + packet.systemAddress.GetPort());
					break;

				case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
					// Ok, we connected, we can now use GetExternalID to get the port to connect from.
					natListenPort = rakPeer2.GetExternalID (packet.systemAddress).GetPort ();
					Debug.Log ("lisport " + natListenPort);
					Debug.Log("Connected");
					// Now we wait for the server to disconnect us so that we know it is ready for the UNet connection
					break;

				case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
					// Server has disconnected us. We are ready to connect via UNet

					// Shut down RakNet
					rakPeer2.Shutdown(0);

					// Hole is punched!
					onHolePunched2(natListenPort, natConnectPort);

					//StartCoroutine(connectToNATFacilitator());

					yield break; // Totally done

				default:
					Debug.Log("RakNet Client received unexpected message type: " + ((DefaultMessageIDTypes)packet.data[0]).ToString());
					break;
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}


















    
    void OnApplicationQuit()
    {
        rakPeer.Shutdown(0);
    }
}
