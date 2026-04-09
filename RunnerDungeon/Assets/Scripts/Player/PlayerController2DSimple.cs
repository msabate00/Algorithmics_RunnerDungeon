using UnityEngine;

public class PlayerController2DSimple : MonoBehaviour
{
    [Header("Componentes")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    [Header("Puntos de comprobacion")]
    public Transform groundCheck;
    public Transform attackPoint;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    [Header("Movimiento")]
    public float speed = 5f;
    public float jumpForce = 10f;
    public float groundCheckRadius = 0.2f;

    [Header("Ataque")]
    public float attackRadius = 0.5f;
    public int attackDamage = 1;

    private float moveX;
    private bool isGrounded;
    private bool isAttacking;

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Leer izquierda y derecha
        moveX = Input.GetAxisRaw("Horizontal");

        // Mirar a la izquierda o a la derecha
        if (moveX > 0)
            spriteRenderer.flipX = false;
        else if (moveX < 0)
            spriteRenderer.flipX = true;

        // Comprobar si esta en el suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Saltar
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Atacar
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        // Mover al personaje
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);
    }

    void Attack()
    {
        isAttacking = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        // Solo para que el Animator vea el ataque este frame
        Invoke(nameof(StopAttack), 0.1f);
    }

    void StopAttack()
    {
        isAttacking = false;
    }

    void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", Mathf.Abs(moveX));
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Jump", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("Fall", !isGrounded && rb.linearVelocity.y < -0.1f);
        animator.SetBool("Attack", isAttacking);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}

public interface IDamageable
{
    void TakeDamage(int damage);
}
