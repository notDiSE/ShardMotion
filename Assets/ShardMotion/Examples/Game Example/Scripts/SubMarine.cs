using System;
using UnityEngine;

namespace ShardMotion.Examples
{
    /// <summary>
    /// Player script
    /// </summary>
    public class SubMarine : MonoBehaviour
    {

        public Action OnCollided;
        private void LateUpdate()
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0); // limits the subMarine movement on z axis. (no depth) 
        }
        
        /// <summary>
        /// Collision
        /// </summary>
        /// <param name="other"> collider that collided </param>
        private void OnTriggerEnter(Collider other)
        {
            OnCollided?.Invoke(); // call on collided action
        }
    }
    
}
