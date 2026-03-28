using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ParryFlashFX : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float flashDuration = 0.2f;

    private ColorAdjustments colorAdjust;

    void Awake()
    {
        if (volume != null)
        {
            if (volume.profile.TryGet(out colorAdjust))
            {
                Debug.Log("[ParryFlashFX] Found ColorAdjustments override.");
            }
            else
            {
                Debug.LogWarning("[ParryFlashFX] No ColorAdjustments override found in Volume profile!");
            }
        }
        else
        {
            Debug.LogWarning("[ParryFlashFX] Volume reference not assigned!");
        }
    }

    public void TriggerFlash()
    {
        if (colorAdjust != null)
        {
            Debug.Log("[ParryFlashFX] TriggerFlash called, starting flash routine.");
            StartCoroutine(FlashRoutine());
        }
        else
        {
            Debug.LogWarning("[ParryFlashFX] TriggerFlash called but ColorAdjustments is null!");
        }
    }

    private IEnumerator FlashRoutine()
    {
        Debug.Log("[ParryFlashFX] FlashRoutine started.");
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / flashDuration;

            // Debug current progress
            Debug.Log($"[ParryFlashFX] Flash progress: {p:F2}");

            // Use postExposure for a proper flash
            colorAdjust.postExposure.value = Mathf.Lerp(2f, 0f, p);

            yield return null;
        }

        // Reset
        colorAdjust.postExposure.value = 0f;
        Debug.Log("[ParryFlashFX] FlashRoutine finished, exposure reset.");
    }
}
