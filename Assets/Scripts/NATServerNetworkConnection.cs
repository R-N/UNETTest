using UnityEngine.Networking;


/**
 * These connections are created on the server whenever a client connects via NAT punchthrough
 * They get added to NetworkServer via AddExternalConnection. 
 * When the connection is initialized we have to add an offset to the networkConnectionId that 
 * Unity sends in to prevent it overlapping with other connections added to NetworkServer.
 * When the NetworkServer attempts to send data out over the connection we intercept it and un-offset the connectionId
 * to get back to the actual hostId / connectionId combo that we want to use to send.
 */
public class NATServerNetworkConnection : NetworkConnection 
{
    private int offset;
    
    /**
    * Add an offset to the connectionId. We first offset by the maxinum number of connections that the server expects
    * and then further offset by the hostId since each additional host will only have one connection
    */
    public override void Initialize(string networkAddress, int networkHostId, int networkConnectionId, HostTopology hostTopology)
    {
        offset = NetworkServer.hostTopology.MaxDefaultConnections + networkHostId;
        base.Initialize(networkAddress, networkHostId, networkConnectionId + offset, hostTopology);
    }

    /**
     * Instead of sending out on the NetworkServer we need to send out on
     * the appropriate NATServer that the client connected to.
     * We also un-offset the connectionId to get the id of the connection on the NATServer
     */
    public override bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
    {
        return NetworkTransport.Send(hostId, connectionId - offset, channelId, bytes, numBytes, out error);
    }
	
}
