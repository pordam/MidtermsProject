using UnityEngine;
using System.Collections;

public class AfterImageController : MonoBehaviour
{
    [Header("Afterimage Settings")]
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float startingOpacity = 0.6f;
    [SerializeField] private float fadeDuration = 0.3f;

    [SerializeField] private Vector3 afterImageScale = Vector3.one;

    private SpriteRenderer sourceRenderer;
    private bool spawning = false;

    void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
    }

    public void StartAfterImages(float duration)
    {
        if (!spawning)
            StartCoroutine(SpawnRoutine(duration));
    }

    private IEnumerator SpawnRoutine(float duration)
    {
        spawning = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            InstantiateAfterImage();
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }

        spawning = false;
    }

    private void InstantiateAfterImage()
    {
        GameObject go = new GameObject("AfterImage");
        go.transform.position = sourceRenderer.transform.position;
        go.transform.rotation = sourceRenderer.transform.rotation;
        go.layer = gameObject.layer;

        SpriteRenderer srCopy = go.AddComponent<SpriteRenderer>();
        srCopy.sprite = sourceRenderer.sprite;
        srCopy.material = sourceRenderer.material;
        srCopy.sortingLayerID = sourceRenderer.sortingLayerID;
        srCopy.sortingOrder = sourceRenderer.sortingOrder - 1;
        srCopy.flipX = sourceRenderer.flipX;
        srCopy.flipY = sourceRenderer.flipY;

        // Apply custom scale
        go.transform.localScale = afterImageScale;

        Color c = srCopy.color;
        c.a = startingOpacity;
        srCopy.color = c;

        StartCoroutine(FadeAndDestroy(srCopy));
    }


    private IEnumerator FadeAndDestroy(SpriteRenderer srCopy)
    {
        float t = 0f;
        Color original = srCopy.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            srCopy.color = new Color(original.r, original.g, original.b,
                                     Mathf.Lerp(original.a, 0f, t / fadeDuration));
            yield return null;
        }
        Destroy(srCopy.gameObject);
    }
}
