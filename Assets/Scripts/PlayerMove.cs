using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts
{
    public class PlayerMove : MonoBehaviour
    {
        private static readonly int IsRunning = Animator.StringToHash("IsWalking");
        public float moveSpeed = 5f;
        public bool ignoreWalls;
        private Vector2 movementInput;
        private Rigidbody2D rb;
        private InputAction moveAction;
        private Animator anim;

        private void Awake()
        {
            if (ignoreWalls)
            {
                if(TryGetComponent<Collider2D>(out var collider2d))
                    collider2d.excludeLayers = LayerMask.GetMask("Level");
            }

            TryGetComponent(out rb);
            TryGetComponent(out anim);
            moveAction = new InputAction("Move", binding: "");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            moveAction.Enable();
        }

        private void OnDestroy()
        {
            moveAction.Disable();
        }

        private void Update()
        {
            movementInput = moveAction.ReadValue<Vector2>();
            if ((movementInput.x != 0 || movementInput.y != 0) && anim)
            {
                anim.SetBool(IsRunning, true);
            }
        }

        private void FixedUpdate()
        {
            if (rb)
                rb.MovePosition(rb.position + movementInput * (moveSpeed * Time.fixedDeltaTime));
        }
    }
}
