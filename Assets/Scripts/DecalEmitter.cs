using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DecalEmitter : MonoBehaviour {
	
	public float lifeTime = 0;
	public float lifeTimeRange = 0;
	public Vector2 tiling = Vector2.one;
	public Vector2 offset = Vector2.zero;
	public Vector2 deltaOffset = Vector2.zero;
	public bool randomOffset = false;
	public Vector2 startScale = Vector2.one;
	public Vector2 endScale = Vector2.one;
	public float startRot = 0;
	public float startRotRange = 0;
	public float deltaRot = 0;
	public float deltaTex = 0;
	public bool randomTex = false;
	public List<Color> colors = new List<Color>();
	public List<float> alpha = new List<float>();
	public List<Texture2D> texs = new List<Texture2D>();
	public LayerMask affectedLayers = -1;
	public int decalStock = 0;
	public float deltaTime = 1;
	public PoolManager pool = null;
	public string decalName = "EmptyDecal";
	float timer = 0;
	public bool local = true;
	// Use this for initialization
	void Start () {
		timer = deltaTime;
		PoolManager.Add (decalName, Mathf.CeilToInt((float) 1 / deltaTime) + 1);
		Debug.Log ("z");
	}
	
	// Update is called once per frame
	void Update () {
		Debug.Log ("n");
		if (timer > 0) {
			timer -= Time.deltaTime;
			Debug.Log ("t " + timer);
		} else {
			Debug.Log ("m");
			if (decalStock > 0){
				Debug.Log ("x");
				GameObject spawned = null;
				DecalTry3 decal = null;
				Debug.Log ("a");
				if (pool){
					spawned = PoolManager.Spawn(decalName);
				}else{
					spawned = (GameObject) GameObject.Instantiate(Resources.Load(decalName));
				}
				Debug.Log ("b");
				decal = spawned.GetComponent<DecalTry3>();
				
				decal.tiling = tiling;
				decal.offset = offset;
				decal.deltaOffset = deltaOffset;
				decal.startScale = startScale;
				decal.endScale = endScale;
				decal.deltaRot = deltaRot;
				decal.deltaTex = deltaTex;
				decal.randomTex = randomTex;
				if (texs.Count > 0){
					decal.texs = texs;
				}
				if (colors.Count > 0){
					decal.colors = colors;
				}
				if (alpha.Count > 0){
					decal.alpha = alpha;
				}
				decal.affectedLayers = affectedLayers;
				Debug.Log ("c");
				decal.lifeTime = lifeTime + Random.Range(-lifeTimeRange, lifeTimeRange);
				if (randomTex){
					decal.curTex = Random.Range(0, texs.Count - 1);
				}
				if (randomOffset){
					decal.offset += deltaOffset * Random.Range (0, 16);
				}
				spawned.transform.parent = transform;
				spawned.transform.localPosition = Vector3.zero;
				spawned.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, startRot + Random.Range(-startRotRange, startRotRange)));
				if (!local){
					spawned.transform.parent = null;
				}
				decalStock -= 1;
				if (deltaTime > 0){
					while (timer <= 0){
						timer += deltaTime;
					}
				}
			}else{
				Debug.Log ("f");
				if(transform.childCount == 0){
					Destroy(gameObject);
				}
			}
		}
	}
}
