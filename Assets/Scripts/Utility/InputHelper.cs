using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Universal Input Helper for HAZEBUSTER.
    /// Seamlessly bridges Unity's New Input System (UnityEngine.InputSystem)
    /// and the Legacy Input Manager (UnityEngine.Input) so movement and aiming
    /// work out-of-the-box in ANY project settings without throwing exceptions!
    /// </summary>
    public static class InputHelper
    {
        public static Vector2 GetMovementVector()
        {
            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            // If new input didn't register or isn't enabled, fallback to legacy
            if (move == Vector2.zero)
            {
                try
                {
                    move.x = Input.GetAxisRaw("Horizontal");
                    move.y = Input.GetAxisRaw("Vertical");
                }
                catch
                {
                    // Catch exception if legacy is disabled in ProjectSettings
                }
            }
#endif

            return move.normalized;
        }

        public static Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return Input.mousePosition;
            }
            catch
            {
                // Fallback if legacy input is disabled
            }
#endif

            return Vector3.zero;
        }

        public static bool IsFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.isPressed) return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetMouseButton(0)) return true;
            }
            catch { }
#endif

            return false;
        }

        public static bool IsDashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) return true;
                if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.wasPressedThisFrame) return true;
            }
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1))
                    return true;
            }
            catch { }
#endif

            return false;
        }

        public static bool IsPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch { }
#endif

            return false;
        }
    }
}
