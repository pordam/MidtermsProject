using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // Movement
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

    // Animation
    private Animator animator;
    private Vector2 moveInput;
    private SpriteRenderer sr;
    private Vector2 lastDirection = Vector2.down;

    // Gun Logic
    public bool hasGun = false;
    public Camera cam;
    public Transform gunTransform;
    public Vector3 gunOffsetRight = new Vector3(0.0347f, -0.0351f, 0); // gun position when facing right
    public Vector3 gunOffsetLeft = new Vector3(-0.0748f, -0.0351f, 0); // gun position when facing left

    private Vector2 movement;
    private Vector2 aimDirection;

    public GameObject GunObject;
    private PlayerInputAction actions;

    // Health
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI Health")]
    [SerializeField] private GameObject healthBarRoot;      // assign the Bar GameObject
    [SerializeField] private Image healthBarFill;           // assign HealthBarFill Image (Image.Type = Filled recommended)

    [Header("Invulnerability")]
    [SerializeField] private float iFrameDuration = 0.6f;
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.35f);
    private bool isInvulnerable = false;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 300f;

    [Header("Adjustable muzzle offsets (local to gun pivot)")]
    public Transform shootPoint;                       // assign muzzle/firepoint here
    public Vector3 shootLocalOffsetRight = new Vector3(0.5f, 0.05f, 0f);
    public Vector3 shootLocalOffsetLeft = new Vector3(-0.5f, 0.05f, 0f);

    // Function (drop this into your class, replaces your existing UpdateGun)
    private void UpdateGun()
    {
        if (gunTransform == null || cam == null) return;

        // Get mouse position and direction
        Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 lookDir = mousePos - (Vector2)transform.position;
        bool facingLeft = lookDir.x < 0f;

        // Keep your original pivot placement and rotation
        gunTransform.localPosition = facingLeft ? gunOffsetLeft : gunOffsetRight;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Keep your original flip style (as you requested)
        gunTransform.localScale = facingLeft ? new Vector3(1f, -1f, 1f) : Vector3.one;

        // Choose adjustable local offset depending on facing
        Vector3 localOffset = facingLeft ? shootLocalOffsetLeft : shootLocalOffsetRight;

        // Compute world position from rotation + local offset (ignores parent negative scale)
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        Vector3 worldPos = gunTransform.position + rot * localOffset;

        // Apply to shootPoint (if assigned)
        if (shootPoint != null)
        {
            shootPoint.position = worldPos;
            shootPoint.rotation = rot;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(int amount);
        void TakeDamage(float amount, Vector2 knockback);
    }


    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        actions = new PlayerInputAction();

        if (!animator)
            animator = GetComponent<Animator>();
        if (!sr)
            sr = GetComponent<SpriteRenderer>();

        actions.PlayerMovement.EquipGun.performed += ctx => ToggleGun();
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            float t = (float)currentHealth / (float)maxHealth;
            healthBarFill.fillAmount = Mathf.Clamp01(t);
        }

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(currentHealth > 0);
        }
    }

    // Public API: damage, heal, set health
    public void TakeDamage(int amount, Vector2 knockback)
    {
        TakeDamage((float)amount, knockback);
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (isInvulnerable) return;

        currentHealth -= Mathf.RoundToInt(damage);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update UI immediately
        UpdateHealthUI();

        // Play hit animation if available
        if (animator != null) animator.SetTrigger("Hit");

        // Apply knockback
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback.normalized * knockbackForce);
        }

        // Start invulnerability frames and visual feedback
        StartCoroutine(InvulnerabilityCoroutine());

        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUI();
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHealthUI();
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;

        if (sr != null)
        {
            Color original = sr.color;
            float elapsed = 0f;
            bool flashOn = true;

            while (elapsed < iFrameDuration)
            {
                sr.color = flashOn ? flashColor : original;
                flashOn = !flashOn;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }

            sr.color = original;
        }
        else
        {
            yield return new WaitForSeconds(iFrameDuration);
        }

        isInvulnerable = false;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        // Play death animation if you have one
        if (animator != null) animator.SetTrigger("Death");

        // Disable player controls (simple approach)
        // You can expand this to a proper respawn or game over flow
        this.enabled = false;
        GunObject?.SetActive(false);
        // Optionally destroy after a delay:
        // Destroy(gameObject, 1.5f);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnEnable()
    {
        actions.PlayerMovement.Enable();
    }

    private void OnDisable()
    {
        actions.PlayerMovement.Disable();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + moveInput * moveSpeed * Time.fixedDeltaTime
        );

        if (hasGun && gunTransform != null)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 lookDir = mousePos - (Vector2)gunTransform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            gunTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void Update()
    {
        if (hasGun && gunTransform != null)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            aimDirection = mousePos - (Vector2)transform.position;

            if (aimDirection != Vector2.zero)
                lastDirection = aimDirection.normalized;

            animator.SetFloat("X Input", aimDirection.x);
            animator.SetFloat("Y Input", aimDirection.y);

            sr.flipX = lastDirection.x < 0;

            UpdateGun();
        }
        else
        {
            if (moveInput != Vector2.zero)
            {
                // Update lastDirection only while pressing movement keys
                lastDirection = moveInput.normalized;

                animator.SetFloat("X Input", moveInput.x);
                animator.SetFloat("Y Input", moveInput.y);
            }

            sr.flipX = lastDirection.x < 0;

            if (gunTransform != null)
                UpdateGun();
        }

        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    private void ToggleGun()
    {
        hasGun = !hasGun; // toggle gun
        animator.SetBool("hasGun", hasGun);
        GunObject.SetActive(hasGun); // show/hide gun sprite
    }
}
