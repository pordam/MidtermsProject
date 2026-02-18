using UnityEngine;

public interface IDamageable
{
    // Basic damage
    void TakeDamage(int amount);

    // Optional richer API: damage with knockback
    void TakeDamage(float amount, Vector2 knockback);
}
