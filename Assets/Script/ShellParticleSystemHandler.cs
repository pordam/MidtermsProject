/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class ShellParticleSystemHandler : MonoBehaviour {

    [SerializeField] private Vector2 casingSize = new Vector2(0.1f, 0.2f);
    [SerializeField] private string sortingLayerName = "Foreground";
    [SerializeField] private int sortingOrder = 50;


    public static ShellParticleSystemHandler Instance { get; private set; }

    private MeshParticleSystem meshParticleSystem;
    private List<Single> singleList;

    private void Awake()
    {
        Instance = this;
        meshParticleSystem = GetComponent<MeshParticleSystem>();
        singleList = new List<Single>();

        // Apply sorting layer and order to the MeshRenderer
        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = sortingLayerName; // ensure this layer exists
            mr.sortingOrder = sortingOrder;         // set to a value higher than tilemap
                                                    // Ensure material uses sprite/transparent queue so sorting layer/order are respected
            if (mr.material != null)
            {
                var spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null && mr.material.shader != spriteShader)
                    mr.material.shader = spriteShader;
                mr.material.renderQueue = 3000;
            }
        }

    }


    private void Update() {
        for (int i=0; i<singleList.Count; i++) {
            Single single = singleList[i];
            single.Update();
            if (single.IsMovementComplete()) {
                singleList.RemoveAt(i);
                i--;
            }
        }
    }

    public void SpawnShell(Vector3 worldPosition, Vector3 worldDirection)
    {
        // Convert to mesh local space so vertices are placed correctly
        Transform meshTransform = meshParticleSystem.transform;
        Vector3 localPosition = meshTransform.InverseTransformPoint(worldPosition);
        Vector3 localDirection = meshTransform.InverseTransformDirection(worldDirection).normalized;

        singleList.Add(new Single(localPosition, localDirection, meshParticleSystem, casingSize));
    }

    /*
     * Represents a single Shell
     * */
    private class Single {

        private MeshParticleSystem meshParticleSystem;
        private Vector3 position;
        private Vector3 direction;
        private int quadIndex;
        private Vector3 quadSize;
        private float rotation;
        private float moveSpeed;

        public Single(Vector3 position, Vector3 direction, MeshParticleSystem meshParticleSystem, Vector2 size) {
            this.position = position;
            this.direction = direction;
            this.meshParticleSystem = meshParticleSystem;

            quadSize = new Vector3(size.x, size.y);
            rotation = Random.Range(0, 360f);
            moveSpeed = Random.Range(30f, 50f);

            quadIndex = meshParticleSystem.AddQuad(position, rotation, quadSize, true, 0);
        }

        public void Update() {
            position += direction * moveSpeed * Time.deltaTime;
            rotation += 360f * (moveSpeed / 10f) * Time.deltaTime;

            meshParticleSystem.UpdateQuad(quadIndex, position, rotation, quadSize, true, 0);

            float slowDownFactor = 25.5f;
            moveSpeed -= moveSpeed * slowDownFactor * Time.deltaTime;
        }

        public bool IsMovementComplete() {
            return moveSpeed < .1f;
        }

    }

}
