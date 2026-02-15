using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // Optional: keep the visible sprite oriented as you currently do (no changes here)
        // If you want to flip the sprite child instead of rotating it, handle that elsewhere.
    }


    private void Awake()
    {
        currentHealth = maxHealth;

        actions = new PlayerInputAction();

        if (!animator)
            animator = GetComponent<Animator>();
        if (!sr)
        {
            sr = GetComponent<SpriteRenderer>();
        }


        actions.PlayerMovement.EquipGun.performed += ctx => ToggleGun();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Remaining health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        Destroy(gameObject);
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
