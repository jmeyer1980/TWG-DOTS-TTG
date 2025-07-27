using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Performance benchmarks and edge case handling tests for terrain generation.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationPerformanceTests : ECSTestsFixture
    {
        private TerrainGenerationSystem terrainGenerationSystem;
        private MeshCreationSystem meshCreationSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            terrainGenerationSystem = GetOrCreateSystem<TerrainGenerationSystem>();
            meshCreationSystem = GetOrCreateSystem<MeshCreationSystem>();
        }
        
        [Test]
        public void Performance_SinglePlanarTerrain_CompletesInReasonableTime()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var entity = CreateSingleTerrainEntity(TerrainType.Planar);
            RunCompleteWorkflow(entity);
            
            stopwatch.Stop();
            
            // Should complete within 5 seconds
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Single planar terrain should complete within 5 seconds");
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void Performance_SingleSphericalTerrain_CompletesInReasonableTime()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var entity = CreateSingleTerrainEntity(TerrainType.Spherical);
            RunCompleteWorkflow(entity);
            
            stopwatch.Stop();
            
            // Should complete within 5 seconds
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Single spherical terrain should complete within 5 seconds");
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void Performance_MultipleTerrainsSimultaneous_ScalesLinearlyish()
        {
            const int terrainCount = 10;
            
            var stopwatch = Stopwatch.StartNew();
            
            var entities = new Entity[terrainCount];
            
            // Create multiple terrains
            for (int i = 0; i < terrainCount; i++)
            {
                entities[i] = CreateSingleTerrainEntity(i % 2 == 0 ? TerrainType.Planar : TerrainType.Spherical);
            }
            
            // Process all terrains
            RunCompleteWorkflowBatch(entities);
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (30 seconds for 10 terrains)
            Assert.Less(stopwatch.ElapsedMilliseconds, 30000, "10 terrains should complete within 30 seconds");
            
            // Clean up
            for (int i = 0; i < terrainCount; i++)
            {
                CleanupEntityMeshData(entities[i]);
            }
        }
        
        [Test]
        public void Performance_MemoryAllocation_WithinReasonableLimits()
        {
            var initialMemory = System.GC.GetTotalMemory(true);
            
            // Create and process multiple terrains
            var entities = new Entity[20];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = CreateSingleTerrainEntity(TerrainType.Planar);
            }
            
            RunCompleteWorkflowBatch(entities);
            
            var peakMemory = System.GC.GetTotalMemory(false);
            var memoryUsed = peakMemory - initialMemory;
            
            // Clean up
            for (int i = 0; i < entities.Length; i++)
            {
                CleanupEntityMeshData(entities[i]);
            }
            
            System.GC.Collect();
            var finalMemory = System.GC.GetTotalMemory(true);
            var memoryLeaked = finalMemory - initialMemory;
            
            // Assert reasonable memory usage (less than 50MB for 20 terrains)
            Assert.Less(memoryUsed, 50 * 1024 * 1024, "Memory usage for 20 terrains should be under 50MB");
            
            // Assert minimal memory leaks (less than 5MB)
            Assert.Less(memoryLeaked, 5 * 1024 * 1024, "Memory leak should be under 5MB");
        }
        
        [Test]
        public void Performance_LargeTerrainGeneration_CompletesSuccessfully()
        {
            // Test with high-detail terrain (many sides)
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 50f,
                Depth = 6, // High detail
                Sides = 10, // Maximum supported sides
                Radius = 100f, // Large radius
                Seed = 12345,
                BaseFrequency = 0.01f, // Fine detail
                Octaves = 8, // Many octaves
                Persistence = 0.5f,
                Lacunarity = 2f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateLargeTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Run complete workflow
            RunCompleteWorkflow(entity);
            
            stopwatch.Stop();
            
            // Verify completion within reasonable time (10 seconds max)
            Assert.Less(stopwatch.ElapsedMilliseconds, 10000, "Large terrain should complete within 10 seconds");
            
            // Verify mesh was created
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_MinimumSides_WorksCorrectly()
        {
            // Test with minimum number of sides (3)
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 3, // Minimum supported
                Radius = 5f,
                MaxHeight = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Should complete without error
            RunCompleteWorkflow(entity);
            
            // Verify results
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            
            // FIXED: Check component exists and validate triangle mesh
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity), "Entity should have MeshDataComponent after complete workflow");
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(3, meshData.VertexCount, "Triangle should have 3 vertices");
            Assert.AreEqual(3, meshData.IndexCount, "Triangle should have 3 indices");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_MaximumSides_WorksCorrectly()
        {
            // Test with maximum number of sides (10)
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 10, // Maximum supported
                Radius = 5f,
                MaxHeight = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Should complete without error
            RunCompleteWorkflow(entity);
            
            // Verify results
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            
            // FIXED: Check component exists and validate decagon mesh
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity), "Entity should have MeshDataComponent after complete workflow");
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(10, meshData.VertexCount, "Decagon should have 10 vertices");
            Assert.AreEqual(24, meshData.IndexCount, "Decagon should have 24 indices (8 triangles)");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ZeroRadius_HandledGracefully()
        {
            // Test with zero radius (should handle gracefully or use minimum)
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 0f, // Edge case
                MaxHeight = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Run workflow - should not crash
            RunCompleteWorkflow(entity);
            
            // Verify completion (may have error flag set, but shouldn't crash)
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_NegativeHeight_HandledCorrectly()
        {
            // Test with negative heights
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = -5f, // Negative
                MaxHeight = 10f,
                Depth = 3
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Should handle negative heights gracefully
            RunCompleteWorkflow(entity);
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ExtremeNoise_Parameters_WorkCorrectly()
        {
            // Test with extreme noise parameters
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f,
                MaxHeight = 10f,
                BaseFrequency = 10f, // Very high frequency
                Octaves = 1, // Minimum octaves
                Persistence = 0.1f, // Low persistence
                Lacunarity = 10f // High lacunarity
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Should handle extreme parameters
            RunCompleteWorkflow(entity);
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_EmptyTerraceHeights_HandledCorrectly()
        {
            // Test with no terrace heights
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f,
                MaxHeight = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            // Create empty terrace config
            var terraceConfig = CreateEmptyTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Should handle empty terraces gracefully
            RunCompleteWorkflow(entity);
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [UnityTest]
        public IEnumerator StressTest_ManyTerrainsOverTime_StablePerformance()
        {
            const int batchSize = 5;
            const int batchCount = 10;
            var processingTimes = new float[batchCount];
            
            for (int batch = 0; batch < batchCount; batch++)
            {
                var startTime = Time.realtimeSinceStartup;
                
                // Create batch of terrains
                var entities = new Entity[batchSize];
                for (int i = 0; i < batchSize; i++)
                {
                    entities[i] = CreateSingleTerrainEntity(i % 2 == 0 ? TerrainType.Planar : TerrainType.Spherical);
                }
                
                // Process batch
                RunCompleteWorkflowBatch(entities);
                
                var endTime = Time.realtimeSinceStartup;
                processingTimes[batch] = endTime - startTime;
                
                // Clean up
                for (int i = 0; i < batchSize; i++)
                {
                    CleanupEntityMeshData(entities[i]);
                    if (Manager.HasComponent<TerrainGenerationState>(entities[i]))
                    {
                        var state = Manager.GetComponentData<TerrainGenerationState>(entities[i]);
                        if (state.ResultMeshEntity != Entity.Null && Manager.Exists(state.ResultMeshEntity))
                        {
                            if (Manager.HasComponent<MeshGameObjectReference>(state.ResultMeshEntity))
                            {
                                var meshRef = Manager.GetComponentData<MeshGameObjectReference>(state.ResultMeshEntity);
                                #if UNITY_EDITOR
                                var go = UnityEditor.EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
                                if (go != null) Object.DestroyImmediate(go);
                                #endif
                            }
                        }
                    }
                }
                
                yield return null; // Allow frame to complete
            }
            
            // Verify stable performance (later batches shouldn't be significantly slower)
            var firstBatchTime = processingTimes[0];
            var lastBatchTime = processingTimes[batchCount - 1];
            var performanceDegradation = (lastBatchTime - firstBatchTime) / firstBatchTime;
            
            Assert.Less(performanceDegradation, 0.5f, "Performance should not degrade by more than 50% over time");
        }
        
        #region Helper Methods
        
        private Entity CreateSingleTerrainEntity(TerrainType terrainType)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = terrainType,
                MinHeight = terrainType == TerrainType.Spherical ? 1f : 0f,
                MaxHeight = 10f,
                Depth = 3,
                Sides = 6,
                Radius = 10f,
                Seed = 12345,
                BaseFrequency = 0.1f,
                Octaves = 4,
                Persistence = 0.5f,
                Lacunarity = 2f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            return entity;
        }
        
        private void RunCompleteWorkflow(Entity entity)
        {
            // Run terrain generation until complete
            for (int i = 0; i < 10; i++) // Max 10 iterations to prevent infinite loop
            {
                terrainGenerationSystem.Update();
                CompleteAllJobs();
                
                if (Manager.HasComponent<TerrainGenerationState>(entity))
                {
                    var state = Manager.GetComponentData<TerrainGenerationState>(entity);
                    if (state.CurrentPhase == GenerationPhase.Complete)
                    {
                        break;
                    }
                }
            }
            
            // Run mesh creation if needed
            meshCreationSystem.Update();
            CompleteAllJobs();
        }
        
        private void RunCompleteWorkflowBatch(Entity[] entities)
        {
            // Run terrain generation until all complete
            for (int i = 0; i < 10; i++) // Max 10 iterations
            {
                terrainGenerationSystem.Update();
                CompleteAllJobs();
                
                bool allComplete = true;
                foreach (var entity in entities)
                {
                    if (Manager.HasComponent<TerrainGenerationState>(entity))
                    {
                        var state = Manager.GetComponentData<TerrainGenerationState>(entity);
                        if (state.CurrentPhase != GenerationPhase.Complete)
                        {
                            allComplete = false;
                            break;
                        }
                    }
                    else
                    {
                        allComplete = false;
                        break;
                    }
                }
                
                if (allComplete) break;
            }
            
            // Run mesh creation
            meshCreationSystem.Update();
            CompleteAllJobs();
        }
        
        private TerraceConfigData CreateTestTerraceConfig()
        {
            var heights = new float[] { 2f, 5f, 8f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateLargeTerraceConfig()
        {
            var heights = new float[] { 5f, 10f, 15f, 20f, 25f, 30f, 35f, 40f, 45f, 50f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateEmptyTerraceConfig()
        {
            return CreateTerraceConfigFromHeights(new float[0]);
        }
        
        private TerraceConfigData CreateTerraceConfigFromHeights(float[] heights)
        {
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
        
        #endregion
    }
}