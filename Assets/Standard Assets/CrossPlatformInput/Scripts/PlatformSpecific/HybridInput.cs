using System;
using UnityEngine;

namespace UnityStandardAssets.CrossPlatformInput.PlatformSpecific
{
    public class HybridInput : VirtualInput
    {
        private void AddButton(string name)
        {
            // we have not registered this button yet so add it, happens in the constructor
            CrossPlatformInputManager.RegisterVirtualButton(new CrossPlatformInputManager.VirtualButton(name));
        }


        private void AddAxes(string name)
        {
            // we have not registered this button yet so add it, happens in the constructor
            CrossPlatformInputManager.RegisterVirtualAxis(new CrossPlatformInputManager.VirtualAxis(name));
        }


        public override float GetAxis(string name, bool raw)
        {
            if (!m_VirtualAxes.ContainsKey(name))
            {
                AddAxes(name);
            }
			return Mathf.Clamp(m_VirtualAxes[name].GetValue + (raw ? Input.GetAxisRaw(name) : Input.GetAxis(name)), -1, 1);
        }


        public override void SetButtonDown(string name)
        {
            if (!m_VirtualButtons.ContainsKey(name))
            {
                AddButton(name);
            }
            m_VirtualButtons[name].Pressed();
        }


        public override void SetButtonUp(string name)
        {
            if (!m_VirtualButtons.ContainsKey(name))
            {
                AddButton(name);
            }
            m_VirtualButtons[name].Released();
        }


        public override void SetAxisPositive(string name)
        {
            if (!m_VirtualAxes.ContainsKey(name))
            {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(1f);
        }


        public override void SetAxisNegative(string name)
        {
            if (!m_VirtualAxes.ContainsKey(name))
            {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(-1f);
        }


        public override void SetAxisZero(string name)
        {
            if (!m_VirtualAxes.ContainsKey(name))
            {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(0f);
        }


        public override void SetAxis(string name, float value)
        {
            if (!m_VirtualAxes.ContainsKey(name))
            {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(value);
        }


        public override bool GetButtonDown(string name)
        {
            if (m_VirtualButtons.ContainsKey(name))
            {
				return (m_VirtualButtons[name].GetButtonDown || Input.GetButtonDown(name));
            }

			AddButton(name);
			return (m_VirtualButtons[name].GetButtonDown || Input.GetButtonDown(name));
        }


        public override bool GetButtonUp(string name)
        {
            if (m_VirtualButtons.ContainsKey(name))
            {
				return (m_VirtualButtons[name].GetButtonUp || Input.GetButtonUp(name));
            }

			AddButton(name);
			return (m_VirtualButtons[name].GetButtonUp || Input.GetButtonUp(name));
        }


        public override bool GetButton(string name)
        {
            if (m_VirtualButtons.ContainsKey(name))
            {
				return (m_VirtualButtons[name].GetButton || Input.GetButton(name));
            }

			AddButton(name);
			return (m_VirtualButtons[name].GetButton || Input.GetButton(name));
        }


        public override Vector3 MousePosition()
        {
            return virtualMousePosition;
        }
    }
}
