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

    // add these fields to the top of your Shoot class
    [Header("Spread")]
    public float sprayAngle = 5f; // max degrees of random spread (±)


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

        // Calculate base angle from shootPoint forward
        float baseAngle = Mathf.Atan2(shootPoint.right.y, shootPoint.right.x) * Mathf.Rad2Deg;

        // Random spread
        float randomOffset = Random.Range(-sprayAngle, sprayAngle);
        float finalAngle = baseAngle + randomOffset;
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, finalAngle);

        // Spawn projectile with spread rotation
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, spawnRot);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 sprayedDirection = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                                                   Mathf.Sin(finalAngle * Mathf.Deg2Rad)).normalized;
            rb.linearVelocity = sprayedDirection * shootForce;
        }

        // prefer using the muzzleVfxPrefab assigned on this gun
        if (VfxPool.Instance != null)
        {
            // pass the local prefab as an override; if it's null, pool will use its own prefab
            VfxPool.Instance.PlayAt(shootPoint.position, shootPoint.rotation, muzzleVfxPrefab);
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
