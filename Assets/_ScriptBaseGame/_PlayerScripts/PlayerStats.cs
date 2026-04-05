using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseMoveSpeed = 5f;
    public float baseAttackSpeed = 1f;
    public float baseCritChance = 0.05f;

    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float attackSpeed;

    private float moveSpeedMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private float critChanceMultiplier = 1f;

    public int baseDamage = 10;
    [Range(0f, 1f)] public float critChance = 0.2f;
    public float critMultiplier = 2f;
    private float damageMultiplier = 1f; // NEW

    // Passive regen
    [Header("Passive Regen")]
    [SerializeField] private float baseRegen = 0f;      // HP per second
    [SerializeField] private float regenBonus = 0f;     // HP per second from items/upgrades

    private float regenAccumulator = 0f;                // accumulates fractional HP
    public float CurrentRegen => baseRegen + regenBonus; // total HP/sec

    public void AddRegen(float amount) => regenBonus += amount;
    public void SetBaseRegen(float value) => baseRegen = value;
    public void SetRegenBonus(float value) => regenBonus = value;

    private PlayerController cachedPlayer; // add this near other private fields


    public int CalculateFinalDamage()
    {
        int damage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        if (Random.value < critChance)
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);
            Debug.Log("[PlayerStats] CRITICAL STRIKE!");
        }
        return damage;
    }
    private void Awake()
    {
        ResetStats();
        // cache player controller for regen/heal calls
        cachedPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        // Passive regen: accumulate fractional HP and apply whole HP heals
        if (CurrentRegen <= 0f) return;

        regenAccumulator += CurrentRegen * Time.deltaTime;

        if (regenAccumulator >= 1f)
        {
            int healAmount = Mathf.FloorToInt(regenAccumulator);
            regenAccumulator -= healAmount;

            if (cachedPlayer == null)
                cachedPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

            if (cachedPlayer != null && cachedPlayer.GetCurrentHealth() < cachedPlayer.maxHealth)
            {
                // PlayerController exposes Heal(int) — call with an int
                cachedPlayer.Heal(healAmount);
            }
        }
    }


    public void ApplyUpgrade(PlayerUpgradeData data)
    {
        switch (data.statType)
        {
            case StatType.MoveSpeed:
                if (data.valueType == UpgradeValueType.Flat)
                    moveSpeed += data.value;
                else
                {
                    moveSpeedMultiplier += data.value;
                    moveSpeed = baseMoveSpeed * moveSpeedMultiplier;
                }
                break;

            case StatType.AttackSpeed:
                if (data.valueType == UpgradeValueType.Flat)
                    attackSpeed += data.value;
                else
                {
                    attackSpeedMultiplier += data.value;
                    attackSpeed = baseAttackSpeed * attackSpeedMultiplier;
                }
                break;

            case StatType.CritChance:
                if (data.valueType == UpgradeValueType.Flat)
                    critChance += data.value;
                else
                {
                    critChanceMultiplier += data.value;
                    critChance = baseCritChance * critChanceMultiplier;
                }
                break;
            case StatType.Health:
                var player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                    ApplyHealthUpgrade(player, data);
                break;
            case StatType.Damage:
                if (data.valueType == UpgradeValueType.Flat)
                {
                    baseDamage += (int)data.value;
                }
                else
                {
                    damageMultiplier += data.value; // e.g. +0.1f for +10%
                }
                break;
            case StatType.HealthRegen:
                if (data.valueType == UpgradeValueType.Flat)
                    AddRegen(data.value);        // data.value is HP per second
                else
                    baseRegen *= (1f + data.value);
                break;

        }

        Debug.Log($"Applied {data.upgradeName}: {data.value} {data.valueType} {data.statType}");
    }

    public void ResetStats()
    {
        moveSpeedMultiplier = 1f;
        attackSpeedMultiplier = 1f;
        critChanceMultiplier = 1f;

        moveSpeed = baseMoveSpeed;
        attackSpeed = baseAttackSpeed;
        critChance = baseCritChance;
    }

    public int CalculateDamage(int baseDamage)
    {
        bool isCrit = UnityEngine.Random.value < critChance;
        int finalDamage = isCrit ? baseDamage * 2 : baseDamage;

        if (isCrit)
        {
            Debug.Log($"[PlayerStats] Critical strike rolled! Base={baseDamage}, Final={finalDamage}");
        }

        return finalDamage;
    }

    public void ApplyHealthUpgrade(PlayerController player, PlayerUpgradeData data)
    {
        if (data.valueType == UpgradeValueType.Flat)
        {
            player.SetHealth(player.GetCurrentHealth() + (int)data.value);
        }
        else
        {
            player.maxHealth = Mathf.RoundToInt(player.maxHealth * (1f + data.value));
            player.SetHealth(player.maxHealth); // reset to new max
        }

        Debug.Log($"Applied {data.upgradeName}: {data.value} {data.valueType} Health");
    }
}
