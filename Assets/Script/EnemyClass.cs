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
    [SerializeField] private float squeezeX = 0.6f;           // target X scale multiplier (0.6 = 40% narrower)
    [SerializeField] private float squeezeInDuration = 0.04f; // unscaled seconds to squeeze in
    [SerializeField] private float squeezeOutDuration = 0.08f;// unscaled seconds to return to normal
    private Vector3 spriteOriginalScale = Vector3.one;
    private Coroutine squeezeCoroutine;



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

        Debug.Log($"{name} cached components: FSMs={cachedFSMs.Length}, Ants={cachedAnts.Length}, Seekers={cachedSeekers.Length}, AIPaths={cachedAIPaths.Length}");

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
        Vector3 target = new Vector3(spriteOriginalScale.x * squeezeX, spriteOriginalScale.y, spriteOriginalScale.z);

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

            Instantiate(bulletPrefab, firePoint.position, rotation);
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

        else
        {
            Debug.LogWarning($"{name} No Rigidbody2D to apply knockback");
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"{name} Health <= 0, calling Die()");
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

        // visual: restore color and swap sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            if (deadSprite != null) spriteRenderer.sprite = deadSprite;
        }

        // Disable cached AI components for corpse (keeps them off)
        if (cachedFSMs != null) foreach (var f in cachedFSMs) if (f != null) f.enabled = false;
        if (cachedAnts != null) foreach (var a in cachedAnts) if (a != null) a.enabled = false;
        if (cachedSeekers != null) foreach (var s in cachedSeekers) if (s != null) s.enabled = false;
        if (cachedAIPaths != null) foreach (var p in cachedAIPaths) if (p != null) p.enabled = false;

        // Disable the CircleCollider2D on this GameObject (if present)
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            circle.enabled = false;
            Debug.Log($"{name} disabled CircleCollider2D on {circle.gameObject.name}");
        }

        // Optional: disable any Collider2D on children (uncomment if desired)
        // foreach (var col in GetComponentsInChildren<Collider2D>()) if (col != null) col.enabled = false;

        // Restore global time immediately (safe fallback) so hitstop can't persist
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Ensure AI is restored to original state before stopping coroutines
        RestoreAI();

        // Stop coroutines on this object (we already restored time and AI)
        StopAllCoroutines();

        // Make physics static so corpse doesn't move (optional)
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.bodyType = RigidbodyType2D.Static;
        }

        // Destroy once (only once)
        Destroy(gameObject, 20f);
    }

}