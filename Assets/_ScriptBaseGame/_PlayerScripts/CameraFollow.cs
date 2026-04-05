using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.125f;

    [Header("Camera Bounds")]
    public bool useBounds = true;                 // toggle bounds on/off
    public bool useColliderBounds = false;       // if true, use boundsCollider to set limits
    public BoxCollider2D boundsCollider;         // optional: assign a BoxCollider2D in scene

    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

    [Header("Extras")]
    public Vector2 boundsPadding = Vector2.zero; // shrink usable area inside bounds
    public bool centerWhenSmallerThanBounds = true;
    public bool allowZoomToFit = false;          // orthographic only
    public float minOrthoSize = 1f;
    public float maxOrthoSize = 100f;

    [Header("Debug Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.1f, 0.8f, 0.6f, 0.6f);

    public Transform target; // assign in inspector or via SetTarget()

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    // --- Debug additions ---
    [Header("Runtime Debugging")]
    [Tooltip("Enable runtime debug logs and debug draw lines.")]
    public bool enableDebug = false;

    [Tooltip("Minimum seconds between debug log entries to avoid spamming the Console.")]
    public float debugLogInterval = 0.5f;

    [Tooltip("Color for runtime Debug.DrawLine (bounds/padded inset).")]
    public Color runtimeDebugColor = Color.cyan;

    private float debugTimer = 0f;

    private void Reset()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (boundsCollider == null) boundsCollider = GetComponent<BoxCollider2D>();
    }

    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (boundsCollider == null) boundsCollider = GetComponent<BoxCollider2D>();

        smoothTime = Mathf.Max(0f, smoothTime);
        minOrthoSize = Mathf.Max(0.01f, minOrthoSize);
        maxOrthoSize = Mathf.Max(minOrthoSize, maxOrthoSize);
        debugLogInterval = Mathf.Max(0.01f, debugLogInterval);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return; // nothing to do without a camera

        // Desired camera center (apply offset.x/y only to follow target; keep camera Z separate)
        Vector3 desiredCenter = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

        // Smooth toward desired center (preserve Z)
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredCenter, ref velocity, smoothTime);
        smoothed.z = transform.position.z; // ensure Z remains unchanged by smoothing

        // Reset debug timer
        debugTimer -= Time.deltaTime;

        if (useBounds)
        {
            if (useColliderBounds && boundsCollider != null)
            {
                ApplyColliderBounds(ref smoothed, cam, boundsCollider);
                if (enableDebug) DebugDrawColliderBounds(boundsCollider);
            }
            else
            {
                ApplyNumericBounds(ref smoothed, cam);
                if (enableDebug) DebugDrawNumericBounds();
            }
        }

        // Debug logging (throttled)
        if (enableDebug && debugTimer <= 0f)
        {
            LogDebugState(desiredCenter, smoothed, cam);
            debugTimer = debugLogInterval;
        }

        transform.position = smoothed;
    }

    private void ApplyColliderBounds(ref Vector3 smoothed, Camera cam, BoxCollider2D b)
    {
        Bounds bounds = b.bounds;

        if (cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            if (allowZoomToFit)
            {
                FitOrthographicSizeToBounds(cam, b);
                halfHeight = cam.orthographicSize;
                halfWidth = halfHeight * cam.aspect;
            }

            float minClampX = bounds.min.x + boundsPadding.x + halfWidth;
            float maxClampX = bounds.max.x - boundsPadding.x - halfWidth;
            float minClampY = bounds.min.y + boundsPadding.y + halfHeight;
            float maxClampY = bounds.max.y - boundsPadding.y - halfHeight;

            bool smallerX = minClampX > maxClampX;
            bool smallerY = minClampY > maxClampY;

            if (smallerX)
                smoothed.x = centerWhenSmallerThanBounds ? bounds.center.x : Mathf.Clamp(smoothed.x, minClampX, maxClampX);
            else
                smoothed.x = Mathf.Clamp(smoothed.x, minClampX, maxClampX);

            if (smallerY)
                smoothed.y = centerWhenSmallerThanBounds ? bounds.center.y : Mathf.Clamp(smoothed.y, minClampY, maxClampY);
            else
                smoothed.y = Mathf.Clamp(smoothed.y, minClampY, maxClampY);
        }
        else
        {
            // Perspective fallback: compute approximate half extents at bounds Z
            float planeZ = bounds.center.z;
            float camToPlane = Mathf.Abs(cam.transform.position.z - planeZ);
            if (camToPlane <= 0f) camToPlane = 0.0001f;

            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, camToPlane));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, camToPlane));
            float halfWidth = Mathf.Abs(tr.x - bl.x) * 0.5f;
            float halfHeight = Mathf.Abs(tr.y - bl.y) * 0.5f;

            float minClampX = bounds.min.x + boundsPadding.x + halfWidth;
            float maxClampX = bounds.max.x - boundsPadding.x - halfWidth;
            float minClampY = bounds.min.y + boundsPadding.y + halfHeight;
            float maxClampY = bounds.max.y - boundsPadding.y - halfHeight;

            bool smallerX = minClampX > maxClampX;
            bool smallerY = minClampY > maxClampY;

            if (smallerX)
                smoothed.x = centerWhenSmallerThanBounds ? bounds.center.x : Mathf.Clamp(smoothed.x, minClampX, maxClampX);
            else
                smoothed.x = Mathf.Clamp(smoothed.x, minClampX, maxClampX);

            if (smallerY)
                smoothed.y = centerWhenSmallerThanBounds ? bounds.center.y : Mathf.Clamp(smoothed.y, minClampY, maxClampY);
            else
                smoothed.y = Mathf.Clamp(smoothed.y, minClampY, maxClampY);
        }
    }

    private void ApplyNumericBounds(ref Vector3 smoothed, Camera cam)
    {
        if (cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            if (allowZoomToFit)
            {
                // Fit to numeric bounds
                float boundsWidth = Mathf.Max(0.0001f, maxX - minX - boundsPadding.x * 2f);
                float boundsHeight = Mathf.Max(0.0001f, maxY - minY - boundsPadding.y * 2f);
                float requiredHalf = Mathf.Max(boundsHeight * 0.5f, (boundsWidth * 0.5f) / cam.aspect);
                requiredHalf = Mathf.Clamp(requiredHalf, minOrthoSize, maxOrthoSize);
                cam.orthographicSize = requiredHalf;
                halfHeight = requiredHalf;
                halfWidth = halfHeight * cam.aspect;
            }

            float minClampX = minX + boundsPadding.x + halfWidth;
            float maxClampX = maxX - boundsPadding.x - halfWidth;
            float minClampY = minY + boundsPadding.y + halfHeight;
            float maxClampY = maxY - boundsPadding.y - halfHeight;

            bool smallerX = minClampX > maxClampX;
            bool smallerY = minClampY > maxClampY;

            if (smallerX)
                smoothed.x = centerWhenSmallerThanBounds ? (minX + maxX) * 0.5f : Mathf.Clamp(smoothed.x, minClampX, maxClampX);
            else
                smoothed.x = Mathf.Clamp(smoothed.x, minClampX, maxClampX);

            if (smallerY)
                smoothed.y = centerWhenSmallerThanBounds ? (minY + maxY) * 0.5f : Mathf.Clamp(smoothed.y, minClampY, maxClampY);
            else
                smoothed.y = Mathf.Clamp(smoothed.y, minClampY, maxClampY);
        }
        else
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minX + boundsPadding.x, maxX - boundsPadding.x);
            smoothed.y = Mathf.Clamp(smoothed.y, minY + boundsPadding.y, maxY - boundsPadding.y);
        }
    }

    private void FitOrthographicSizeToBounds(Camera cam, BoxCollider2D b)
    {
        if (cam == null || b == null || !cam.orthographic) return;

        float boundsWidth = b.bounds.size.x - boundsPadding.x * 2f;
        float boundsHeight = b.bounds.size.y - boundsPadding.y * 2f;

        boundsWidth = Mathf.Max(0.0001f, boundsWidth);
        boundsHeight = Mathf.Max(0.0001f, boundsHeight);

        float requiredHalf = Mathf.Max(boundsHeight * 0.5f, (boundsWidth * 0.5f) / cam.aspect);
        requiredHalf = Mathf.Clamp(requiredHalf, minOrthoSize, maxOrthoSize);

        cam.orthographicSize = requiredHalf;
    }

    private void FitOrthographicSizeToBounds(Camera cam, float minX, float maxX, float minY, float maxY)
    {
        if (cam == null || !cam.orthographic) return;

        float boundsWidth = Mathf.Max(0.0001f, maxX - minX - boundsPadding.x * 2f);
        float boundsHeight = Mathf.Max(0.0001f, maxY - minY - boundsPadding.y * 2f);

        float requiredHalf = Mathf.Max(boundsHeight * 0.5f, (boundsWidth * 0.5f) / cam.aspect);
        requiredHalf = Mathf.Clamp(requiredHalf, minOrthoSize, maxOrthoSize);

        cam.orthographicSize = requiredHalf;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        DrawGizmos();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        Gizmos.color = gizmoColor;

        if (boundsCollider != null)
        {
            Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);

            Vector3 paddedSize = new Vector3(
                Mathf.Max(0f, boundsCollider.bounds.size.x - boundsPadding.x * 2f),
                Mathf.Max(0f, boundsCollider.bounds.size.y - boundsPadding.y * 2f),
                boundsCollider.bounds.size.z
            );
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
            Gizmos.DrawWireCube(boundsCollider.bounds.center, paddedSize);
        }
        else
        {
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            Vector3 size = new Vector3(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0.1f);
            Gizmos.DrawWireCube(center, size);

            Vector3 paddedSize = new Vector3(
                Mathf.Max(0f, size.x - boundsPadding.x * 2f),
                Mathf.Max(0f, size.y - boundsPadding.y * 2f),
                0.1f
            );
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
            Gizmos.DrawWireCube(center, paddedSize);
        }
    }

    // --- Debug helpers ---

    private void LogDebugState(Vector3 desiredCenter, Vector3 smoothed, Camera cam)
    {
        string camName = cam != null ? cam.name : "null";
        string targetName = target != null ? target.name : "null";

        // Compute frustum extents for logging
        float halfHeight = cam.orthographic ? cam.orthographicSize : 0f;
        float halfWidth = cam.orthographic ? halfHeight * cam.aspect : 0f;

        if (!cam.orthographic)
        {
            // approximate extents at camera Z plane (use target Z)
            float planeZ = transform.position.z;
            float camToPlane = Mathf.Abs(cam.transform.position.z - planeZ);
            if (camToPlane <= 0f) camToPlane = 0.0001f;
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, camToPlane));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, camToPlane));
            halfWidth = Mathf.Abs(tr.x - bl.x) * 0.5f;
            halfHeight = Mathf.Abs(tr.y - bl.y) * 0.5f;
        }

        string boundsType = (useColliderBounds && boundsCollider != null) ? "Collider" : "Numeric";
        string boundsInfo = boundsType == "Collider" ?
            $"BoundsCenter={boundsCollider.bounds.center}, BoundsSize={boundsCollider.bounds.size}" :
            $"minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}";

        Debug.Log($"[CameraFollow Debug] cam={camName} target={targetName}\n" +
                  $"DesiredCenter={desiredCenter} Smoothed={smoothed}\n" +
                  $"HalfWidth={halfWidth:F2} HalfHeight={halfHeight:F2}\n" +
                  $"{boundsInfo}\n" +
                  $"Padding={boundsPadding} centerWhenSmaller={centerWhenSmallerThanBounds}\n");
    }

    private void DebugDrawColliderBounds(BoxCollider2D b)
    {
        if (!Application.isPlaying) return; // only draw runtime debug lines while playing
        Bounds bounds = b.bounds;
        Vector3 bl = new Vector3(bounds.min.x, bounds.min.y, transform.position.z);
        Vector3 br = new Vector3(bounds.max.x, bounds.min.y, transform.position.z);
        Vector3 tl = new Vector3(bounds.min.x, bounds.max.y, transform.position.z);
        Vector3 tr = new Vector3(bounds.max.x, bounds.max.y, transform.position.z);

        Debug.DrawLine(bl, br, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(br, tr, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(tr, tl, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(tl, bl, runtimeDebugColor, debugLogInterval);

        // padded inset
        Vector3 paddedBL = new Vector3(bounds.min.x + boundsPadding.x, bounds.min.y + boundsPadding.y, transform.position.z);
        Vector3 paddedBR = new Vector3(bounds.max.x - boundsPadding.x, bounds.min.y + boundsPadding.y, transform.position.z);
        Vector3 paddedTL = new Vector3(bounds.min.x + boundsPadding.x, bounds.max.y - boundsPadding.y, transform.position.z);
        Vector3 paddedTR = new Vector3(bounds.max.x - boundsPadding.x, bounds.max.y - boundsPadding.y, transform.position.z);

        Debug.DrawLine(paddedBL, paddedBR, runtimeDebugColor * 0.7f, debugLogInterval);
        Debug.DrawLine(paddedBR, paddedTR, runtimeDebugColor * 0.7f, debugLogInterval);
        Debug.DrawLine(paddedTR, paddedTL, runtimeDebugColor * 0.7f, debugLogInterval);
        Debug.DrawLine(paddedTL, paddedBL, runtimeDebugColor * 0.7f, debugLogInterval);
    }

    private void DebugDrawNumericBounds()
    {
        if (!Application.isPlaying) return;
        Vector3 bl = new Vector3(minX + boundsPadding.x, minY + boundsPadding.y, transform.position.z);
        Vector3 br = new Vector3(maxX - boundsPadding.x, minY + boundsPadding.y, transform.position.z);
        Vector3 tl = new Vector3(minX + boundsPadding.x, maxY - boundsPadding.y, transform.position.z);
        Vector3 tr = new Vector3(maxX - boundsPadding.x, maxY - boundsPadding.y, transform.position.z);

        Debug.DrawLine(bl, br, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(br, tr, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(tr, tl, runtimeDebugColor, debugLogInterval);
        Debug.DrawLine(tl, bl, runtimeDebugColor, debugLogInterval);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
