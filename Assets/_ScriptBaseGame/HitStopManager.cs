using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private int activeRequests = 0;
    private float savedTimeScale = 1f;
    private float savedFixedDelta = 0.02f;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    /// <summary>
    /// Request a hitstop for duration (unscaled seconds). Multiple requests stack safely.
    /// </summary>
    public void StartHitStop(float duration, bool fullFreeze = true)
    {
        Debug.Log($"HitStop requested: duration={duration}, fullFreeze={fullFreeze}");
        StartCoroutine(HitStopCoroutine(duration, fullFreeze));
    }

    private IEnumerator HitStopCoroutine(float duration, bool fullFreeze)
    {
        // First request: save global state and apply freeze
        if (activeRequests == 0)
        {
            savedTimeScale = Time.timeScale;
            savedFixedDelta = Time.fixedDeltaTime;

            if (fullFreeze)
            {
                Time.timeScale = 0f;
                Time.fixedDeltaTime = 0f;
            }
            else
            {
                Time.timeScale = 0.01f;
                Time.fixedDeltaTime = savedFixedDelta * Time.timeScale;
            }
        }

        activeRequests++;

        // Wait in real time so this is unaffected by timescale
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // This request finished
        activeRequests = Mathf.Max(0, activeRequests - 1);

        // Only restore when no active requests remain
        if (activeRequests == 0)
        {
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDelta;
        }
    }
}
