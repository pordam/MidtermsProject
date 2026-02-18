using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;                 // set per-enemy when spawning
    public GameObject owner;                // set to the enemy that fired this bullet
    public float lifetime = 5f;

    private Vector3 moveDirection;

    public void Initialize(Vector3 direction, GameObject owner = null, int damage = 10)
    {
        moveDirection = direction.normalized;
        this.owner = owner;
        this.damage = damage;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        // Ignore collision with the owner (friendly fire prevention)
        if (owner != null && other == owner) return;

        // Hit wall or environment
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        // Hit player
        if (other.CompareTag("Player"))
        {
            // Compute knockback direction away from bullet
            Vector2 knockDir = (other.transform.position - transform.position).normalized;

            // Try to call a knockback-capable API first
            var playerWithKnockback = other.GetComponent<PlayerController>();
            if (playerWithKnockback != null)
            {
                // If your PlayerController has a TakeDamage(float, Vector2) overload, call it.
                // We'll attempt to call the overload with reflection-like safety:
                // First try the float+knockback signature via dynamic dispatch (C# will pick the best match).
                try
                {
                    // If PlayerController defines TakeDamage(float, Vector2), this will call it.
                    // If not, the next line will throw and we'll fall back to the int-only call.
                    playerWithKnockback.GetType()
                        .GetMethod("TakeDamage", new System.Type[] { typeof(float), typeof(Vector2) })
                        ?.Invoke(playerWithKnockback, new object[] { (float)damage, knockDir });

                    // Destroy bullet and return if the method existed and was invoked
                    Destroy(gameObject);
                    return;
                }
                catch
                {
                    // ignore and fall back
                }

                // Fallback: call the int-only signature if present
                var intMethod = playerWithKnockback.GetType().GetMethod("TakeDamage", new System.Type[] { typeof(int) });
                if (intMethod != null)
                {
                    intMethod.Invoke(playerWithKnockback, new object[] { damage });
                    Destroy(gameObject);
                    return;
                }

                // If neither method exists, try a common interface (IDamageable) or log
                var dmg = other.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(damage, knockDir);
                    Destroy(gameObject);
                    return;
                }

                Debug.LogWarning("EnemyBullet: PlayerController has no compatible TakeDamage method.");
            }

            // If we couldn't find PlayerController, try IDamageable directly
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, knockDir);
                Destroy(gameObject);
                return;
            }

            // Default: just destroy the bullet if it hits the player but no API found
            Destroy(gameObject);
            return;
        }

        // Hit other things (optional: damage destructibles)
        var destructible = other.GetComponent<IDamageable>();
        if (destructible != null)
        {
            Vector2 knockDir = (other.transform.position - transform.position).normalized;
            destructible.TakeDamage(damage, knockDir);
        }

        Destroy(gameObject);
    }
}
