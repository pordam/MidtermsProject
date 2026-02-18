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
    [SerializeField] protected int bulletDamage = 10;

    protected Transform player;
    protected float lastAttackTime;

    [Header("Death Settings")]
    [SerializeField] private Sprite deadSprite;
    private SpriteRenderer spriteRenderer;
    private bool isFlashing = false;

    // Header additions (place these with your other serialized fields)
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 6f;      // impulse strength
    [SerializeField] private float knockbackDuration = 0.2f; // how long the knockback lasts
    private Rigidbody2D rb;
    private Coroutine knockbackCoroutine;

    // cached AI components (may be on this GameObject or children)
    private EnemyFSM[] cachedFSMs;
    private AntEnemy[] cachedAnts;
    private Seeker[] cachedSeekers;
    private AIPath[] cachedAIPaths;

    // cached original enabled states (persist across overlapping hits)
    private bool originalFSMEnabled;
    private bool originalAntEnabled;
    private bool originalSeekerEnabled;
    private bool originalAIPathEnabled;
    private bool aiStateCached = false; // whether original states have been cached

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.08f; // unscaled seconds of freeze
    [SerializeField] private bool useFullFreeze = true;    // if false, uses tiny timescale instead of 0

    [Header("Hit Squeeze")]
    [SerializeField] private float squeezeY = 0.6f;           // target X scale multiplier (0.6 = 40% narrower)
    [SerializeField] private float squeezeInDuration = 0.04f; // unscaled seconds to squeeze in
    [SerializeField] private float squeezeOutDuration = 0.08f;// unscaled seconds to return to normal
    private Vector3 spriteOriginalScale = Vector3.one;
    private Coroutine squeezeCoroutine;

    [Header("Loot Drop")]
    [SerializeField] private GameObject lootPrefab;    // assign Loot prefab in Inspector
    [SerializeField] private int dropCount = 1;         // how many items to spawn
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 1f;    // probability to drop (0..1)
    [SerializeField] private float lootSpawnRadius = 0.2f; // small random offset

    private Color originalColor = Color.white;

    // Modify Awake to cache Rigidbody2D (add this line inside Awake)
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>(); // cache rigidbody (may be null if not present)
        // cache AI components on this GameObject and its children so we can reliably disable them later
        cachedFSMs = GetComponentsInChildren<EnemyFSM>(includeInactive: true);
        cachedAnts = GetComponentsInChildren<AntEnemy>(includeInactive: true);
        cachedSeekers = GetComponentsInChildren<Seeker>(includeInactive: true);
        cachedAIPaths = GetComponentsInChildren<AIPath>(includeInactive: true);

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteOriginalScale = spriteRenderer.transform.localScale;
        }

    }

    private IEnumerator DoSqueezeUnscaled()
    {
        if (spriteRenderer == null) yield break;

        // stop any running squeeze
        if (squeezeCoroutine != null) StopCoroutine(squeezeCoroutine);

        Transform t = spriteRenderer.transform;
        Vector3 start = t.localScale;
        Vector3 target = new Vector3(spriteOriginalScale.x, spriteOriginalScale.y * squeezeY, spriteOriginalScale.z);

        // squeeze in (unscaled)
        float ttime = 0f;
        while (ttime < squeezeInDuration)
        {
            ttime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(ttime / Mathf.Max(0.0001f, squeezeInDuration));
            t.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }

        // squeeze out (unscaled)
        ttime = 0f;
        while (ttime < squeezeOutDuration)
        {
            ttime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(ttime / Mathf.Max(0.0001f, squeezeOutDuration));
            t.localScale = Vector3.Lerp(target, spriteOriginalScale, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }

        t.localScale = spriteOriginalScale;
        squeezeCoroutine = null;
    }

    public virtual void Shoot()
    {
        if (isDead) return; // prevent shooting when dead
        if (player == null || bulletPrefab == null || firePoint == null) return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Vector3 direction = (player.position - firePoint.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            // Instantiate and initialize bullet so it knows its owner and damage
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, rotation);
            var bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.Initialize(direction, this.gameObject, bulletDamage);
            }

            lastAttackTime = Time.time;
        }
    }


    private IEnumerator DelayedKnockbackAfterHitStop(Vector2 knockDir, float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // interrupt previous knockback safely
        if (knockbackCoroutine != null)
        {
            Debug.Log($"{name} Interrupting existing knockback before starting new one");
            RestoreAI();
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        knockbackCoroutine = StartCoroutine(DoKnockback(knockDir));
    }



    // Replace your existing TakeDamage method with this (keeps flash + death logic, adds knockback)
    public virtual void TakeDamage(int amount)
    {
        if (isDead)
        {
            Debug.Log($"{name} TakeDamage ignored: already dead");
            return;
        }

        currentHealth -= amount;
        Debug.Log($"{name} TakeDamage: amount={amount}, currentHealth={currentHealth}");

        if (spriteRenderer != null)
        {
            if (!isFlashing) StartCoroutine(DamageFlash());
            else spriteRenderer.color = Color.red;
        }

        // Compute knockback direction (away from player if available)
        Vector2 knockDir = player != null
            ? ((Vector2)transform.position - (Vector2)player.position).normalized
            : Vector2.up;

        // start visual squeeze (unscaled) � stop previous if running
        if (spriteRenderer != null)
        {
            if (squeezeCoroutine != null) StopCoroutine(squeezeCoroutine);
            squeezeCoroutine = StartCoroutine(DoSqueezeUnscaled());
        }

        // inside TakeDamage (after computing knockDir)
        if (rb != null)
        {
            // interrupt existing knockback safely
            if (knockbackCoroutine != null)
            {
                RestoreAI();
                StopCoroutine(knockbackCoroutine);
                knockbackCoroutine = null;
            }

            // ask manager to perform hitstop
            HitStopManager.Instance?.StartHitStop(hitStopDuration, useFullFreeze);

            // locally wait unscaled then start knockback
            StartCoroutine(DelayedKnockbackAfterHitStop(knockDir, hitStopDuration));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private void CacheAndDisableAI()
    {
        if (!aiStateCached)
        {
            var fsm = GetComponentInChildren<EnemyFSM>();
            var ant = GetComponentInChildren<AntEnemy>();
            var seeker = GetComponentInChildren<Seeker>();
            var aiPath = GetComponentInChildren<AIPath>();

            originalFSMEnabled = fsm != null && fsm.enabled;
            originalAntEnabled = ant != null && ant.enabled;
            originalSeekerEnabled = seeker != null && seeker.enabled;
            originalAIPathEnabled = aiPath != null && aiPath.enabled;

            aiStateCached = true;
        }

        // disable all found components (safe to call repeatedly)
        foreach (var f in cachedFSMs) if (f != null) f.enabled = false;
        foreach (var a in cachedAnts) if (a != null) a.enabled = false;
        foreach (var s in cachedSeekers) if (s != null) s.enabled = false;
        foreach (var p in cachedAIPaths) if (p != null) p.enabled = false;
    }

    private void RestoreAI()
    {
        // restore to the original cached values (only if we cached them)
        if (!aiStateCached) return;

        // restore components that exist to their original enabled state
        foreach (var f in cachedFSMs)
            if (f != null) f.enabled = originalFSMEnabled;

        foreach (var a in cachedAnts)
            if (a != null) a.enabled = originalAntEnabled;

        foreach (var s in cachedSeekers)
            if (s != null) s.enabled = originalSeekerEnabled;

        foreach (var p in cachedAIPaths)
            if (p != null) p.enabled = originalAIPathEnabled;

        aiStateCached = false;
    }


    // New coroutine to handle knockback and temporarily disable AI movement
    private IEnumerator DoKnockback(Vector2 direction)
    {
        if (rb == null)
        {
            Debug.LogWarning($"{name} DoKnockback aborted: rb is null");
            yield break;
        }

        Debug.Log($"{name} DoKnockback start: dir={direction}, force={knockbackForce}, duration={knockbackDuration}");

        // Cache original enabled states and disable AI components
        CacheAndDisableAI();

        // Apply impulse once
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
        Debug.Log($"{name} Applied impulse. rb.velocity={rb.linearVelocity}");

        // Wait for knockback duration (use scaled time so knockback respects hitstop timing)
        float timer = 0f;
        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Clear residual velocity
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"{name} DoKnockback end. Cleared velocity.");

        // Restore AI components to their original cached state
        RestoreAI();

        knockbackCoroutine = null;
        Debug.Log($"{name} DoKnockback coroutine finished, AI restored.");
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
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} died.");

        // --- Spawn loot ---
        if (lootPrefab != null)
        {
            for (int i = 0; i < dropCount; i++)
            {
                if (Random.value <= dropChance)
                {
                    // small random offset so items don't overlap exactly
                    Vector2 offset = Random.insideUnitCircle * lootSpawnRadius;
                    Vector3 spawnPos = transform.position + (Vector3)offset;

                    GameObject lootObj = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
                    // Optional: if you want to set a different sprite per enemy:
                    // var sr = lootObj.GetComponent<SpriteRenderer>();
                    // if (sr != null) sr.sprite = someEnemySpecificSprite;

                    // If you want the loot to immediately simulate a drop with a custom horizontal force:
                    var lootScript = lootObj.GetComponent<Loot>();
                    if (lootScript != null)
                    {
                        // The Loot script auto-starts SimulateDrop() in Start(), but if you want to
                        // pass a custom initial ground velocity you can call Initialize after instantiation:
                        // lootScript.Initialize(new Vector2(Random.Range(-1f,1f), Random.Range(-1f,1f)));
                        // Otherwise the prefab's Start() will call SimulateDrop() and use its settings.
                    }
                }
            }
        }

        // --- existing death behavior (keep your current code) ---
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            if (deadSprite != null) spriteRenderer.sprite = deadSprite;
        }

        // disable AI components, colliders, stop coroutines, etc.
        if (cachedFSMs != null) foreach (var f in cachedFSMs) if (f != null) f.enabled = false;
        if (cachedAnts != null) foreach (var a in cachedAnts) if (a != null) a.enabled = false;
        if (cachedSeekers != null) foreach (var s in cachedSeekers) if (s != null) s.enabled = false;
        if (cachedAIPaths != null) foreach (var p in cachedAIPaths) if (p != null) p.enabled = false;

        var circle = GetComponent<CircleCollider2D>();
        if (circle != null) circle.enabled = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        RestoreAI();
        StopAllCoroutines();

        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.bodyType = RigidbodyType2D.Static;
        }

        Destroy(gameObject, 20f);
    }
}