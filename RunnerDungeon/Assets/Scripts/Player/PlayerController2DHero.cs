using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2DHero : MonoBehaviour
{
    private enum HeroAnimState
    {
        None,
        Idle,
        Run,
        Push,
        JumpUp,
        JumpDown,
        JumpDouble,
        BeforeOrAfterJump,
        Attack,
        SwordAttack,
        Hit,
        Death
    }

    [Header("Componentes")]
    public Rigidbody2D rb;
    public Collider2D bodyCollider;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform attackPoint;
    public Transform swordAttackPoint;
    public Transform pogoPoint;
    public Transform attackFxPoint;
    public Transform wallCheck;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    [Tooltip("Capas que permiten rebote tipo pogo aunque no reciban damage. Ej: enemigos, proyectiles, pinchos, bounce pads.")]
    public LayerMask pogoLayer;

    [Header("Movimiento")]
    public float speed = 6f;
    public float acceleration = 14f;
    public float airAcceleration = 10f;
    public float jumpForce = 12f;
    public bool allowDoubleJump = true;
    public float groundCheckRadius = 0.18f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float pushCheckDistance = 0.1f;

    [Header("Ataque")]
    public float attackRadius = 0.55f;
    public float swordAttackRadius = 0.75f;
    public float pogoRadius = 0.75f;
    public int attackDamage = 1;
    public int swordAttackDamage = 1;
    public float attackHitDelay = 0.05f;
    public float attackDuration = 0.20f;
    public float swordAttackHitDelay = 0.06f;
    public float swordAttackDuration = 0.24f;
    public float attackMoveMultiplier = 0.45f;
    public float attackCooldown = 0.05f;

    [Header("Pogo")]
    public bool enablePogo = true;
    public float pogoBounceVelocity = 12f;
    public float pogoSelfLock = 0.08f;
    [Tooltip("Baja al menos esta velocidad antes de permitir pogo, para evitar rebotes raros justo al despegar.")]
    public float minFallSpeedForPogo = -0.1f;

    [Header("Vida / daño")]
    public int maxHealth = 5;
    public float hitStunDuration = 0.22f;
    public float hurtInvulnerability = 0.45f;
    public float hurtKnockbackX = 5f;
    public float hurtKnockbackY = 6f;

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode attackKey = KeyCode.J;
    public KeyCode swordAttackKey = KeyCode.K;

    [Header("FX opcionales")]
    public ParticleSystem jumpDust;
    public ParticleSystem landDust;
    public ParticleSystem slashParticles;
    public ParticleSystem pogoParticles;
    public ParticleSystem hitParticles;
    [Tooltip("Opcional. Animator aparte para los clips SwordEffect / SwordAttackEffect. Debe tener triggers con esos mismos nombres.")]
    public Animator attackFxAnimator;
    public SpriteRenderer attackFxSpriteRenderer;

    private float moveX;
    private float moveY;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private bool hasDoubleJumped;
    private bool facingRight = true;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float attackCooldownCounter;
    private float animationLockCounter;
    private int currentHealth;
    private HeroAnimState currentAnimState = HeroAnimState.None;
    private Coroutine actionRoutine;
    private Coroutine attackFxRoutine;

    private Vector3 attackPointLocal;
    private Vector3 swordAttackPointLocal;
    private Vector3 pogoPointLocal;
    private Vector3 attackFxPointLocal;
    private Vector3 wallCheckLocal;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;
    public bool IsAttacking => isAttacking;
    public bool FacingRight => facingRight;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;

        if (attackPoint != null)
            attackPointLocal = attackPoint.localPosition;

        if (swordAttackPoint != null)
            swordAttackPointLocal = swordAttackPoint.localPosition;

        if (pogoPoint != null)
            pogoPointLocal = pogoPoint.localPosition;

        if (attackFxPoint != null)
            attackFxPointLocal = attackFxPoint.localPosition;

        if (wallCheck != null)
            wallCheckLocal = wallCheck.localPosition;

        if (attackFxSpriteRenderer != null)
            attackFxSpriteRenderer.enabled = false;

        attackFxRoutine = null;
    }

    void Start()
    {
        ForceAnimation(HeroAnimState.Idle);
    }

    void Update()
    {
        UpdateTimers();
        UpdateGrounded();

        if (isDead)
            return;

        ReadInput();
        UpdateFacing();
        HandleJumpInput();
        HandleAttackInput();
        UpdateAnimatorState();
    }

    void FixedUpdate()
    {
        if (rb == null || isDead)
            return;

        float targetSpeed = moveX * speed;
        float accel = isGrounded ? acceleration : airAcceleration;

        if (isAttacking)
            targetSpeed *= attackMoveMultiplier;

        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime * speed);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    void ReadInput()
    {
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey))
            jumpBufferCounter = jumpBufferTime;
    }

    void UpdateTimers()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (attackCooldownCounter > 0f)
            attackCooldownCounter -= Time.deltaTime;

        if (animationLockCounter > 0f)
            animationLockCounter -= Time.deltaTime;
    }

    void UpdateGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;

            if (!wasGrounded)
            {
                hasDoubleJumped = false;
                PlayLandingFeedback();
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        if (isAttacking || isHurt)
            return;

        if (Mathf.Abs(moveX) < 0.01f)
            return;

        if (moveX > 0f)
        {
            facingRight = true;
            spriteRenderer.flipX = false;
        }
        else if (moveX < 0f)
        {
            facingRight = false;
            spriteRenderer.flipX = true;
        }
    }

    void HandleJumpInput()
    {
        if (jumpBufferCounter <= 0f || isHurt || isDead)
            return;

        if (CanUseGroundJump())
        {
            jumpBufferCounter = 0f;
            DoJump(false);
            return;
        }

        if (CanUseDoubleJump())
        {
            jumpBufferCounter = 0f;
            DoJump(true);
        }
    }

    bool CanUseGroundJump()
    {
        return isGrounded || coyoteCounter > 0f;
    }

    bool CanUseDoubleJump()
    {
        return allowDoubleJump && !isGrounded && !hasDoubleJumped;
    }

    void DoJump(bool doubleJump)
    {
        coyoteCounter = 0f;

        Vector2 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        if (doubleJump)
        {
            hasDoubleJumped = true;
            ForceAnimation(HeroAnimState.JumpDouble, 0.10f);
        }
        else
        {
            ForceAnimation(HeroAnimState.JumpUp, 0.05f);
        }

        if (jumpDust != null)
            jumpDust.Play();
    }

    void HandleAttackInput()
    {
        if (isAttacking || isHurt || attackCooldownCounter > 0f)
            return;

        bool attackPressed = Input.GetKeyDown(attackKey);
        bool swordPressed = Input.GetKeyDown(swordAttackKey);

        if (!attackPressed && !swordPressed)
            return;

        bool wantsPogo = enablePogo && !isGrounded && moveY < -0.5f && rb.linearVelocity.y <= minFallSpeedForPogo;

        if (wantsPogo)
        {
            StartAction(DoPogoAttack());
            return;
        }

        if (swordPressed)
        {
            StartAction(DoSwordAttack());
            return;
        }

        StartAction(DoNormalAttack());
    }

    void StartAction(IEnumerator routine)
    {
        if (actionRoutine != null)
            StopCoroutine(actionRoutine);

        actionRoutine = StartCoroutine(routine);
    }

    IEnumerator DoNormalAttack()
    {
        isAttacking = true;
        attackCooldownCounter = attackCooldown;

        ForceAnimation(HeroAnimState.Attack, attackDuration);
        PlaySlashFeedback(false, GetMirroredWorldPoint(attackFxPoint, attackFxPointLocal));

        yield return new WaitForSeconds(attackHitDelay);

        Vector2 point = GetMirroredWorldPoint(attackPoint, attackPointLocal);
        DealDamage(point, attackRadius, attackDamage, enemyLayer, false);

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitDelay));

        isAttacking = false;
        actionRoutine = null;
    }

    IEnumerator DoSwordAttack()
    {
        isAttacking = true;
        attackCooldownCounter = attackCooldown;

        ForceAnimation(HeroAnimState.SwordAttack, swordAttackDuration);
        PlaySlashFeedback(true, GetMirroredWorldPoint(attackFxPoint, attackFxPointLocal));

        yield return new WaitForSeconds(swordAttackHitDelay);

        Vector2 point = GetMirroredWorldPoint(swordAttackPoint != null ? swordAttackPoint : attackPoint, swordAttackPoint != null ? swordAttackPointLocal : attackPointLocal);
        DealDamage(point, swordAttackRadius, swordAttackDamage, enemyLayer, false);

        yield return new WaitForSeconds(Mathf.Max(0f, swordAttackDuration - swordAttackHitDelay));

        isAttacking = false;
        actionRoutine = null;
    }

    IEnumerator DoPogoAttack()
    {
        isAttacking = true;
        attackCooldownCounter = attackCooldown;

        ForceAnimation(HeroAnimState.SwordAttack, swordAttackDuration);

        Vector2 fxPoint = GetPogoWorldPoint();
        PlaySlashFeedback(true, fxPoint);

        yield return new WaitForSeconds(swordAttackHitDelay);

        bool hitSomething = DealDamage(fxPoint, pogoRadius, swordAttackDamage, enemyLayer, true);

        if (!hitSomething && pogoLayer.value != 0)
        {
            hitSomething = Physics2D.OverlapCircle(fxPoint, pogoRadius, pogoLayer) != null;
        }

        if (hitSomething)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.y = pogoBounceVelocity;
            rb.linearVelocity = velocity;
            hasDoubleJumped = false;

            if (pogoParticles != null)
            {
                pogoParticles.transform.position = fxPoint;
                pogoParticles.Play();
            }

            ForceAnimation(HeroAnimState.JumpDouble, pogoSelfLock);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, swordAttackDuration - swordAttackHitDelay));

        isAttacking = false;
        actionRoutine = null;
    }

    bool DealDamage(Vector2 point, float radius, int damage, LayerMask mask, bool canHitSameObjectMultipleTimes)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius, mask);
        if (hits == null || hits.Length == 0)
            return false;

        bool hitSomething = false;
        HashSet<Transform> alreadyHit = canHitSameObjectMultipleTimes ? null : new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            Transform root = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform.root;

            if (!canHitSameObjectMultipleTimes)
            {
                if (alreadyHit.Contains(root))
                    continue;

                alreadyHit.Add(root);
            }

            hit.gameObject.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hitSomething = true;

            if (hitParticles != null)
            {
                hitParticles.transform.position = hit.ClosestPoint(point);
                hitParticles.Play();
            }
        }

        return hitSomething;
    }

    void PlayLandingFeedback()
    {
        if (landDust != null)
            landDust.Play();

        if (!isAttacking && !isHurt && !isDead)
            ForceAnimation(HeroAnimState.BeforeOrAfterJump, 0.08f);
    }

    void PlaySlashFeedback(bool sword, Vector2 worldPoint)
    {
        if (slashParticles != null)
        {
            slashParticles.transform.position = worldPoint;
            slashParticles.Play();
        }

        if (attackFxAnimator != null)
        {
            if (attackFxPoint != null)
                attackFxAnimator.transform.position = worldPoint;

            if (attackFxSpriteRenderer != null)
            {
                attackFxSpriteRenderer.enabled = true;
                attackFxSpriteRenderer.flipX = !facingRight;
                if (attackFxRoutine != null)
                    StopCoroutine(attackFxRoutine);

                attackFxRoutine = StartCoroutine(HideAttackFxAfter(sword ? swordAttackDuration : attackDuration));
            }

            attackFxAnimator.ResetTrigger("SwordEffect");
            attackFxAnimator.ResetTrigger("SwordAttackEffect");
            attackFxAnimator.SetTrigger(sword ? "SwordAttackEffect" : "SwordEffect");
        }
    }

    IEnumerator HideAttackFxAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attackFxSpriteRenderer != null)
            attackFxSpriteRenderer.enabled = false;

        attackFxRoutine = null;
    }

    void UpdateAnimatorState()
    {
        if (animator == null || isDead || isHurt)
            return;

        if (animationLockCounter > 0f)
            return;

        if (isAttacking)
            return;

        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.05f)
                ForceAnimation(HeroAnimState.JumpUp);
            else if (rb.linearVelocity.y < -0.05f)
                ForceAnimation(HeroAnimState.JumpDown);

            return;
        }

        if (IsPushing())
        {
            ForceAnimation(HeroAnimState.Push);
            return;
        }

        if (Mathf.Abs(moveX) > 0.01f)
        {
            ForceAnimation(HeroAnimState.Run);
            return;
        }

        ForceAnimation(HeroAnimState.Idle);
    }

    bool IsPushing()
    {
        if (Mathf.Abs(moveX) < 0.01f || !isGrounded)
            return false;

        Vector2 dir = new Vector2(Mathf.Sign(moveX), 0f);

        if (wallCheck != null)
        {
            return Physics2D.Raycast(GetMirroredWorldPoint(wallCheck, wallCheckLocal), dir, pushCheckDistance, groundLayer).collider != null;
        }

        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.85f);
        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, size, 0f, dir, pushCheckDistance, groundLayer);
        return hit.collider != null && hit.collider != bodyCollider;
    }

    void ForceAnimation(HeroAnimState state, float lockTime = 0f)
    {
        if (animator == null)
            return;

        if (currentAnimState == state)
        {
            if (lockTime > animationLockCounter)
                animationLockCounter = lockTime;

            return;
        }

        currentAnimState = state;
        ResetAllAnimatorTriggers();
        animator.SetTrigger(state.ToString());

        if (lockTime > 0f)
            animationLockCounter = lockTime;
    }

    void ResetAllAnimatorTriggers()
    {
        animator.ResetTrigger(HeroAnimState.Idle.ToString());
        animator.ResetTrigger(HeroAnimState.Run.ToString());
        animator.ResetTrigger(HeroAnimState.Push.ToString());
        animator.ResetTrigger(HeroAnimState.JumpUp.ToString());
        animator.ResetTrigger(HeroAnimState.JumpDown.ToString());
        animator.ResetTrigger(HeroAnimState.JumpDouble.ToString());
        animator.ResetTrigger(HeroAnimState.BeforeOrAfterJump.ToString());
        animator.ResetTrigger(HeroAnimState.Attack.ToString());
        animator.ResetTrigger(HeroAnimState.SwordAttack.ToString());
        animator.ResetTrigger(HeroAnimState.Hit.ToString());
        animator.ResetTrigger(HeroAnimState.Death.ToString());
    }

    Vector2 GetMirroredWorldPoint(Transform point, Vector3 cachedLocal)
    {
        if (point == null)
            return transform.position;

        Vector3 local = cachedLocal;
        local.x = Mathf.Abs(local.x) * (facingRight ? 1f : -1f);
        return transform.TransformPoint(local);
    }

    Vector2 GetPogoWorldPoint()
    {
        if (pogoPoint != null)
            return transform.TransformPoint(pogoPointLocal);

        if (bodyCollider != null)
            return new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y - 0.1f);

        return (Vector2)transform.position + Vector2.down * 0.75f;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void TakeDamage(int damage, Vector2 hitOrigin)
    {
        if (isDead || isHurt)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartAction(DoHit(hitOrigin));
    }


    public void TakeHit(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void TakeHit(int damage, Vector2 hitOrigin)
    {
        TakeDamage(damage, hitOrigin);
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    IEnumerator DoHit(Vector2 hitOrigin)
    {
        isHurt = true;
        isAttacking = false;

        ForceAnimation(HeroAnimState.Hit, hitStunDuration);

        if (rb != null)
        {
            float dirX = hitOrigin == Vector2.zero ? (facingRight ? -1f : 1f) : Mathf.Sign(transform.position.x - hitOrigin.x);
            Vector2 velocity = rb.linearVelocity;
            velocity.x = dirX * hurtKnockbackX;
            velocity.y = hurtKnockbackY;
            rb.linearVelocity = velocity;
        }

        if (hitParticles != null)
            hitParticles.Play();

        yield return new WaitForSeconds(hitStunDuration);
        yield return new WaitForSeconds(Mathf.Max(0f, hurtInvulnerability - hitStunDuration));

        isHurt = false;
        actionRoutine = null;
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;
        isHurt = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        ForceAnimation(HeroAnimState.Death, 999f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.red;
        if (Application.isPlaying)
        {
            Gizmos.DrawWireSphere(GetMirroredWorldPoint(attackPoint, attackPointLocal), attackRadius);
            Gizmos.DrawWireSphere(GetMirroredWorldPoint(swordAttackPoint != null ? swordAttackPoint : attackPoint, swordAttackPoint != null ? swordAttackPointLocal : attackPointLocal), swordAttackRadius);
            Gizmos.DrawWireSphere(GetPogoWorldPoint(), pogoRadius);
        }
        else
        {
            if (attackPoint != null)
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);

            if (swordAttackPoint != null)
                Gizmos.DrawWireSphere(swordAttackPoint.position, swordAttackRadius);
            else if (attackPoint != null)
                Gizmos.DrawWireSphere(attackPoint.position, swordAttackRadius);

            if (pogoPoint != null)
                Gizmos.DrawWireSphere(pogoPoint.position, pogoRadius);
        }

        Gizmos.color = Color.yellow;
        if (wallCheck != null)
        {
            Vector3 from = Application.isPlaying ? (Vector3)GetMirroredWorldPoint(wallCheck, wallCheckLocal) : wallCheck.position;
            float dir = Application.isPlaying ? (facingRight ? 1f : -1f) : 1f;
            Gizmos.DrawLine(from, from + Vector3.right * dir * pushCheckDistance);
        }
    }
}
