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
    public GameObject gunFlashLightPrefab; // assign your global light prefab in Inspector
    [Range(0.05f, 0.5f)] public float impactLightFadeDuration = 0.15f;


    private bool hasHit = false;
    private Collider2D col;
    private Rigidbody2D rb;

    private bool isParried = false;
    public bool IsParried => isParried;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        hasHit = false;
        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;
        CancelInvoke(nameof(Disable));
        Invoke(nameof(Disable), lifetime);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Disable));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;                 // guard against double hits
        if (collision.gameObject.CompareTag("Player")) return;

        hasHit = true;

        // Immediately stop physics and disable collider to avoid further collisions
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
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            PlayHitEffects(hitPoint);
            Disable();
            return;
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
        if (isParried) return; // prevent multiple parries

        isParried = true;      // mark as parried

        if (rb != null)
        {
            rb.simulated = true;
            col.enabled = true;
            hasHit = false;

            rb.linearVelocity = newDirection * speed;
        }
    }

    private void Disable()
    {
        BulletPool.Instance.ReturnBullet(gameObject);
    }

    private System.Collections.IEnumerator FadeAndDestroyLight(Light2D light, float duration)
    {
        float startIntensity = light.intensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            yield return null;
        }
        Destroy(light.gameObject);
    }

}
