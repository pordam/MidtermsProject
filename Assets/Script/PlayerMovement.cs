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


    private void UpdateGun()
    {
        if (gunTransform == null) return;

        // Get mouse position
        Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 lookDir = mousePos - (Vector2)transform.position;

        gunTransform.localPosition = lookDir.x < 0 ? gunOffsetLeft : gunOffsetRight;

        // Calculate angle
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        gunTransform.rotation = Quaternion.Euler(0, 0, angle);

        // flip gun sprite vertically
        gunTransform.localScale = lookDir.x < 0 ? new Vector3(1, -1, 1) : Vector3.one;
    }

    private void ToggleGun()
    {
        hasGun = !hasGun; // toggle gun
        animator.SetBool("hasGun", hasGun);
        GunObject.SetActive(hasGun); // show/hide gun sprite
    }

}
