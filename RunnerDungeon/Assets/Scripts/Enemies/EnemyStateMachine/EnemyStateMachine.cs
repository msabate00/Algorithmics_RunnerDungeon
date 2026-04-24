using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Referencias")]
    public Transform[] patrolPoints;
    public Transform player;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public EnemyHealth health;
    public Transform groundCheck;

    [Header("Movimiento")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float returnSpeed = 2.5f;
    public float pointReachedDistance = 0.15f;
    public bool canJump = true;
    public float jumpForce = 8f;
    public float jumpCooldown = 0.35f;
    public bool allowDropFromPlatforms = false;
    public float playerHigherJumpThreshold = 0.75f;

    [Header("Checks")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float groundCheckRadius = 0.15f;
    public float wallCheckDistance = 0.35f;
    public float edgeCheckDistance = 0.8f;

    [Header("Distancias")]
    public float detectionDistance = 5f;
    public float loseDistance = 7f;
    public float attackDistance = 1.2f;

    [Header("Ataque")]
    public int damage = 1;
    public float attackCooldown = 1f;
    public Vector2 attackOffset = new Vector2(0.6f, 0f);
    public float attackRadius = 0.4f;
    public string playerTag = "Player";

    [Header("Hurt")]
    public float hurtTime = 0.2f;
    public Vector2 hurtKnockback = new Vector2(4f, 4f);

    [HideInInspector] public EnemyPatrolState PatrolState;
    [HideInInspector] public EnemyChaseState ChaseState;
    [HideInInspector] public EnemyReturnState ReturnState;
    [HideInInspector] public EnemyAttackState AttackState;
    [HideInInspector] public EnemyHurtState HurtState;
    [HideInInspector] public EnemyDeadState DeadState;

    public EnemyStateBase CurrentState { get; private set; }
    public Rigidbody2D RB { get; private set; }
    public bool IsDead { get; private set; }
    public bool FacingRight { get; private set; } = true;
    public int PatrolIndex { get; set; }
    public int ReturnIndex { get; set; }
    public float AttackTimer { get; set; }
    public float StateTimer { get; set; }
    public bool SuspendAutoMovement { get; set; }
    public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;

    private float desiredVelocityX;
    private float nextJumpTime;

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();

        if (health == null)
            health = GetComponent<EnemyHealth>();

        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        ReturnState = new EnemyReturnState(this);
        AttackState = new EnemyAttackState(this);
        HurtState = new EnemyHurtState(this);
        DeadState = new EnemyDeadState(this);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }

        ChangeState(PatrolState);
    }

    private void Update()
    {
        if (AttackTimer > 0f)
            AttackTimer -= Time.deltaTime;

        CurrentState?.LogicUpdate();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CurrentState?.PhysicsUpdate();
        ApplyHorizontalMovement();
    }

    public void ChangeState(EnemyStateBase newState)
    {
        if (newState == null)
            return;

        if (IsDead && newState != DeadState)
            newState = DeadState;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void SetHorizontalMovement(float direction, float speed)
    {
        if (Mathf.Abs(direction) < 0.01f)
        {
            desiredVelocityX = 0f;
            return;
        }

        desiredVelocityX = Mathf.Sign(direction) * speed;
        FaceDirection(desiredVelocityX > 0f);
    }

    public void StopMoving()
    {
        desiredVelocityX = 0f;
    }

    private void ApplyHorizontalMovement()
    {
        if (RB == null)
            return;

        if (IsDead)
        {
            RB.linearVelocity = new Vector2(0f, RB.linearVelocity.y);
            return;
        }

        if (SuspendAutoMovement)
            return;

        RB.linearVelocity = new Vector2(desiredVelocityX, RB.linearVelocity.y);
    }

    public void StopAllMotion()
    {
        desiredVelocityX = 0f;
        if (RB != null)
            RB.linearVelocity = Vector2.zero;
    }

    public bool CanSeePlayer()
    {
        if (player == null)
            return false;

        return Vector2.Distance(transform.position, player.position) <= detectionDistance;
    }

    public float DistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector2.Distance(transform.position, player.position);
    }

    public float HorizontalDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Mathf.Abs(player.position.x - transform.position.x);
    }

    public float VerticalDifferenceToPlayer()
    {
        if (player == null)
            return 0f;

        return player.position.y - transform.position.y;
    }

    public void FaceTarget(float targetX)
    {
        if (targetX > transform.position.x)
            FaceDirection(true);
        else if (targetX < transform.position.x)
            FaceDirection(false);
    }

    public void FaceDirection(bool faceRight)
    {
        FacingRight = faceRight;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !FacingRight;
    }

    public bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public bool WallAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(FacingRight ? 0.35f : -0.35f, 0f);
        Vector2 direction = FacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    public bool GroundAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(FacingRight ? 0.4f : -0.4f, -0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDistance, groundLayer);
        return hit.collider != null;
    }

    public bool CanJumpNow()
    {
        return canJump && IsGrounded() && Time.time >= nextJumpTime;
    }

    public void Jump()
    {
        if (!CanJumpNow() || RB == null)
            return;

        nextJumpTime = Time.time + jumpCooldown;
        RB.linearVelocity = new Vector2(RB.linearVelocity.x, jumpForce);
    }

    public void DoAttack()
    {
        if (player == null)
            return;

        Vector2 center = (Vector2)transform.position + new Vector2(
            FacingRight ? attackOffset.x : -attackOffset.x,
            attackOffset.y
        );

        Collider2D hit = Physics2D.OverlapCircle(center, attackRadius, playerLayer);

        if (hit == null && DistanceToPlayer() <= attackDistance + 0.05f)
            hit = player.GetComponent<Collider2D>();

        if (hit != null)
            hit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    public Transform GetCurrentPatrolPoint()
    {
        if (!HasPatrolPoints)
            return null;

        PatrolIndex = Mathf.Clamp(PatrolIndex, 0, patrolPoints.Length - 1);
        return patrolPoints[PatrolIndex];
    }

    public Transform GetReturnPoint()
    {
        if (!HasPatrolPoints)
            return null;

        ReturnIndex = Mathf.Clamp(ReturnIndex, 0, patrolPoints.Length - 1);
        return patrolPoints[ReturnIndex];
    }

    public void AdvancePatrolIndex()
    {
        if (!HasPatrolPoints)
            return;

        PatrolIndex = (PatrolIndex + 1) % patrolPoints.Length;
    }

    public void PrepareReturnToNearestPatrolPoint()
    {
        ReturnIndex = GetNearestPatrolPointIndex();
    }

    public int GetNearestPatrolPointIndex()
    {
        if (!HasPatrolPoints)
            return 0;

        int nearest = 0;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    public void ApplyHurtKnockback()
    {
        if (RB == null)
            return;

        float direction = FacingRight ? -1f : 1f;

        if (player != null)
            direction = transform.position.x < player.position.x ? -1f : 1f;

        RB.linearVelocity = new Vector2(hurtKnockback.x * direction, hurtKnockback.y);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float speed = RB != null ? Mathf.Abs(RB.linearVelocity.x) : 0f;

        animator.SetFloat("speed", speed);
        animator.SetBool("isGrounded", IsGrounded());
        animator.SetBool("isChasing", CurrentState == ChaseState);
        animator.SetBool("isReturning", CurrentState == ReturnState);
        animator.SetBool("isAttacking", CurrentState == AttackState);
        animator.SetBool("isHurt", CurrentState == HurtState);
        animator.SetBool("isDead", CurrentState == DeadState);
    }

    private void OnEnemyHurt()
    {
        if (IsDead)
            return;

        ChangeState(HurtState);
    }

    private void OnEnemyDied()
    {
        IsDead = true;
        ChangeState(DeadState);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseDistance);

        Gizmos.color = Color.red;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(
            FacingRight ? attackOffset.x : -attackOffset.x,
            attackOffset.y
        );
        Gizmos.DrawWireSphere(attackCenter, attackRadius);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
