using UnityEngine;
using System.Collections;

public class AntEnemy : Enemy
{
    [Header("Ant Settings")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float sprayAngle = 5f; 
    [SerializeField] private int burstCount = 3;   
    [SerializeField] private float burstInterval = 0.1f; 
    [SerializeField] private float burstCooldown = 1.5f; 

    private bool isFiringBurst = false;

    public override void Shoot()
    {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        if (!isFiringBurst && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(FireBurstCycle());
            lastAttackTime = Time.time;
        }
    }

    private IEnumerator FireBurstCycle()
    {
        isFiringBurst = true;

        // Fire a burst of bullets
        for (int i = 0; i < burstCount; i++)
        {
            FireSingleBullet();
            yield return new WaitForSeconds(burstInterval);
        }

        // Wait before next burst
        yield return new WaitForSeconds(burstCooldown);

        isFiringBurst = false;
    }

    private void FireSingleBullet()
    {
        Vector2 direction = (player.position - firePoint.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float randomOffset = Random.Range(-sprayAngle, sprayAngle);
        float finalAngle = angle + randomOffset;

        Vector2 sprayedDirection = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                                               Mathf.Sin(finalAngle * Mathf.Deg2Rad));

        Quaternion rotation = Quaternion.AngleAxis(finalAngle, Vector3.forward);
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);

        bullet.GetComponent<EnemyBullet>().Initialize(sprayedDirection);

        Destroy(bullet, 3f);

        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }
    }
}
