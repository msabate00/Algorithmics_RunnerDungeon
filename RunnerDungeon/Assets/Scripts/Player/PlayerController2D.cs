using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    private enum MotorState
    {
        Normal,
        Dashing,
        DashAttacking,
        Attacking,
        GroundSliding,
        Hurt,
        Dead,
        LedgeHang,
        Ladder
    }

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D bodyCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Mechanic Toggles")]
    public bool enableHorizontalMovement = true;
    public bool enableJump = true;
    public bool enableWallSlide = true;
    public bool enableWallJump = true;
    public bool enableDash = true;
    public bool enableDashAttack = true;
    public bool enableGroundSlide = true;
    public bool enableAttack = true;
    public bool enableLedgeGrab = true;
    public bool enableLadderClimb = true;
    public bool enableCrouch = true;
    public bool enableDamage = true;

    [Header("Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private Transform leftWallCheck;
    [SerializeField] private Transform rightWallCheck;
    [SerializeField] private Transform leftLedgeCheck;
    [SerializeField] private Transform rightLedgeCheck;
    [SerializeField] private Transform ladderCheck;
    [SerializeField] private Transform attackPoint;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Check Radius")]
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private float ceilingCheckRadius = 0.18f;
    [SerializeField] private float wallCheckRadius = 0.16f;
    [SerializeField] private float ledgeCheckRadius = 0.16f;
    [SerializeField] private float ladderCheckRadius = 0.2f;
    [SerializeField] private float attackRadius = 0.45f;

    [Header("Run / Air")]
    [SerializeField] private float maxRunSpeed = 8.5f;
    [SerializeField] private float groundAcceleration = 70f;
    [SerializeField] private float groundDeceleration = 85f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;
    [SerializeField] private float maxFallSpeed = 18f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 13.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Wall")]
    [SerializeField] private float wallSlideSpeed = 2.5f;
    [SerializeField] private float wallJumpHorizontalForce = 9f;
    [SerializeField] private float wallJumpVerticalForce = 13f;
    [SerializeField] private float wallJumpLockTime = 0.12f;
    [SerializeField] private bool resetDashOnWallSlide = true;

    [Header("Dash")]
    [SerializeField] private int maxDashCharges = 1;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.18f;
    [SerializeField] private float dashEndHorizontalMultiplier = 0.8f;

    [Header("Dash Attack")]
    [SerializeField] private float dashAttackDuration = 0.16f;
    [SerializeField] private float dashAttackSpeed = 14f;
    [SerializeField] private int dashAttackDamage = 1;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.16f;
    [SerializeField] private float attackLunge = 2f;
    [SerializeField] private int attackDamage = 1;

    [Header("Slide")]
    [SerializeField] private float groundSlideSpeed = 10f;
    [SerializeField] private float groundSlideDuration = 0.28f;

    [Header("Ledge Grab")]
    [SerializeField] private Vector2 ledgeHangOffset = new Vector2(0.35f, -0.55f);
    [SerializeField] private Vector2 ledgeClimbOffset = new Vector2(0.55f, -0.1f);
    [SerializeField] private float ledgeClimbTime = 0.12f;

    [Header("Ladder")]
    [SerializeField] private float ladderClimbSpeed = 5f;

    [Header("Damage / Death")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float hurtDuration = 0.3f;
    [SerializeField] private Vector2 hurtKnockback = new Vector2(8f, 7f);
    [SerializeField] private float invulnerabilityTime = 0.8f;

    [Header("Crouch Collider")]
    [SerializeField] private Vector2 standingColliderSize = new Vector2(0.8f, 1.6f);
    [SerializeField] private Vector2 standingColliderOffset = new Vector2(0f, 0.8f);
    [SerializeField] private Vector2 crouchColliderSize = new Vector2(0.8f, 1.05f);
    [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, 0.52f);

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private MotorState state = MotorState.Normal;
    private MotorState last_state = MotorState.Normal;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool attackPressed;
    private bool dashPressed;

    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isAtLeftLedge;
    private bool isAtRightLedge;
    private bool isInLadderZone;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float dashTimer;
    private float dashCooldownTimer;
    private float attackTimer;
    private float hurtTimer;
    private float slideTimer;
    private float wallJumpLockCounter;
    private float invulnerabilityCounter;

    private int facing = 1;
    private int dashCharges;
    private int currentHealth;
    private int currentWallSide;   // -1 left, 1 right, 0 none
    private int currentLedgeSide;  // -1 left, 1 right, 0 none

    private bool attackAlreadyHit;
    private bool ledgeClimbing;
    private Vector2 ledgeHangPosition;
    private Vector2 ledgeClimbTargetPosition;
    private float defaultGravityScale;

    public bool IsDead => state == MotorState.Dead;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHealth = maxHealth;
        dashCharges = maxDashCharges;
        defaultGravityScale = rb.gravityScale;

        SetupInput();

        if (bodyCollider != null)
        {
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
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

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Keyboard>/k");
        dashAction.AddBinding("<Gamepad>/rightShoulder");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/j");
        attackAction.AddBinding("<Keyboard>/leftCtrl");
        attackAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void Update()
    {
        ReadInput();
        CheckEnvironment();
        UpdateTimers();

        if (state == MotorState.Dead)
        {
            UpdateAnimator();
            return;
        }

        RefreshFacing();
        HandleStateMachine();
        HandleCrouchCollider();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (state == MotorState.Dead || ledgeClimbing)
            return;

        switch (state)
        {
            case MotorState.Normal:
            case MotorState.Attacking:
            case MotorState.Hurt:
                ApplyHorizontalMovement();
                ApplyVerticalClamp();
                ApplyWallSlide();
                break;

            case MotorState.Dashing:
                rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);
                break;

            case MotorState.DashAttacking:
                rb.linearVelocity = new Vector2(facing * dashAttackSpeed, 0f);
                break;

            case MotorState.GroundSliding:
                rb.linearVelocity = new Vector2(facing * groundSlideSpeed, rb.linearVelocity.y);
                break;

            case MotorState.LedgeHang:
                rb.linearVelocity = Vector2.zero;
                rb.position = ledgeHangPosition;
                break;

            case MotorState.Ladder:
                rb.linearVelocity = new Vector2(0f, moveInput.y * ladderClimbSpeed);
                break;
        }
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput.x = Mathf.Abs(moveInput.x) < 0.15f ? 0f : moveInput.x;
        moveInput.y = Mathf.Abs(moveInput.y) < 0.15f ? 0f : moveInput.y;

        jumpPressed = jumpAction.WasPressedThisFrame();
        jumpReleased = jumpAction.WasReleasedThisFrame();
        dashPressed = dashAction.WasPressedThisFrame();
        attackPressed = attackAction.WasPressedThisFrame();

        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
    }

    private void CheckEnvironment()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isTouchingWallLeft = Physics2D.OverlapCircle(leftWallCheck.position, wallCheckRadius, wallLayer);
        isTouchingWallRight = Physics2D.OverlapCircle(rightWallCheck.position, wallCheckRadius, wallLayer);

        bool leftLedgeBlocked = Physics2D.OverlapCircle(leftLedgeCheck.position, ledgeCheckRadius, wallLayer);
        bool rightLedgeBlocked = Physics2D.OverlapCircle(rightLedgeCheck.position, ledgeCheckRadius, wallLayer);

        isAtLeftLedge = isTouchingWallLeft && !leftLedgeBlocked;
        isAtRightLedge = isTouchingWallRight && !rightLedgeBlocked;

        isInLadderZone = Physics2D.OverlapCircle(ladderCheck.position, ladderCheckRadius, ladderLayer);

        currentWallSide = 0;
        if (isTouchingWallLeft) currentWallSide = -1;
        else if (isTouchingWallRight) currentWallSide = 1;

        currentLedgeSide = 0;
        if (isAtLeftLedge) currentLedgeSide = -1;
        else if (isAtRightLedge) currentLedgeSide = 1;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            dashCharges = maxDashCharges;
        }
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
            coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        wallJumpLockCounter -= Time.deltaTime;
        invulnerabilityCounter -= Time.deltaTime;

        switch (state)
        {
            case MotorState.Dashing:
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                    ExitDash();
                break;

            case MotorState.DashAttacking:
                attackTimer -= Time.deltaTime;
                if (!attackAlreadyHit)
                    DoAttackHit(dashAttackDamage);

                if (attackTimer <= 0f)
                    ExitDashAttack();
                break;

            case MotorState.Attacking:
                attackTimer -= Time.deltaTime;
                if (!attackAlreadyHit)
                    DoAttackHit(attackDamage);

                if (attackTimer <= 0f)
                    state = MotorState.Normal;
                break;

            case MotorState.Hurt:
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f)
                    state = MotorState.Normal;
                break;

            case MotorState.GroundSliding:
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f)
                    state = MotorState.Normal;
                break;
        }
    }

    private void HandleStateMachine()
    {
        if (ledgeClimbing)
            return;

        if (!enableLedgeGrab && state == MotorState.LedgeHang)
        {
            ExitLedgeHang(dropDown: true);
            return;
        }

        if (!enableLadderClimb && state == MotorState.Ladder)
        {
            ExitLadder();
            return;
        }

        if (state == MotorState.LedgeHang)
        {
            HandleLedgeHangInput();
            return;
        }

        if (state == MotorState.Ladder)
        {
            HandleLadderState();
            return;
        }

        if (state == MotorState.Dashing)
        {
            if (attackPressed && enableDashAttack)
                EnterDashAttack();

            return;
        }

        if (state == MotorState.Hurt || state == MotorState.DashAttacking)
            return;

        if (CanEnterLedgeHang())
        {
            EnterLedgeHang();
            return;
        }

        if (CanEnterLadder())
        {
            EnterLadder();
            return;
        }

        if (dashPressed && CanDash())
        {
            if (enableGroundSlide && ShouldGroundSlide())
                EnterGroundSlide();
            else
                EnterDash();

            return;
        }

        if (attackPressed && CanAttack())
        {
            EnterAttack();
            return;
        }

        if (jumpPressed || jumpBufferCounter > 0f)
        {
            if (TryConsumeJump())
                return;
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void ApplyHorizontalMovement()
    {
        bool crouching = ShouldStayCrouched();
        float targetSpeed = (!enableHorizontalMovement || crouching) ? 0f : moveInput.x * maxRunSpeed;

        if (wallJumpLockCounter > 0f)
            targetSpeed = rb.linearVelocity.x;

        float accel = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? groundAcceleration : airAcceleration)
            : (isGrounded ? groundDeceleration : airDeceleration);

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);

        if (state == MotorState.Attacking && isGrounded)
            newX = Mathf.MoveTowards(newX, 0f, groundDeceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void ApplyVerticalClamp()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void ApplyWallSlide()
    {
        if (!CanWallSlide())
            return;

        if (rb.linearVelocity.y < -wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

        if (resetDashOnWallSlide)
            dashCharges = maxDashCharges;
    }

    private bool CanWallSlide()
    {
        if (!enableWallSlide) return false;
        if (isGrounded) return false;
        if (state != MotorState.Normal && state != MotorState.Attacking && state != MotorState.Hurt) return false;
        if (currentWallSide == 0) return false;
        if (rb.linearVelocity.y >= 0f) return false;

        bool pushingTowardWall = moveInput.x * currentWallSide > 0.1f;
        return pushingTowardWall;
    }

    private bool TryConsumeJump()
    {
        if (!enableJump) return false;

        if (state == MotorState.Ladder)
        {
            ExitLadder();
            PerformJump();
            return true;
        }

        if (isGrounded || coyoteCounter > 0f)
        {
            PerformJump();
            return true;
        }

        if (CanWallJump())
        {
            PerformWallJump();
            return true;
        }

        return false;
    }

    private void PerformJump()
    {
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        state = MotorState.Normal;
    }

    private bool CanWallJump()
    {
        return enableWallJump && !isGrounded && currentWallSide != 0;
    }

    private void PerformWallJump()
    {
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        wallJumpLockCounter = wallJumpLockTime;

        int jumpDir = -currentWallSide;
        facing = jumpDir;

        rb.linearVelocity = new Vector2(jumpDir * wallJumpHorizontalForce, wallJumpVerticalForce);
        state = MotorState.Normal;
    }

    private bool CanDash()
    {
        if (!enableDash) return false;
        if (dashCharges <= 0) return false;
        if (dashCooldownTimer > 0f) return false;
        if (state == MotorState.Attacking || state == MotorState.Hurt) return false;
        return true;
    }

    private void EnterDash()
    {
        dashCharges--;
        dashCooldownTimer = dashCooldown;
        dashTimer = dashDuration;
        state = MotorState.Dashing;
        rb.gravityScale = 0f;

        if (Mathf.Abs(moveInput.x) > 0.15f)
            facing = moveInput.x > 0 ? 1 : -1;

        rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);
    }

    private void ExitDash()
    {
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * dashEndHorizontalMultiplier, rb.linearVelocity.y);
        state = MotorState.Normal;
    }

    private bool CanAttack()
    {
        return enableAttack && state == MotorState.Normal && !ledgeClimbing && state != MotorState.Dead;
    }

    private void EnterAttack()
    {
        state = MotorState.Attacking;
        attackTimer = attackDuration;
        attackAlreadyHit = false;

        if (isGrounded)
            rb.linearVelocity = new Vector2(facing * attackLunge, rb.linearVelocity.y);
    }

    private void EnterDashAttack()
    {
        rb.gravityScale = defaultGravityScale;
        state = MotorState.DashAttacking;
        attackTimer = dashAttackDuration;
        attackAlreadyHit = false;
        rb.linearVelocity = new Vector2(facing * dashAttackSpeed, 0f);
    }

    private void ExitDashAttack()
    {
        state = MotorState.Normal;
    }

    private bool ShouldGroundSlide()
    {
        bool crouching = moveInput.y < -0.45f;
        bool hasHorizontalIntent = Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(rb.linearVelocity.x) > 0.5f;
        return isGrounded && crouching && hasHorizontalIntent;
    }

    private void EnterGroundSlide()
    {
        state = MotorState.GroundSliding;
        slideTimer = groundSlideDuration;

        if (Mathf.Abs(moveInput.x) > 0.15f)
            facing = moveInput.x > 0f ? 1 : -1;
    }

    private bool CanEnterLadder()
    {
        if (!enableLadderClimb) return false;
        if (!isInLadderZone) return false;
        if (state != MotorState.Normal) return false;
        if (Mathf.Abs(moveInput.y) < 0.2f) return false;
        return true;
    }

    private void EnterLadder()
    {
        state = MotorState.Ladder;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        dashCharges = maxDashCharges;
    }

    private void ExitLadder()
    {
        rb.gravityScale = defaultGravityScale;
        state = MotorState.Normal;
    }

    private void HandleLadderState()
    {
        if (!enableLadderClimb)
        {
            ExitLadder();
            return;
        }

        dashCharges = maxDashCharges;

        if (!isInLadderZone)
        {
            ExitLadder();
            return;
        }

        if (jumpPressed)
        {
            ExitLadder();
            PerformJump();
            return;
        }

        if (dashPressed && CanDash())
        {
            ExitLadder();
            EnterDash();
        }
    }

    private bool CanEnterLedgeHang()
    {
        if (!enableLedgeGrab) return false;
        if (isGrounded) return false;
        if (state != MotorState.Normal) return false;
        if (rb.linearVelocity.y > 0f) return false;
        if (currentLedgeSide == 0) return false;
        if (moveInput.y < -0.4f) return false;
        return true;
    }

    private void EnterLedgeHang()
    {
        state = MotorState.LedgeHang;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        dashCharges = maxDashCharges;

        Vector2 ledgePoint = currentLedgeSide == -1
            ? leftLedgeCheck.position
            : rightLedgeCheck.position;

        ledgeHangPosition = ledgePoint + new Vector2(-currentLedgeSide * ledgeHangOffset.x, ledgeHangOffset.y);
        ledgeClimbTargetPosition = ledgePoint + new Vector2(-currentLedgeSide * ledgeClimbOffset.x, ledgeClimbOffset.y);

        facing = -currentLedgeSide;
        rb.position = ledgeHangPosition;
    }

    private void HandleLedgeHangInput()
    {
        rb.linearVelocity = Vector2.zero;
        rb.position = ledgeHangPosition;

        if (moveInput.y < -0.4f)
        {
            ExitLedgeHang(dropDown: true);
            return;
        }

        if (moveInput.y > 0.45f)
        {
            StartCoroutine(ClimbLedgeRoutine());
            return;
        }

        if (jumpPressed)
        {
            ExitLedgeHang(dropDown: false);
            int jumpDir = -currentLedgeSide;
            facing = jumpDir;
            wallJumpLockCounter = wallJumpLockTime;
            rb.linearVelocity = new Vector2(jumpDir * wallJumpHorizontalForce, wallJumpVerticalForce);
            return;
        }
    }

    private IEnumerator ClimbLedgeRoutine()
    {
        ledgeClimbing = true;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos = rb.position;
        float elapsed = 0f;

        while (elapsed < ledgeClimbTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ledgeClimbTime);
            rb.position = Vector2.Lerp(startPos, ledgeClimbTargetPosition, t);
            yield return null;
        }

        rb.position = ledgeClimbTargetPosition;
        ledgeClimbing = false;
        ExitLedgeHang(dropDown: false);
    }

    private void ExitLedgeHang(bool dropDown)
    {
        rb.gravityScale = defaultGravityScale;
        state = MotorState.Normal;

        if (dropDown)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
    }

    private void DoAttackHit(int damage)
    {
        attackAlreadyHit = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, hittableLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector2 hitDir = new Vector2(facing, 0f);
                damageable.TakeDamage(damage);
            }
        }
    }

    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (!enableDamage) return;
        if (state == MotorState.Dead) return;
        if (invulnerabilityCounter > 0f) return;

        currentHealth -= amount;
        invulnerabilityCounter = invulnerabilityTime;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        state = MotorState.Hurt;
        hurtTimer = hurtDuration;
        rb.gravityScale = defaultGravityScale;

        int knockDir = transform.position.x >= sourcePosition.x ? 1 : -1;
        facing = knockDir;
        rb.linearVelocity = new Vector2(knockDir * hurtKnockback.x, hurtKnockback.y);
    }

    private void Die()
    {
        state = MotorState.Dead;
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    private void RefreshFacing()
    {
        if (state == MotorState.LedgeHang || state == MotorState.Dashing || state == MotorState.DashAttacking)
            return;

        if (moveInput.x > 0.15f)
            facing = 1;
        else if (moveInput.x < -0.15f)
            facing = -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;
    }

    private bool ShouldStayCrouched()
    {
        bool blockedAbove = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer | wallLayer);

        if (state == MotorState.GroundSliding)
            return enableGroundSlide || blockedAbove;

        if (!enableCrouch)
            return blockedAbove;

        bool wantsCrouch = isGrounded && moveInput.y < -0.45f;
        return wantsCrouch || blockedAbove;
    }

    private void HandleCrouchCollider()
    {
        if (bodyCollider == null) return;

        if (ShouldStayCrouched())
        {
            bodyCollider.size = crouchColliderSize;
            bodyCollider.offset = crouchColliderOffset;
        }
        else
        {
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("SpeedX", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("SpeedY", rb.linearVelocity.y);
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Idle", state == MotorState.Normal && isGrounded && Mathf.Abs(rb.linearVelocity.x) < 0.05f && !ShouldStayCrouched());
        animator.SetBool("Run", state == MotorState.Normal && isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.05f && !ShouldStayCrouched());
        animator.SetBool("Crouch", ShouldStayCrouched() && state != MotorState.GroundSliding);
        animator.SetBool("Jump", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("Fall", !isGrounded && rb.linearVelocity.y < -0.1f && !CanWallSlide());
        animator.SetBool("WallSlide", CanWallSlide());
        animator.SetBool("Dash", state == MotorState.Dashing);
        animator.SetBool("DashAttack", state == MotorState.DashAttacking);
        animator.SetBool("Attack", state == MotorState.Attacking);
        animator.SetBool("Slide", state == MotorState.GroundSliding);

        if(state == MotorState.LedgeHang && last_state != state)
        {
             animator.SetTrigger("LedgeGrab");
        }

       
        animator.SetBool("LedgeHang", state == MotorState.LedgeHang);
        animator.SetBool("Ladder", state == MotorState.Ladder);
        animator.SetBool("Hurt", state == MotorState.Hurt);
        animator.SetBool("Dead", state == MotorState.Dead);

        last_state = state;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        if (ceilingCheck != null) Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);

        Gizmos.color = Color.blue;
        if (leftWallCheck != null) Gizmos.DrawWireSphere(leftWallCheck.position, wallCheckRadius);
        if (rightWallCheck != null) Gizmos.DrawWireSphere(rightWallCheck.position, wallCheckRadius);

        Gizmos.color = Color.cyan;
        if (leftLedgeCheck != null) Gizmos.DrawWireSphere(leftLedgeCheck.position, ledgeCheckRadius);
        if (rightLedgeCheck != null) Gizmos.DrawWireSphere(rightLedgeCheck.position, ledgeCheckRadius);

        Gizmos.color = Color.yellow;
        if (ladderCheck != null) Gizmos.DrawWireSphere(ladderCheck.position, ladderCheckRadius);

        Gizmos.color = Color.red;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}

//public interface IDamageable
//{
//    void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitDirection);
//}