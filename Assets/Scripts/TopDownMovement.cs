using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMovement : MonoBehaviour
{
    [Header("Movement")]
    public float velocidad = 4f;

    private Rigidbody2D rb;
    private Animator animator;                  
    private Vector2 input;
    private Vector2 lastDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.gravityScale = 0f;                   
        rb.freezeRotation = true;
    }

    private void Update()
    {
        input = ReadInput();
        if (input.sqrMagnitude > 1f) input.Normalize();  //--diagonal movement normalization
        if (input.sqrMagnitude > 0.01f) lastDirection = input;
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 v = input * velocidad;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    private Vector2 ReadInput()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1f : 0f)
                - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1f : 0f);
        float y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f)
                - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1f : 0f);
        return new Vector2(x, y);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool itMoves = input.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", itMoves);
        animator.SetFloat("MoveX", itMoves ? input.x : lastDirection.x);
        animator.SetFloat("MoveY", itMoves ? input.y : lastDirection.y);
    }
}
