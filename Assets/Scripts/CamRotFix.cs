using UnityEngine;
using System.Collections;

public class CamRotFix : MonoBehaviour {

	
	void OnPreRender(){
		transform.parent.rotation = Quaternion.identity;
	}
}
