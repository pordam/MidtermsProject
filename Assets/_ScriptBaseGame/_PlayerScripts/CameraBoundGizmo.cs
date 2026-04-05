using UnityEngine;

/// <summary>
/// CameraBoundsController
/// Keeps a Camera's view inside a BoxCollider2D bounds. Designed for 2D orthographic cameras,
/// but includes a fallback for perspective cameras (uses ViewportToWorldPoint).
///
/// Usage:
/// - Attach this to any GameObject (often the same GameObject that has the BoxCollider2D).
/// - Assign the BoxCollider2D that defines the allowed camera area (boundsCollider).
/// - Assign the Camera to control (targetCamera). If left empty, Camera.main is used.
/// - Tweak padding, smoothing, and behavior in the inspector.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera to constrain. If null, Camera.main will be used.")]
    public Camera targetCamera;

    [Tooltip("BoxCollider2D that defines the allowed camera area. If left null, the collider on this GameObject will be used.")]
    public BoxCollider2D boundsCollider;

    [Header("Behavior")]
    [Tooltip("Extra padding inside the bounds (in world units). Positive values shrink the usable area.")]
    public Vector2 padding = Vector2.zero;

    [Tooltip("If true, the camera will smoothly move to the clamped position.")]
    public bool useSmoothing = true;

    [Tooltip("Smoothing time for camera movement (smaller = snappier).")]
    [Range(0f, 1f)]
    public float smoothTime = 0.08f;

    [Tooltip("If true and the bounds are smaller than the camera view, the camera will center on the bounds instead of trying to fit.")]
    public bool centerWhenSmallerThanBounds = true;

    [Header("Optional Zoom Fit (Orthographic only)")]
    [Tooltip("If true, the orthographic camera will zoom out to fit the bounds when the bounds are smaller than the view.")]
    public bool allowZoomToFit = false;

    [Tooltip("Minimum orthographic size when auto-fitting.")]
    public float minOrthoSize = 1f;

    [Tooltip("Maximum orthographic size when auto-fitting.")]
    public float maxOrthoSize = 100f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.1f, 0.8f, 0.6f, 0.6f);

    // internal smoothing state
    private Vector3 velocity = Vector3.zero;

    private void Reset()
    {
        // Auto-assign the collider on this GameObject if present
        if (boundsCollider == null)
            boundsCollider = GetComponent<BoxCollider2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnValidate()
    {
        // Keep references up to date in editor
        if (boundsCollider == null)
            boundsCollider = GetComponent<BoxCollider2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        smoothTime = Mathf.Max(0f, smoothTime);
        minOrthoSize = Mathf.Max(0.01f, minOrthoSize);
        maxOrthoSize = Mathf.Max(minOrthoSize, maxOrthoSize);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            // In editor mode, still allow manual testing
            if (targetCamera == null) targetCamera = Camera.main;
            if (boundsCollider == null) boundsCollider = GetComponent<BoxCollider2D>();
        }

        if (targetCamera == null || boundsCollider == null) return;

        // Compute camera view extents at the plane of the bounds (Z)
        float planeZ = boundsCollider.transform.position.z; // use collider's transform Z as reference
        Vector3 camPos = targetCamera.transform.position;

        // For orthographic camera, extents are simple
        if (targetCamera.orthographic)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            // Optionally auto-fit orthographic size to bounds if requested
            if (allowZoomToFit)
            {
                FitOrthographicSizeToBounds(ref halfHeight, ref halfWidth);
            }

            // Recompute halfWidth if Fit changed halfHeight
            halfWidth = halfHeight * targetCamera.aspect;

            Vector2 minCameraCenter = new Vector2(
                boundsCollider.bounds.min.x + padding.x + halfWidth,
                boundsCollider.bounds.min.y + padding.y + halfHeight
            );

            Vector2 maxCameraCenter = new Vector2(
                boundsCollider.bounds.max.x - padding.x - halfWidth,
                boundsCollider.bounds.max.y - padding.y - halfHeight
            );

            Vector3 targetPos = camPos;

            // If bounds are smaller than the view in X or Y, handle according to centerWhenSmallerThanBounds
            bool smallerX = minCameraCenter.x > maxCameraCenter.x;
            bool smallerY = minCameraCenter.y > maxCameraCenter.y;

            if (smallerX)
                targetPos.x = boundsCollider.bounds.center.x; // center horizontally
            else
                targetPos.x = Mathf.Clamp(camPos.x, minCameraCenter.x, maxCameraCenter.x);

            if (smallerY)
                targetPos.y = boundsCollider.bounds.center.y; // center vertically
            else
                targetPos.y = Mathf.Clamp(camPos.y, minCameraCenter.y, maxCameraCenter.y);

            // Preserve camera Z
            targetPos.z = camPos.z;

            // Smooth or snap
            if (useSmoothing && Application.isPlaying)
            {
                targetCamera.transform.position = Vector3.SmoothDamp(camPos, targetPos, ref velocity, smoothTime);
            }
            else
            {
                targetCamera.transform.position = targetPos;
            }
        }
        else
        {
            // Perspective camera: compute world-space corners at the plane distance and derive extents
            float camToPlane = Mathf.Abs(targetCamera.transform.position.z - planeZ);
            if (camToPlane <= 0f) camToPlane = 0.0001f;

            Vector3 bl = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, camToPlane));
            Vector3 tr = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, camToPlane));

            float halfWidth = Mathf.Abs(tr.x - bl.x) * 0.5f;
            float halfHeight = Mathf.Abs(tr.y - bl.y) * 0.5f;

            Vector2 minCameraCenter = new Vector2(
                boundsCollider.bounds.min.x + padding.x + halfWidth,
                boundsCollider.bounds.min.y + padding.y + halfHeight
            );

            Vector2 maxCameraCenter = new Vector2(
                boundsCollider.bounds.max.x - padding.x - halfWidth,
                boundsCollider.bounds.max.y - padding.y - halfHeight
            );

            Vector3 targetPos = camPos;

            bool smallerX = minCameraCenter.x > maxCameraCenter.x;
            bool smallerY = minCameraCenter.y > maxCameraCenter.y;

            if (smallerX)
                targetPos.x = boundsCollider.bounds.center.x;
            else
                targetPos.x = Mathf.Clamp(camPos.x, minCameraCenter.x, maxCameraCenter.x);

            if (smallerY)
                targetPos.y = boundsCollider.bounds.center.y;
            else
                targetPos.y = Mathf.Clamp(camPos.y, minCameraCenter.y, maxCameraCenter.y);

            targetPos.z = camPos.z;

            if (useSmoothing && Application.isPlaying)
            {
                targetCamera.transform.position = Vector3.SmoothDamp(camPos, targetPos, ref velocity, smoothTime);
            }
            else
            {
                targetCamera.transform.position = targetPos;
            }
        }
    }

    /// <summary>
    /// If allowZoomToFit is enabled, adjust orthographicSize so the camera can fit the bounds.
    /// This modifies the camera.orthographicSize (and thus halfHeight) passed by reference.
    /// </summary>
    private void FitOrthographicSizeToBounds(ref float halfHeight, ref float halfWidth)
    {
        if (!targetCamera.orthographic) return;

        // bounds size
        float boundsWidth = boundsCollider.bounds.size.x - padding.x * 2f;
        float boundsHeight = boundsCollider.bounds.size.y - padding.y * 2f;

        // If bounds are negative due to large padding, treat as zero
        boundsWidth = Mathf.Max(0.0001f, boundsWidth);
        boundsHeight = Mathf.Max(0.0001f, boundsHeight);

        // Required half sizes to fit bounds
        float requiredHalfHeight = boundsHeight * 0.5f;
        float requiredHalfWidth = (boundsWidth * 0.5f) / targetCamera.aspect;

        float requiredHalf = Mathf.Max(requiredHalfHeight, requiredHalfWidth);

        // Clamp to min/max
        requiredHalf = Mathf.Clamp(requiredHalf, minOrthoSize, maxOrthoSize);

        // Apply immediately (no smoothing for zoom)
        targetCamera.orthographicSize = requiredHalf;
        halfHeight = requiredHalf;
        halfWidth = halfHeight * targetCamera.aspect;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        DrawDebugGizmos();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        if (boundsCollider == null) return;

        Gizmos.color = gizmoColor;
        // Draw the bounds box
        Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);

        // Draw padding inset rectangle
        Vector3 paddedSize = new Vector3(
            Mathf.Max(0f, boundsCollider.bounds.size.x - padding.x * 2f),
            Mathf.Max(0f, boundsCollider.bounds.size.y - padding.y * 2f),
            boundsCollider.bounds.size.z
        );
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        Gizmos.DrawWireCube(boundsCollider.bounds.center, paddedSize);
    }
}
