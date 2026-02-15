using UnityEngine;
using UnityEngine.InputSystem;

public class GunScript : MonoBehaviour
{

    public Camera cam;
    Vector2 mousePos;

    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }


    private void FixedUpdate()
    {
        mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Vector2 lookDir = mousePos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
