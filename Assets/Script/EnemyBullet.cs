using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f; // adjust in Inspector

    private Vector3 moveDirection;

    public void Initialize(Vector3 direction)
    {
        moveDirection = direction.normalized;
    }

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Bullet collided with: " + collision.gameObject.name + " (tag: " + collision.gameObject.tag + ")");
        if (collision.gameObject.CompareTag("Wall"))
        {

            Destroy(gameObject);
        }

        // Optional: damage player if it hits them
        if (collision.gameObject.CompareTag("Player"))
        {

            Destroy(gameObject);
        }
    }
}
