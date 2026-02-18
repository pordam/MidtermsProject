using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool")]
    public int poolSize = 8;
    public AudioSource audioSourcePrefab; // optional prefab; if null we create simple sources

    [Header("Pitch Randomization")]
    [Range(0.1f, 2f)] public float minPitch = 0.95f;
    [Range(0.1f, 2f)] public float maxPitch = 1.05f;

    private Queue<AudioSource> pool = new Queue<AudioSource>();

    // Add this field near the top of the AudioManager class (serialize so you can toggle it in the Inspector)
    [Header("Debug")]
    [SerializeField] private bool debugLogPlaySfx = true;

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        // Temporary debug logging to trace callers and frequency
        if (debugLogPlaySfx)
        {
            // Short timestamp + clip info
            string info = $"PlaySfx called: {clip.name} vol={volume:F2} time={Time.unscaledTime:F2}";
            // Stack trace to find the caller (skip first few frames if you want)
            string stack = System.Environment.StackTrace;
            Debug.Log(info + "\nCaller stack:\n" + stack);
        }

        // --- existing PlaySfx implementation follows ---
        AudioSource src = GetSourceFromPool();
        if (src == null)
        {
            src = CreateSource();
        }

        // Defensive checks and activation
        if (!src.gameObject.activeSelf) src.gameObject.SetActive(true);
        if (!src.enabled) src.enabled = true;

        float randomPitch = Random.Range(minPitch, maxPitch);
        src.pitch = randomPitch;

        float finalVolume = Mathf.Clamp01(volume * src.volume);

        src.PlayOneShot(clip, finalVolume);

        StartCoroutine(ReturnAfter(src, clip.length / Mathf.Abs(src.pitch)));
    }


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = CreateSource();
            src.gameObject.SetActive(false);
            pool.Enqueue(src);
        }
    }

    private AudioSource CreateSource()
    {
        GameObject go = new GameObject("SfxSource");
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D sound
        return src;
    }

    private AudioSource GetSourceFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();
        // if pool exhausted, create a temporary source
        AudioSource extra = CreateSource();
        return extra;
    }

    private System.Collections.IEnumerator ReturnAfter(AudioSource src, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        src.Stop();
        src.clip = null;
        src.gameObject.SetActive(false);
        pool.Enqueue(src);
    }
}
