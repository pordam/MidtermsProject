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

    /// <summary>
    /// Play an SFX with randomized pitch. Safe for overlapping sounds.
    /// </summary>
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource src = GetSourceFromPool();
        src.gameObject.SetActive(true);

        float randomPitch = Random.Range(minPitch, maxPitch);
        src.pitch = randomPitch;
        src.volume = Mathf.Clamp01(volume);
        src.clip = clip;
        src.Play();

        StartCoroutine(ReturnAfter(src, clip.length / Mathf.Abs(src.pitch)));
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
