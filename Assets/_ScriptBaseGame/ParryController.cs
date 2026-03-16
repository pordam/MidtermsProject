using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class ParryController : MonoBehaviour
{
    [Header("Parry Settings")]
    [SerializeField] private float parryDuration = 0.3f; // adjustable in Inspector
    [SerializeField] private CircleCollider2D parryCollider;

    [Header("HitStop Settings")]
    [SerializeField] private float hitStopDuration = 0.1f; // tweakable in Inspector
    [SerializeField] private bool fullFreeze = true;       // toggle full freeze vs slow motion

    private bool isParrying = false;

    public static event System.Action OnParrySuccess;

    void Awake()
    {
        if (parryCollider == null)
            parryCollider = GetComponent<CircleCollider2D>();

        parryCollider.isTrigger = true;
        parryCollider.enabled = false;
    }

    public void OnParry()
    {
        if (!isParrying)
            StartCoroutine(DoParry());
    }

    private IEnumerator DoParry()
    {
        isParrying = true;
        parryCollider.enabled = true;

        yield return new WaitForSeconds(parryDuration);

        parryCollider.enabled = false;
        isParrying = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBullet enemyBullet = other.GetComponent<EnemyBullet>();
        if (enemyBullet != null && !enemyBullet.IsParried)
        {
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            enemyBullet.Deflect(randomDir);

            TriggerParrySuccess(); // reload only on enemy parry
            return;
        }

        Bullet playerBullet = other.GetComponent<Bullet>();
        if (playerBullet != null && !playerBullet.IsParried)
        {
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            playerBullet.Deflect(randomDir);

            // no TriggerParrySuccess here
        }
    }

    private void TriggerParrySuccess()
    {
        // Call hitstop when parry succeeds
        HitStopManager.Instance?.StartHitStop(hitStopDuration, fullFreeze);

        OnParrySuccess?.Invoke(); // notify reload system
    }

    void OnDrawGizmosSelected()
    {
        if (parryCollider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, parryCollider.radius);
        }
    }
}
