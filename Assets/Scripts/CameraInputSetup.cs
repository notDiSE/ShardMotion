using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraInputSetup", menuName = "Shard Motion/CameraInputSetup")]
public class CameraInputSetup : ScriptableObject
{
    public List<InputSource> sources = new List<InputSource>();
    
}


