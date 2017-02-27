using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class RoomInfo : MonoBehaviour {

	public Text roomName = null;
	public Text playerCount = null;

	public static RoomInfo singleton = null;

	public bool isNetGame = false;

	public string gameID = string.Empty;
	public string address = string.Empty;

	public InputField passField = null;
	public GameObject panel = null;

	public int maxPlyr = 4;

	void Awake(){
		singleton = this;
	}

	public void Join(){
		if (!NetworkServer.active) {
			NATNetworkManager_PHP.singleton.myRoom.Clear ();
			NATNetworkManager_PHP.singleton.maxConnections = maxPlyr;
			NATNetworkManager_PHP.singleton.myRoom.maxPlayers = maxPlyr;
			NATNetworkManager_PHP.singleton.myRoom.gameID = gameID;
			if (isNetGame) {
				NATNetworkManager_PHP.singleton.GetRoomAddressToJoin (gameID, passField.text);
			} else {
				MyNetworkDiscovery.singleton.PasswordCheckClient (address, passField.text);
			}
			SetActive (false);
		}
	}

	public void SetName(string newName){
		roomName.text = newName;
	}

	public void SetPlayerCount(string text){
		playerCount.text = text;
	}


	public void SetActive(bool act){
		panel.SetActive (act);
	}
}
