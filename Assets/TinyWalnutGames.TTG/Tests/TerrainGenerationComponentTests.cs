using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Component-specific functionality tests for terrain generation.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationComponentTests : ECSTestsFixture
    {
        [Test]
        public void TerrainGenerationData_ComponentAddition_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 15f,
                Depth = 4,
                Sides = 8,
                Radius = 12f,
                Seed = 54321,
                BaseFrequency = 0.05f,
                Octaves = 6,
                Persistence = 0.3f,
                Lacunarity = 2.5f
            };
            
            Manager.AddComponentData(entity, terrainData);
            
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationData>(entity));
            var retrievedData = Manager.GetComponentData<TerrainGenerationData>(entity);
            
            Assert.AreEqual(TerrainType.Planar, retrievedData.TerrainType);
            Assert.AreEqual(0f, retrievedData.MinHeight);
            Assert.AreEqual(15f, retrievedData.MaxHeight);
            Assert.AreEqual(4, retrievedData.Depth);
            Assert.AreEqual(8, retrievedData.Sides);
            Assert.AreEqual(12f, retrievedData.Radius);
            Assert.AreEqual(54321, retrievedData.Seed);
            Assert.AreEqual(0.05f, retrievedData.BaseFrequency, 0.001f);
            Assert.AreEqual(6u, retrievedData.Octaves);
            Assert.AreEqual(0.3f, retrievedData.Persistence, 0.001f);
            Assert.AreEqual(2.5f, retrievedData.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerrainGenerationData_ComponentModification_UpdatesCorrectly()
        {
            var entity = CreateEntity();
            
            var originalData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f
            };
            Manager.AddComponentData(entity, originalData);
            
            // Modify the component data
            var modifiedData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = 2f,
                MaxHeight = 20f,
                Depth = 5
            };
            Manager.SetComponentData(entity, modifiedData);
            
            var retrievedData = Manager.GetComponentData<TerrainGenerationData>(entity);
            Assert.AreEqual(TerrainType.Spherical, retrievedData.TerrainType);
            Assert.AreEqual(2f, retrievedData.MinHeight);
            Assert.AreEqual(20f, retrievedData.MaxHeight);
            Assert.AreEqual(5, retrievedData.Depth);
        }
        
        [Test]
        public void TerraceConfigData_BlobAssetLifecycle_ManagedCorrectly()
        {
            var entity = CreateEntity();
            
            // Create terrace config with blob asset
            var heights = new float[] { 1f, 4f, 7f, 10f };
            var terraceConfig = CreateTerraceConfigFromHeights(heights);
            
            Manager.AddComponentData(entity, terraceConfig);
            
            Assert.IsTrue(Manager.HasComponent<TerraceConfigData>(entity));
            var retrievedConfig = Manager.GetComponentData<TerraceConfigData>(entity);
            
            Assert.AreEqual(heights.Length, retrievedConfig.TerraceCount);
            Assert.IsTrue(retrievedConfig.TerraceHeights.IsCreated);
            
            // Verify blob data
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.AreEqual(heights[i], retrievedConfig.TerraceHeights.Value[i], 0.001f);
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerraceConfigData_BlobAssetReplacement_HandledCorrectly()
        {
            var entity = CreateEntity();
            
            // Add initial terrace config
            var initialHeights = new float[] { 2f, 5f };
            var initialConfig = CreateTerraceConfigFromHeights(initialHeights);
            Manager.AddComponentData(entity, initialConfig);
            
            // Replace with new terrace config
            var newHeights = new float[] { 1f, 3f, 6f, 9f };
            var newConfig = CreateTerraceConfigFromHeights(newHeights);
            
            // Clean up old config first
            CleanupEntityMeshData(entity);
            Manager.SetComponentData(entity, newConfig);
            
            var retrievedConfig = Manager.GetComponentData<TerraceConfigData>(entity);
            Assert.AreEqual(newHeights.Length, retrievedConfig.TerraceCount);
            
            for (int i = 0; i < newHeights.Length; i++)
            {
                Assert.AreEqual(newHeights[i], retrievedConfig.TerraceHeights.Value[i], 0.001f);
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationState_StateTransitions_WorkCorrectly()
        {
            var entity = CreateEntity();
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.NotStarted,
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.AddComponentData(entity, state);
            
            // Transition through phases
            var phases = new[]
            {
                GenerationPhase.ShapeGeneration,
                GenerationPhase.Fragmentation,
                GenerationPhase.Sculpting,
                GenerationPhase.Terracing,
                GenerationPhase.MeshCreation,
                GenerationPhase.Complete
            };
            
            foreach (var phase in phases)
            {
                state.CurrentPhase = phase;
                Manager.SetComponentData(entity, state);
                
                var retrievedState = Manager.GetComponentData<TerrainGenerationState>(entity);
                Assert.AreEqual(phase, retrievedState.CurrentPhase);
            }
            
            // Mark as complete
            state.IsComplete = true;
            Manager.SetComponentData(entity, state);
            
            var finalState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(finalState.IsComplete);
        }
        
        [Test]
        public void TerrainGenerationState_ErrorHandling_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration,
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.AddComponentData(entity, state);
            
            // Set error state
            state.HasError = true;
            state.CurrentPhase = GenerationPhase.ShapeGeneration; // Stay in same phase
            Manager.SetComponentData(entity, state);
            
            var retrievedState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(retrievedState.HasError);
            Assert.IsFalse(retrievedState.IsComplete);
            Assert.AreEqual(GenerationPhase.ShapeGeneration, retrievedState.CurrentPhase);
        }
        
        [Test]
        public void TerrainGenerationRequest_ComponentBehavior_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            var request = new TerrainGenerationRequest
            {
                UseAsyncGeneration = true
            };
            Manager.AddComponentData(entity, request);
            
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationRequest>(entity));
            var retrievedRequest = Manager.GetComponentData<TerrainGenerationRequest>(entity);
            Assert.IsTrue(retrievedRequest.UseAsyncGeneration);
            
            // Change to sync
            request.UseAsyncGeneration = false;
            Manager.SetComponentData(entity, request);
            
            retrievedRequest = Manager.GetComponentData<TerrainGenerationRequest>(entity);
            Assert.IsFalse(retrievedRequest.UseAsyncGeneration);
        }
        
        [Test]
        public void MeshDataComponent_BlobAssetManagement_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            // Create mesh data
            var vertices = new float3[]
            {
                new(0, 0, 0),
                new(1, 1, 0),
                new(0, 1, 1),
                new(1, 0, 1)
            };
            var indices = new int[] { 0, 1, 2, 0, 2, 3, 1, 3, 2, 0, 3, 1 };
            
            var meshData = CreateMeshDataComponent(vertices, indices);
            Manager.AddComponentData(entity, meshData);
            
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var retrievedMeshData = Manager.GetComponentData<MeshDataComponent>(entity);
            
            Assert.AreEqual(vertices.Length, retrievedMeshData.VertexCount);
            Assert.AreEqual(indices.Length, retrievedMeshData.IndexCount);
            Assert.IsTrue(retrievedMeshData.Vertices.IsCreated);
            Assert.IsTrue(retrievedMeshData.Indices.IsCreated);
            
            // Verify vertex data
            for (int i = 0; i < vertices.Length; i++)
            {
                var expected = vertices[i];
                var actual = retrievedMeshData.Vertices.Value[i];
                Assert.AreEqual(expected.x, actual.x, 0.001f);
                Assert.AreEqual(expected.y, actual.y, 0.001f);
                Assert.AreEqual(expected.z, actual.z, 0.001f);
            }
            
            // Verify index data
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.AreEqual(indices[i], retrievedMeshData.Indices.Value[i]);
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshGameObjectReference_ComponentFunctionality_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            // Create a test GameObject
            var testGameObject = new GameObject("TestMeshObject");
            var instanceID = testGameObject.GetInstanceID();
            
            var meshRef = new MeshGameObjectReference
            {
                GameObjectInstanceID = instanceID
            };
            Manager.AddComponentData(entity, meshRef);
            
            Assert.IsTrue(Manager.HasComponent<MeshGameObjectReference>(entity));
            var retrievedRef = Manager.GetComponentData<MeshGameObjectReference>(entity);
            Assert.AreEqual(instanceID, retrievedRef.GameObjectInstanceID);
            
            // Verify we can retrieve the GameObject
            #if UNITY_EDITOR
            var retrievedGameObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            Assert.IsNotNull(retrievedGameObject);
            Assert.AreEqual("TestMeshObject", retrievedGameObject.name);
            #endif
            
            // Clean up
            Object.DestroyImmediate(testGameObject);
        }
        
        [Test]
        public void GeneratedTerrainMeshTag_TagFunctionality_WorksCorrectly()
        {
            var entity = CreateEntity();
            
            // Initially should not have tag
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            // Add tag
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity);
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            // Remove tag
            Manager.RemoveComponent<GeneratedTerrainMeshTag>(entity);
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
        }
        
        [Test]
        public void ComponentCombinations_AllTerrainComponents_WorkTogether()
        {
            var entity = CreateEntity();
            
            // Add all terrain-related components
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f,
                MaxHeight = 15f
            };
            Manager.AddComponentData(entity, terrainData);
            
            var terraceConfig = CreateTerraceConfigFromHeights(new float[] { 3f, 6f, 9f, 12f });
            Manager.AddComponentData(entity, terraceConfig);
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.AddComponentData(entity, state);
            
            var request = new TerrainGenerationRequest
            {
                UseAsyncGeneration = false
            };
            Manager.AddComponentData(entity, request);
            
            // Verify all components exist
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationData>(entity));
            Assert.IsTrue(Manager.HasComponent<TerraceConfigData>(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationRequest>(entity));
            
            // Verify data integrity
            var retrievedTerrainData = Manager.GetComponentData<TerrainGenerationData>(entity);
            Assert.AreEqual(TerrainType.Planar, retrievedTerrainData.TerrainType);
            
            var retrievedTerraceConfig = Manager.GetComponentData<TerraceConfigData>(entity);
            Assert.AreEqual(4, retrievedTerraceConfig.TerraceCount);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void ComponentQuery_FiltersByComponents_WorksCorrectly()
        {
            // Create entities with different component combinations
            var entity1 = CreateEntity();
            Manager.AddComponentData(entity1, new TerrainGenerationData { TerrainType = TerrainType.Planar });
            
            var entity2 = CreateEntity();
            Manager.AddComponentData(entity2, new TerrainGenerationData { TerrainType = TerrainType.Spherical });
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity2);
            
            var entity3 = CreateEntity();
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity3);
            
            // Query for entities with TerrainGenerationData
            using var terrainDataQuery = Manager.CreateEntityQuery(typeof(TerrainGenerationData));
            var terrainEntities = terrainDataQuery.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(2, terrainEntities.Length);
            terrainEntities.Dispose();
            
            // Query for entities with GeneratedTerrainMeshTag
            using var meshTagQuery = Manager.CreateEntityQuery(typeof(GeneratedTerrainMeshTag));
            var meshEntities = meshTagQuery.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(2, meshEntities.Length);
            meshEntities.Dispose();
            
            // Query for entities with both
            using var bothQuery = Manager.CreateEntityQuery(typeof(TerrainGenerationData), typeof(GeneratedTerrainMeshTag));
            var bothEntities = bothQuery.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(1, bothEntities.Length);
            bothEntities.Dispose();
        }
        
        [Test]
        public void ComponentRemoval_CleansUpCorrectly()
        {
            var entity = CreateEntity();
            
            // Add components
            var terrainData = new TerrainGenerationData { TerrainType = TerrainType.Planar };
            Manager.AddComponentData(entity, terrainData);
            
            var terraceConfig = CreateTerraceConfigFromHeights(new float[] { 5f, 10f });
            Manager.AddComponentData(entity, terraceConfig);
            
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity);
            
            // Verify components exist
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationData>(entity));
            Assert.IsTrue(Manager.HasComponent<TerraceConfigData>(entity));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            // Remove components
            Manager.RemoveComponent<TerrainGenerationData>(entity);
            Manager.RemoveComponent<GeneratedTerrainMeshTag>(entity);
            
            // Verify removal
            Assert.IsFalse(Manager.HasComponent<TerrainGenerationData>(entity));
            Assert.IsTrue(Manager.HasComponent<TerraceConfigData>(entity)); // Still has this one
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void ComponentArchetypeChanges_HandleGracefully()
        {
            var entity = CreateEntity();
            
            // Start with basic terrain data
            Manager.AddComponentData(entity, new TerrainGenerationData { TerrainType = TerrainType.Planar });
            
            // Add state component (changes archetype)
            Manager.AddComponentData(entity, new TerrainGenerationState { CurrentPhase = GenerationPhase.ShapeGeneration });
            
            // Add request component (changes archetype again)
            Manager.AddComponentData(entity, new TerrainGenerationRequest { UseAsyncGeneration = false });
            
            // Add tag component (changes archetype again)
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity);
            
            // Verify entity still exists and all components are present
            Assert.IsTrue(Manager.Exists(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationData>(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationRequest>(entity));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
        }
        
        #region Helper Methods
        
        private MeshDataComponent CreateMeshDataComponent(float3[] vertices, int[] indices)
        {
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