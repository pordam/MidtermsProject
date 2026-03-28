using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer debugSprite;
    [SerializeField] private PlayerStats playerStats;

    [Header("Damage")]
    [SerializeField] private int meleeDamage = 10;

    [Header("Knockback")]
    [SerializeField] private float meleeKnockbackForce = 10f; // tweak in Inspector

    public void Initialize(PlayerStats stats)
    {
        playerStats = stats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && playerStats != null)
        {
            // Calculate damage
            int baseDamage = playerStats.CalculateFinalDamage();
            int finalDamage = baseDamage + meleeDamage;

            enemy.TakeDamage(finalDamage);

            // Apply knockback if enemy has Rigidbody2D
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                rb.AddForce(knockbackDir * meleeKnockbackForce, ForceMode2D.Impulse);
            }

            Camera.main.GetComponent<ScreenShake>()?.triggershake(0.1f, 0.2f);
            HitStopManager.Instance?.StartHitStop(0.1f, true);

            Debug.Log($"[Melee] Hit enemy for {finalDamage} with knockback {meleeKnockbackForce}");
        }
    }
}
