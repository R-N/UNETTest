using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.CrossPlatformInput;
public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

	public string Name = string.Empty;
	CrossPlatformInputManager.VirtualButton myBt = null;
	void Start(){
		if (CrossPlatformInputManager.ButtonExists (Name)) {
			CrossPlatformInputManager.UnRegisterVirtualButton (Name);
		}
		myBt = new CrossPlatformInputManager.VirtualButton(Name);
		CrossPlatformInputManager.RegisterVirtualButton (myBt);
	}

	void Update(){
	}


	public void OnPointerDown(PointerEventData data)
	{
		//CrossPlatformInputManager.SetButtonDown(Name);
		myBt.Pressed ();
	}


	public void OnPointerUp(PointerEventData data)
	{
		//CrossPlatformInputManager.SetButtonUp(Name);
		myBt.Released();
	}
}
