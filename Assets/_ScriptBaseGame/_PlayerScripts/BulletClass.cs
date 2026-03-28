using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet")]
    public float speed = 20f;
    public float lifetime = 2f;
    public int damage = 10;

    [Header("Effects (fallback)")]
    public ParticleSystem hitVfxPrefab;
    public ParticleSystem gunFlashPrefab;
    public AudioClip hitSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Impact Light")]
    public GameObject gunFlashLightPrefab;
    [Range(0.05f, 0.5f)] public float impactLightFadeDuration = 0.15f;

    protected bool hasHit = false;
    protected Collider2D col;
    protected Rigidbody2D rb;

    protected bool isParried = false;
    public bool IsParried => isParried;

    protected PlayerStats stats;

    public void Initialize(PlayerStats stats)
    {
        this.stats = stats;
    }

    protected virtual int CalculateFinalDamage()
    {
        if (stats == null)
        {
            Debug.LogWarning("[Bullet] PlayerStats not set, using base damage");
            return damage;
        }

        int finalDamage = stats.CalculateDamage(damage);

        if (finalDamage > damage)
        {
            Debug.Log($"[Bullet] CRITICAL STRIKE! Enemy took {finalDamage} damage (base {damage})");
        }
        else
        {
            Debug.Log($"[Bullet] Normal hit. Enemy took {finalDamage} damage");
        }

        return finalDamage;
    }


    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        hasHit = false;
        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;
        CancelInvoke(nameof(Disable));
        Invoke(nameof(Disable), lifetime);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        if (collision.gameObject.CompareTag("Player")) return;

        hasHit = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
        if (col != null) col.enabled = false;

        Vector3 hitPoint = transform.position;
        if (collision.contacts != null && collision.contacts.Length > 0)
            hitPoint = collision.contacts[0].point;

        if (collision.gameObject.CompareTag("Wall"))
        {
            PlayHitEffects(hitPoint);
            Disable();
            return;
        }

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null && stats != null)
        {
            int damage = stats.CalculateFinalDamage();
            enemy.TakeDamage(damage);
            Debug.Log($"[Bullet] Hit enemy for {damage}");
        }

        PlayHitEffects(hitPoint);
        Disable();
    }

    private void PlayHitEffects(Vector3 position)
    {
        // 1) Impact / explosion FX (existing)
        if (VfxPool.Instance != null && hitVfxPrefab != null)
        {
            VfxPool.Instance.PlayAt(position, Quaternion.identity, hitVfxPrefab.gameObject);
        }
        else if (hitVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(hitVfxPrefab, position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        // 2) Gun flash at impact (separate instance)
        if (gunFlashPrefab != null)
        {
            VfxPool.Instance.PlayAt(position, Quaternion.identity, gunFlashPrefab.gameObject);
        }

        // 3) Impact light flash
        if (gunFlashLightPrefab != null)
        {
            VfxPool.Instance.PlayAt(position, Quaternion.identity, gunFlashLightPrefab, impactLightFadeDuration);
        }

        // Audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(hitSfx, sfxVolume);
        }
        else if (hitSfx != null)
        {
            AudioSource.PlayClipAtPoint(hitSfx, position, sfxVolume);
        }
    }

    public void Deflect(Vector2 newDirection)
    {
        if (isParried) return;
        isParried = true;

        if (rb != null)
        {
            rb.simulated = true;
            col.enabled = true;
            hasHit = false;
            rb.linearVelocity = newDirection * speed;
        }
    }

    protected virtual void Disable()
    {
        BulletPool.Instance.ReturnBullet(gameObject);
    }
}
