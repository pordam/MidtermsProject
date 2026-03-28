using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Settings")]
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private PlayerStats playerStats;

    [Header("Durations")]
    [SerializeField] private float attackDuration = 0.2f;   // how long hitbox is active
    [SerializeField] private float cooldownDuration = 0.5f; // time before next attack
    [SerializeField] private float bufferWindow = 0.15f;    // input buffer window

    [Header("Swing Settings")]
    [SerializeField] private float radius = 1f;       // distance from player
    [SerializeField] private float swingRange = 90f;  // arc degrees

    [HideInInspector] public bool isMeleeAttacking = false;
    [SerializeField] public float meleeLockout = 0.05f; // tweak in Inspector
    [SerializeField] public float lastMeleeTime;

    private Camera cam;
    private PlayerInputAction inputActions;
    private bool isOnCooldown = false;
    private bool bufferedAttack = false;

    void Awake()
    {
        cam = Camera.main;
        meleeHitbox.gameObject.SetActive(false);
        meleeHitbox.Initialize(playerStats);

        inputActions = new PlayerInputAction();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.PlayerMovement.Melee.performed += OnMelee;
    }

    void OnDisable()
    {
        inputActions.PlayerMovement.Melee.performed -= OnMelee;
        inputActions.Disable();
    }

    public void OnMelee(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if (!isOnCooldown)
            {
                isMeleeAttacking = true;
                lastMeleeTime = Time.time;
                StartCoroutine(DoMelee());
            }
            else
            {
                StartCoroutine(BufferAttack());
            }
        }
    }


    private IEnumerator BufferAttack()
    {
        bufferedAttack = true;
        yield return new WaitForSeconds(bufferWindow);
        bufferedAttack = false;
    }

    private IEnumerator DoMelee()
    {
        isOnCooldown = true;

        // Get mouse direction
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = cam.ScreenToWorldPoint(mousePos);
        Vector2 dir = (worldPos - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Enable hitbox
        meleeHitbox.gameObject.SetActive(true);

        float startAngle = targetAngle - swingRange / 2f;
        float endAngle = targetAngle + swingRange / 2f;

        float t = 0f;
        while (t < attackDuration)
        {
            t += Time.deltaTime;
            float p = t / attackDuration;

            float currentAngle = Mathf.Lerp(startAngle, endAngle, p);
            Vector2 offset = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                                         Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;

            meleeHitbox.transform.position = (Vector2)transform.position + offset;
            meleeHitbox.transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            yield return null;
        }

        meleeHitbox.gameObject.SetActive(false);

        // Wait for cooldown
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;

        // If buffered attack exists, trigger immediately
        if (bufferedAttack)
        {
            bufferedAttack = false;
            StartCoroutine(DoMelee());
        }

        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
        isMeleeAttacking = false;
    }
}
