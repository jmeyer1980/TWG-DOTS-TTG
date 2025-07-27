using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: ECS
    /// @Domain: Runtime
    /// @Role: Scene Loading
    /// 
    /// System responsible for loading subscenes at runtime in builds.
    /// Editor uses live linking, but builds require explicit subscene loading.
    /// This system processes RuntimeSceneLoadingRequest components and coordinates
    /// with Unity's scene loading system for proper runtime initialization.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class RuntimeEntityLoaderSystem : SystemBase
    {
        private bool hasLoadedInitialScenes = false;
        private EntityQuery sceneLoadingQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Create query for scene loading requests
            sceneLoadingQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingRequest>());
            
            // Only run this system when there are scene loading requests
            RequireForUpdate(sceneLoadingQuery);
        }
        
        protected override void OnUpdate()
        {
            // Only load scenes once at startup
            if (hasLoadedInitialScenes)
                return;
                
            hasLoadedInitialScenes = true;
            
            Debug.Log("RuntimeEntityLoaderSystem: Processing scene loading requests for runtime build...");
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Process all scene loading requests
            Entities
                .WithAll<RuntimeSceneLoadingRequest>()
                .ForEach((Entity entity, in RuntimeSceneLoadingRequest request) =>
                {
                    Debug.Log($"Processing scene loading request for: {request.SceneName}");
                    
                    // For now, this system primarily logs and tracks scene loading requests
                    // The actual scene loading is handled by RuntimeTerrainManager using Unity's SceneManager
                    // In future versions, this could be enhanced to use Unity's ECS SceneSystem directly
                    
                    // Mark this request as processed by removing the component
                    ecb.RemoveComponent<RuntimeSceneLoadingRequest>(entity);
                    
                    // Add a tag to indicate processing is complete
                    ecb.AddComponent<RuntimeSceneLoadingProcessed>(entity);
                    
                    Debug.Log($"Scene loading request processed for: {request.SceneName}");
                    
                }).WithoutBurst().Run();
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
            
            // Log system status
            Debug.Log("RuntimeEntityLoaderSystem: Scene loading request processing complete.");
        }
        
        /// <summary>
        /// Check if any scene loading requests are still pending.
        /// </summary>
        public bool HasPendingSceneRequests()
        {
            return sceneLoadingQuery.CalculateEntityCount() > 0;
        }
        
        /// <summary>
        /// Get count of processed scene loading requests.
        /// </summary>
        public int GetProcessedSceneCount()
        {
            using var processedQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingProcessed>());
            return processedQuery.CalculateEntityCount();
        }
    }
    
    /// <summary>
    /// Component to request runtime loading of a subscene.
    /// Add this component to an entity with scene references you want loaded at runtime.
    /// </summary>
    public struct RuntimeSceneLoadingRequest : IComponentData
    {
        public FixedString64Bytes SceneName;
        public int SceneIndex; // Use scene index as fallback
        public bool UseUnitySceneManager; // Flag to use traditional Unity scene loading
    }
    
    /// <summary>
    /// Tag component indicating that a scene loading request has been processed.
    /// </summary>
    public struct RuntimeSceneLoadingProcessed : IComponentData
    {
        public float ProcessedTime; // Time when the request was processed
    }
}