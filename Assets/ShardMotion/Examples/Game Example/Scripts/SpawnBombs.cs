using System.Collections;
using UnityEngine;

namespace ShardMotion.Examples
{
    /// <summary>
    /// Component, that spawns bombs
    /// </summary>
    public class SpawnBombs : MonoBehaviour
    {
        [SerializeField] private Bomb bombPrefab; // prefab of bomb
        
        public Vector2 spawnRange; // range of Y coordinates bombs spawn in 
        public Vector2 bombSpeedRange; // bombs vary in speed of this range

        public float spawnFrequency;  // how often do bombs spawn
        private Coroutine routine; // reference to running loop. so it can be stopped

        /// <summary>
        /// Loop begins
        /// </summary>
        public void StartSpawning()
        {
            if (routine != null) StopCoroutine(routine);
            
            routine = StartCoroutine(Loop());
        }

        /// <summary>
        /// Loop stops
        /// </summary>
        public void StopSpawning()
        {
            if (routine != null) StopCoroutine(routine);
        }
        
        private IEnumerator Loop()
        {
            SpawnBomb(); // spawns one bomb
            yield return new WaitForSeconds(spawnFrequency); // waits for X seconds
            routine = StartCoroutine(Loop()); // calls itself -> loop
        }
        
        /// <summary>
        /// One bomb is spawned
        /// </summary>
        void SpawnBomb()
        {
            float height = Random.Range(spawnRange.x, spawnRange.y); // height is chosen
            Bomb spawnedBomb = GameObject.Instantiate(bombPrefab, new Vector3(transform.position.x, transform.position.y + height, transform.position.z), Quaternion.identity); // bomb is spawned at chosen location
            spawnedBomb.direction *= Random.Range(bombSpeedRange.x, bombSpeedRange.y); // bomb moves in given direction at speed X
            spawnedBomb.transform.parent = transform; // bomb is moved as child to this object for easy clearing
        }

        public void ClearAllBombs()
        {
            // Destroys every children
            foreach (Transform bombTransform in transform)
            {
                Destroy(bombTransform.gameObject);
            }
        }
    }
    
}
