using UnityEngine;

public enum UpgradeRarity { Common, Rare, Epic, Legendary }
public enum StatType { MoveSpeed, AttackSpeed, CritChance, Health, Damage, HealthRegen }

public enum UpgradeValueType { Flat, Percentage }

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/PlayerUpgrade")]
public class PlayerUpgradeData : ScriptableObject
{
    public string upgradeName;
    public string description;
    public UpgradeRarity rarity;
    public StatType statType;
    public UpgradeValueType valueType;  // Flat or Percentage
    public float value;                 // e.g. 2 (flat) or 0.1 (10%)
    public GameObject visualPrefab;
}

