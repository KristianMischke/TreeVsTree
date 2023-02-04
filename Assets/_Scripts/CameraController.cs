using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float AcellerationMultiplier;
    public float Dampening;

    private Vector2 velocity;

    // Update is called once per frame
    void Update()
    {
        Vector2 acceleration = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            acceleration += Vector2.left;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            acceleration += Vector2.right;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            acceleration += Vector2.up;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            acceleration += Vector2.down;
        }
        
        acceleration.Normalize();
        acceleration *= AcellerationMultiplier;

        velocity += acceleration;
        velocity *= Dampening;

        transform.position += new Vector3(velocity.x, velocity.y) * Time.deltaTime;
    }
}
