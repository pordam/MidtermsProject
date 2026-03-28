using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("UI References")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider frontBar; // instant health
    [SerializeField] private Slider backBar;  // delayed bar

    [Header("Bar Behavior")]
    [SerializeField] private float delayBeforeShrink = 1f;
    [SerializeField] private float shrinkSpeed = 0.5f;
    [SerializeField] private float hideDelay = 3f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        frontBar.value = 1f;
        backBar.value = 1f;
        healthCanvas.enabled = false;
        Debug.Log($"[EnemyHealthUI] Awake: health={currentHealth}, canvas hidden");
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        float normalized = (float)currentHealth / maxHealth;

        Debug.Log($"[EnemyHealthUI] TakeDamage: amount={amount}, newHealth={currentHealth}, normalized={normalized}");

        healthCanvas.enabled = true;
        Debug.Log("[EnemyHealthUI] Canvas enabled");

        frontBar.value = normalized;
        Debug.Log($"[EnemyHealthUI] Front bar set to {frontBar.value}");

        StopAllCoroutines();
        StartCoroutine(DelayedShrink(normalized));

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideCanvasAfterDelay());
    }

    private IEnumerator DelayedShrink(float targetValue)
    {
        Debug.Log($"[EnemyHealthUI] DelayedShrink started, waiting {delayBeforeShrink}s");
        yield return new WaitForSeconds(delayBeforeShrink);

        Debug.Log("[EnemyHealthUI] DelayedShrink running");
        while (backBar.value > targetValue)
        {
            backBar.value = Mathf.MoveTowards(backBar.value, targetValue, shrinkSpeed * Time.deltaTime);
            Debug.Log($"[EnemyHealthUI] Back bar value={backBar.value}, target={targetValue}");
            yield return null;
        }
        Debug.Log("[EnemyHealthUI] DelayedShrink finished");
    }

    private IEnumerator HideCanvasAfterDelay()
    {
        Debug.Log($"[EnemyHealthUI] HideCanvasAfterDelay started, waiting {hideDelay}s");
        yield return new WaitForSeconds(hideDelay);

        if (currentHealth > 0)
        {
            healthCanvas.enabled = false;
            Debug.Log("[EnemyHealthUI] Canvas hidden after delay");
        }
        else
        {
            Debug.Log("[EnemyHealthUI] Enemy dead, canvas stays hidden");
        }
    }
    private void LateUpdate()
    {
        // Always keep upright
        transform.rotation = Quaternion.identity;
    }
    public void UpdateHealth(int current, int max)
    {
        float normalized = (float)current / max;

        healthCanvas.enabled = true;
        frontBar.value = normalized;

        StopAllCoroutines();
        StartCoroutine(DelayedShrink(normalized));

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideCanvasAfterDelay());
    }

}
