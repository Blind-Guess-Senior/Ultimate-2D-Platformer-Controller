using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Core.Input
{
    public class InputManager : MonoBehaviour
    {
        #region Fields

        #region References

        public static PlayerInput PlayerInput;

        private static InputAction _moveAction;
        private static InputAction _jumpAction;
        private static InputAction _runAction;

        #endregion

        #region Runtime Fields

        public static Vector2 Movement;
        public static bool JumpWasPressed;
        public static bool JumpIsHeld;
        public static bool JumpWasReleased;
        public static bool RunIsHeld;

        #endregion

        #endregion

        #region Unity Event Methods

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();

            _moveAction = PlayerInput.actions["Move"];
            _jumpAction = PlayerInput.actions["Jump"];
            _runAction = PlayerInput.actions["Run"];
        }

        private void Update()
        {
            Movement = _moveAction.ReadValue<Vector2>();

            JumpWasPressed = _jumpAction.WasPressedThisFrame();
            JumpIsHeld = _jumpAction.IsPressed();
            JumpWasReleased = _jumpAction.WasReleasedThisFrame();

            RunIsHeld = _runAction.IsPressed();
        }

        #endregion
    }
}