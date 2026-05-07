using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 3;
    public bool destroyOnDeath = true;
    public float destroyDelay = 0.6f;

    [Header("Opcional")]
    public Collider2D[] collidersToDisable;
    public MonoBehaviour[] behavioursToDisable;
    public GameObject deathEffect;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        SendMessage("OnEnemyHurt", SendMessageOptions.DontRequireReceiver);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        foreach (Collider2D col in collidersToDisable)
        {
            if (col != null)
                col.enabled = false;
        }

        foreach (MonoBehaviour behaviour in behavioursToDisable)
        {
            if (behaviour == null)
                continue;

            if (behaviour is Animator)
                continue;

            if (behaviour is EnemyStateMachine)
                continue;

            behaviour.enabled = false;
        }

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        SendMessage("OnEnemyDied", SendMessageOptions.DontRequireReceiver);

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}
