using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for terrain generation data structures and component validation.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationDataTests : ECSTestsFixture
    {
        [Test]
        public void TerrainGenerationData_DefaultValues_AreValid()
        {
            var data = new TerrainGenerationData();
            
            // Verify default values are sensible
            Assert.AreEqual(0f, data.MinHeight);
            Assert.AreEqual(0f, data.MaxHeight);
            Assert.AreEqual(0, data.Depth);
            Assert.AreEqual(TerrainType.Planar, data.TerrainType);
            Assert.AreEqual(0, data.Sides);
            Assert.AreEqual(0f, data.Radius);
            Assert.AreEqual(0, data.Seed);
            Assert.AreEqual(0f, data.BaseFrequency);
            Assert.AreEqual(0u, data.Octaves);
            Assert.AreEqual(0f, data.Persistence);
            Assert.AreEqual(0f, data.Lacunarity);
        }
        
        [Test]
        public void TerrainGenerationData_PlanarConfiguration_IsValid()
        {
            var data = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 10f,
                Depth = 3,
                Sides = 6,
                Radius = 5f,
                Seed = 12345,
                BaseFrequency = 0.1f,
                Octaves = 4,
                Persistence = 0.5f,
                Lacunarity = 2f
            };
            
            Assert.AreEqual(TerrainType.Planar, data.TerrainType);
            Assert.AreEqual(0f, data.MinHeight);
            Assert.AreEqual(10f, data.MaxHeight);
            Assert.AreEqual(3, data.Depth);
            Assert.AreEqual(6, data.Sides);
            Assert.AreEqual(5f, data.Radius);
            Assert.AreEqual(12345, data.Seed);
            Assert.AreEqual(0.1f, data.BaseFrequency, 0.001f);
            Assert.AreEqual(4u, data.Octaves);
            Assert.AreEqual(0.5f, data.Persistence, 0.001f);
            Assert.AreEqual(2f, data.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerrainGenerationData_SphericalConfiguration_IsValid()
        {
            var data = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = 1f,
                MaxHeight = 15f,
                Depth = 5,
                Seed = 54321,
                BaseFrequency = 0.05f,
                Octaves = 6,
                Persistence = 0.3f,
                Lacunarity = 1.8f
            };
            
            Assert.AreEqual(TerrainType.Spherical, data.TerrainType);
            Assert.AreEqual(1f, data.MinHeight);
            Assert.AreEqual(15f, data.MaxHeight);
            Assert.AreEqual(5, data.Depth);
            Assert.AreEqual(54321, data.Seed);
            Assert.AreEqual(0.05f, data.BaseFrequency, 0.001f);
            Assert.AreEqual(6u, data.Octaves);
            Assert.AreEqual(0.3f, data.Persistence, 0.001f);
            Assert.AreEqual(1.8f, data.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerraceConfigData_BlobAssetCreation_WorksCorrectly()
        {
            var heights = new float[] { 2f, 5f, 8f, 12f };
            
            // Create blob asset
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var heightArray = ref builder.ConstructRoot<BlobArray<float>>();
            var heightArrayBuilder = builder.Allocate(ref heightArray, heights.Length);
            
            for (int i = 0; i < heights.Length; i++)
            {
                heightArrayBuilder[i] = heights[i];
            }
            
            var heightBlob = builder.CreateBlobAssetReference<BlobArray<float>>(Allocator.Persistent);
            
            var terraceConfig = new TerraceConfigData
            {
                TerraceCount = heights.Length,
                TerraceHeights = heightBlob
            };
            
            // Verify data
            Assert.AreEqual(heights.Length, terraceConfig.TerraceCount);
            Assert.IsTrue(terraceConfig.TerraceHeights.IsCreated);
            Assert.AreEqual(heights.Length, terraceConfig.TerraceHeights.Value.Length);
            
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.AreEqual(heights[i], terraceConfig.TerraceHeights.Value[i], 0.001f);
            }
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationState_DefaultValues_AreCorrect()
        {
            var state = new TerrainGenerationState();
            
            Assert.AreEqual(GenerationPhase.NotStarted, state.CurrentPhase);
            Assert.IsFalse(state.IsComplete);
            Assert.IsFalse(state.HasError);
            Assert.AreEqual(Entity.Null, state.ResultMeshEntity);
        }
        
        [Test]
        public void TerrainGenerationState_PhaseProgression_WorksCorrectly()
        {
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration,
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            
            Assert.AreEqual(GenerationPhase.ShapeGeneration, state.CurrentPhase);
            Assert.IsFalse(state.IsComplete);
            
            // Progress through phases
            state.CurrentPhase = GenerationPhase.Fragmentation;
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase);
            
            state.CurrentPhase = GenerationPhase.Sculpting;
            Assert.AreEqual(GenerationPhase.Sculpting, state.CurrentPhase);
            
            state.CurrentPhase = GenerationPhase.Terracing;
            Assert.AreEqual(GenerationPhase.Terracing, state.CurrentPhase);
            
            state.CurrentPhase = GenerationPhase.MeshCreation;
            Assert.AreEqual(GenerationPhase.MeshCreation, state.CurrentPhase);
            
            state.CurrentPhase = GenerationPhase.Complete;
            state.IsComplete = true;
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            Assert.IsTrue(state.IsComplete);
        }
        
        [Test]
        public void TerrainGenerationRequest_Configuration_IsValid()
        {
            var request = new TerrainGenerationRequest
            {
                UseAsyncGeneration = true
            };
            
            Assert.IsTrue(request.UseAsyncGeneration);
            
            request.UseAsyncGeneration = false;
            Assert.IsFalse(request.UseAsyncGeneration);
        }
        
        [Test]
        public void MeshDataComponent_BlobAssetCreation_WorksCorrectly()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0.5f, 1, 0)
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
            
            var meshData = new MeshDataComponent
            {
                Vertices = vertexBlob,
                Indices = indexBlob,
                VertexCount = vertices.Length,
                IndexCount = indices.Length
            };
            
            // Verify mesh data
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            Assert.AreEqual(vertices.Length, meshData.VertexCount);
            Assert.AreEqual(indices.Length, meshData.IndexCount);
            Assert.AreEqual(vertices.Length, meshData.Vertices.Value.Length);
            Assert.AreEqual(indices.Length, meshData.Indices.Value.Length);
            
            // Verify vertex data
            for (int i = 0; i < vertices.Length; i++)
            {
                var expected = vertices[i];
                var actual = meshData.Vertices.Value[i];
                Assert.AreEqual(expected.x, actual.x, 0.001f);
                Assert.AreEqual(expected.y, actual.y, 0.001f);
                Assert.AreEqual(expected.z, actual.z, 0.001f);
            }
            
            // Verify index data
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.AreEqual(indices[i], meshData.Indices.Value[i]);
            }
            
            // Clean up
            meshData.Vertices.Dispose();
            meshData.Indices.Dispose();
        }
        
        [Test]
        public void MeshGameObjectReference_InstanceID_IsValid()
        {
            var gameObject = new UnityEngine.GameObject("TestMesh");
            var instanceID = gameObject.GetInstanceID();
            
            var meshRef = new MeshGameObjectReference
            {
                GameObjectInstanceID = instanceID
            };
            
            Assert.AreEqual(instanceID, meshRef.GameObjectInstanceID);
            
            // Clean up
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void GeneratedTerrainMeshTag_IsValidComponent()
        {
            var entity = CreateEntity();
            Manager.AddComponent<GeneratedTerrainMeshTag>(entity);
            
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            
            Manager.RemoveComponent<GeneratedTerrainMeshTag>(entity);
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
        }
        
        [Test]
        public void TerrainType_EnumValues_AreCorrect()
        {
            Assert.AreEqual(0, (byte)TerrainType.Planar);
            Assert.AreEqual(1, (byte)TerrainType.Spherical);
        }
        
        [Test]
        public void GenerationPhase_EnumValues_AreCorrect()
        {
            Assert.AreEqual(0, (byte)GenerationPhase.NotStarted);
            Assert.AreEqual(1, (byte)GenerationPhase.ShapeGeneration);
            Assert.AreEqual(2, (byte)GenerationPhase.Fragmentation);
            Assert.AreEqual(3, (byte)GenerationPhase.Sculpting);
            Assert.AreEqual(4, (byte)GenerationPhase.Terracing);
            Assert.AreEqual(5, (byte)GenerationPhase.MeshCreation);
            Assert.AreEqual(6, (byte)GenerationPhase.Complete);
        }
        
        [Test]
        public void EntityComponentIntegration_AllComponents_CanBeAddedToEntity()
        {
            var entity = CreateEntity();
            
            // Add all terrain generation components
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f
            };
            Manager.AddComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.AddComponentData(entity, terraceConfig);
            
            var generationState = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.AddComponentData(entity, generationState);
            
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
            
            // Verify component data
            var retrievedTerrainData = Manager.GetComponentData<TerrainGenerationData>(entity);
            Assert.AreEqual(TerrainType.Planar, retrievedTerrainData.TerrainType);
            Assert.AreEqual(6, retrievedTerrainData.Sides);
            Assert.AreEqual(10f, retrievedTerrainData.Radius, 0.001f);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        private TerraceConfigData CreateTestTerraceConfig()
        {
            var heights = new float[] { 2f, 5f, 8f };
            
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
    }
}