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
        private EntityQuery sceneLoadingQuery;
        private EntityQuery processedSceneQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Create query for scene loading requests
            sceneLoadingQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingRequest>());
            
            // Create query for processed scenes (managed by system, not disposable)
            processedSceneQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeSceneLoadingProcessed>());
            
            // Only run this system when there are scene loading requests
            RequireForUpdate(sceneLoadingQuery);
        }
        
        protected override void OnUpdate()
        {
            // Check if there are pending requests
            var pendingCount = sceneLoadingQuery.CalculateEntityCount();
            if (pendingCount == 0)
                return;
                
            Debug.Log("RuntimeEntityLoaderSystem: Processing scene loading requests for runtime build...");
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Process all scene loading requests efficiently in batch
            Entities
                .WithAll<RuntimeSceneLoadingRequest>()
                .WithName("ProcessSceneLoadingRequests")
                .ForEach((Entity entity, in RuntimeSceneLoadingRequest request) =>
                {
                    Debug.Log($"Processing scene loading request for: {request.SceneName}");
                    
                    // For now, this system primarily logs and tracks scene loading requests
                    // The actual scene loading is handled by RuntimeTerrainManager using Unity's SceneManager
                    // In future versions, this could be enhanced to use Unity's ECS SceneSystem directly
                    
                    // Mark this request as processed by removing the component
                    ecb.RemoveComponent<RuntimeSceneLoadingRequest>(entity);
                    
                    // Add a tag to indicate processing is complete with timestamp
                    ecb.AddComponent(entity, new RuntimeSceneLoadingProcessed
                    {
                        ProcessedTime = (float)SystemAPI.Time.ElapsedTime
                    });
                    
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
        /// FIXED: Use system-managed query instead of creating disposable query.
        /// </summary>
        public int GetProcessedSceneCount()
        {
            // Use the system-managed query instead of creating a disposable one
            return processedSceneQuery.CalculateEntityCount();
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