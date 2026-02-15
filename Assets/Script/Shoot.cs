using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootForce = 700f;

    [Header("Screen Shake")]
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.2f;
    private ScreenShake screenShake;

    [Header("Gun")]
    public GunScript gunScript; // assign in inspector

    [Header("Fire Effects")]
    public AudioClip fireSfx;                 // firing sound
    [Range(0f, 1f)] public float fireVolume = 1f;
    public ParticleSystem muzzleVfxPrefab;    // optional fallback muzzle VFX

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShootProjectile();
        }
    }

    public void ShootProjectile()
    {
        // Trigger squeeze on the gun (DOTween)
        if (gunScript != null) gunScript.TriggerSqueezeDOT();

        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = (Vector2)shootPoint.right * shootForce;
        }

        // Muzzle VFX via pool if available, otherwise fallback prefab
        if (VfxPool.Instance != null && VfxPool.Instance.prefab != null)
        {
            VfxPool.Instance.PlayAt(shootPoint.position, shootPoint.rotation);
        }
        else if (muzzleVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(muzzleVfxPrefab, shootPoint.position, shootPoint.rotation);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        // Play firing SFX via AudioManager (randomized pitch handled there)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(fireSfx, fireVolume);
        }
        else if (fireSfx != null)
        {
            // fallback: play at camera position for consistent 2D volume
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(fireSfx, pos, fireVolume);
        }

        // Screen shake
        if (screenShake == null && Camera.main != null)
        {
            screenShake = Camera.main.GetComponent<ScreenShake>();
        }
        if (screenShake != null)
        {
            screenShake.triggershake(shakeDuration, shakeMagnitude);
        }
    }
}
