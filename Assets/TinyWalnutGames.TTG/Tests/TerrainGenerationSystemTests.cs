using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the main terrain generation system workflow.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationSystemTests : ECSTestsFixture
    {
        private TerrainGenerationSystem terrainGenerationSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            terrainGenerationSystem = GetOrCreateSystem<TerrainGenerationSystem>();
        }
        
        [Test]
        public void TerrainGenerationSystem_NewRequest_CreatesGenerationState()
        {
            // Create entity with terrain generation request
            var entity = CreateTerrainRequestEntity();
            
            // Verify initial state
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationRequest>(entity));
            Assert.IsFalse(Manager.HasComponent<TerrainGenerationState>(entity));
            
            // Run system update
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify request was processed
            Assert.IsFalse(Manager.HasComponent<TerrainGenerationRequest>(entity));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity));
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.ShapeGeneration, state.CurrentPhase);
            Assert.IsFalse(state.IsComplete);
            Assert.IsFalse(state.HasError);
            Assert.AreEqual(Entity.Null, state.ResultMeshEntity);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_PlanarShapeGeneration_CreatesMeshData()
        {
            // Create entity ready for shape generation
            var entity = CreateEntityForShapeGeneration(TerrainType.Planar);
            
            // Run system update
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh data was created
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            Assert.IsTrue(meshData.VertexCount > 0);
            Assert.IsTrue(meshData.IndexCount > 0);
            
            // Verify state progression
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_SphericalShapeGeneration_CreatesMeshData()
        {
            // Create entity ready for shape generation
            var entity = CreateEntityForShapeGeneration(TerrainType.Spherical);
            
            // Run system update
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh data was created
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            Assert.IsTrue(meshData.VertexCount > 0);
            Assert.IsTrue(meshData.IndexCount > 0);
            
            // Verify state progression
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_PlanarMeshGeometry_IsValid()
        {
            // Create planar terrain with specific parameters
            var entity = CreateEntityForShapeGeneration(TerrainType.Planar, sides: 6, radius: 10f);
            
            // Run system update
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            
            // Verify vertex count matches sides
            Assert.AreEqual(6, meshData.VertexCount);
            
            // Verify triangulation (sides - 2) * 3 indices
            Assert.AreEqual((6 - 2) * 3, meshData.IndexCount);
            
            // Verify vertices form a circle with specified radius
            for (int i = 0; i < meshData.VertexCount; i++)
            {
                var vertex = meshData.Vertices.Value[i];
                var distance = math.length(new float2(vertex.x, vertex.z));
                Assert.AreEqual(10f, distance, 0.001f, $"Vertex {i} distance from center should be radius");
                Assert.AreEqual(0f, vertex.y, 0.001f, $"Vertex {i} Y should be 0 for planar terrain");
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_SphericalMeshGeometry_IsValid()
        {
            // Create spherical terrain entity with required components
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = 1f,
                MaxHeight = 5f,
                Sides = 6, // Set sides for spherical (affects subdivision)
                Radius = 1f, // Use unit radius for easier validation
                Depth = 2 // Fewer subdivisions for predictable vertex count
            };
            Manager.SetComponentData(entity, terrainData);
            
            // Add required TerraceConfigData component
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            // Set state to shape generation phase
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration,
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.SetComponentData(entity, state);
            
            // Run shape generation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh geometry was created
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity), "MeshDataComponent should be created");
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            
            // Verify basic mesh properties
            Assert.IsTrue(meshData.Vertices.IsCreated, "Vertices blob should be created");
            Assert.IsTrue(meshData.Indices.IsCreated, "Indices blob should be created");
            Assert.IsTrue(meshData.VertexCount > 0, "Should have vertices");
            Assert.IsTrue(meshData.IndexCount > 0, "Should have indices");
            
            // For spherical terrain, verify vertices are roughly on sphere surface
            ref var vertices = ref meshData.Vertices.Value;
            bool allVerticesOnSurface = true;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var distance = math.length(vertex);
                
                // Allow for reasonable tolerance in sphere generation
                if (math.abs(distance - terrainData.MinHeight) > 0.1f)
                {
                    allVerticesOnSurface = false;
                    break;
                }
            }
            
            Assert.IsTrue(allVerticesOnSurface, "All vertices should be approximately on sphere surface");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_CompleteWorkflow_ProgressesThroughAllPhases()
        {
            // Create entity with terrain request
            var entity = CreateTerrainRequestEntity();
            
            // Phase 1: Request processing
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.ShapeGeneration, state.CurrentPhase);
            
            // Phase 2: Shape generation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase);
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            
            // Phase 3: Fragmentation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Sculpting, state.CurrentPhase);
            
            // Phase 4: Sculpting
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Terracing, state.CurrentPhase);
            
            // Phase 5: Terracing
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.MeshCreation, state.CurrentPhase);
            
            // Phase 6: Mesh creation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Complete, state.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_MultipleEntities_ProcessedIndependently()
        {
            // Create multiple terrain entities with different parameters to ensure different mesh data
            var entity1 = CreateTerrainRequestEntity(TerrainType.Planar);
            var entity2 = CreateTerrainRequestEntity(TerrainType.Spherical);
            var entity3 = CreateTerrainRequestEntity(TerrainType.Planar);
            
            // Modify entity1 to have 4 sides instead of 6 to create difference
            var terrainData1 = Manager.GetComponentData<TerrainGenerationData>(entity1);
            terrainData1.Sides = 4;
            Manager.SetComponentData(entity1, terrainData1);
            
            // Process requests
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify all entities have generation state
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity1));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity2));
            Assert.IsTrue(Manager.HasComponent<TerrainGenerationState>(entity3));
            
            // Process shape generation
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify all entities have mesh data
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity1));
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity2));
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity3));
            
            // Verify different terrain parameters produce different vertex counts
            var meshData1 = Manager.GetComponentData<MeshDataComponent>(entity1);
            var meshData2 = Manager.GetComponentData<MeshDataComponent>(entity2);
            var meshData3 = Manager.GetComponentData<MeshDataComponent>(entity3);
            
            // Entity1 has 4 sides, Entity3 has 6 sides (default)
            Assert.AreEqual(4, meshData1.VertexCount);
            Assert.AreEqual(6, meshData3.VertexCount);
            
            // Entity1 and Entity3 should be different (4 vs 6 sides)
            Assert.AreNotEqual(meshData1.VertexCount, meshData3.VertexCount);
            
            // Both planar and spherical can have 6 vertices (hex vs octahedron), so just verify they exist
            Assert.IsTrue(meshData2.VertexCount > 0);
            
            // Clean up
            CleanupEntityMeshData(entity1);
            CleanupEntityMeshData(entity2);
            CleanupEntityMeshData(entity3);
        }
        
        [Test]
        public void TerrainGenerationSystem_NoRequestComponent_NoProcessing()
        {
            // Create entity without request component
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            // Run system
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify no processing occurred
            Assert.IsFalse(Manager.HasComponent<TerrainGenerationState>(entity));
            Assert.IsFalse(Manager.HasComponent<MeshDataComponent>(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_WrongPhase_NoProcessing()
        {
            // Create entity in wrong phase for shape generation
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.Sculpting, // Wrong phase
                IsComplete = false,
                HasError = false
            };
            Manager.SetComponentData(entity, state);
            
            // Run system
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify no shape generation occurred
            Assert.IsFalse(Manager.HasComponent<MeshDataComponent>(entity));
            
            // Verify state unchanged
            var updatedState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Sculpting, updatedState.CurrentPhase);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_AlreadyHasMeshData_NoReprocessing()
        {
            // Create entity that already has mesh data
            var entity = CreateEntityWithMeshData();
            
            var originalMeshData = Manager.GetComponentData<MeshDataComponent>(entity);
            var originalVertexCount = originalMeshData.VertexCount;
            
            // Run system
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh data unchanged
            var currentMeshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(originalVertexCount, currentMeshData.VertexCount);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void TerrainGenerationSystem_DifferentSides_GeneratesDifferentMeshes()
        {
            // Create planar terrains with different side counts
            var entity3 = CreateEntityForShapeGeneration(TerrainType.Planar, sides: 3);
            var entity8 = CreateEntityForShapeGeneration(TerrainType.Planar, sides: 8);
            
            // Run system
            terrainGenerationSystem.Update();
            CompleteAllJobs();
            
            var meshData3 = Manager.GetComponentData<MeshDataComponent>(entity3);
            var meshData8 = Manager.GetComponentData<MeshDataComponent>(entity8);
            
            // Verify different vertex counts
            Assert.AreEqual(3, meshData3.VertexCount);
            Assert.AreEqual(8, meshData8.VertexCount);
            
            // Verify different index counts
            Assert.AreEqual((3 - 2) * 3, meshData3.IndexCount); // 3 indices
            Assert.AreEqual((8 - 2) * 3, meshData8.IndexCount); // 18 indices
            
            // Clean up
            CleanupEntityMeshData(entity3);
            CleanupEntityMeshData(entity8);
        }
        
        #region Helper Methods
        
        private Entity CreateTerrainRequestEntity(TerrainType terrainType = TerrainType.Planar)
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
            
            var request = new TerrainGenerationRequest
            {
                UseAsyncGeneration = false
            };
            Manager.SetComponentData(entity, request);
            
            return entity;
        }
        
        private Entity CreateEntityForShapeGeneration(TerrainType terrainType, ushort sides = 6, float radius = 10f, float minHeight = 0f)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = terrainType,
                MinHeight = minHeight,
                MaxHeight = 10f,
                Depth = 3,
                Sides = sides,
                Radius = radius,
                Seed = 12345,
                BaseFrequency = 0.1f,
                Octaves = 4,
                Persistence = 0.5f,
                Lacunarity = 2f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration,
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.SetComponentData(entity, state);
            
            return entity;
        }
        
        private Entity CreateEntityWithMeshData()
        {
            var entity = CreateEntityForShapeGeneration(TerrainType.Planar);
            
            // Create and add mesh data
            var vertices = new float3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0.5f, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };
            
            var meshData = CreateMeshDataComponent(vertices, indices);
            Manager.AddComponentData(entity, meshData);
            
            return entity;
        }
        
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
        
        #endregion
    }
}