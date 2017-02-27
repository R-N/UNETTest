using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoolEntry{
	public string name = "Empty";
	public Vector3 pos = Vector3.zero;
	public Quaternion rot = Quaternion.identity;
	public PoolEntry (){
		name = "Empty";
		pos = Vector3.zero;
		rot = Quaternion.identity;
	}
	public PoolEntry (string newName){
		name = newName;
		pos = Vector3.zero;
		rot = Quaternion.identity;
	}
	public PoolEntry(string newName, Vector3 newPos, Quaternion newRot){
		name = newName;
		pos = newPos;
		rot = newRot;
	}
}

public class PoolValue{
	public int number = 0;
	public int pending = 0;
	public List<GameObject> objects = new List<GameObject> ();
	public PoolValue(){
		number = 0;
		pending = 0;
		objects = new List<GameObject> ();
	}
	public PoolValue(int a){
		number = a;
		pending = 0;
		objects = new List<GameObject> ();
	}
}


public class PoolManager : MonoBehaviour {
	public static Dictionary<string, PoolValue> poolDict = new Dictionary<string, PoolValue>();
	List<GameObject> poolSpawn = new List<GameObject>();
	Dictionary<string, int> poolRegen = new Dictionary<string, int> ();
	Dictionary<string, int> poolDelete = new Dictionary<string, int> ();
	public float maxProcess = 4;
	public bool inited = false;
	public static PoolManager pool = null;
	// Use this for initialization
	
	void Awake(){
		DontDestroyOnLoad (gameObject);
	}

	void Start () {
		poolDict.Clear ();
		poolSpawn.Clear ();
		poolRegen.Clear ();
		poolDelete.Clear ();
		pool = gameObject.GetComponent<PoolManager> ();
	}
	
	void Init(){
		
	}
	
	/*void Init(){
		Refill ();
		if (poolSpawn.Count > 0){
			for (int i = 0; i < poolSpawn.Count; i++){
				GameObject spawned = poolSpawn [i];
				spawned.SetActive (true);
			}
			poolSpawn.Clear ();
		}
		if (poolRegen.Count > 0){
			for (int i = 0; i < poolRegen.Count; i++){
				GameObject spawned = (GameObject)Instantiate (Resources.Load(poolRegen [i]));
				spawned.SetActive (false);
				spawned.name = poolRegen [i];
				poolDict [poolRegen [i]].objects.Add (spawned);
			}
			poolRegen.Clear ();
		}
		if (poolDelete.Count > 0){
			for (int i = 0; i < poolDelete.Count; i++){
				GameObject removed = poolDict [poolDelete [i]].objects[0];
				poolDict [poolDelete [i]].objects.Remove (removed);
				Destroy (removed);
			}
			poolDelete.Clear ();
		}
	}*/
	
	// Update is called once per frame
	void Update () {
		if (inited == false) {
			Init ();
			inited = true;
		}
		Refill ();
		Regenerate ();
	}
	
	public static void Add(string name, int number){
		if (poolDict.ContainsKey (name)) {
			poolDict [name].number += number;
		} else {
			poolDict.Add (name, new PoolValue (number));
		}
	}
	
	public static GameObject Spawn(string name){
		GameObject returnGO = null;
		if (pool) {
			if (poolDict.ContainsKey (name)) {
				if (poolDict [name].objects.Count > 0) {
					returnGO = poolDict [name].objects [poolDict [name].objects.Count - 1];
					poolDict [name].objects.Remove (returnGO);
				} else {
					if (name == "Empty") {
						returnGO = new GameObject ();
					} else {
						returnGO = (GameObject)Instantiate (Resources.Load (name));
					}
					returnGO.name = name;
				}
			} else {
				Add (name, 1);
				returnGO = Spawn (name);
			}
		} else {
			returnGO = (GameObject)Instantiate (Resources.Load (name));
		}
		returnGO.SetActive(true);
		return returnGO;
	}
	
	public static GameObject Spawn(string name, Vector3 pos, Quaternion rot){
		GameObject returnGO = Spawn (name);
		returnGO.transform.position = pos;
		returnGO.transform.rotation = rot;
		return returnGO;
	}
	
	public void Refill(){
		for (int k = 0; k < poolDict.Keys.Count; k++){
			foreach (string i in poolDict.Keys){
				int left = (int)(poolDict [i].number - poolDict [i].objects.Count) - poolDict[i].pending;
				if (left > 0) {
					/*for (int j = 0; j < left; j++) {
					poolRegen.Add (i);
				}*/
					if (!poolRegen.ContainsKey(i)){
						poolRegen.Add (i, left);
					}else{
						poolRegen[i] += left;
					}
					poolDict[i].pending += left;
				} 
			}
		}
	}
	
	public void Regenerate(){
		float i = 0;
		if (poolSpawn.Count > 0){
			for (int k = 0; i < maxProcess && k < poolSpawn.Count; k++) {
				GameObject spawned = poolSpawn [0];
				poolSpawn.Remove (spawned);
				spawned.SetActive (true);
				i += 0.5f;
			}
		}
		
		if (poolRegen.Count > 0) {
			List<string>keys = new List<string>(poolRegen.Keys);
			List<string> del = new List<string>();
			foreach(string name in keys){
				for (int j = 0; i < maxProcess && j < poolRegen[name] && poolRegen[name] >= 0; j++){
					if (poolRegen[name] == 0){
						del.Add (name);
						break;
					}else{
						GameObject spawned = null;
						if (name == "Empty"){
							spawned = new GameObject();
						}else{
							spawned = (GameObject)Instantiate (Resources.Load(name));
						}
						spawned.name = name;
						poolDict [name].objects.Add (spawned);
						poolDict[name].pending -= 1;
						poolRegen[name] -= 1;
						i += 1;
						spawned.SetActive(false);
					}
				}
			}
			if (del.Count > 0){
				for (int k = 0; k < del.Count; k++){
					poolRegen.Remove (del[k]);
				}
			}
		}
		
	}
	
	/*public void Regenerate(){
		float i = 0;
		if (poolSpawn.Count > 0){
			for (int k = 0; i < maxProcess && k < poolSpawn.Count; k++) {
				GameObject spawned = poolSpawn [0];
				poolSpawn.Remove (spawned);
				spawned.SetActive (true);
				i += 0.5f;
			}
		}
		if (poolRegen.Count > 0){
			for (int k = 0; i < maxProcess && k < poolRegen.Count; k++) {
				GameObject spawned = (GameObject)Instantiate (Resources.Load(poolRegen [0]));
				spawned.SetActive (false);
				spawned.name = poolRegen [0];
				poolDict [poolRegen [0]].objects.Add (spawned);
				poolDict[poolRegen[0]].pending -= 1;
				poolRegen.Remove (poolRegen [0]);
				i += 1;
			}
		}
		if (poolDelete.Count > 0){
			for (int k = 0; i < maxProcess && k < poolDelete.Count; k++) {
				GameObject removed = poolDict [poolDelete [0]].objects[0];
				poolDict [poolDelete [0]].objects.Remove (removed);
				Destroy (removed);
				poolDelete.Remove (poolDelete [0]);
				i += 0.5f;
			}
		}
	}*/
	
	public static void Kill(GameObject killed){
		if (pool && pool != null) {
			killed.transform.parent = null;
			killed.SetActive (false);
			poolDict [killed.name].objects.Add (killed);
		} else {
			Destroy (killed);
		}
	}
}
