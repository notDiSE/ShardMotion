using System;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public Vector3 direction;

    private void FixedUpdate()
    {
        transform.position += direction * Time.deltaTime;
        
        if(transform.position.x <= -12) Destroy(gameObject);
    }
}
