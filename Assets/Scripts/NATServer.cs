using UnityEngine.Networking;

/**
 * Each time a client punches through to the server a NATServer is spawned to handle the connection
 * The NATServer listens on the port that was punched through to and when a connection
 * comes in it is added to the NetworkServer via AddExternalConnection so that all the HLAPI stuff
 * will work. YAY
 */
public class NATServer : NetworkServerSimple
{
    
    public override void Initialize()
    {
        base.Initialize();

        // Use the custom connection class to prevent connectionId conflicts
        SetNetworkConnectionClass<NATServerNetworkConnection>();
    }
    
    public override void OnConnected(NetworkConnection conn)
    {
        base.OnConnected(conn);

        NetworkServer.AddExternalConnection(conn);
    }
}
