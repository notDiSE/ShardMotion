using System;
using ShardMotion;
using UnityEngine;

public class FixHipsMovement : MonoBehaviour
{
    [SerializeField] private Transform hipsTracking;
    [SerializeField] private Vector3 positionOffset;

    private void Update()
    {
        transform.position = hipsTracking.position + positionOffset;
        transform.rotation = hipsTracking.rotation;
    }
}
