using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    [SerializeField] private Transform projectileVisual;
    [SerializeField] private Transform projectileShadow;
    [SerializeField] private Projectile projectile;

    private Vector3 trajectoryStartPosition;
    private Vector3 fixedTargetPosition; // NEW

    private float shadowPositionDivider = 6f;

    private void Start()
    {
        trajectoryStartPosition = transform.position;
    }

    private void Update()
    {
        UpdateProjectileRotation();
        UpdateShadowPosition();

        // If you still need progress checks, guard against zero distance
        float trajectoryProgressMagnitude = (transform.position - trajectoryStartPosition).magnitude;
        float trajectoryMagnitude = Mathf.Max(0.0001f, (fixedTargetPosition - trajectoryStartPosition).magnitude);

        float trajectoryProgressNormalized = trajectoryProgressMagnitude / trajectoryMagnitude;

        if (trajectoryProgressNormalized < .7f)
        {
            UpdateProjectileShadowRotation();
        }
    }

    private void UpdateShadowPosition()
    {
        Vector3 newPosition = transform.position;
        Vector3 trajectoryRange = fixedTargetPosition - trajectoryStartPosition;

        if (Mathf.Abs(trajectoryRange.normalized.x) < Mathf.Abs(trajectoryRange.normalized.y))
        {
            // Projectile is curved on the X axis
            newPosition.x = trajectoryStartPosition.x + projectile.GetNextXTrajectoryPosition() / shadowPositionDivider + projectile.GetNextPositionXCorrectionAbsolute();

        }
        else
        {
            // Projectile is curved on the Y axis
            newPosition.y = trajectoryStartPosition.y + projectile.GetNextYTrajectoryPosition() / shadowPositionDivider + projectile.GetNextPositionYCorrectionAbsolute();
        }

        projectileShadow.position = newPosition;
    }

    private void UpdateProjectileRotation()
    {
        Vector3 projectileMoveDir = projectile.GetProjectileMoveDir();

        projectileVisual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(projectileMoveDir.y, projectileMoveDir.x) * Mathf.Rad2Deg);
    }

    private void UpdateProjectileShadowRotation()
    {
        Vector3 projectileMoveDir = projectile.GetProjectileMoveDir();

        projectileShadow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(projectileMoveDir.y, projectileMoveDir.x) * Mathf.Rad2Deg);
    }

    // NEW: accept a fixed target position snapshot
    // Existing Vector3 setter (keep)
    public void SetTargetPosition(Vector3 targetPosition)
    {
        fixedTargetPosition = targetPosition;
        trajectoryStartPosition = transform.position;
    }

    // NEW overload that accepts a Transform and forwards to SetTargetPosition
    public void SetTarget(Transform target)
    {
        if (target == null) return;
        SetTargetPosition(target.position);
    }

}
