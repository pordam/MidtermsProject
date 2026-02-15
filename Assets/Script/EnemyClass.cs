using Pathfinding;
using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;
    private bool isDead = false; 

    [Header("Attack Settings")]
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float attackCooldown = 1f;

    protected Transform player;
    protected float lastAttackTime;

    [Header("Death Settings")]
    [SerializeField] private Sprite deadSprite;
    private SpriteRenderer spriteRenderer;
    private bool isFlashing = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void Shoot()
    {
        if (isDead) return; // prevent shooting when dead
        if (player == null || bulletPrefab == null || firePoint == null) return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Vector3 direction = (player.position - firePoint.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            Instantiate(bulletPrefab, firePoint.position, rotation);
            lastAttackTime = Time.time;
        }
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return; // ignore hits when dead

        currentHealth -= amount;

        if (spriteRenderer != null) {
          if (!isFlashing) {
            StartCoroutine(DamageFlash()); }

          else
            {
              spriteRenderer.color = Color.red; } 
        }

          if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        isFlashing = true;
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    protected virtual void Die()
    {
        if (isDead) return; // prevent multiple death calls
        isDead = true;

        Debug.Log($"{gameObject.name} died.");

        if (spriteRenderer != null && deadSprite != null)
        {
            spriteRenderer.sprite = deadSprite;
        }

        // Disable AI scripts so it stops moving/shooting
        GetComponent<EnemyFSM>().enabled = false;
        GetComponent<AntEnemy>().enabled = false;
        GetComponent<Seeker>().enabled = false;
        GetComponent<AIPath>().enabled = false;

        Destroy(gameObject, 20f);
    }
}
