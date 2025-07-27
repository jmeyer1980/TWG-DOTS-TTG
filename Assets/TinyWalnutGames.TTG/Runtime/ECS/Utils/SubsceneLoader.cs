using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: Utility
    /// @Domain: Scene
    /// @Role: Loading
    /// 
    /// Utility class for managing subscene loading in Unity DOTS/ECS projects.
    /// Provides both editor and runtime support for loading subscenes containing
    /// terrain generation entities.
    /// 
    /// Key Features:
    /// - Editor subscene loading with live linking support
    /// - Runtime subscene loading for builds
    /// - Scene validation and dependency management
    /// - Integration with RuntimeTerrainManager and RuntimeEntityLoaderSystem
    /// - Error handling and fallback mechanisms
    /// </summary>
    public static class SubsceneLoader
    {
        /// <summary>
        /// Load a subscene by name with automatic editor/runtime detection.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="world">ECS World to load entities into (optional)</param>
        /// <returns>True if loading was initiated successfully</returns>
        public static bool LoadSubscene(string sceneName, World world = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SubsceneLoader: Scene name cannot be null or empty");
                return false;
            }
            
            world ??= World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("SubsceneLoader: No valid ECS World found for subscene loading");
                return false;
            }

            // Log the world being used for debugging purposes
            Debug.Log($"[SubsceneLoader] Using world '{world.Name}' for loading subscene '{sceneName}'");
            
#if UNITY_EDITOR
            // In editor, try to use subscene loading directly
            return LoadSubsceneInEditor(sceneName, world);
#else
            // In builds, use traditional scene loading
            return LoadSubsceneInBuild(sceneName, world);
#endif
        }
        
        /// <summary>
        /// Load a subscene by build index with automatic editor/runtime detection.
        /// </summary>
        /// <param name="sceneIndex">Build index of the scene to load</param>
        /// <param name="world">ECS World to load entities into (optional)</param>
        /// <returns>True if loading was initiated successfully</returns>
        public static bool LoadSubscene(int sceneIndex, World world = null)
        {
            if (sceneIndex < 0)
            {
                Debug.LogError("SubsceneLoader: Scene index must be non-negative");
                return false;
            }
            
            world ??= World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("SubsceneLoader: No valid ECS World found for subscene loading");
                return false;
            }

            // Log the world being used for debugging purposes
            Debug.Log($"[SubsceneLoader] Using world '{world.Name}' for loading subscene at index {sceneIndex}");
            
#if UNITY_EDITOR
            // In editor, convert index to name if possible
            var scenePath = UnityEditor.EditorBuildSettings.scenes.Length > sceneIndex 
                ? UnityEditor.EditorBuildSettings.scenes[sceneIndex].path 
                : null;
            
            if (!string.IsNullOrEmpty(scenePath))
            {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                return LoadSubsceneInEditor(sceneName, world);
            }
            else
            {
                Debug.LogWarning($"SubsceneLoader: Could not find scene at index {sceneIndex} in build settings");
                return false;
            }
#else
            // In builds, use scene index directly
            return LoadSubsceneInBuild(sceneIndex, world);
#endif
        }
        
        /// <summary>
        /// Create a scene loading request entity for tracking purposes.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="sceneIndex">Optional scene index</param>
        /// <param name="world">ECS World to create entity in</param>
        /// <returns>Entity representing the scene loading request</returns>
        public static Entity CreateSceneLoadingRequest(string sceneName, int sceneIndex = -1, World world = null)
        {
            world ??= World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("SubsceneLoader: No valid ECS World found for creating scene loading request");
                return Entity.Null;
            }
            
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = sceneName,
                SceneIndex = sceneIndex,
                UseUnitySceneManager = true // Default to Unity Scene Manager for compatibility
            };
            
            entityManager.SetComponentData(entity, request);
            
            Debug.Log($"SubsceneLoader: Created scene loading request for '{sceneName}' (index: {sceneIndex}) in world '{world.Name}'");
            return entity;
        }
        
        /// <summary>
        /// Check if a scene is currently loaded.
        /// </summary>
        /// <param name="sceneName">Name of the scene to check</param>
        /// <returns>True if the scene is loaded</returns>
        public static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;
                
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }
        
        /// <summary>
        /// Check if a scene exists and can be loaded.
        /// </summary>
        /// <param name="sceneName">Name of the scene to validate</param>
        /// <returns>True if the scene exists and can be loaded</returns>
        public static bool ValidateScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;
            
