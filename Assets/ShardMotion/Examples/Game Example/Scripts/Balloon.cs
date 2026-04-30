using System;
using UnityEngine;

public class Balloon : MonoBehaviour
{

    public Action OnCollided;
    private void LateUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0); // limits the balloon movement on z axis. (no depth) 
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        OnCollided?.Invoke();
    }
}
