using System.Linq;
using UnityEngine;
using System.Collections;

public class Chest : MonoBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private int cost = 50;                  // Fixed cost to open
    [SerializeField] private float lootSpawnRadius = 0.2f;   // Random offset radius
    [SerializeField] private PlayerUpgradeData[] allUpgrades;

    private bool isOpened = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isOpened) return;

        if (collision.CompareTag("Player"))
        {
            if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpend(cost))
            {
                OpenChest();
            }
            else
            {
                Debug.Log("Not enough money to open chest.");
                // Do nothing: chest stays closed and collider stays active
            }
        }
    }

    private PlayerUpgradeData GetRandomUpgrade()
    {
        float roll = Random.value;
        UpgradeRarity chosenRarity;

        if (roll < 0.7f) chosenRarity = UpgradeRarity.Common;
        else if (roll < 0.9f) chosenRarity = UpgradeRarity.Rare;
        else if (roll < 0.98f) chosenRarity = UpgradeRarity.Epic;
        else chosenRarity = UpgradeRarity.Legendary;

        var candidates = allUpgrades.Where(u => u.rarity == chosenRarity).ToList();
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No upgrades found for rarity {chosenRarity}, defaulting to Common.");
            candidates = allUpgrades.Where(u => u.rarity == UpgradeRarity.Common).ToList();
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private void OpenChest()
    {
        isOpened = true;
        Debug.Log("Chest opened!");

        // disable chest collider and visuals
        Collider2D chestCollider = GetComponent<Collider2D>();
        if (chestCollider != null) chestCollider.enabled = false;

        var rend = GetComponentInChildren<SpriteRenderer>();
        if (rend != null) rend.enabled = false;

        // destroy chest after a delay (optional)
        Destroy(gameObject, 2f);


        PlayerUpgradeData upgrade = GetRandomUpgrade();
        if (upgrade != null)
        {
            Vector2 offset = Random.insideUnitCircle * lootSpawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;
            GameObject item = Instantiate(upgrade.visualPrefab, spawnPos, Quaternion.identity);

            Collider2D col = item.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(col, 2f));
            }
        }
    }


    private IEnumerator EnableColliderAfterDelay(Collider2D col, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null) col.enabled = true; // only re-enable if still valid
    }


}
