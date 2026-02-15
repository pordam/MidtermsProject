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

        for (int i = 0; i < initialSize; i++)
        {
            var p = Instantiate(prefab, transform);
            p.gameObject.SetActive(false);
            pool.Enqueue(p);
        }
    }

    void OnDestroy()
    {
        // mark shutting down so coroutines stop trying to return items
        shuttingDown = true;
    }

    public void PlayAt(Vector3 position, Quaternion rotation)
    {
        if (shuttingDown) return;

        ParticleSystem p;
        if (pool.Count > 0) p = pool.Dequeue();
        else
        {
            p = Instantiate(prefab, transform);
        }

        if (p == null) return;

        // Prepare and play
        p.transform.position = position;
        p.transform.rotation = rotation;
        p.gameObject.SetActive(true);

        // Reset state safely
        try
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear(true);
            p.time = 0f;
        }
        catch { /* if p was destroyed between dequeue and here, bail out */ }

        p.Play();

        // Start coroutine to return it when finished
        StartCoroutine(ReturnAfterSafe(p));
    }

    private IEnumerator ReturnAfterSafe(ParticleSystem p)
    {
        if (p == null) yield break;

        // Wait until the particle system is no longer alive (including children)
        // Use a timeout as a safety net in case IsAlive behaves unexpectedly
        float timeout = 10f; // generous safety timeout
        float elapsed = 0f;

        while (!shuttingDown && p != null && p.IsAlive(true) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // If pool was destroyed while waiting, just stop
        if (shuttingDown) yield break;

        // If the particle was destroyed externally, bail out
        if (p == null) yield break;

        // Safely stop/clear and return to pool
        try
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear(true);
            p.gameObject.SetActive(false);

            // If pool still exists, enqueue; otherwise destroy to avoid orphan objects
            if (Instance != null && Instance == this)
            {
                pool.Enqueue(p);
            }
            else
            {
                // pool no longer exists, destroy the particle to avoid leaks
                Destroy(p.gameObject);
            }
        }
        catch
        {
            // If Stop/Clear throws because object was destroyed, ignore
        }
    }
}
