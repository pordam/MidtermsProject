using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }
    public ParticleSystem prefab;
    public int initialSize = 8;

    private Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
    private bool shuttingDown = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Only prewarm if prefab is assigned
        if (prefab != null)
        {
            for (int i = 0; i < initialSize; i++)
            {
                var p = Instantiate(prefab, transform);
                p.gameObject.SetActive(false);
                pool.Enqueue(p);
            }
        }
    }

    void OnDestroy()
    {
        shuttingDown = true;
    }

    // Existing PlayAt uses the pool's prefab
    public void PlayAt(Vector3 position, Quaternion rotation)
    {
        PlayAt(position, rotation, prefab);
    }

    // New overload: allow passing a prefab to use for this play
    public void PlayAt(Vector3 position, Quaternion rotation, ParticleSystem overridePrefab)
    {
        if (shuttingDown) return;

        // If no prefab provided at all, do nothing
        if (overridePrefab == null && prefab == null) return;

        // If overridePrefab is provided, instantiate it directly (do not add to pool)
        if (overridePrefab != null)
        {
            ParticleSystem p = Instantiate(overridePrefab, position, rotation);
            p.gameObject.SetActive(true);
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear(true);
            p.time = 0f;
            p.Play();
            StartCoroutine(ReturnAndDestroyAfter(p));
            return;
        }

        // Otherwise use pooled prefab (prefab != null guaranteed here)
        ParticleSystem pooled;
        if (pool.Count > 0) pooled = pool.Dequeue();
        else pooled = Instantiate(prefab, transform);

        if (pooled == null) return;

        pooled.transform.position = position;
        pooled.transform.rotation = rotation;
        pooled.gameObject.SetActive(true);

        try
        {
            pooled.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pooled.Clear(true);
            pooled.time = 0f;
        }
        catch { }

        pooled.Play();
        StartCoroutine(ReturnAfterSafe(pooled));
    }

    // Return-and-destroy for one-off instantiates
    private IEnumerator ReturnAndDestroyAfter(ParticleSystem p)
    {
        if (p == null) yield break;
        float timeout = 10f;
        float elapsed = 0f;
        while (!shuttingDown && p != null && p.IsAlive(true) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (p == null) yield break;
        try
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear(true);
        }
        catch { }
        Destroy(p.gameObject);
    }

    private IEnumerator ReturnAfterSafe(ParticleSystem p)
    {
        if (p == null) yield break;
        float timeout = 10f;
        float elapsed = 0f;
        while (!shuttingDown && p != null && p.IsAlive(true) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (shuttingDown) yield break;
        if (p == null) yield break;
        try
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear(true);
            p.gameObject.SetActive(false);
            if (Instance != null && Instance == this)
            {
                pool.Enqueue(p);
            }
            else
            {
                Destroy(p.gameObject);
            }
        }
        catch { }
    }

    // Optional helper: replace the pool's prefab and rebuild pool
    public void ClearAndSetPrefab(ParticleSystem newPrefab, int newInitialSize = -1)
    {
        // destroy existing pooled instances
        while (pool.Count > 0)
        {
            var p = pool.Dequeue();
            if (p != null) Destroy(p.gameObject);
        }

        prefab = newPrefab;
        if (newInitialSize > 0) initialSize = newInitialSize;

        if (prefab != null)
        {
            for (int i = 0; i < initialSize; i++)
            {
                var p = Instantiate(prefab, transform);
                p.gameObject.SetActive(false);
                pool.Enqueue(p);
            }
        }
    }
}
