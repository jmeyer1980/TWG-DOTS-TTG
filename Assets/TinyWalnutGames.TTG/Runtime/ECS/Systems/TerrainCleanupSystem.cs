using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// System responsible for cleaning up blob assets and preventing memory leaks.
    /// Runs after all other terrain systems to ensure proper disposal of resources.
    /// AGGRESSIVE MODE: Runs more frequently to catch authoring blob asset leaks.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(MeshCreationSystem))]
    public partial class TerrainCleanupSystem : SystemBase
    {
        private EntityQuery destroyedEntitiesQuery;
        private EntityQuery completeEntitiesQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Query for entities that have been marked for destruction but still have blob assets
            destroyedEntitiesQuery = GetEntityQuery(
                ComponentType.ReadOnly<MeshDataComponent>(),
                ComponentType.Exclude<TerrainGenerationState>()
            );
            
            // Query for entities that completed generation and may need cleanup
            completeEntitiesQuery = GetEntityQuery(
                ComponentType.ReadOnly<MeshDataComponent>(),
                ComponentType.ReadOnly<TerrainGenerationState>()
            );
        }
        
        protected override void OnUpdate()
        {
            // Clean up entities marked for mesh data cleanup  
            CleanupMarkedEntities();
            
            // Clean up orphaned mesh data (entities without generation state)
            CleanupOrphanedMeshData();
            
            // Clean up completed entities that are marked for destruction
            CleanupCompletedEntities();
            
            // Clean up entities with error states
            CleanupErrorEntities();
            
            // CRITICAL AUTHORING FIX: Clean up authoring blob assets aggressively
            CleanupAuthoringBlobAssets();
            
            // ULTRA-AGGRESSIVE: Clean up ANY blob assets not actively being used
            CleanupAnyUnusedBlobAssets();
        }
        
        protected override void OnDestroy()
        {
            // Force cleanup of all remaining blob assets
            CleanupAllBlobAssets();
            base.OnDestroy();
        }
        
        private void CleanupMarkedEntities()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Clean up entities explicitly marked for cleanup
            Entities
                .WithAll<MeshDataComponent, CleanupMeshDataTag>()
                .ForEach((Entity entity, in MeshDataComponent meshData) =>
                {
                    DisposeMeshData(meshData);
                    ecb.RemoveComponent<MeshDataComponent>(entity);
                    ecb.RemoveComponent<CleanupMeshDataTag>(entity);
                    
                    Debug.Log($"Cleaned up mesh data for entity {entity}");
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private void CleanupOrphanedMeshData()
        {
            Entities
                .WithAll<MeshDataComponent>()
                .WithNone<TerrainGenerationState>()
                .ForEach((Entity entity, in MeshDataComponent meshData) =>
                {
                    // This entity has mesh data but no generation state - likely orphaned
                    DisposeMeshData(meshData);
                    EntityManager.RemoveComponent<MeshDataComponent>(entity);
                }).WithStructuralChanges().Run();
        }
        
        private void CleanupCompletedEntities()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in MeshDataComponent meshData, in TerrainGenerationState state) =>
                {
                    // Clean up entities that are complete and have been processed
                    if (state.IsComplete && EntityManager.HasComponent<GeneratedTerrainMeshTag>(entity))
                    {
                        // Check if this entity should be cleaned up (you can add your own logic here)
                        // For now, we'll clean up after the mesh has been created
                        
                        // Dispose blob assets since the mesh has been converted to Unity Mesh
                        DisposeMeshData(meshData);
                        ecb.RemoveComponent<MeshDataComponent>(entity);
                    }
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private void CleanupErrorEntities()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in MeshDataComponent meshData, in TerrainGenerationState state) =>
                {
                    // Clean up entities that encountered errors
                    if (state.HasError)
                    {
                        DisposeMeshData(meshData);
                        ecb.RemoveComponent<MeshDataComponent>(entity);
                        
                        // Optionally log the error
                        Debug.LogWarning($"Cleaning up entity {entity} due to generation error");
                    }
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private void CleanupAllBlobAssets()
        {
            // Emergency cleanup - dispose all remaining blob assets
            Entities
                .WithAll<MeshDataComponent>()
                .ForEach((in MeshDataComponent meshData) =>
                {
                    DisposeMeshData(meshData);
                }).WithoutBurst().Run();
                
            Entities
                .WithAll<TerraceConfigData>()
                .ForEach((in TerraceConfigData terraceConfig) =>
                {
                    DisposeTerraceConfig(terraceConfig);
                }).WithoutBurst().Run();
        }
        
        private void CleanupAuthoringBlobAssets()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // AGGRESSIVE CLEANUP: Clean up entities that have authoring blob assets but no active generation
            Entities
                .WithAll<TerraceConfigData>()
                .WithNone<TerrainGenerationState, TerrainGenerationRequest>()
                .ForEach((Entity entity, in TerraceConfigData terraceConfig) =>
                {
                    // This entity has terrace config but no active generation - clean it up
                    DisposeTerraceConfig(terraceConfig);
                    ecb.RemoveComponent<TerraceConfigData>(entity);
                    
                    Debug.Log($"Cleaned up orphaned terrace config for entity {entity}");
                }).WithoutBurst().Run();
                
            // AGGRESSIVE CLEANUP: Clean up material data that's no longer needed
            Entities
                .WithAll<TerrainMaterialData>()
                .WithNone<TerrainGenerationState, TerrainGenerationRequest>()
                .ForEach((Entity entity, in TerrainMaterialData materialData) =>
                {
                    // This entity has material data but no active generation - clean it up
                    DisposeMaterialData(materialData);
                    ecb.RemoveComponent<TerrainMaterialData>(entity);
                    
                    Debug.Log($"Cleaned up orphaned material data for entity {entity}");
                }).WithoutBurst().Run();
                
            // NEW: Also clean up any entities with mesh data but completed generation
            Entities
                .WithAll<MeshDataComponent, GeneratedTerrainMeshTag>()
                .ForEach((Entity entity, in MeshDataComponent meshData) =>
                {
                    // This entity has mesh data but generation is complete - clean it up
                    DisposeMeshData(meshData);
                    ecb.RemoveComponent<MeshDataComponent>(entity);
                    
                    Debug.Log($"Cleaned up completed mesh data for entity {entity}");
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private void CleanupAnyUnusedBlobAssets()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // ULTRA-AGGRESSIVE APPROACH: Clean up any terrace config data on entities that aren't actively generating
            Entities
                .WithAll<TerraceConfigData>()
                .ForEach((Entity entity, in TerraceConfigData terraceConfig) =>
                {
                    // Check if this entity is actively in generation pipeline
                    bool isActivelyGenerating = EntityManager.HasComponent<TerrainGenerationState>(entity) && 
                                              EntityManager.HasComponent<TerrainGenerationRequest>(entity);
                    
                    if (!isActivelyGenerating)
                    {
                        // If not actively generating, check if generation is complete
                        bool hasGenerationState = EntityManager.HasComponent<TerrainGenerationState>(entity);
                        if (hasGenerationState)
                        {
                            var state = EntityManager.GetComponentData<TerrainGenerationState>(entity);
                            if (state.IsComplete || state.HasError)
                            {
                                // Generation is complete or errored - clean up terrace config
                                DisposeTerraceConfig(terraceConfig);
                                ecb.RemoveComponent<TerraceConfigData>(entity);
                                Debug.Log($"ULTRA-CLEANUP: Disposed terrace config for completed/errored entity {entity}");
                            }
                        }
                        else
                        {
                            // No generation state at all - definitely orphaned
                            DisposeTerraceConfig(terraceConfig);
                            ecb.RemoveComponent<TerraceConfigData>(entity);
                            Debug.Log($"ULTRA-CLEANUP: Disposed orphaned terrace config for entity {entity}");
                        }
                    }
                }).WithoutBurst().Run();
                
            // Same for material data
            Entities
                .WithAll<TerrainMaterialData>()
                .ForEach((Entity entity, in TerrainMaterialData materialData) =>
                {
                    bool isActivelyGenerating = EntityManager.HasComponent<TerrainGenerationState>(entity) && 
                                              EntityManager.HasComponent<TerrainGenerationRequest>(entity);
                    
                    if (!isActivelyGenerating)
                    {
                        bool hasGenerationState = EntityManager.HasComponent<TerrainGenerationState>(entity);
                        if (hasGenerationState)
                        {
                            var state = EntityManager.GetComponentData<TerrainGenerationState>(entity);
                            if (state.IsComplete || state.HasError)
                            {
                                DisposeMaterialData(materialData);
                                ecb.RemoveComponent<TerrainMaterialData>(entity);
                                Debug.Log($"ULTRA-CLEANUP: Disposed material data for completed/errored entity {entity}");
                            }
                        }
                        else
                        {
                            DisposeMaterialData(materialData);
                            ecb.RemoveComponent<TerrainMaterialData>(entity);
                            Debug.Log($"ULTRA-CLEANUP: Disposed orphaned material data for entity {entity}");
                        }
                    }
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        private static void DisposeMeshData(in MeshDataComponent meshData)
        {
            try
            {
                if (meshData.Vertices.IsCreated)
                {
                    meshData.Vertices.Dispose();
                }
            }
            catch (System.InvalidOperationException)
            {
                // Blob asset was already disposed - ignore
            }
            
            try
            {
                if (meshData.Indices.IsCreated)
                {
                    meshData.Indices.Dispose();
                }
            }
            catch (System.InvalidOperationException)
            {
                // Blob asset was already disposed - ignore
            }
        }
        
        private static void DisposeTerraceConfig(in TerraceConfigData terraceConfig)
        {
            try
            {
                if (terraceConfig.TerraceHeights.IsCreated)
                {
                    terraceConfig.TerraceHeights.Dispose();
                }
            }
            catch (System.InvalidOperationException)
            {
                // Blob asset was already disposed - ignore
            }
        }
        
        private static void DisposeMaterialData(in TerrainMaterialData materialData)
        {
            try
            {
                if (materialData.MaterialInstanceIDs.IsCreated)
                {
                    materialData.MaterialInstanceIDs.Dispose();
                }
            }
            catch (System.InvalidOperationException)
            {
                // Blob asset was already disposed - ignore
            }
        }
    }
}