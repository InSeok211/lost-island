using UnityEngine;
using UnityEngine.InputSystem;

namespace LostIsland
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class IslandPlayer : MonoBehaviour
    {
        public Transform view;
        public float walkSpeed = 4.5f;
        public float runSpeed = 7f;
        public float turnSpeed = 14f;
        CharacterController controller;
        float verticalSpeed;
        Vector3 spawn;

        void Awake() { controller = GetComponent<CharacterController>(); spawn = transform.position; }

        void Update()
        {
            var keys = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keys != null && Application.isFocused)
            {
                input.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0)
                    - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0)
                    - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                if (keys.rKey.wasPressedThisFrame) { Respawn(); return; }
            }
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 forward = view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view ? Vector3.ProjectOnPlane(view.right, Vector3.up).normalized : Vector3.right;
            Vector3 direction = forward * input.y + right * input.x;
            float speed = keys != null && keys.leftShiftKey.isPressed ? runSpeed : walkSpeed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime * 2;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-turnSpeed * Time.deltaTime));
            if (transform.position.y < -3) Respawn();
        }

        void Respawn()
        {
            controller.enabled = false;
            transform.position = spawn;
            controller.enabled = true;
            verticalSpeed = 0;
        }

        void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.height / 800f, 0.8f, 1.6f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.Box(new Rect(20, 20, 340, 94), GUIContent.none);
            GUI.Label(new Rect(36, 29, 310, 25), "LOST ISLAND  /  EXPLORATION PROTOTYPE");
            GUI.Label(new Rect(36, 55, 310, 24), "WASD / Arrows : Move     Shift : Run");
            GUI.Label(new Rect(36, 79, 310, 24), "Mouse wheel : Zoom       R : Return to camp");
        }
    }
}
