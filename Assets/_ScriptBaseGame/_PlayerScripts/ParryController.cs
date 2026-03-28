using DG.Tweening.Core.Easing;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private ParryFlashFX flashFX;

    public static event System.Action OnParrySuccess;

    private PlayerInputAction inputActions;

    void Awake()
    {
        if (parryCollider == null)
            parryCollider = GetComponent<CircleCollider2D>();

        parryCollider.isTrigger = true;
        parryCollider.enabled = false;

        flashFX = Object.FindFirstObjectByType<ParryFlashFX>();
        inputActions = new PlayerInputAction();
    }
    public void OnParry()
    {
        if (!isParrying)
            StartCoroutine(DoParry());
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.PlayerMovement.Parry.performed += OnParryInput;
    }

    void OnDisable()
    {
        inputActions.PlayerMovement.Parry.performed -= OnParryInput;
        inputActions.Disable();
    }

    private void OnParryInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnParry(); // call your existing method
        }
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
        HitStopManager.Instance?.StartHitStop(hitStopDuration, fullFreeze);
        OnParrySuccess?.Invoke();

        flashFX?.TriggerFlash(); // play the white flash
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
