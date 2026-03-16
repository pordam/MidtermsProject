using UnityEngine;
using UnityEngine.UI;

public class DifficultyManager : MonoBehaviour
{
    [Header("Difficulty Settings")]
    [SerializeField] private float timeToMaxDifficulty = 300f; // 5 minutes
    [SerializeField] private Image difficultyBar; // UI fill image

    [Header("Difficulty Gradient")]
    [SerializeField] private Gradient difficultyGradient; // assign in Inspector

    private float difficultyMultiplier = 1f;
    public float DifficultyMultiplier => difficultyMultiplier;

    void Update()
    {
        float t = Mathf.Clamp01(Time.time / timeToMaxDifficulty);
        difficultyMultiplier = Mathf.Lerp(1f, 3f, t);

        UpdateUI(t);
    }

    private void UpdateUI(float t)
    {
        if (difficultyBar != null)
        {
            difficultyBar.fillAmount = t;
            difficultyBar.color = difficultyGradient.Evaluate(t);
        }
    }
}
