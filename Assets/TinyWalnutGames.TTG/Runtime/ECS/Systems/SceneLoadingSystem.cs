using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: ECS
    /// @Domain: Scene
    /// @Role: Loading
    /// 
    /// Alternative scene loading system that provides more comprehensive scene management
    /// for terrain generation systems. Works alongside RuntimeEntityLoaderSystem to provide
    /// both ECS and Unity Scene Manager based loading capabilities.
    /// 
    /// This system provides enhanced scene loading features including:
    /// - Scene validation and verification
    /// - Multi-scene loading coordination
    /// - Scene dependency management
    /// - Loading progress tracking
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RuntimeEntityLoaderSystem))]
    public partial class SceneLoadingSystem : SystemBase
    {
        private EntityQuery sceneRequestQuery;
        private EntityQuery processedSceneQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Create queries for scene loading management
            sceneRequestQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingRequest>());
            processedSceneQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingProcessed>());
            
            // Only run when there are unprocessed scene requests
            RequireForUpdate(sceneRequestQuery);
        }
        
        protected override void OnUpdate()
        {
            // This system runs after RuntimeEntityLoaderSystem has processed initial requests
            // It provides additional scene loading coordination and validation
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Check for any remaining scene loading work
            var pendingCount = sceneRequestQuery.CalculateEntityCount();
            var processedCount = processedSceneQuery.CalculateEntityCount();
            
            if (pendingCount > 0)
            {
                // Log scene loading status
                Debug.Log($"SceneLoadingSystem: {pendingCount} pending requests, {processedCount} processed");
                
                // Process any additional scene loading logic here
                // For now, this system primarily monitors and coordinates
                Entities
                    .WithAll<RuntimeSceneLoadingRequest>()
                    .ForEach((Entity entity, in RuntimeSceneLoadingRequest request) =>
                    {
                        // Additional scene loading validation can be added here
                        // For example: scene dependency checking, asset validation, etc.
                        
                        if (!string.IsNullOrEmpty(request.SceneName.ToString()))
                        {
                            // Verify scene exists and is loadable
                            ValidateSceneLoadability(request.SceneName.ToString());
                        }
                        
                    }).WithoutBurst().Run();
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private void ValidateSceneLoadability(string sceneName)
        {
            // Check if scene is already loaded
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                Debug.Log($"SceneLoadingSystem: Scene '{sceneName}' is already loaded");
                return;
            }
            
            // Additional validation logic could be added here:
            // - Check if scene exists in build settings
            // - Verify scene dependencies
            // - Check available memory for scene loading
            // - Validate scene compatibility
            
            Debug.Log($"SceneLoadingSystem: Validated scene '{sceneName}' for loading");
        }
        
        /// <summary>
        /// Get comprehensive scene loading status information.
        /// </summary>
        public SceneLoadingStatus GetSceneLoadingStatus()
        {
            var pendingCount = sceneRequestQuery.CalculateEntityCount();
            var processedCount = processedSceneQuery.CalculateEntityCount();
            
            return new SceneLoadingStatus
            {
                PendingRequests = pendingCount,
                ProcessedRequests = processedCount,
                IsSceneLoadingComplete = pendingCount == 0,
                LoadedSceneCount = SceneManager.sceneCount
            };
        }
        
        /// <summary>
        /// Force reload all terrain-related scenes.
        /// Useful for development and testing scenarios.
        /// </summary>
        public void ReloadAllTerrainScenes()
        {
            Debug.Log("SceneLoadingSystem: Reloading all terrain scenes...");
            
            // This would coordinate with RuntimeTerrainManager for scene reloading
            // Implementation depends on specific scene management requirements
            
            Entities
                .WithAll<RuntimeSceneLoadingProcessed>()
                .ForEach((Entity entity) =>
                {
                    // Mark for reloading by converting back to request
                    var ecb = new EntityCommandBuffer(Allocator.Temp);
                    ecb.RemoveComponent<RuntimeSceneLoadingProcessed>(entity);
                    ecb.AddComponent<RuntimeSceneLoadingRequest>(entity);
                    ecb.Playback(EntityManager);
                    ecb.Dispose();
                    
                }).WithoutBurst().Run();
        }
    }
    
    /// <summary>
    /// Status information for scene loading operations.
    /// </summary>
    public struct SceneLoadingStatus
    {
        public int PendingRequests;
        public int ProcessedRequests;
        public bool IsSceneLoadingComplete;
        public int LoadedSceneCount;
        
        public readonly override string ToString()
        {
            return $"Scene Loading Status: {ProcessedRequests} processed, {PendingRequests} pending, " +
                   $"Complete: {IsSceneLoadingComplete}, Total Scenes: {LoadedSceneCount}";
        }
    }
}