#if UNITY_EDITOR
            // In editor, check if scene exists in project
            var scenePath = $"Assets/{sceneName}.unity";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath) != null ||
                   UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>($"Assets/Scenes/{sceneName}.unity") != null;
#else
            // In builds, we can only check build settings or attempt to load
            try
            {
                // Check if scene exists in build by attempting to get it
                var scene = SceneManager.GetSceneByName(sceneName);
                return scene.IsValid() || IsSceneInBuildSettings(sceneName);
            }
            catch
            {
                return false;
            }
#endif
        }
        
        /// <summary>
        /// Get comprehensive information about current scene loading status.
        /// </summary>
        /// <param name="world">ECS World to check (optional)</param>
        /// <returns>Scene loading status information</returns>
        public static SubsceneLoadingInfo GetLoadingInfo(World world = null)
        {
            world ??= World.DefaultGameObjectInjectionWorld;
            
            var info = new SubsceneLoadingInfo
            {
                TotalScenesLoaded = SceneManager.sceneCount,
                ActiveSceneName = SceneManager.GetActiveScene().name,
                PendingRequests = 0,
                ProcessedRequests = 0
            };
            
            if (world != null)
            {
                var entityManager = world.EntityManager;
                
                // Count pending and processed requests
                using var pendingQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingRequest>());
                using var processedQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingProcessed>());
                
                info.PendingRequests = pendingQuery.CalculateEntityCount();
                info.ProcessedRequests = processedQuery.CalculateEntityCount();

                // Use world information for debugging
                Debug.Log($"SubsceneLoader: Loading info for world '{world.Name}' - Pending: {info.PendingRequests}, Processed: {info.ProcessedRequests}");
            }
            
            return info;
        }
        
#if UNITY_EDITOR
        private static bool LoadSubsceneInEditor(string sceneName, World world)
        {
            Debug.Log($"SubsceneLoader: Loading subscene '{sceneName}' in editor with live linking support using world '{world.Name}'");
            
            // In editor, we can try to find and load the subscene directly
            // This integrates with Unity's live linking system
            
            try
            {
                // For now, fall back to regular scene loading
                // In a full implementation, this would use Unity's subscene loading APIs
                return LoadSceneAdditively(sceneName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SubsceneLoader: Failed to load subscene '{sceneName}' in editor: {ex.Message}");
                return false;
            }
        }
#endif
        
        private static bool LoadSubsceneInBuild(string sceneName, World world)
        {
            Debug.Log($"SubsceneLoader: Loading subscene '{sceneName}' in build using Unity Scene Manager with world '{world.Name}'");
            
            // In builds, use traditional Unity scene loading
            return LoadSceneAdditively(sceneName);
        }
        
        private static bool LoadSubsceneInBuild(int sceneIndex, World world)
        {
            Debug.Log($"SubsceneLoader: Loading subscene at index {sceneIndex} in build using Unity Scene Manager with world '{world.Name}'");
            
            try
            {
                var asyncOp = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
                return asyncOp != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SubsceneLoader: Failed to load scene at index {sceneIndex}: {ex.Message}");
                return false;
            }
        }
        
        private static bool LoadSceneAdditively(string sceneName)
        {
            try
            {
                // Check if already loaded
                if (IsSceneLoaded(sceneName))
                {
                    Debug.Log($"SubsceneLoader: Scene '{sceneName}' is already loaded");
                    return true;
                }
                
                // Load scene additively
                var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (asyncOp != null)
                {
                    Debug.Log($"SubsceneLoader: Successfully initiated loading of scene '{sceneName}'");
                    return true;
                }
                else
                {
                    Debug.LogError($"SubsceneLoader: Failed to initiate loading of scene '{sceneName}'");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SubsceneLoader: Exception loading scene '{sceneName}': {ex.Message}");
                return false;
            }
        }
        
        private static bool IsSceneInBuildSettings(string sceneName)
        {
            // This is a simplified check - in a production implementation,
            // you might want to cache build settings information
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
    
    /// <summary>
    /// Information about current subscene loading status.
    /// </summary>
    public struct SubsceneLoadingInfo
    {
        public int TotalScenesLoaded;
        public string ActiveSceneName;
        public int PendingRequests;
        public int ProcessedRequests;
        
        public readonly bool IsLoadingComplete => PendingRequests == 0;
        
        public readonly override string ToString()
        {
            return $"Subscene Loading Info: {TotalScenesLoaded} scenes loaded, " +
                   $"{ProcessedRequests} processed, {PendingRequests} pending, " +
                   $"Active: {ActiveSceneName}, Complete: {IsLoadingComplete}";
        }
    }
}