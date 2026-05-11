using System;
using UnityEngine;

namespace ShardMotion.Examples
{
    /// <summary>
    /// Script used to move bombs on screen
    /// </summary>
    public class Bomb : MonoBehaviour
    {
        public Vector3 direction;

        private void FixedUpdate()
        {
            transform.position += direction * Time.deltaTime; // time delta time used to make it not frame dependant
            
            if(transform.position.x <= -12) Destroy(gameObject); // if the bomb is out fo screen, destroy
        }
    }
    
}
