using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityStandardAssets.CrossPlatformInput
{
	public class Joystick : MonoBehaviour, IPointerUpHandler, IDragHandler, IPointerDownHandler
	{
		
		 int MovementRange = 32;
		 Vector3 m_StartPos;
		public static Vector2 padAxis = Vector2.zero;
		Vector2 padAxis2 = Vector2.zero;
		float accDt = 0;
		bool waitingForUpdate = false;

        void Start()
        {
            m_StartPos = transform.position;
        }

		void Update(){
			if (!waitingForUpdate) {
				if (accDt >= 0.05f) {
					waitingForUpdate = true;
					//StartCoroutine (UpdateAxis ());
				}else
					accDt += Time.deltaTime;
			}
		}

		/*IEnumerator UpdateAxis(){
			while (waitingForUpdate) {
				if (padAxis == padAxis2) {
					yield return new WaitForEndOfFrame ();
				}else{
					padAxis = padAxis2;
					waitingForUpdate = false;
					accDt = 0;
					yield break;
				}
			}
		}*/



		public void OnDrag(PointerEventData data)
		{
			Vector2 deltaPos = Vector2.ClampMagnitude(new Vector2(data.position.x - m_StartPos.x, data.position.y - m_StartPos.y), MovementRange);
			transform.position = m_StartPos + new Vector3(deltaPos.x, deltaPos.y, 0);
			padAxis = deltaPos / MovementRange;
		}


		public void OnPointerUp(PointerEventData data)
		{
			transform.position = m_StartPos;
			padAxis = Vector2.zero;
		}
		public void OnPointerDown(PointerEventData data){
		}








	}
}