using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootForce = 700f;

    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.2f;

    private ScreenShake screenShake;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShootProjectile();
        }
    }

    
    public void ShootProjectile()
    {

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        rb.linearVelocity = shootPoint.right * shootForce;

        if (screenShake == null)
        {
            screenShake = Camera.main.GetComponent<ScreenShake>();
        }
        if (screenShake != null)
        {
            screenShake.triggershake(shakeDuration, shakeMagnitude);
        }
    }
}
