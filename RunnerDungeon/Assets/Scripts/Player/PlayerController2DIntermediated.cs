using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2DIntermediated : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform attackPoint;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Check Radius")]
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private float attackRadius = 0.45f;

    [Header("Movement")]
    [SerializeField] private float maxRunSpeed = 8f;
    [SerializeField] private float groundAcceleration = 70f;
    [SerializeField] private float groundDeceleration = 85f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;
    [SerializeField] private float maxFallSpeed = 18f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.16f;
    [SerializeField] private float attackLunge = 1.5f;
    [SerializeField] private int attackDamage = 1;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool attackPressed;

    private bool isGrounded;
    private bool isAttacking;
    private bool attackAlreadyHit;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float attackTimer;

    private int facing = 1;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        SetupInput();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
    }

    private void SetupInput()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/c");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/j");
        attackAction.AddBinding("<Keyboard>/leftCtrl");
        attackAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        UpdateTimers();
        RefreshFacing();
        HandleJump();
        HandleAttack();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyVerticalClamp();
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput.x = Mathf.Abs(moveInput.x) < 0.15f ? 0f : moveInput.x;

        jumpPressed = jumpAction.WasPressedThisFrame();
        jumpReleased = jumpAction.WasReleasedThisFrame();
        attackPressed = attackAction.WasPressedThisFrame();

        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
    }

    private void CheckGround()
    {
        isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
            coyoteCounter = coyoteTime;
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
            coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;

            if (!attackAlreadyHit)
                DoAttackHit();

            if (attackTimer <= 0f)
                isAttacking = false;
        }
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void HandleAttack()
    {
        if (!attackPressed || isAttacking)
            return;

        isAttacking = true;
        attackAlreadyHit = false;
        attackTimer = attackDuration;

        if (isGrounded)
            rb.linearVelocity = new Vector2(facing * attackLunge, rb.linearVelocity.y);
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput.x * maxRunSpeed;
        float accel = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? groundAcceleration : airAcceleration)
            : (isGrounded ? groundDeceleration : airDeceleration);

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void ApplyVerticalClamp()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void RefreshFacing()
    {
        if (moveInput.x > 0.15f)
            facing = 1;
        else if (moveInput.x < -0.15f)
            facing = -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;
    }

    private void DoAttackHit()
    {
        attackAlreadyHit = true;

        if (attackPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, hittableLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector2 hitDirection = new Vector2(facing, 0f);
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("SpeedX", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("SpeedY", rb.linearVelocity.y);
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Idle", isGrounded && Mathf.Abs(rb.linearVelocity.x) < 0.05f);
        animator.SetBool("Run", isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        animator.SetBool("Jump", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("Fall", !isGrounded && rb.linearVelocity.y < -0.1f);
        animator.SetBool("Attack", isAttacking);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.red;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}

//public interface IDamageable
//{
//    void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitDirection);
//}
