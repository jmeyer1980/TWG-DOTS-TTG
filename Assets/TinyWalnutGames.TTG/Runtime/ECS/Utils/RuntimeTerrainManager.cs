using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: MonoBehaviour
    /// @Domain: Runtime
    /// @Role: Initialization
    /// 
    /// Runtime manager that handles terrain entities and scene loading for builds.
    /// This MonoBehaviour should be placed in your main scene to automatically
    /// manage terrain generation entities when the game starts.
    /// 
    /// Key Features:
    /// - Automatic runtime initialization for builds
    /// - Default terrain entity creation if none exist
    /// - Traditional Unity scene loading support
    /// - ECS system integration and monitoring
    /// - Build vs Editor parity for entity availability
    /// </summary>
    public class RuntimeTerrainManager : MonoBehaviour
    {
        [Header("Scene Loading")]
        [Tooltip("If using separate scene, specify the scene name directly")]
        public string terrainSceneName = "TerrainGenerationScene";
        
        [Tooltip("Scene build index (if using scene index instead of name)")]
        public int terrainSceneIndex = -1;
        
        [Header("Runtime Settings")]
        [Tooltip("Whether to automatically initialize on Start (recommended for builds)")]
        public bool autoInitializeOnStart = true;
        
        [Tooltip("Show debug logs for initialization process")]
        public bool enableDebugLogs = true;
        
        [Tooltip("Use traditional Unity scene loading for additional scenes")]
        public bool useUnitySceneLoading = false;
        
        [Tooltip("Wait time in seconds before creating default entities")]
        [Range(0f, 5f)]
        public float initializationDelay = 0.5f;
        
        [Header("Entity Creation")]
        [Tooltip("Create a default terrain entity if none exist")]
        public bool createDefaultTerrainEntity = true;
        
        [Tooltip("Default terrain generation parameters")]
        public TerrainGenerationData defaultTerrainData = new()
        {
            TerrainType = TerrainType.Planar,
            Sides = 6,
            Radius = 10f,
            MinHeight = 0f,
            MaxHeight = 5f,
            Depth = 3,
            Seed = 12345,
            BaseFrequency = 0.1f,
            Octaves = 4,
            Persistence = 0.5f,
            Lacunarity = 2f
        };
        
        [Header("Runtime Status")]
        [SerializeField, Tooltip("Current initialization status (Read-only)")]
        private InitializationStatus currentStatus = InitializationStatus.NotStarted;
        
        [SerializeField, Tooltip("Number of terrain entities found (Read-only)")]
        private int terrainEntityCount = 0;
        
        [SerializeField, Tooltip("Initialization completion time (Read-only)")]
        private float initializationTime = 0f;
        
        private World world;
        private bool hasInitialized = false;
        private RuntimeEntityLoaderSystem loaderSystem;
        
        public enum InitializationStatus
        {
            NotStarted,
            InProgress,
            WaitingForDelay,
            CreatingEntities,
            Complete,
            Failed
        }
        
        // Public properties for external monitoring
        public InitializationStatus Status => currentStatus;
        public int TerrainEntityCount => terrainEntityCount;
        public bool IsInitialized => hasInitialized;
        public bool IsTerrainSystemReady => IsTerrainSystemAvailable();
        
        private void Start()
        {
            if (autoInitializeOnStart && !hasInitialized)
            {
                StartCoroutine(InitializeWithDelay());
            }
        }
        
        private System.Collections.IEnumerator InitializeWithDelay()
        {
            currentStatus = InitializationStatus.WaitingForDelay;
            
            if (initializationDelay > 0f)
            {
                if (enableDebugLogs)
                    Debug.Log($"RuntimeTerrainManager: Waiting {initializationDelay}s before initialization...");
                
                yield return new WaitForSeconds(initializationDelay);
            }
            
            InitializeRuntimeLoading();
        }
        
        /// <summary>
        /// Manually initialize runtime terrain management.
        /// Call this if autoInitializeOnStart is disabled.
        /// </summary>
        public void InitializeRuntimeLoading()
        {
            if (hasInitialized)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("RuntimeTerrainManager already initialized!");
                return;
            }
            
            currentStatus = InitializationStatus.InProgress;
            var startTime = Time.realtimeSinceStartup;
            
            if (enableDebugLogs)
                Debug.Log("RuntimeTerrainManager: Initializing terrain system for runtime build...");
            
            try
            {
                // Get the default world
                world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    Debug.LogError("Default ECS World not found! Make sure Unity.Entities is properly set up.");
                    currentStatus = InitializationStatus.Failed;
                    return;
                }
                
                // Get the loader system for monitoring
                loaderSystem = world.GetExistingSystemManaged<RuntimeEntityLoaderSystem>();
                
                // Try different loading approaches
                if (useUnitySceneLoading)
                {
                    LoadUsingUnitySceneManager();
                }
                
                // Create scene loading requests for ECS tracking
                CreateSceneLoadingRequests();
                
                // Check if we need to create default entities
                currentStatus = InitializationStatus.CreatingEntities;
                if (createDefaultTerrainEntity)
                {
                    CreateDefaultTerrainEntityIfNeeded();
                }
                
                // Update status
                hasInitialized = true;
                currentStatus = InitializationStatus.Complete;
                initializationTime = Time.realtimeSinceStartup - startTime;
                
                if (enableDebugLogs)
                    Debug.Log($"RuntimeTerrainManager: Initialization complete in {initializationTime:F3}s");
                
                UpdateTerrainEntityCount();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"RuntimeTerrainManager initialization failed: {ex.Message}");
                currentStatus = InitializationStatus.Failed;
            }
        }
        
        private void LoadUsingUnitySceneManager()
        {
            if (enableDebugLogs)
                Debug.Log("RuntimeTerrainManager: Using Unity SceneManager for additional scene loading...");
            
            if (!string.IsNullOrEmpty(terrainSceneName))
            {
                // Check if scene is already loaded
                var scene = SceneManager.GetSceneByName(terrainSceneName);
                if (scene.isLoaded)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Scene {terrainSceneName} is already loaded.");
                    return;
                }
                
                // Load scene additively
                var asyncOperation = SceneManager.LoadSceneAsync(terrainSceneName, LoadSceneMode.Additive);
                if (asyncOperation != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Loading scene: {terrainSceneName}");
                    
                    // Optional: Add callback when scene loading completes
                    asyncOperation.completed += (op) =>
                    {
                        if (enableDebugLogs)
                            Debug.Log($"Scene loading completed: {terrainSceneName}");
                        UpdateTerrainEntityCount();
                    };
                }
            }
            else if (terrainSceneIndex >= 0)
            {
                var asyncOperation = SceneManager.LoadSceneAsync(terrainSceneIndex, LoadSceneMode.Additive);
                if (asyncOperation != null && enableDebugLogs)
                    Debug.Log($"Loading scene by index: {terrainSceneIndex}");
            }
        }
        
        private void CreateDefaultTerrainEntityIfNeeded()
        {
            var entityManager = world.EntityManager;
            
            // Check if terrain entities already exist
            using var existingQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TerrainGenerationData>()
            );
            
            var existingCount = existingQuery.CalculateEntityCount();
            if (existingCount > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"Found {existingCount} existing terrain entities. Skipping default creation.");
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log("No terrain entities found. Creating default terrain entity...");
            
            try
            {
                // Create default terrain entity
                var terrainEntity = entityManager.CreateEntity(
                    typeof(TerrainGenerationData),
                    typeof(TerraceConfigData),
                    typeof(TerrainGenerationRequest)
                );
                
                // Set terrain data
                entityManager.SetComponentData(terrainEntity, defaultTerrainData);
                
                // Create default terrace configuration
                var terraceConfig = CreateDefaultTerraceConfig();
                entityManager.SetComponentData(terrainEntity, terraceConfig);
                
                // Set generation request
                entityManager.SetComponentData(terrainEntity, new TerrainGenerationRequest
                {
                    UseAsyncGeneration = false
                });
                
                if (enableDebugLogs)
                    Debug.Log("Successfully created default terrain entity with basic configuration.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create default terrain entity: {ex.Message}");
            }
        }
        
        private TerraceConfigData CreateDefaultTerraceConfig()
        {
            // Create a simple 3-terrace configuration
            var terraceHeights = new float[] { 0f, 0.33f, 0.66f, 1f };
            
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var heightArray = ref builder.ConstructRoot<BlobArray<float>>();
            var arrayBuilder = builder.Allocate(ref heightArray, terraceHeights.Length);
            
            for (int i = 0; i < terraceHeights.Length; i++)
            {
                arrayBuilder[i] = terraceHeights[i];
            }
            
            var blobAsset = builder.CreateBlobAssetReference<BlobArray<float>>(Allocator.Persistent);
            
            return new TerraceConfigData
            {
                TerraceCount = terraceHeights.Length - 1,
                TerraceHeights = blobAsset
            };
        }
        
        private void CreateSceneLoadingRequests()
        {
            var entityManager = world.EntityManager;
            
            // Create entity to hold scene loading request for tracking
            var sceneLoaderEntity = entityManager.CreateEntity(
                typeof(RuntimeSceneLoadingRequest)
            );
            
            string sceneName = "";
            int sceneIndex = -1;
            
            if (!string.IsNullOrEmpty(terrainSceneName))
            {
                sceneName = terrainSceneName;
                if (enableDebugLogs)
                    Debug.Log($"Using scene name: {sceneName}");
            }
            else if (terrainSceneIndex >= 0)
            {
                sceneName = $"Scene_{terrainSceneIndex}";
                sceneIndex = terrainSceneIndex;
                if (enableDebugLogs)
                    Debug.Log($"Using scene index: {terrainSceneIndex}");
            }
            else
            {
                sceneName = "DefaultTerrainScene";
                if (enableDebugLogs)
                    Debug.Log("Using default scene name for tracking");
            }
            
            var loadingRequest = new RuntimeSceneLoadingRequest
            {
                SceneName = sceneName,
                SceneIndex = sceneIndex,
                UseUnitySceneManager = useUnitySceneLoading
            };
            
            entityManager.SetComponentData(sceneLoaderEntity, loadingRequest);
            
            if (enableDebugLogs)
                Debug.Log($"Created scene loading request for tracking: {sceneName}");
        }
        
        /// <summary>
        /// Check if terrain entities are available in the world.
        /// Useful for UI loading screens or game state management.
        /// </summary>
        public bool IsTerrainSystemAvailable()
        {
            // SAFETY: Check world validity before accessing EntityManager
            if (world == null || !world.IsCreated)
                return false;
                
            try
            {
                var entityManager = world.EntityManager;
                
                // Check if terrain generation entities exist
                using var query = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<TerrainGenerationData>()
                );
                
                return query.CalculateEntityCount() > 0;
            }
            catch (System.ObjectDisposedException)
            {
                // World was disposed, return false
                return false;
            }
            catch (System.Exception)
            {
                // Any other error, assume not available
                return false;
            }
        }
        
        /// <summary>
        /// Get count of terrain entities in the world.
        /// </summary>
        public int GetTerrainEntityCount()
        {
            // SAFETY: Check world validity before accessing EntityManager
            if (world == null || !world.IsCreated)
                return 0;
                
            try
            {
                var entityManager = world.EntityManager;
                
                using var query = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<TerrainGenerationData>()
                );
                
                return query.CalculateEntityCount();
            }
            catch (System.ObjectDisposedException)
            {
                // World was disposed, return 0
                return 0;
            }
            catch (System.Exception ex)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"Error getting terrain entity count: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get detailed system status information.
        /// </summary>
        public string GetSystemStatusInfo()
        {
            var info = $"Status: {currentStatus}\n";
            info += $"Initialized: {hasInitialized}\n";
            info += $"Terrain Entities: {terrainEntityCount}\n";
            info += $"Init Time: {initializationTime:F3}s\n";
            
            if (loaderSystem != null)
            {
                try
                {
                    info += $"Pending Scene Requests: {loaderSystem.HasPendingSceneRequests()}\n";
                }
                catch (System.Exception)
                {
                    info += "Pending Scene Requests: Unknown (system unavailable)\n";
                }
            }
            else
            {
                info += "Loader System: Not available\n";
            }
            
            if (world != null && world.IsCreated)
            {
                info += $"ECS World: Available\n";
            }
            else
            {
                info += $"ECS World: Not available\n";
            }
            
            return info;
        }
        
        /// <summary>
        /// Manually create a terrain generation request.
        /// </summary>
        [ContextMenu("Create Terrain Generation Request")]
        public void CreateTerrainGenerationRequest()
        {
            if (world == null)
            {
                Debug.LogWarning("World not initialized! Call InitializeRuntimeLoading() first.");
                return;
            }
            
            CreateDefaultTerrainEntityIfNeeded();
            UpdateTerrainEntityCount();
        }
        
        /// <summary>
        /// Force reload using Unity SceneManager (useful for development/testing).
        /// </summary>
        [ContextMenu("Reload Using Unity SceneManager")]
        public void ReloadUsingUnitySceneManager()
        {
            if (!string.IsNullOrEmpty(terrainSceneName))
            {
                var scene = SceneManager.GetSceneByName(terrainSceneName);
                if (scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                    if (enableDebugLogs)
                        Debug.Log($"Unloaded scene: {terrainSceneName}");
                }
                
                var asyncOp = SceneManager.LoadSceneAsync(terrainSceneName, LoadSceneMode.Additive);
                if (asyncOp != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Reloading scene: {terrainSceneName}");
                    
                    asyncOp.completed += (op) => UpdateTerrainEntityCount();
                }
            }
        }
        
        /// <summary>
        /// Update terrain entity count safely.
        /// </summary>
        public void UpdateTerrainEntityCount()
        {
            int newTerrainEntityCount = GetTerrainEntityCount();
            terrainEntityCount = newTerrainEntityCount;
            
            if (enableDebugLogs && hasInitialized)
                Debug.Log($"Terrain entity count updated: {terrainEntityCount}");
        }
        
        private float lastUpdateTime;
        
        // SAFETY: Update method should handle disposed world gracefully
        private void Update()
        {
            // Don't update if not initialized or world is invalid
            if (!hasInitialized || world == null || !world.IsCreated)
                return;
                
            try
            {
                // Update terrain entity count periodically
                if (Time.time - lastUpdateTime >= 1.0f) // Update every second
                {
                    UpdateTerrainEntityCount();
                    lastUpdateTime = Time.time;
                }
            }
            catch (System.ObjectDisposedException)
            {
                // World was disposed during test cleanup, this is expected
                hasInitialized = false;
                world = null;
            }
            catch (System.Exception ex)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"RuntimeTerrainManager Update error: {ex.Message}");
            }
        }
        
        private void OnDisable()
        {
            // Cleanup if needed
            hasInitialized = false;
            currentStatus = InitializationStatus.NotStarted;
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Show System Status")]
        private void ShowSystemStatus()
        {
            Debug.Log($"RuntimeTerrainManager Status:\n{GetSystemStatusInfo()}");
        }
        #endif
    }
}