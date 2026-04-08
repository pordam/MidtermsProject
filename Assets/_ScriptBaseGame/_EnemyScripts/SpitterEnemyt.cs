using System.Collections;
using UnityEngine;
using Pathfinding; // remove if you don't use AIPath

[RequireComponent(typeof(Animator))]
public class CurvedShooterEnemy : Enemy
{
    [Header("Shooter Settings (merged from ShooterFinal)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootRate = 1f; // seconds between shots
    [SerializeField] private float projectileMaxMoveSpeed = 5f;
    [SerializeField] private float projectileMaxHeight = 0.5f;

    [Header("Trajectory Curves")]
    [SerializeField] private AnimationCurve trajectoryAnimationCurve;
    [SerializeField] private AnimationCurve axisCorrectionAnimationCurve;
    [SerializeField] private AnimationCurve projectileSpeedAnimationCurve;

    [Header("Animator")]
    [SerializeField] private string animSpeedParam = "Speed";
    [SerializeField] private string animIsDeadParam = "isDead";
    [SerializeField] private float speedSmoothTime = 10f;
    [SerializeField] private string animAttackParam = "Attack"; // name of the trigger parameter in your Animator
    [SerializeField] private bool debugLogs = false;

    private Rigidbody2D rb;

    // cached components
    private Animator animator;
    private Rigidbody2D localRb;
    private AIPath aiPath;

    // internal
    private float shootTimer = 0f;
    private bool shooterEnabled = true; // can be toggled by Die() or other logic

    [SerializeField] private SpriteRenderer sr; // assign in Inspector

    protected override void Awake()
    {
        base.Awake();

        animator = GetComponentInChildren<Animator>();
        localRb = GetComponent<Rigidbody2D>();
        aiPath = GetComponentInChildren<AIPath>(includeInactive: true);

        // initialize animator isDead state
        if (animator != null) animator.SetBool(animIsDeadParam, isDead);

        // initialize timer so first shot occurs after shootRate seconds (or set to 0 for immediate)
        shootTimer = shootRate;
    }

    private void Update()
    {
        // update animator speed every frame
        UpdateAnimatorSpeed();
        UpdateSpriteFlip();

        // If you want automatic timed shooting (non-animation), use Shoot() here.
        // It will respect isDead and shooterEnabled.
        if (shooterEnabled && !isDead)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                // Use the Enemy cooldown if you prefer, otherwise use shootRate
                // Here we use shootRate (from ShooterFinal)
                shootTimer = shootRate;
                // call Shoot override which will snapshot player and spawn projectile
                Shoot();
            }
        }
    }

    private void UpdateSpriteFlip()
    {
        float xVel = 0f;

        if (rb != null)
            xVel = rb.linearVelocity.x; // use Rigidbody2D velocity
        else if (aiPath != null)
            xVel = aiPath.velocity.x;   // use AIPath velocity

        if (xVel < 0.01f)
            sr.flipX = false; // facing right
        else if (xVel > -0.01f)
            sr.flipX = true;  // facing left
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null) return;

        float rawSpeed = 0f;

        if (localRb != null)
        {
            rawSpeed = localRb.linearVelocity.magnitude;
        }
        else if (aiPath != null)
        {
            rawSpeed = aiPath.velocity.magnitude;
        }

        float current = animator.GetFloat(animSpeedParam);
        float smoothed = Mathf.Lerp(current, rawSpeed, Time.deltaTime * speedSmoothTime);
        animator.SetFloat(animSpeedParam, smoothed);
    }

    // Override Shoot to use the merged shooter logic (snapshots player position once)
    public override void Shoot()
    {
        if (!shooterEnabled) return;
        if (isDead) return;
        if (player == null || projectilePrefab == null || firePoint == null) return;

        // Optional: respect base attackCooldown if you want both cooldowns
        if (Time.time < lastAttackTime + attackCooldown) return;

        // snapshot target position at fire time (prevents homing)
        Vector3 targetPos = player.position;

        // instantiate projectile at firePoint
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            // Initialize with fixed target position and curves
            proj.InitializeProjectile(targetPos, projectileMaxMoveSpeed, projectileMaxHeight);
            proj.InitializeAnimationCurves(trajectoryAnimationCurve, axisCorrectionAnimationCurve, projectileSpeedAnimationCurve);
        }

        if (animator != null)
        {
            if (debugLogs) Debug.Log($"{name} Shoot(): setting animator trigger '{animAttackParam}'");
            animator.SetTrigger(animAttackParam);
        }
        else
        {
            // fallback: spawn immediately if animator missing
            if (debugLogs) Debug.LogWarning($"{name} Shoot(): animator missing, spawning immediately");
            OnAttack();
        }

        lastAttackTime = Time.time;
    }

    // Animation Event friendly method: call this from the attack animation at the exact frame
    public void OnAttack()
    {
        // This is identical to Shoot but doesn't check lastAttackTime so animation can control timing.
        if (!shooterEnabled) return;
        if (isDead) return;
        if (player == null || projectilePrefab == null || firePoint == null) return;

        Vector3 targetPos = player.position;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.InitializeProjectile(targetPos, projectileMaxMoveSpeed, projectileMaxHeight);
            proj.InitializeAnimationCurves(trajectoryAnimationCurve, axisCorrectionAnimationCurve, projectileSpeedAnimationCurve);
        }

        lastAttackTime = Time.time;
        // reset shootTimer so automatic shooting doesn't immediately fire again if you want:
        shootTimer = shootRate;
    }

    // Disable shooter when enemy dies and set animator isDead bool
    protected override void Die()
    {
        // stop shooter behavior
        shooterEnabled = false;

        // disable this component if you want to fully stop Update logic (optional)
        // this.enabled = false;

        // set animator isDead
        if (animator != null) animator.SetBool(animIsDeadParam, true);

        base.Die();
    }

    // Optional: if you want to temporarily disable shooting while hurt/knockback,
    // you can override TakeDamage and set shooterEnabled = false, then re-enable
    // when base.RestoreAI() runs (requires making RestoreAI protected) or by polling.
    // For now we only disable on death as requested.

    // Helper to re-enable shooter (useful for respawn)
    public void RestoreShooter()
    {
        shooterEnabled = true;
        if (animator != null) animator.SetBool(animIsDeadParam, isDead);
    }
}
