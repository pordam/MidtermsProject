using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public GameObject owner;        // who fired it
    public float lifetime = 5f;

    private Vector3 moveDirection;
    private bool isParried = false; // NEW flag

    public bool IsParried => isParried; // expose read-only flag

    public void Initialize(Vector3 direction, GameObject owner = null, int damage = 10)
    {
        moveDirection = direction.normalized;
        this.owner = owner;
        this.damage = damage;
        isParried = false;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    public void Deflect(Vector2 newDirection)
    {
        if (isParried) return;

        moveDirection = newDirection.normalized;
        owner = GameObject.FindWithTag("Player"); // mark player as new owner
        isParried = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        // Ignore collision with the owner (friendly fire prevention)
        if (owner != null && other == owner) return;

        // If parried, skip hitting the player
        if (isParried && other.CompareTag("Player"))
            return;

        // Hit wall
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        // Hit player (only if not parried)
        if (other.CompareTag("Player"))
        {
            Vector2 knockDir = (other.transform.position - transform.position).normalized;
            var playerWithKnockback = other.GetComponent<PlayerController>();
            if (playerWithKnockback != null)
            {
                playerWithKnockback.TakeDamage(damage, knockDir);
            }
            Destroy(gameObject);
            return;
        }

        // Hit enemy (parried bullets can damage them)
        if (isParried && other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // Hit other destructibles
        var destructible = other.GetComponent<IDamageable>();
        if (destructible != null)
        {
            Vector2 knockDir = (other.transform.position - transform.position).normalized;
            destructible.TakeDamage(damage, knockDir);
        }

        Destroy(gameObject);
    }
}
