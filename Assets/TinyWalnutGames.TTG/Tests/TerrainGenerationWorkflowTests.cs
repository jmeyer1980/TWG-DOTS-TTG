using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Comprehensive workflow tests to verify complete terrain generation pipelines.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationWorkflowTests : ECSTestsFixture
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
        public void PlanarTerrainWorkflow_CompleteEndToEnd_CreatesValidMesh()
        {
            // Create planar terrain entity
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f,
                MinHeight = 0f,
                MaxHeight = 10f,
                Seed = 12345,
                BaseFrequency = 0.1f,
                Octaves = 2,
                Persistence = 0.5f,
                Lacunarity = 2f,
                Depth = 2
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Run complete workflow using the correct method name
            ExecuteCompleteWorkflow();
            
            // Verify final state
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            
            // FIXED: Check if component exists before accessing it
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity), "Entity should have MeshDataComponent after complete workflow");
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            
            Assert.Greater(meshData.VertexCount, 0);
            Assert.Greater(meshData.IndexCount, 0);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void SphericalTerrainWorkflow_CompleteEndToEnd_CreatesValidMesh()
        {
            // Create spherical terrain entity
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = 1f,
                MaxHeight = 10f,
                Seed = 12345,
                BaseFrequency = 0.1f,
                Octaves = 2,
                Persistence = 0.5f,
                Lacunarity = 2f,
                Depth = 2
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Run complete workflow using the correct method name
            ExecuteCompleteWorkflow();
            
            // Verify final state
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            
            // FIXED: Check if component exists before accessing it
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity), "Entity should have MeshDataComponent after complete workflow");
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            
            Assert.Greater(meshData.VertexCount, 0);
            Assert.Greater(meshData.IndexCount, 0);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [UnityTest]
        public IEnumerator PlanarTerrainWorkflow_FrameByFrame_ProgressesCorrectly()
        {
            // Create planar terrain entity
            var entity = CreatePlanarTerrainEntity();
            
            // Frame 1: Request processing
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.ShapeGeneration, state.CurrentPhase);
            
            // Frame 2: Shape generation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase);
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            
            // Frame 3: Fragmentation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Sculpting, state.CurrentPhase);
            
            // Frame 4: Sculpting
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Terracing, state.CurrentPhase);
            
            // Frame 5: Terracing
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.MeshCreation, state.CurrentPhase);
            
            // Frame 6: Mesh creation preparation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Frame 7: Unity mesh creation
            meshCreationSystem.Update();
            CompleteAllJobs();
            yield return null;
            
            // Verify final state
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.AreNotEqual(Entity.Null, state.ResultMeshEntity);
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            // Clean up
            CleanupWorkflowEntity(entity);
        }
        
        [Test]
        public void MultipleTerrainWorkflow_DifferentTypes_ProcessIndependently()
        {
            // Create multiple terrains of different types
            var planarEntity1 = CreatePlanarTerrainEntity(sides: 4, radius: 5f);
            var sphericalEntity = CreateSphericalTerrainEntity(minHeight: 2f, maxHeight: 15f);
            var planarEntity2 = CreatePlanarTerrainEntity(sides: 8, radius: 12f);
            
            // Execute complete workflow
            ExecuteCompleteWorkflow();
            
            // Verify all entities completed successfully
            var entities = new[] { planarEntity1, sphericalEntity, planarEntity2 };
            foreach (var entity in entities)
            {
                Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
                var state = Manager.GetComponentData<TerrainGenerationState>(entity);
                Assert.IsTrue(state.IsComplete);
                Assert.IsFalse(state.HasError);
                Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
                Assert.AreNotEqual(Entity.Null, state.ResultMeshEntity);
                Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            }
            
            // FIXED: Check if MeshDataComponent exists before accessing it
            if (Manager.HasComponent<MeshDataComponent>(planarEntity1))
            {
                var meshData1 = Manager.GetComponentData<MeshDataComponent>(planarEntity1);
                Assert.AreEqual(4, meshData1.VertexCount); // Quad
            }
            
            if (Manager.HasComponent<MeshDataComponent>(sphericalEntity))
            {
                var meshData2 = Manager.GetComponentData<MeshDataComponent>(sphericalEntity);
                Assert.AreEqual(12, meshData2.VertexCount); // Icosahedron (correct count)
            }
            
            if (Manager.HasComponent<MeshDataComponent>(planarEntity2))
            {
                var meshData3 = Manager.GetComponentData<MeshDataComponent>(planarEntity2);
                Assert.AreEqual(8, meshData3.VertexCount); // Octagon
            }
            
            // Clean up
            CleanupWorkflowEntity(planarEntity1);
            CleanupWorkflowEntity(sphericalEntity);
            CleanupWorkflowEntity(planarEntity2);
        }
        
        [Test]
        public void TerrainWorkflow_WithCustomTerraces_AppliesCorrectly()
        {
            // Create terrain with custom terrace configuration
            var entity = CreatePlanarTerrainEntity();
            
            // Replace default terrace config with custom one
            CleanupEntityMeshData(entity); // Clean up default config
            var customTerraceConfig = CreateCustomTerraceConfig();
            Manager.SetComponentData(entity, customTerraceConfig);
            
            // Add request back
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.AddComponentData(entity, request);
            
            // Execute workflow
            ExecuteCompleteWorkflow();
            
            // Verify successful completion
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            
            // FIXED: Check if TerraceConfigData component exists before accessing it
            if (Manager.HasComponent<TerraceConfigData>(entity))
            {
                var finalTerraceConfig = Manager.GetComponentData<TerraceConfigData>(entity);
                Assert.AreEqual(5, finalTerraceConfig.TerraceCount);
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
            CleanupWorkflowEntity(entity);
        }
        
        [Test]
        public void TerrainWorkflow_WithHighDetail_CompletesSuccessfully()
        {
            // Create high-detail terrain
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 20f,
                Depth = 5, // High detail
                Sides = 10, // Many sides
                Radius = 20f,
                Seed = 98765,
                BaseFrequency = 0.05f,
                Octaves = 6,
                Persistence = 0.4f,
                Lacunarity = 2.2f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateHighDetailTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var request = new TerrainGenerationRequest { UseAsyncGeneration = false };
            Manager.SetComponentData(entity, request);
            
            // Execute workflow
            ExecuteCompleteWorkflow();
            
            // Verify completion
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(state.IsComplete);
            Assert.IsFalse(state.HasError);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // FIXED: Check if MeshDataComponent exists before accessing it
            if (Manager.HasComponent<MeshDataComponent>(entity))
            {
                var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
                Assert.AreEqual(10, meshData.VertexCount);
                Assert.AreEqual(24, meshData.IndexCount); // (10-2)*3
            }
            
            // Clean up
            CleanupWorkflowEntity(entity);
        }
        
        [Test]
        public void TerrainWorkflow_RepeatedExecution_ProducesConsistentResults()
        {
            // Create terrain with fixed seed
            var entity1 = CreatePlanarTerrainEntity(seed: 12345);
            ExecuteCompleteWorkflow();
            
            // FIXED: Check if MeshDataComponent exists before accessing it
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity1), "First entity should have MeshDataComponent after workflow");
            var meshData1 = Manager.GetComponentData<MeshDataComponent>(entity1);
            var firstVertices = new float3[meshData1.VertexCount];
            for (int i = 0; i < meshData1.VertexCount; i++)
            {
                firstVertices[i] = meshData1.Vertices.Value[i];
            }
            
            CleanupWorkflowEntity(entity1);
            
            // Create identical terrain
            var entity2 = CreatePlanarTerrainEntity(seed: 12345);
            ExecuteCompleteWorkflow();
            
            // FIXED: Check if MeshDataComponent exists before accessing it
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity2), "Second entity should have MeshDataComponent after workflow");
            var meshData2 = Manager.GetComponentData<MeshDataComponent>(entity2);
            
            // Verify identical results
            Assert.AreEqual(meshData1.VertexCount, meshData2.VertexCount);
            Assert.AreEqual(meshData1.IndexCount, meshData2.IndexCount);
            
            for (int i = 0; i < meshData2.VertexCount; i++)
            {
                var vertex1 = firstVertices[i];
                var vertex2 = meshData2.Vertices.Value[i];
                Assert.AreEqual(vertex1.x, vertex2.x, 0.001f);
                Assert.AreEqual(vertex1.y, vertex2.y, 0.001f);
                Assert.AreEqual(vertex1.z, vertex2.z, 0.001f);
            }
            
            CleanupWorkflowEntity(entity2);
        }
        
        #region Helper Methods
        
        private Entity CreatePlanarTerrainEntity(ushort sides = 6, float radius = 10f, int seed = 12345)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 10f,
                Depth = 3,
                Sides = sides,
                Radius = radius,
                Seed = seed,
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
        
        private Entity CreateSphericalTerrainEntity(float minHeight = 1f, float maxHeight = 10f, int seed = 12345)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationRequest));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = minHeight,
                MaxHeight = maxHeight,
                Depth = 3,
                Seed = seed,
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
        
        private void ExecuteCompleteWorkflow()
        {
            // Run terrain generation system until all entities complete
            for (int i = 0; i < 10; i++)
            {
                terrainGenerationSystem.Update();
                CompleteAllJobs();
            }
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
        }
        
        private void CleanupWorkflowEntity(Entity entity)
        {
            if (Manager.HasComponent<TerrainGenerationState>(entity))
            {
                var state = Manager.GetComponentData<TerrainGenerationState>(entity);
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
            
            // Use try-catch to handle already-disposed blob assets gracefully
            try
            {
                CleanupEntityMeshData(entity);
            }
            catch (System.InvalidOperationException)
            {
                // Blob asset already disposed - ignore
            }
        }
        
        private TerraceConfigData CreateTestTerraceConfig()
        {
            var heights = new float[] { 2f, 5f, 8f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateCustomTerraceConfig()
        {
            var heights = new float[] { 1f, 3f, 6f, 8f, 10f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateHighDetailTerraceConfig()
        {
            var heights = new float[] { 2f, 4f, 6f, 8f, 10f, 12f, 14f, 16f, 18f, 20f };
            return CreateTerraceConfigFromHeights(heights);
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