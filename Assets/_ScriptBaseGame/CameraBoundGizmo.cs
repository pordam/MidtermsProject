using UnityEngine;

[ExecuteAlways]
public class CameraBoundsGizmo : MonoBehaviour
{
    public Color gizmoColor = new Color(0.1f, 0.8f, 0.6f, 1f);
    public bool showAlways = false;            // true = OnDrawGizmos, false = OnDrawGizmosSelected
    public Camera targetCamera;                // optional, falls back to Camera.main
    public Vector3 offset = Vector3.zero;      // offset from this transform for the drawn rectangle
    public bool useBoxCollider2D = true;       // if true and a BoxCollider2D exists, draw that instead

    private void OnDrawGizmos()
    {
        if (!showAlways) return;
        DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (showAlways) return;
        DrawGizmo();
    }

    private void DrawGizmo()
    {
        Gizmos.color = gizmoColor;

        // If requested and a BoxCollider2D exists on this object, draw its bounds
        if (useBoxCollider2D)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                // BoxCollider2D bounds are in world space already
                Gizmos.DrawWireCube(box.bounds.center + (Vector3)box.offset, box.bounds.size);
                return;
            }
        }

        // Otherwise draw camera frustum rectangle at this object's Z plane
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        // Compute rectangle size at the Z of this transform
        float planeZ = transform.position.z + offset.z;
        float camToPlane = Mathf.Abs(cam.transform.position.z - planeZ);

        float width, height;
        if (cam.orthographic)
        {
            height = cam.orthographicSize * 2f;
            width = height * cam.aspect;
        }
        else
        {
            // perspective: compute frustum size at distance camToPlane
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            height = 2f * camToPlane * Mathf.Tan(fovRad * 0.5f);
            width = height * cam.aspect;
        }

        Vector3 center = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, planeZ);

        // Draw rectangle as wire lines (Z is planeZ)
        Vector3 half = new Vector3(width * 0.5f, height * 0.5f, 0f);
        Vector3 bl = center + new Vector3(-half.x, -half.y, 0f);
        Vector3 br = center + new Vector3(half.x, -half.y, 0f);
        Vector3 tl = center + new Vector3(-half.x, half.y, 0f);
        Vector3 tr = center + new Vector3(half.x, half.y, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        // optional cross at center for quick visual
        Gizmos.DrawLine(center + Vector3.left * 0.1f, center + Vector3.right * 0.1f);
        Gizmos.DrawLine(center + Vector3.up * 0.1f, center + Vector3.down * 0.1f);
    }
}
