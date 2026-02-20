using System.Collections;
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
    private Vector3 originalScale; // cached scale

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null, worldPositionStays: true);

        DontDestroyOnLoad(gameObject);

        // Cache the original scale from whichever rect we animate
        if (moneyVisualRect != null)
            originalScale = moneyVisualRect.localScale;
        else if (moneyText != null)
            originalScale = moneyText.rectTransform.localScale;
        else
            originalScale = Vector3.one;

        // Initialize money
        money = startingMoney;
        UpdateUIImmediate();

        if (debugLogs) Debug.Log($"CurrencyManager Awake: instance={name} money={money}");
    }

    public int GetMoney() => money;

    public void ApplySavedValue(int savedMoney, bool playVisual = false)
    {
        money = savedMoney;
        UpdateUIImmediate();
        if (playVisual) PlayPopTween();
    }

    public void SetMoney(int amount, bool playVisual = true)
    {
        money = amount;
        UpdateUIImmediate();
        if (playVisual) PlayPopTween();
    }

    public void Add(int amount, bool playVisual = true)
    {
        if (amount == 0) return;
        money += amount;
        UpdateUIImmediate();

        if (debugLogs) Debug.Log($"CurrencyManager.Add amount={amount} playVisual={playVisual} time={Time.unscaledTime:F2}");

        if (playVisual) PlayPopTween();
    }

    public void AddAndPop(int amount)
    {
        Add(amount, playVisual: false);
        PlayPopTween();
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

    public void TriggerPop() => PlayPopTween();

    [ContextMenu("Test Pop")]
    public void TestPop() => PlayPopTween();

    // Visual-only pop tween (kill + restart approach)
    private void PlayPopTween()
    {
        if (moneyText != null)
        {
            moneyText.text = moneyPrefix + money.ToString("N0");
            moneyText.ForceMeshUpdate();
        }

        if (moneyVisualRect != null)
        {
            moneyVisualRect.DOKill(true);
            moneyVisualRect.localScale = originalScale;

            moneyVisualRect.DOScale(originalScale * 1.25f, 0.09f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    moneyVisualRect.DOScale(originalScale, 0.09f).SetEase(Ease.InSine);
                });
            return;
        }

        if (moneyText != null)
        {
            RectTransform rt = moneyText.rectTransform;
            rt.DOKill(true);
            rt.localScale = originalScale;

            rt.DOScale(originalScale * 1.25f, 0.09f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    rt.DOScale(originalScale, 0.09f).SetEase(Ease.InSine);
                });
        }
    }

    [ContextMenu("Reset Money")]
    public void ResetMoney()
    {
        money = startingMoney;
        UpdateUIImmediate();
    }
}
