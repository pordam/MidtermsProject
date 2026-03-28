using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

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
    public ParticleSystem gunFlashPrefab;     // small gun flash prefab to also play at impact

    // add these fields to the top of your Shoot class
    [Header("Spread")]
    public float sprayAngle = 5f; // max degrees of random spread (±)

    [Header("Casing Eject")]
    public Transform casingEjectPoint;

    [Header("Gun Flash Light")]
    public GameObject gunFlashLightPrefab; // assign your global light prefab in Inspector
    [Range(0.05f, 0.5f)] public float lightFadeDuration = 0.15f;

    [SerializeField] private AmmoManager ammoManager;

    [SerializeField] private PlayerStats playerStats;

    private PlayerInputAction inputActions;

    [SerializeField] private PlayerCombat playerCombat;

    void Awake()
    {
        inputActions = new PlayerInputAction();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.PlayerMovement.Shoot.performed += OnShoot;   // M2
        // Melee will be handled in your PlayerCombat script
    }

    void OnDisable()
    {
        inputActions.PlayerMovement.Shoot.performed -= OnShoot;
        inputActions.Disable();
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (playerCombat != null)
        {
            // block if melee flag is active OR just started
            if (playerCombat.isMeleeAttacking ||
                Time.time - playerCombat.lastMeleeTime < playerCombat.meleeLockout)
            {
                Debug.Log("Cannot shoot while meleeing!");
                return;
            }
        }

        ShootProjectile();
    }



    void OnDrawGizmos()
    {
        if (casingEjectPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(casingEjectPoint.position, 0.05f);
            Gizmos.DrawLine(casingEjectPoint.position, casingEjectPoint.position + casingEjectPoint.right * 0.5f);
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

        if (!ammoManager.TryConsumeBullet())
        {
            // No ammo, gun just clicks
            return;
        }

        GameObject projectile = BulletPool.Instance.GetBullet(shootPoint.position, spawnRot, playerStats);
        var bulletScript = projectile.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.gunFlashPrefab = gunFlashPrefab;
        }

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
            Debug.Log("[Shoot] Playing muzzle VFX via VfxPool at " + shootPoint.position);
            VfxPool.Instance.PlayAt(shootPoint.position, shootPoint.rotation, muzzleVfxPrefab?.gameObject);
        }
        else if (muzzleVfxPrefab != null)
        {
            Debug.Log("[Shoot] Instantiating muzzle VFX directly at " + shootPoint.position);
            ParticleSystem vfx = Instantiate(muzzleVfxPrefab, shootPoint.position, shootPoint.rotation);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }
        else
        {
            Debug.LogWarning("[Shoot] No muzzle VFX prefab assigned!");
        }

        if (gunFlashPrefab != null)
        {
            Debug.Log("[Shoot] Instantiating gun flash directly for test.");
            var flash = Instantiate(gunFlashPrefab,
                                    shootPoint.position + shootPoint.right * 0.1f,
                                    shootPoint.rotation);
            flash.Play();
            Destroy(flash.gameObject,
                    flash.main.duration + flash.main.startLifetime.constantMax);
        }

        // Gun light test (bypassing pool)
        if (gunFlashLightPrefab != null)
        {
            Debug.Log("[Shoot] Instantiating gun flash light directly for test.");
            var lightObj = Instantiate(gunFlashLightPrefab,
                                       shootPoint.position + shootPoint.right * 0.1f,
                                       shootPoint.rotation);
            // If it has a Light2D component, fade it out
            var light2D = lightObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null)
            {
                StartCoroutine(FadeAndDestroyLight(light2D, lightFadeDuration));
            }
        }

        if (ShellParticleSystemHandler.Instance != null)
        {
            Vector3 spawnPos = casingEjectPoint != null ? casingEjectPoint.position : shootPoint.position;
            Vector3 ejectDir = casingEjectPoint != null ? casingEjectPoint.right : (shootPoint.right + shootPoint.up * 0.3f).normalized;
            ShellParticleSystemHandler.Instance.SpawnShell(spawnPos, ejectDir);
        }

        if (gunFlashPrefab != null)
        {
            VfxPool.Instance.PlayAt(shootPoint.position, shootPoint.rotation, gunFlashPrefab.gameObject);
        }

        if (gunFlashLightPrefab != null)
        {
            VfxPool.Instance.PlayAt(shootPoint.position, shootPoint.rotation, gunFlashLightPrefab, lightFadeDuration);
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
    private System.Collections.IEnumerator FadeAndDestroyLight(Light2D light, float duration)
    {
        float startIntensity = light.intensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            yield return null;
        }
        Destroy(light.gameObject);
    }

}
