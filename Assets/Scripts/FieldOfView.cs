using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

public class FieldOfView : MonoBehaviour {
	public Camera frontCam = null;

	Transform camTrans = null;
	Vector3 camPos = Vector3.zero;
	Vector3 camDir = Vector3.forward;
	public Shader distShad = null;

	public RenderTexture rt = null;
	Material maskMat = null;
	Material combineMat = null;
	Material emptyMat = null;

	public Transform topCamTrans = null;
	public Transform camRot = null;

	public BlurOptimized m_blur = null;

	RenderTexture temp = null;

	public InputField field = null;

	float tolerance = 1.5f;

	public Color teamColor;

	public void SetTolerance(){
		if (string.IsNullOrEmpty (field.text)) {
			field.text = tolerance.ToString ();
		} else {
			tolerance = Mathf.Abs (float.Parse (field.text));
			Shader.SetGlobalFloat ("tolerance", tolerance);
		}
	}

	void Start(){
		camTrans = frontCam.transform;
		camPos = camTrans.position;
		camDir = camTrans.forward;


		frontCam.depthTextureMode = DepthTextureMode.None;
		/*if (SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.R8)) {
			rt = new RenderTexture (Screen.width, Screen.height, 0, RenderTextureFormat.R8);
		} else*/
			rt = new RenderTexture (Screen.width, Screen.height, 0);
		frontCam.targetTexture =  new RenderTexture (Screen.width, Screen.height, 16);


		/*frontCam.SetReplacementShader (wpShad, string.Empty);
		depthCam.SetReplacementShader (wpShad2, string.Empty);*/
		frontCam.SetReplacementShader (distShad, string.Empty);
		if(field != null)
			field.text = tolerance.ToString ();
		Shader.SetGlobalFloat ("sightRange", 10f);
		Shader.SetGlobalFloat ("onePerSightRange", 1 / 10f);
		Shader.SetGlobalFloat ("minHearRange", 2.5f);
		Shader.SetGlobalFloat ("maxHearRange", 5f);
		Shader.SetGlobalFloat ("onePerMaxHearRange", 1 / 5f);
		Shader.SetGlobalFloat ("fadeHeight", 3f);
		Shader.SetGlobalFloat ("onePerFadeHeight", 1 / 3f);
		Shader.SetGlobalFloat ("sightEdgeBlur", 0.5f);
		Shader.SetGlobalFloat ("onePerSightEdgeBlur", 1 / 0.5f);
		Shader.SetGlobalFloat ("tolerance", tolerance);
	}

	void LateUpdate(){
		/*if (topCamTrans.hasChanged) {
			topCamTrans.rotation = Quaternion.identity;
		}*/

		if (camTrans.hasChanged){
			camPos = camTrans.position;
			camDir = camTrans.forward;
			Shader.SetGlobalVector ("_PersCamFwd", camDir);
			Shader.SetGlobalVector ("_PersCamFwdXZ", new Vector3(camDir.x, 0, camDir.z).normalized);
			Shader.SetGlobalVector ("_PersCamPos", camPos);
			Matrix4x4 viewMatrix = GL.GetGPUProjectionMatrix (frontCam.projectionMatrix, false) * frontCam.worldToCameraMatrix;
			Shader.SetGlobalMatrix ("_WorldToPersCam", viewMatrix);
			Shader.SetGlobalMatrix ("_XRAMatrix2", viewMatrix.inverse);
		}
		Shader.SetGlobalColor ("_PlyrColor", teamColor);
		//Shader.SetGlobalMatrix ("_XRAMatrix", visibleCam.cameraToWorldMatrix * GL.GetGPUProjectionMatrix (visibleCam.projectionMatrix, false).inverse);
		//Shader.SetGlobalMatrix("_WorldToLocalWP", frontCam.transform.worldToLocalMatrix);
	//	Shader.SetGlobalMatrix("_WorldToLocalWP2", visibleCam.transform.worldToLocalMatrix);
		//Shader.SetGlobalMatrix("_LocalToWorldWP", frontCam.transform.localToWorldMatrix);
		//Shader.SetGlobalMatrix("_LocalToWorldWP2", visibleCam.transform.localToWorldMatrix);
		temp = RenderTexture.active;
		RenderTexture.active = frontCam.targetTexture;
		GL.Clear (true, true, Color.clear);
		frontCam.Render ();
		RenderTexture.active = temp;
		Shader.SetGlobalTexture ("_PersDepthTex", frontCam.targetTexture);

	}
}