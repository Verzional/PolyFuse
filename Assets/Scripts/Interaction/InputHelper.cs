using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PolyFuse.Interaction
{
    public static class InputHelper
    {
        public static bool IsPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.press.wasPressedThisFrame;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        public static bool IsPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.press.isPressed;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        public static bool IsPointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.press.wasReleasedThisFrame;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(0);
#else
            return false;
#endif
        }

        public static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        public static bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
