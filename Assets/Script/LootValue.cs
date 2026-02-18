using UnityEngine;

public class LootValue : MonoBehaviour
{
    [Tooltip("How much currency this item gives when picked up")]
    public int value = 1;

    [Tooltip("SFX to play when this loot is picked up (optional)")]
    public AudioClip pickupSfx;

    [Range(0f, 1f)]
    public float pickupVolume = 1f;
}
