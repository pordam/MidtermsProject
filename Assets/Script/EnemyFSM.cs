using UnityEngine;
using Pathfinding;
using System.Collections;

public enum EnemyState
{
    Idle,
    Wander,
    Chase,   // kept for future use, not used for "stand still" attack
    Attack
}

[RequireComponent(typeof(IAstarAI))]
public class EnemyFSM : MonoBehaviour
{
    [Header("References")]
    public Transform player;                     // assign in inspector or set at runtime
    private IAstarAI ai;

    [Header("Detection")]
    public float detectionRange = 12f;           // max distance to consider LOS
    public LayerMask lineOfSightMask;            // include Player layer and obstacles
    public string playerTag = "Player";

    [Header("Wander")]
    public float wanderRadius = 6f;              // how far from current position to pick wander targets
    public float wanderInterval = 2.5f;          // how often to pick a new wander target
    public float wanderNearPlayerRadius = 4f;    // when player visible, wander within this radius around player

    [Header("Attack")]
    public float attackRange = 2f;               // optional: if you want close-range attack behavior
    public float shootCooldown = 0.8f;           // seconds between shots while visible

    private EnemyState currentState = EnemyState.Wander;
    private float nextWanderTime = 0f;
    private float lastShootTime = -999f;
    private bool playerVisible = false;

    void Awake()
    {
        ai = GetComponent<IAstarAI>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        ChangeState(EnemyState.Wander);
    }

    void Update()
    {
        if (player == null) return;

        // Check line of sight each frame (or you can move to FixedUpdate if preferred)
        playerVisible = CheckLineOfSight();

        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Wander:
                // If player visible and within detectionRange, go to Attack state (but still move)
                if (playerVisible && distance <= detectionRange)
                {
                    ChangeState(EnemyState.Attack);
                }
                else
                {
                    WanderLogic(false);
                }
                break;

            case EnemyState.Attack:
                // While attacking, we still want movement enabled and wandering behavior,
                // but we bias wander targets to be near the player so the enemy "wanders while shooting".
                WanderLogic(true);

                // Shoot only if visible and cooldown passed
                if (playerVisible && Time.time >= lastShootTime + shootCooldown)
                {
                    // Call your enemy shoot method (ensure Enemy component exists and Shoot() is safe to call)
                    Enemy enemy = GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Shoot();
                        lastShootTime = Time.time;
                    }
                }

                // If player lost or moved out of detection range, return to Wander
                if (!playerVisible || distance > detectionRange)
                {
                    ChangeState(EnemyState.Wander);
                }
                break;

            case EnemyState.Chase:
                // Optional: keep for future behavior
                ai.destination = player.position;
                break;

            case EnemyState.Idle:
                ai.canMove = false;
                break;
        }
    }

    void ChangeState(EnemyState newState)
    {
        currentState = newState;

        // Default: allow movement; specific states can disable it
        ai.canMove = true;

        switch (newState)
        {
            case EnemyState.Wander:
                nextWanderTime = Time.time; // force immediate wander
                break;

            case EnemyState.Attack:
                // keep ai.canMove = true so the enemy moves while shooting
                break;

            case EnemyState.Idle:
                ai.canMove = false;
                break;
        }
    }

    private bool CheckLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 dir = (player.position - transform.position).normalized;

        // Raycast up to detectionRange using the provided mask
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, detectionRange, lineOfSightMask);

        // Debug draw
        if (hit.collider != null)
        {
            Debug.DrawLine(origin, hit.point, hit.collider.CompareTag(playerTag) ? Color.green : Color.red);
            return hit.collider.CompareTag(playerTag);
        }
        else
        {
            Debug.DrawLine(origin, origin + dir * detectionRange, Color.yellow);
            return false;
        }
    }

    private void WanderLogic(bool nearPlayerWhenVisible)
    {
        if (Time.time < nextWanderTime) return;

        Vector3 wanderTarget;

        if (nearPlayerWhenVisible && player != null)
        {
            // Pick a random point around the player, clamped to wanderNearPlayerRadius
            Vector2 offset = Random.insideUnitCircle * wanderNearPlayerRadius;
            wanderTarget = player.position + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            // Pick a random point around current position
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = transform.position + new Vector3(offset.x, offset.y, 0f);
        }

        // Assign destination using A* pathfinding
        ai.canSearch = true;
        ai.destination = wanderTarget;

        // schedule next wander
        nextWanderTime = Time.time + wanderInterval;
    }

    // Optional: visualize detection range and current state in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
