using System.Collections;
using UnityEngine;

public class SpawnBombs : MonoBehaviour
{
    [SerializeField] private Bomb bombPrefab;
    public Vector2 spawnRange;
    public Vector2 bombSpeedRange;

    public float spawnFrequency;
    private Coroutine routine;

    public void StartSpawning()
    {
        if (routine != null) StopCoroutine(routine);
        
        routine = StartCoroutine(Loop());
    }

    public void StopSpawning()
    {
        if (routine != null) StopCoroutine(routine);
    }

    private IEnumerator Loop()
    {
        SpawnBomb();
        yield return new WaitForSeconds(spawnFrequency);
        routine = StartCoroutine(Loop());
    }
    
    void SpawnBomb()
    {
        float height = Random.Range(spawnRange.x, spawnRange.y);
        Bomb spawnedBomb = GameObject.Instantiate(bombPrefab, new Vector3(transform.position.x, transform.position.y + height, transform.position.z), Quaternion.identity);
        spawnedBomb.direction *= Random.Range(bombSpeedRange.x, bombSpeedRange.y);
        spawnedBomb.transform.parent = transform;
    }

    public void ClearAllBombs()
    {
        foreach (Transform bombTransform in transform)
        {
            Destroy(bombTransform.gameObject);
        }
    }
}
