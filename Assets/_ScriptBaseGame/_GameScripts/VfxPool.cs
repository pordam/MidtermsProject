using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private bool shuttingDown = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[VfxPool] Awake at position {transform.position}");
    }

    void OnDestroy() { shuttingDown = true; }

    public GameObject PlayAt(Vector3 position, Quaternion rotation, GameObject prefab, float autoReturnDelay = -1f)
    {
        if (shuttingDown || prefab == null) return null;

        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();

        GameObject obj;
        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();
            Debug.Log($"[VfxPool] Reusing pooled object {obj.name}");
        }
        else
        {
            obj = Instantiate(prefab);
            Debug.Log($"[VfxPool] Instantiated new object {obj.name}");
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, 0f);
        Debug.Log($"[VfxPool] Spawned {obj.name} at {obj.transform.position}");

        obj.SetActive(true);

        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Debug.Log($"[VfxPool] Found ParticleSystem on {obj.name}, starting playback");
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.time = 0f;
            ps.Play();
            StartCoroutine(ReturnAfter(ps, prefab));
        }
        else if (autoReturnDelay > 0f)
        {
            Debug.Log($"[VfxPool] Non-particle prefab {obj.name}, will auto-return after {autoReturnDelay}s");
            StartCoroutine(ReturnAfterDelay(obj, prefab, autoReturnDelay));
        }

        return obj;
    }

    private IEnumerator ReturnAfter(ParticleSystem ps, GameObject prefab)
    {
        float minDuration = 0.2f;
        yield return new WaitForSeconds(minDuration);

        float timeout = 10f;
        float elapsed = 0f;
        while (!shuttingDown && ps != null && ps.IsAlive(true) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (ps == null) yield break;

        Debug.Log($"[VfxPool] Returning particle {ps.gameObject.name} to pool at {ps.transform.position}");

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
        ps.gameObject.SetActive(false);
        pools[prefab].Enqueue(ps.gameObject);
    }

    private IEnumerator ReturnAfterDelay(GameObject obj, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj == null) yield break;

        Debug.Log($"[VfxPool] Returning object {obj.name} to pool at {obj.transform.position}");

        obj.SetActive(false);
        pools[prefab].Enqueue(obj);
    }
}
