using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Money")]
    [SerializeField] private int startingMoney = 0;

    [Header("UI (TextMeshPro)")]
    [Tooltip("The TextMeshProUGUI component on the visual child (MoneyVisual)")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [Tooltip("RectTransform of the visual child (MoneyVisual) that will be animated")]
    [SerializeField] private RectTransform moneyVisualRect;
    [Tooltip("RectTransform of the parent container that holds layout components (MoneyContainer)")]
    [SerializeField] private RectTransform moneyContainerRect;
    [SerializeField] private Image coinImage;
    [SerializeField] private string moneyPrefix = "";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private int money;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null, worldPositionStays: true);

        DontDestroyOnLoad(gameObject);

        // Initialize money from starting value by default.
        money = startingMoney;
        UpdateUIImmediate();

        if (debugLogs) Debug.Log($"CurrencyManager Awake: instance={name} money={money}");
    }

    private void Start()
    {
        // No PlayerPrefs logic here. External save system should call ApplySavedValue if needed.
    }

    public int GetMoney() => money;

    /// <summary>
    /// Explicit setter for external save systems.
    /// Use this to apply a loaded value from your custom save system.
    /// </summary>
    public void ApplySavedValue(int savedMoney, bool playVisual = false)
    {
        money = savedMoney;
        UpdateUIImmediate();
        if (playVisual) PlayPopTween();
    }

    /// <summary>
    /// Set money directly. Optionally play the visual pop.
    /// Useful if your external save system wants to set the value and trigger UI.
    /// </summary>
    public void SetMoney(int amount, bool playVisual = true)
    {
        money = amount;
        UpdateUIImmediate();
        if (playVisual) PlayPopTween();
    }

    /// <summary>
    /// Add money. playVisual controls whether the UI visual pop runs.
    /// Default: playVisual = true.
    /// </summary>
    public void Add(int amount, bool playVisual = true)
    {
        if (amount == 0) return;
        money += amount;
        UpdateUIImmediate();

        if (debugLogs) Debug.Log($"CurrencyManager.Add amount={amount} playVisual={playVisual} time={Time.unscaledTime:F2}");

        if (playVisual)
            PlayPopTween();
    }

    /// <summary>
    /// Convenience: add money silently then explicitly pop visually.
    /// </summary>
    public void AddAndPop(int amount)
    {
        Add(amount, playVisual: false); // update money silently (no visual)
        PlayPopTween();                 // then explicitly trigger the visual pop
    }

    public bool TrySpend(int amount, bool playVisual = true)
    {
        if (amount <= 0) return false;
        if (money < amount) return false;
        money -= amount;
        UpdateUIImmediate();
        if (playVisual) PlayPopTween();
        return true;
    }

    private void UpdateUIImmediate()
    {
        if (moneyText != null)
            moneyText.text = moneyPrefix + money.ToString("N0");
    }

    /// <summary>
    /// Explicitly trigger the UI pop (visual only).
    /// </summary>
    public void TriggerPop()
    {
        if (debugLogs) Debug.Log("CurrencyManager.TriggerPop called");
        PlayPopTween();
    }

    [ContextMenu("Test Pop")]
    public void TestPop()
    {
        if (debugLogs) Debug.Log("CurrencyManager.TestPop invoked");
        PlayPopTween();
    }

    // Visual-only pop tween (no audio)
    private void PlayPopTween()
    {
        if (moneyText != null)
        {
            moneyText.text = moneyPrefix + money.ToString("N0");
            moneyText.ForceMeshUpdate();
        }

        if (debugLogs)
        {
            Debug.Log($"PlayPopTween called. moneyVisualRect={(moneyVisualRect != null)} moneyText={(moneyText != null)} time={Time.unscaledTime:F2}");
        }

        if (moneyVisualRect != null)
        {
            moneyVisualRect.DOKill();

            Vector3 original = moneyVisualRect.localScale;
            moneyVisualRect.localScale = original;

            Vector2 parentAnchored = moneyContainerRect != null ? moneyContainerRect.anchoredPosition : Vector2.zero;

            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                if (moneyVisualRect != null) moneyVisualRect.localScale = original;
            });

            seq.Append(moneyVisualRect.DOScale(original * 1.25f, 0.09f).SetEase(Ease.OutSine));
            seq.Append(moneyVisualRect.DOScale(original, 0.09f).SetEase(Ease.InSine));

            seq.OnUpdate(() =>
            {
                if (moneyContainerRect != null) moneyContainerRect.anchoredPosition = parentAnchored;
            });

            seq.OnComplete(() =>
            {
                if (moneyVisualRect != null) moneyVisualRect.localScale = original;
                if (moneyContainerRect != null) moneyContainerRect.anchoredPosition = parentAnchored;
            });
            seq.OnKill(() =>
            {
                if (moneyVisualRect != null) moneyVisualRect.localScale = original;
                if (moneyContainerRect != null) moneyContainerRect.anchoredPosition = parentAnchored;
            });

            seq.SetUpdate(true);
            return;
        }

        if (moneyText != null)
        {
            RectTransform rt = moneyText.rectTransform;
            rt.DOKill();
            Vector3 original = rt.localScale;
            Vector2 anchored = rt.anchoredPosition;

            Sequence fallback = DOTween.Sequence();
            fallback.AppendCallback(() => rt.localScale = original);
            fallback.Append(rt.DOScale(1.25f, 0.09f).SetEase(Ease.OutSine));
            fallback.Append(rt.DOScale(1f, 0.09f).SetEase(Ease.InSine));
            fallback.OnUpdate(() => rt.anchoredPosition = anchored);
            fallback.OnComplete(() => rt.localScale = original);
            fallback.OnKill(() => rt.localScale = original);
            fallback.SetUpdate(true);
        }
    }

    [ContextMenu("Reset Money")]
    public void ResetMoney()
    {
        money = startingMoney;
        UpdateUIImmediate();
    }
}
