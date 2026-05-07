using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Daño al jugador")]
    public int damage = 1;
    public float hitCooldown = 0.75f;
    public string playerTag = "Player";

    private float nextHitTime;
    private EnemyHealth cachedHealth;

    private void Awake()
    {
        cachedHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider.gameObject);
    }

    private void TryDamage(GameObject other)
    {
        if (other == null)
            return;

        if (cachedHealth != null && cachedHealth.IsDead)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (Time.time < nextHitTime)
            return;

        nextHitTime = Time.time + hitCooldown;
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}
