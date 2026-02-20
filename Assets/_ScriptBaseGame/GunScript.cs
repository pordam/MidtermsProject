using UnityEngine;
using DG.Tweening;

public class GunScript : MonoBehaviour
{
    public Camera cam;
    Vector2 mousePos;

    [Header("DOTween Squeeze (X axis)")]
    public float squeezeX = 0.92f;        // multiply original X by this
    public float squeezeDuration = 0.06f; // time to compress
    public float releaseDuration = 0.12f; // time to return
    public Ease squeezeEase = Ease.OutQuad;
    public Ease releaseEase = Ease.OutQuad;

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
    }

    void FixedUpdate()
    {
        mousePos = cam.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        Vector2 lookDir = mousePos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Call this to play the squeeze using DOTween
    public void TriggerSqueezeDOT()
    {
        // Kill any existing scale tweens on this transform (preserve complete = true)
        transform.DOKill(true);

        // Ensure starting scale is original (prevents drift)
        transform.localScale = originalScale;

        // Compress X then return
        float targetX = originalScale.x * squeezeX;
        transform.DOScaleX(targetX, squeezeDuration).SetEase(squeezeEase)
            .OnComplete(() =>
            {
                transform.DOScaleX(originalScale.x, releaseDuration).SetEase(releaseEase);
            });
    }
}
