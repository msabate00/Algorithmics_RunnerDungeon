using UnityEngine;

public class EnemySimplePatrolChase : MonoBehaviour
{
    [Header("Referencias")]
    public Transform[] patrolPoints;
    public Transform player;

    [Header("Movimiento")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float pointReachedDistance = 0.1f;

    [Header("Detección")]
    public float detectionDistance = 4f;
    public float returnDistance = 7f;

    [Header("Opcional")]
    public bool flipSprite = true;
    public SpriteRenderer spriteRenderer;

    private int patrolIndex;
    private bool isChasing;
    private bool isDead;

    private void Update()
    {
        if (isDead)
            return;

        if (player == null || patrolPoints == null || patrolPoints.Length == 0)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionDistance)
            isChasing = true;
        else if (distanceToPlayer >= returnDistance)
            isChasing = false;

        if (isChasing)
            ChasePlayer();
        else
            Patrol();
    }

    private void Patrol()
    {
        Transform target = patrolPoints[patrolIndex];

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            patrolSpeed * Time.deltaTime);

        FaceTarget(target.position);

        if (Vector2.Distance(transform.position, target.position) <= pointReachedDistance)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void ChasePlayer()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            chaseSpeed * Time.deltaTime);

        FaceTarget(player.position);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        if (!flipSprite || spriteRenderer == null)
            return;

        if (targetPosition.x > transform.position.x)
            spriteRenderer.flipX = false;
        else if (targetPosition.x < transform.position.x)
            spriteRenderer.flipX = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, returnDistance);
    }

    private void OnEnemyDied()
    {
        isDead = true;
    }
}
