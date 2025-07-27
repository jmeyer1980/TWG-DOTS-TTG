using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Edge case and boundary condition tests for terrain generation.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationEdgeCaseTests : ECSTestsFixture
    {
        private TerrainGenerationSystem terrainGenerationSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            terrainGenerationSystem = GetOrCreateSystem<TerrainGenerationSystem>();
        }
        
        [Test]
        public void EdgeCase_MinimumSides_Triangle_GeneratesCorrectly()
        {
            // Test minimum valid number of sides (3)
            var entity = CreatePlanarEntity(sides: 3);
            
            RunShapeGeneration(entity);
            
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(3, meshData.VertexCount);
            Assert.AreEqual(3, meshData.IndexCount); // Single triangle
            
            // Verify triangle vertices form equilateral triangle
            var vertices = new float3[3];
            for (int i = 0; i < 3; i++)
            {
                vertices[i] = meshData.Vertices.Value[i];
            }
            
            // All vertices should be at same distance from origin
            for (int i = 0; i < 3; i++)
            {
                var distance = math.length(new float2(vertices[i].x, vertices[i].z));
                Assert.AreEqual(10f, distance, 0.001f, $"Vertex {i} should be at radius distance");
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_MaximumSides_Decagon_GeneratesCorrectly()
        {
            // Test maximum supported number of sides (10)
            var entity = CreatePlanarEntity(sides: 10);
            
            RunShapeGeneration(entity);
            
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(10, meshData.VertexCount);
            Assert.AreEqual(24, meshData.IndexCount); // (10-2)*3 = 24
            
            // Verify all vertices are at correct radius
            for (int i = 0; i < meshData.VertexCount; i++)
            {
                var vertex = meshData.Vertices.Value[i];
                var distance = math.length(new float2(vertex.x, vertex.z));
                Assert.AreEqual(10f, distance, 0.001f, $"Vertex {i} should be at radius distance");
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ZeroRadius_HandledGracefully()
        {
            // Test with zero radius
            var entity = CreatePlanarEntity(radius: 0f);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Verify entity still exists and has some reasonable state
            Assert.IsTrue(Manager.Exists(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_VerySmallRadius_GeneratesCorrectly()
        {
            // Test with very small radius (should be clamped to minimum)
            var entity = CreatePlanarEntity(6, 0.001f); // Very small radius
            RunShapeGeneration(entity);
            
            // FIXED: System processes all phases automatically, so check for Fragmentation phase (next phase after shape generation)
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase, "System should have progressed to Fragmentation phase");
            
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(6, meshData.VertexCount);
            
            // FIXED: Verify radius is clamped to minimum (0.1f) not the tiny original value
            ref var vertices = ref meshData.Vertices.Value;
            var firstVertex = vertices[0];
            var actualRadius = math.length(new float2(firstVertex.x, firstVertex.z));
            Assert.AreEqual(0.1f, actualRadius, 0.01f, "Radius should be clamped to minimum value of 0.1f");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_VeryLargeRadius_GeneratesCorrectly()
        {
            // Test with very large radius
            var entity = CreatePlanarEntity(radius: 10000f);
            
            RunShapeGeneration(entity);
            
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(6, meshData.VertexCount);
            
            // Verify vertices are at large radius
            for (int i = 0; i < meshData.VertexCount; i++)
            {
                var vertex = meshData.Vertices.Value[i];
                var distance = math.length(new float2(vertex.x, vertex.z));
                Assert.AreEqual(10000f, distance, 0.1f, $"Vertex {i} should be at large radius");
            }
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_NegativeRadius_HandledGracefully()
        {
            // Test with negative radius
            var entity = CreatePlanarEntity(radius: -5f);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // System should handle this gracefully (might use absolute value or default)
            Assert.IsTrue(Manager.Exists(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_NegativeHeights_HandledCorrectly()
        {
            // Test with negative heights (should be corrected to positive for spherical)
            var entity = CreateSphericalEntity(-5f, -1f); // Both negative
            RunShapeGeneration(entity);
            
            // FIXED: System processes all phases automatically, so check for Fragmentation phase (next phase after shape generation)
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase, "System should have progressed to Fragmentation phase");
            
            // FIXED: Spherical terrain generates icosahedron with 12 vertices, not 6
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(12, meshData.VertexCount, "Spherical terrain should have 12 vertices (icosahedron)");
            Assert.AreEqual(60, meshData.IndexCount, "Spherical terrain should have 60 indices (20 triangles)");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_InvertedHeightRange_HandledGracefully()
        {
            // Test when min height > max height
            var entity = CreateSphericalEntity(minHeight: 10f, maxHeight: 5f);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // System should handle gracefully
            Assert.IsTrue(Manager.Exists(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ZeroHeightRange_HandledCorrectly()
        {
            // Test with zero height range (min == max)
            var entity = CreateSphericalEntity(5f, 5f); // Same min and max height
            RunShapeGeneration(entity);
            
            // FIXED: System processes all phases automatically, so check for Fragmentation phase (next phase after shape generation)
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(GenerationPhase.Fragmentation, state.CurrentPhase, "System should have progressed to Fragmentation phase");
            
            // FIXED: Spherical terrain generates icosahedron with 12 vertices, not 6
            Assert.IsTrue(Manager.HasComponent<MeshDataComponent>(entity));
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(12, meshData.VertexCount, "Spherical terrain should have 12 vertices (icosahedron)");
            Assert.AreEqual(60, meshData.IndexCount, "Spherical terrain should have 60 indices (20 triangles)");
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ExtremeNoiseParameters_HandledGracefully()
        {
            // Test with extreme noise parameters
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f,
                MaxHeight = 10f,
                BaseFrequency = float.MaxValue, // Extreme
                Octaves = 0, // Invalid
                Persistence = 2f, // > 1 (invalid)
                Lacunarity = 0.5f // < 1 (invalid)
            };
            Manager.SetComponentData(entity, terrainData);
            
            var terraceConfig = CreateTestTerraceConfig();
            Manager.SetComponentData(entity, terraceConfig);
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.SetComponentData(entity, state);
            
            // Should not crash with extreme parameters
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_ZeroDepth_HandledGracefully()
        {
            // Test with zero depth
            var entity = CreatePlanarEntity(depth: 0);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // System should handle gracefully
            Assert.IsTrue(Manager.Exists(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_MaximumDepth_WorksCorrectly()
        {
            // Test with high depth value
            var entity = CreatePlanarEntity(depth: 10);
            
            RunShapeGeneration(entity);
            
            // Should generate base mesh correctly regardless of depth
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.AreEqual(6, meshData.VertexCount);
            Assert.AreEqual(12, meshData.IndexCount);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_EmptyTerraceHeights_HandledCorrectly()
        {
            // Test with no terrace heights
            var entity = CreatePlanarEntity();
            
            // Replace with empty terrace config
            CleanupEntityMeshData(entity);
            var emptyTerraceConfig = CreateEmptyTerraceConfig();
            Manager.SetComponentData(entity, emptyTerraceConfig);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_SingleTerraceHeight_WorksCorrectly()
        {
            // Test with single terrace height
            var entity = CreatePlanarEntity();
            
            // Replace with single height config
            CleanupEntityMeshData(entity);
            var singleTerraceConfig = CreateSingleTerraceConfig();
            Manager.SetComponentData(entity, singleTerraceConfig);
            
            RunShapeGeneration(entity);
            
            // Should generate mesh correctly
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_VeryLargeTerraceCount_HandledCorrectly()
        {
            // Test with many terrace heights
            var entity = CreatePlanarEntity();
            
            // Replace with large terrace config
            CleanupEntityMeshData(entity);
            var largeTerraceConfig = CreateLargeTerraceConfig();
            Manager.SetComponentData(entity, largeTerraceConfig);
            
            RunShapeGeneration(entity);
            
            // Should handle large number of terraces
            var meshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.IsTrue(meshData.Vertices.IsCreated);
            Assert.IsTrue(meshData.Indices.IsCreated);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_UnsortedTerraceHeights_HandledGracefully()
        {
            // Test with unsorted terrace heights
            var entity = CreatePlanarEntity();
            
            // Replace with unsorted terrace config
            CleanupEntityMeshData(entity);
            var unsortedTerraceConfig = CreateUnsortedTerraceConfig();
            Manager.SetComponentData(entity, unsortedTerraceConfig);
            
            // Should not crash
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_DuplicateTerraceHeights_HandledCorrectly()
        {
            // Test with duplicate terrace heights
            var entity = CreatePlanarEntity();
            
            // Replace with duplicate terrace config
            CleanupEntityMeshData(entity);
            var duplicateTerraceConfig = CreateDuplicateTerraceConfig();
            Manager.SetComponentData(entity, duplicateTerraceConfig);
            
            // Should handle duplicates gracefully
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_VeryLargeTerraceHeights_HandledCorrectly()
        {
            // Test with extremely large terrace heights
            var entity = CreatePlanarEntity();
            
            // Replace with extreme terrace config
            CleanupEntityMeshData(entity);
            var extremeTerraceConfig = CreateExtremeTerraceConfig();
            Manager.SetComponentData(entity, extremeTerraceConfig);
            
            // Should handle extreme values
            Assert.DoesNotThrow(() => RunShapeGeneration(entity));
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void EdgeCase_EntityWithoutRequiredComponents_HandledGracefully()
        {
            // Test entity missing required components
            var entity = CreateEntity(typeof(TerrainGenerationData)); // Missing TerraceConfigData
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                Sides = 6,
                Radius = 10f
            };
            Manager.SetComponentData(entity, terrainData);
            
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.AddComponentData(entity, state);
            
            // Should not crash when missing components
            Assert.DoesNotThrow(() => terrainGenerationSystem.Update());
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        #region Helper Methods
        
        private Entity CreatePlanarEntity(ushort sides = 6, float radius = 10f, ushort depth = 3)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Planar,
                MinHeight = 0f,
                MaxHeight = 10f,
                Depth = depth,
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
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.SetComponentData(entity, state);
            
            return entity;
        }
        
        private Entity CreateSphericalEntity(float minHeight = 1f, float maxHeight = 10f)
        {
            var entity = CreateEntity(typeof(TerrainGenerationData), typeof(TerraceConfigData), typeof(TerrainGenerationState));
            
            var terrainData = new TerrainGenerationData
            {
                TerrainType = TerrainType.Spherical,
                MinHeight = minHeight,
                MaxHeight = maxHeight,
                Depth = 3,
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
                CurrentPhase = GenerationPhase.ShapeGeneration
            };
            Manager.SetComponentData(entity, state);
            
            return entity;
        }
        
        private void RunShapeGeneration(Entity entity)
        {
            terrainGenerationSystem.Update();
            CompleteAllJobs();
        }
        
        private TerraceConfigData CreateTestTerraceConfig()
        {
            var heights = new float[] { 2f, 5f, 8f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateEmptyTerraceConfig()
        {
            return CreateTerraceConfigFromHeights(new float[0]);
        }
        
        private TerraceConfigData CreateSingleTerraceConfig()
        {
            var heights = new float[] { 5f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateLargeTerraceConfig()
        {
            var heights = new float[50];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = i * 0.2f;
            }
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateUnsortedTerraceConfig()
        {
            var heights = new float[] { 8f, 2f, 5f, 1f, 9f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateDuplicateTerraceConfig()
        {
            var heights = new float[] { 5f, 5f, 5f, 8f, 8f };
            return CreateTerraceConfigFromHeights(heights);
        }
        
        private TerraceConfigData CreateExtremeTerraceConfig()
        {
            var heights = new float[] { -1000f, 0f, 1000000f, float.MaxValue };
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