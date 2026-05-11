using ShardMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ShardMotion.Examples
{
    /// <summary>
    /// Game manager, managing the state of the game
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public ArUcoTarget subMarinePrefab; // Prefab of submarine to spawn
        private ArUcoTarget spawnedSubMarine; // reference to spawned submarine in the scene

        // References to components
        [SerializeField] private SpawnBombs spawnBombs;
        [SerializeField] TrackingCamera trackingCamera;
        [SerializeField] TMP_Dropdown cameraDropdown;

        // References to UI screens
        [SerializeField] private GameObject setupScreen;
        [SerializeField] private GameObject gameOverScreen;

        void Start()
        {
            PopulateCameraDropdown();
            setupScreen.SetActive(true); // turns on the Main menu UI screen
        }
        
        /// <summary>
        /// Loads the start menu dropdown with availible cameras
        /// </summary>
        void PopulateCameraDropdown()
        {
            cameraDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach (var device in WebCamTexture.devices) options.Add(device.name);
            cameraDropdown.AddOptions(options);
        }
        
        /// <summary>
        /// Player chose camera to use, the game will begin
        /// </summary>
        public void Play()
        {
            int sel = cameraDropdown.value;
            trackingCamera.OnStartedCapturing += StartGameLoop; // game begins when camera starts capturing
            trackingCamera.StartTracking(sel); // Start the camera tracking
        }

        /// <summary>
        /// Starts the main game loop
        /// </summary>
        public void StartGameLoop()
        {
            // Any UI is turned off
            setupScreen.SetActive(false); 
            gameOverScreen.SetActive(false);
            
            SpawnSubMarine(); // submarine is spawned
            spawnBombs.StartSpawning(); // bombs start to spawn
        }

        /// <summary>
        /// Game loop is stopped
        /// </summary>
        public void StopGameLoop()
        {
            DespawnSubMarine();
            spawnBombs.StopSpawning();
        }

        /// <summary>
        /// Player crashed
        /// </summary>
        public void GameOver()
        {
            StopGameLoop();
            spawnBombs.ClearAllBombs(); // all bombs get cleared off screen
            gameOverScreen.SetActive(true); // game over screen UI is enabled
        }
        
        void SpawnSubMarine()
        {
            spawnedSubMarine = Instantiate(subMarinePrefab); // Submarine is spawned from prefab
            ArUcoRegistry.Register(spawnedSubMarine); // Spawned Target is registered, so it can be tracked
            spawnedSubMarine.GetComponent<SubMarine>().OnCollided += GameOver; // if the submarine collides, game over
        }
        
        void DespawnSubMarine()
        {
            ArUcoRegistry.Unregister(spawnedSubMarine); // submarine unregistred
            GameObject.Destroy(spawnedSubMarine.gameObject); // submarie destroyed from scene
        }
    }
}
