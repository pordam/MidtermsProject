using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private float duration;
    private float magnitude;

    private float shaketime = 0f;
    private Vector3 lastOffset = Vector3.zero;

    private void LateUpdate()
    {
        // Remove last frame's offset so we don't accumulate or drift
        transform.localPosition -= lastOffset;
        lastOffset = Vector3.zero;

        if (shaketime > 0f)
        {
            Vector2 shakeOffset = Random.insideUnitCircle * magnitude;
            Vector3 offset3 = new Vector3(shakeOffset.x, shakeOffset.y, 0f);

            // Apply new offset
            transform.localPosition += offset3;
            lastOffset = offset3;

            shaketime -= Time.deltaTime;
        }
        // else nothing to do — camera follow will set the correct position next frame
    }

    public void triggershake(float durationOverride = -1f, float magnitudeOverride = -1f)
    {
        shaketime = durationOverride > 0f ? durationOverride : duration;
        magnitude = magnitudeOverride > 0f ? magnitudeOverride : magnitude;
    }
}
