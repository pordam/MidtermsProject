using Pathfinding;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public enum EnemyState
{
    Idle,
    Wander,
    Chase,
    Attack
}

public class EnemyFSM : MonoBehaviour
{
    public Transform player;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float wanderRadius = 5f;   // how far the enemy can wander
    public float wanderInterval = 3f; // how often to pick a new wander target

    private IAstarAI ai;
    private EnemyState currentState;
    private float nextWanderTime;

    void Awake()
    {
        ai = GetComponent<IAstarAI>();
        ChangeState(EnemyState.Wander); // start wandering
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Wander:
                WanderLogic();
                if (distance <= detectionRange) ChangeState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                ai.destination = player.position;
                if (distance <= attackRange) ChangeState(EnemyState.Attack);
                else if (distance > detectionRange) ChangeState(EnemyState.Wander);
                break;

            case EnemyState.Attack:
                ai.canMove = false;

                GetComponent<Enemy>().Shoot();

                if (distance > attackRange)
                {
                    ChangeState(EnemyState.Chase);
                }
                break;

            case EnemyState.Idle:
                ai.canMove = false;
                break;
        }
    }

    void ChangeState(EnemyState newState)
    {
        currentState = newState;
        ai.canMove = true;

        switch (newState)
        {
            case EnemyState.Wander:
                nextWanderTime = Time.time; // force immediate wander
                break;

            case EnemyState.Chase:
                ai.destination = player.position;
                break;

            case EnemyState.Attack:
                ai.canMove = false;
                break;

            case EnemyState.Idle:
                ai.canMove = false;
                break;
        }
    }

    void WanderLogic()
    {
        if (Time.time >= nextWanderTime)
        {
            // Pick a random point within wanderRadius
            Vector3 randomOffset = new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                0,
                Random.Range(-wanderRadius, wanderRadius)
            );

            Vector3 wanderTarget = transform.position + randomOffset;

            // Assign destination using ABPath logic
            ai.canSearch = true;
            ai.destination = wanderTarget;

            // Set next wander time
            nextWanderTime = Time.time + wanderInterval;
        }
    }
}
