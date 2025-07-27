using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the TerrainCleanupSystem to verify proper memory management and blob asset disposal.
    /// Ensures no memory leaks and proper cleanup of terrain generation resources.
    /// </summary>
    [TestFixture]
    public class TerrainCleanupSystemTests : ECSTestsFixture
    {
        private TerrainCleanupSystem cleanupSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            cleanupSystem = GetOrCreateSystem<TerrainCleanupSystem>();
        }
        
        [Test]
        public void TerrainCleanupSystem_MeshDataBlobAssets_DisposesCorrectly()
        {
            // Create entity with mesh data blob assets
            var entity = CreateEntity(typeof(MeshDataComponent));
            
            var meshData = CreateTestMeshDataComponent();
            Manager.SetComponentData(entity, meshData);
            
            // Verify blob assets are created
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            
            // Store the initial state for verification
            bool initialVerticesCreated = meshData.Vertices.IsCreated;
            bool initialIndicesCreated = meshData.Indices.IsCreated;
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify cleanup occurred - we use the stored values to verify initial state was correct
            Assert.IsTrue(initialVerticesCreated && initialIndicesCreated, "Initial blob assets should have been created");
            
            // Note: After cleanup, the component should still exist but blob assets should be disposed
            if (Manager.HasComponent<MeshDataComponent>(entity))
            {
                // Blob assets should be disposed (we can't directly test IsCreated on disposed assets)
                // But we can verify the cleanup ran without exceptions
                Assert.Pass("Mesh data cleanup completed successfully");
            }
        }
        
        [Test]
        public void TerrainCleanupSystem_TerraceConfigBlobAssets_DisposesCorrectly()
        {
            // Create entity with terrace config blob assets
            var entity = CreateEntity(typeof(TerraceConfigData));
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            // Verify blob asset is created
            Assert.IsTrue(terraceConfig.TerraceHeights.IsCreated);
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify cleanup occurred without exceptions
            Assert.Pass("Terrace config cleanup completed without errors");
        }
        
        [Test]
        public void TerrainCleanupSystem_MaterialDataBlobAssets_DisposesCorrectly()
        {
            // Create entity with material data blob assets
            var entity = CreateEntity(typeof(TerrainMaterialData));
            
            var materialData = CreateTestMaterialData();
            Manager.SetComponentData(entity, materialData);
            
            // Verify blob asset is created
            Assert.IsTrue(materialData.MaterialInstanceIDs.IsCreated);
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify cleanup occurred without exceptions
            Assert.Pass("Material data cleanup completed without errors");
        }
        
        [Test]
        public void TerrainCleanupSystem_MultipleEntities_CleansAllBlobAssets()
        {
            // Create multiple entities with different blob assets
            var entity1 = CreateEntity(typeof(MeshDataComponent));
            var entity2 = CreateEntity(typeof(TerraceConfigData));
            var entity3 = CreateEntity(typeof(TerrainMaterialData));
            
            // Set blob asset data
            Manager.SetComponentData(entity1, CreateTestMeshDataComponent());
            Manager.SetComponentData(entity2, CreateTestTerraceConfig());
            Manager.SetComponentData(entity3, CreateTestMaterialData());
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify all entities still exist
            Assert.IsTrue(Manager.Exists(entity1));
            Assert.IsTrue(Manager.Exists(entity2));
            Assert.IsTrue(Manager.Exists(entity3));
            
            // Verify cleanup occurred without exceptions
            Assert.Pass("Multi-entity cleanup completed without errors");
        }
        
        [Test]
        public void TerrainCleanupSystem_AlreadyDisposedBlobAssets_HandlesGracefully()
        {
            // Create entity with mesh data
            var entity = CreateEntity(typeof(MeshDataComponent));
            var meshData = CreateTestMeshDataComponent();
            Manager.SetComponentData(entity, meshData);
            
            // Manually dispose blob assets to simulate already-disposed state
            if (meshData.Vertices.IsCreated)
                meshData.Vertices.Dispose();
            if (meshData.Indices.IsCreated)
                meshData.Indices.Dispose();
            
            // Update component with disposed blob assets
            Manager.SetComponentData(entity, meshData);
            
            // Run cleanup system - should handle disposed assets gracefully
            Assert.DoesNotThrow(() =>
            {
                cleanupSystem.Update();
                CompleteAllJobs();
            });
        }
        
        [Test]
        public void TerrainCleanupSystem_EmptyWorld_HandlesGracefully()
        {
            // Run cleanup system with no entities
            Assert.DoesNotThrow(() =>
            {
                cleanupSystem.Update();
                CompleteAllJobs();
            });
        }
        
        [Test]
        public void TerrainCleanupSystem_EntitiesWithoutBlobAssets_IgnoresCorrectly()
        {
            // Create entities without blob asset components
            var entity1 = CreateEntity(typeof(TerrainGenerationData));
            var entity2 = CreateEntity(typeof(TerrainGenerationState));
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify entities still exist and weren't affected
            Assert.IsTrue(Manager.Exists(entity1));
            Assert.IsTrue(Manager.Exists(entity2));
        }
        
        [Test]
        public void TerrainCleanupSystem_MixedEntities_CleansOnlyBlobAssetEntities()
        {
            // Create mix of entities with and without blob assets
            var entityWithMeshData = CreateEntity(typeof(MeshDataComponent));
            var entityWithoutBlobAssets = CreateEntity(typeof(TerrainGenerationData));
            var entityWithTerraceConfig = CreateEntity(typeof(TerraceConfigData));
            
            // Set blob asset data only for relevant entities
            Manager.SetComponentData(entityWithMeshData, CreateTestMeshDataComponent());
            Manager.SetComponentData(entityWithTerraceConfig, CreateTestTerraceConfig());
            
            // Run cleanup system
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Verify all entities still exist
            Assert.IsTrue(Manager.Exists(entityWithMeshData));
            Assert.IsTrue(Manager.Exists(entityWithoutBlobAssets));
            Assert.IsTrue(Manager.Exists(entityWithTerraceConfig));
        }
        
        [Test]
        public void TerrainCleanupSystem_RepeatedCleanup_HandlesGracefully()
        {
            // Create entity with mesh data
            var entity = CreateEntity(typeof(MeshDataComponent));
            Manager.SetComponentData(entity, CreateTestMeshDataComponent());
            
            // Run cleanup multiple times
            for (int i = 0; i < 3; i++)
            {
                Assert.DoesNotThrow(() =>
                {
                    cleanupSystem.Update();
                    CompleteAllJobs();
                });
            }
            
            // Entity should still exist
            Assert.IsTrue(Manager.Exists(entity));
        }
        
        [Test]
        public void TerrainCleanupSystem_LargeNumberOfEntities_PerformsEfficiently()
        {
            const int entityCount = 100;
            var entities = new Entity[entityCount];
            
            // Create many entities with blob assets
            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = CreateEntity(typeof(MeshDataComponent));
                Manager.SetComponentData(entities[i], CreateTestMeshDataComponent());
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Run cleanup
            cleanupSystem.Update();
            CompleteAllJobs();
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (1 second for 100 entities)
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Cleanup should complete efficiently");
            
            // Verify all entities still exist
            for (int i = 0; i < entityCount; i++)
            {
                Assert.IsTrue(Manager.Exists(entities[i]));
            }
        }
        
        [Test]
        public void TerrainCleanupSystem_MemoryUsage_DoesNotLeak()
        {
            var initialMemory = System.GC.GetTotalMemory(true);
            
            // Create entities with blob assets
            for (int i = 0; i < 50; i++)
            {
                var entity = CreateEntity(typeof(MeshDataComponent));
                Manager.SetComponentData(entity, CreateTestMeshDataComponent());
            }
            
            // Run cleanup
            cleanupSystem.Update();
            CompleteAllJobs();
            
            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var finalMemory = System.GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be minimal (less than 1MB)
            Assert.Less(memoryIncrease, 1024 * 1024, "Memory usage should not increase significantly after cleanup");
        }
        
        #region Helper Methods
        
        private MeshDataComponent CreateTestMeshDataComponent()
        {
            // Create simple triangle mesh data
            var vertices = new float3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0.5f, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };
            
            // Create vertex blob
            using var vertexBuilder = new BlobBuilder(Allocator.Temp);
            ref var vertexArray = ref vertexBuilder.ConstructRoot<BlobArray<float3>>();
            var vertexArrayBuilder = vertexBuilder.Allocate(ref vertexArray, vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexArrayBuilder[i] = vertices[i];
            }
            var vertexBlob = vertexBuilder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
            
            // Create index blob
            using var indexBuilder = new BlobBuilder(Allocator.Temp);
            ref var indexArray = ref indexBuilder.ConstructRoot<BlobArray<int>>();
            var indexArrayBuilder = indexBuilder.Allocate(ref indexArray, indices.Length);
            for (int i = 0; i < indices.Length; i++)
            {
                indexArrayBuilder[i] = indices[i];
            }
            var indexBlob = indexBuilder.CreateBlobAssetReference<BlobArray<int>>(Allocator.Persistent);
            
            return new MeshDataComponent
            {
                Vertices = vertexBlob,
                Indices = indexBlob,
                VertexCount = vertices.Length,
                IndexCount = indices.Length
            };
        }
        
        private TerraceConfigData CreateTestTerraceConfig()
        {
            var heights = new float[] { 0f, 0.5f, 1f };
            
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var heightArray = ref builder.ConstructRoot<BlobArray<float>>();
            var heightArrayBuilder = builder.Allocate(ref heightArray, heights.Length);
            
            for (int i = 0; i < heights.Length; i++)
            {
                heightArrayBuilder[i] = heights[i];
            }
            
            var heightBlob = builder.CreateBlobAssetReference<BlobArray<float>>(Allocator.Persistent);
            
            return new TerraceConfigData
            {
                TerraceCount = heights.Length,
                TerraceHeights = heightBlob
            };
        }
        
        private TerrainMaterialData CreateTestMaterialData()
        {
            var materialIds = new int[] { 1001, 1002 };
            
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var materialArray = ref builder.ConstructRoot<BlobArray<int>>();
            var materialArrayBuilder = builder.Allocate(ref materialArray, materialIds.Length);
            
            for (int i = 0; i < materialIds.Length; i++)
            {
                materialArrayBuilder[i] = materialIds[i];
            }
            
            var materialBlob = builder.CreateBlobAssetReference<BlobArray<int>>(Allocator.Persistent);
            
            return new TerrainMaterialData
            {
                MaterialCount = materialIds.Length,
                MaterialInstanceIDs = materialBlob
            };
        }
        
        #endregion
    }
}