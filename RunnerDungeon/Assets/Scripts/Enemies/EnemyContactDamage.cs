using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Daño al jugador")]
    public int damage = 1;
    public float hitCooldown = 0.75f;
    public string playerTag = "Player";

    private float nextHitTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (Time.time < nextHitTime)
            return;

        nextHitTime = Time.time + hitCooldown;

        // Busca cualquier método TakeDamage(int) en el jugador.
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}
