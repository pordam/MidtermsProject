using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private float duration;
    private float magnitude;

    private Vector3 initialpos;
    private float shaketime = 0f;

    private void Awake()
    {
        initialpos = transform.localPosition;
    }

    private void Update()
    {
        if (shaketime > 0)
        {
            Vector2 shakeOffset = Random.insideUnitCircle * magnitude;
            transform.localPosition = initialpos + new Vector3(shakeOffset.x, shakeOffset.y, 0f);

            shaketime -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = initialpos;
        }
    }

    public void triggershake(float durationOverride = -1f, float magnitudeOverride = -1f)
    {
        shaketime = durationOverride > 0 ? durationOverride : duration;
        magnitude = magnitudeOverride > 0 ? magnitudeOverride : magnitude;
    }
}
