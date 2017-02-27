using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class RoomEntry : MonoBehaviour, IPointerClickHandler {
	public bool isNetGame = false;
	public string gameID = string.Empty;
	public string address = string.Empty;
	public Text name = null;
	public Text playerCount = null;
	public GameObject playing = null;
	public GameObject isLocked = null;

	public int maxPlyr = 4;

	public void SetName(string newName){
		name.text = newName;
	}

	public void SetPlaying(bool play){
		playing.SetActive (play);
	}

	public void SetLock(bool locked){
		isLocked.SetActive (locked);
	}

	public void SetPlayerCount (string text){
		playerCount.text = text;
	}

	public void OnPointerClick(PointerEventData data){
		if (!NetworkServer.active) {
			NATNetworkManager_PHP.singleton.myRoom.Clear ();
			RoomInfo.singleton.address = address;
			RoomInfo.singleton.gameID = gameID;
			RoomInfo.singleton.isNetGame = isNetGame;
			RoomInfo.singleton.maxPlyr = maxPlyr;
			RoomInfo.singleton.SetName (name.text);
			RoomInfo.singleton.SetPlayerCount (playerCount.text);
			RoomInfo.singleton.SetActive (true);
		}
	}
		

















}
