using ShardMotion;
using TMPro;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public ArUcoTarget balloonPrefab;
    private ArUcoTarget spawnedBalloon;

    [SerializeField] private SpawnBombs spawnBombs;
    [SerializeField] TrackingCamera trackingCamera;
    [SerializeField] TMP_Dropdown cameraDropdown;

    [SerializeField] private GameObject setupScreen;
    [SerializeField] private GameObject gameOverScreen;

    void Start()
    {
        PopulateCameraDropdown();
        setupScreen.SetActive(true);
    }
    
    void PopulateCameraDropdown()
    {
        cameraDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        foreach (var device in WebCamTexture.devices) options.Add(device.name);
        cameraDropdown.AddOptions(options);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Play()
    {
        int sel = cameraDropdown.value;
        trackingCamera.OnStartedCapturing += StartGameLoop;
        trackingCamera.StartTracking(sel);
    }

    public void StartGameLoop()
    {
        setupScreen.SetActive(false);
        gameOverScreen.SetActive(false);
        SpawnBalloon();
        spawnBombs.StartSpawning();
    }

    public void StopGameLoop()
    {
        DespawnBalloon();
        spawnBombs.StopSpawning();
    }

    public void GameOver()
    {
        StopGameLoop();
        spawnBombs.ClearAllBombs();
        gameOverScreen.SetActive(true);
    }
    
    void SpawnBalloon()
    {
        spawnedBalloon = Instantiate(balloonPrefab);
        ArUcoRegistry.Register(spawnedBalloon);
        spawnedBalloon.GetComponent<SubMarine>().OnCollided += GameOver;
    }
    
    void DespawnBalloon()
    {
        ArUcoRegistry.Unregister(spawnedBalloon);
        GameObject.Destroy(spawnedBalloon.gameObject);
    }
}
