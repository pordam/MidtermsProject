using UnityEngine;

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


    public Transform target; // assign in inspector or via SetTarget()

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // desired camera position (preserve current Z)
        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // apply smoothing
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        if (useBounds)
        {
            Camera cam = Camera.main;
            if (useColliderBounds && boundsCollider != null)
            {
                Bounds b = boundsCollider.bounds;
                if (cam != null && cam.orthographic)
                {
                    float camHalfHeight = cam.orthographicSize;
                    float camHalfWidth = camHalfHeight * cam.aspect;

                    float minClampX = b.min.x + camHalfWidth;
                    float maxClampX = b.max.x - camHalfWidth;
                    float minClampY = b.min.y + camHalfHeight;
                    float maxClampY = b.max.y - camHalfHeight;

                    smoothed.x = Mathf.Clamp(smoothed.x, minClampX, maxClampX);
                    smoothed.y = Mathf.Clamp(smoothed.y, minClampY, maxClampY);
                }
                else
                {
                    smoothed.x = Mathf.Clamp(smoothed.x, b.min.x, b.max.x);
                    smoothed.y = Mathf.Clamp(smoothed.y, b.min.y, b.max.y);
                }
            }
            else
            {
                if (Camera.main != null && Camera.main.orthographic)
                {
                    float camHalfHeight = Camera.main.orthographicSize;
                    float camHalfWidth = camHalfHeight * Camera.main.aspect;

                    float minClampX = minX + camHalfWidth;
                    float maxClampX = maxX - camHalfWidth;
                    float minClampY = minY + camHalfHeight;
                    float maxClampY = maxY - camHalfHeight;

                    smoothed.x = Mathf.Clamp(smoothed.x, minClampX, maxClampX);
                    smoothed.y = Mathf.Clamp(smoothed.y, minClampY, maxClampY);
                }
                else
                {
                    smoothed.x = Mathf.Clamp(smoothed.x, minX, maxX);
                    smoothed.y = Mathf.Clamp(smoothed.y, minY, maxY);
                }
            }
        }

        transform.position = smoothed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
