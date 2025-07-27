using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the terrain generation authoring system and data conversion.
    /// </summary>
    [TestFixture]
    public class TerrainGenerationAuthoringTests : ECSTestsFixture
    {
        private GameObject testGameObject;
        private TerrainGenerationAuthoring authoring;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            testGameObject = new GameObject("TestAuthoring");
            authoring = testGameObject.AddComponent<TerrainGenerationAuthoring>();
        }
        
        [TearDown]
        public override void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            base.TearDown();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_DefaultValues_AreValid()
        {
            // Create new authoring component and check defaults
            var terrainData = authoring.ToTerrainGenerationData();
            
            Assert.AreEqual(TerrainType.Planar, terrainData.TerrainType);
            Assert.AreEqual(0f, terrainData.MinHeight);
            Assert.AreEqual(10f, terrainData.MaxHeight);
            Assert.AreEqual(3, terrainData.Depth);
            Assert.AreEqual(6, terrainData.Sides);
            Assert.AreEqual(10f, terrainData.Radius);
            Assert.AreEqual(12345, terrainData.Seed);
            Assert.AreEqual(0.1f, terrainData.BaseFrequency, 0.001f);
            Assert.AreEqual(4u, terrainData.Octaves);
            Assert.AreEqual(0.5f, terrainData.Persistence, 0.001f);
            Assert.AreEqual(2f, terrainData.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerrainGenerationData_PlanarConfiguration()
        {
            // Configure for planar terrain
            SetPrivateField(authoring, "terrainType", TerrainType.Planar);
            SetPrivateField(authoring, "minHeight", 1f);
            SetPrivateField(authoring, "maxHeight", 20f);
            SetPrivateField(authoring, "depth", (ushort)5);
            SetPrivateField(authoring, "sides", (ushort)8);
            SetPrivateField(authoring, "radius", 15f);
            SetPrivateField(authoring, "seed", 98765);
            SetPrivateField(authoring, "baseFrequency", 0.2f);
            SetPrivateField(authoring, "octaves", 6u);
            SetPrivateField(authoring, "persistence", 0.3f);
            SetPrivateField(authoring, "lacunarity", 1.8f);
            
            var terrainData = authoring.ToTerrainGenerationData();
            
            Assert.AreEqual(TerrainType.Planar, terrainData.TerrainType);
            Assert.AreEqual(1f, terrainData.MinHeight);
            Assert.AreEqual(20f, terrainData.MaxHeight);
            Assert.AreEqual(5, terrainData.Depth);
            Assert.AreEqual(8, terrainData.Sides);
            Assert.AreEqual(15f, terrainData.Radius);
            Assert.AreEqual(98765, terrainData.Seed);
            Assert.AreEqual(0.2f, terrainData.BaseFrequency, 0.001f);
            Assert.AreEqual(6u, terrainData.Octaves);
            Assert.AreEqual(0.3f, terrainData.Persistence, 0.001f);
            Assert.AreEqual(1.8f, terrainData.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerrainGenerationData_SphericalConfiguration()
        {
            // Configure for spherical terrain
            SetPrivateField(authoring, "terrainType", TerrainType.Spherical);
            SetPrivateField(authoring, "minHeight", 2f);
            SetPrivateField(authoring, "maxHeight", 25f);
            SetPrivateField(authoring, "depth", (ushort)4);
            SetPrivateField(authoring, "seed", 11111);
            SetPrivateField(authoring, "baseFrequency", 0.05f);
            SetPrivateField(authoring, "octaves", 8u);
            SetPrivateField(authoring, "persistence", 0.6f);
            SetPrivateField(authoring, "lacunarity", 2.5f);
            
            var terrainData = authoring.ToTerrainGenerationData();
            
            Assert.AreEqual(TerrainType.Spherical, terrainData.TerrainType);
            Assert.AreEqual(2f, terrainData.MinHeight);
            Assert.AreEqual(25f, terrainData.MaxHeight);
            Assert.AreEqual(4, terrainData.Depth);
            Assert.AreEqual(11111, terrainData.Seed);
            Assert.AreEqual(0.05f, terrainData.BaseFrequency, 0.001f);
            Assert.AreEqual(8u, terrainData.Octaves);
            Assert.AreEqual(0.6f, terrainData.Persistence, 0.001f);
            Assert.AreEqual(2.5f, terrainData.Lacunarity, 0.001f);
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerraceConfigData_RelativeHeightsConversion()
        {
            // Set up relative heights
            var relativeHeights = new float[] { 0.2f, 0.4f, 0.6f, 0.8f };
            SetPrivateField(authoring, "relativeTerraceHeights", relativeHeights);
            SetPrivateField(authoring, "minHeight", 5f);
            SetPrivateField(authoring, "maxHeight", 15f);
            
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.AreEqual(relativeHeights.Length, terraceConfig.TerraceCount);
            Assert.IsTrue(terraceConfig.TerraceHeights.IsCreated);
            Assert.AreEqual(relativeHeights.Length, terraceConfig.TerraceHeights.Value.Length);
            
            // Verify absolute height conversion (minHeight + relative * (maxHeight - minHeight))
            var heightDelta = 15f - 5f; // 10f
            for (int i = 0; i < relativeHeights.Length; i++)
            {
                var expectedAbsoluteHeight = 5f + relativeHeights[i] * heightDelta;
                var actualHeight = terraceConfig.TerraceHeights.Value[i];
                Assert.AreEqual(expectedAbsoluteHeight, actualHeight, 0.001f, 
                    $"Terrace height {i} conversion failed");
            }
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerraceConfigData_EmptyHeights()
        {
            // Test with empty heights array
            SetPrivateField(authoring, "relativeTerraceHeights", new float[0]);
            SetPrivateField(authoring, "minHeight", 0f);
            SetPrivateField(authoring, "maxHeight", 10f);
            
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.AreEqual(0, terraceConfig.TerraceCount);
            Assert.IsTrue(terraceConfig.TerraceHeights.IsCreated);
            Assert.AreEqual(0, terraceConfig.TerraceHeights.Value.Length);
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerraceConfigData_SingleHeight()
        {
            // Test with single height
            var relativeHeights = new float[] { 0.5f };
            SetPrivateField(authoring, "relativeTerraceHeights", relativeHeights);
            SetPrivateField(authoring, "minHeight", 0f);
            SetPrivateField(authoring, "maxHeight", 20f);
            
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.AreEqual(1, terraceConfig.TerraceCount);
            Assert.AreEqual(10f, terraceConfig.TerraceHeights.Value[0], 0.001f);
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerrainGenerationRequest_AsyncEnabled()
        {
            authoring.useAsyncGeneration = true;
            
            var request = authoring.ToTerrainGenerationRequest();
            
            Assert.IsTrue(request.UseAsyncGeneration);
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ToTerrainGenerationRequest_AsyncDisabled()
        {
            authoring.useAsyncGeneration = false;
            
            var request = authoring.ToTerrainGenerationRequest();
            
            Assert.IsFalse(request.UseAsyncGeneration);
        }
        
        [Test]
        public void TerrainGenerationAuthoring_BlobAssetMemoryManagement_NoLeaks()
        {
            // Create multiple terrace configs and ensure proper disposal
            var relativeHeights = new float[] { 0.1f, 0.3f, 0.7f, 0.9f };
            SetPrivateField(authoring, "relativeTerraceHeights", relativeHeights);
            SetPrivateField(authoring, "minHeight", 0f);
            SetPrivateField(authoring, "maxHeight", 10f);
            
            var configs = new TerraceConfigData[5];
            
            // Create multiple configurations
            for (int i = 0; i < configs.Length; i++)
            {
                configs[i] = authoring.ToTerraceConfigData();
                Assert.IsTrue(configs[i].TerraceHeights.IsCreated);
                Assert.AreEqual(relativeHeights.Length, configs[i].TerraceCount);
            }
            
            // Dispose all configurations
            for (int i = 0; i < configs.Length; i++)
            {
                configs[i].TerraceHeights.Dispose();
            }
            
            // Verify no exceptions during cleanup
            Assert.Pass("Memory management test completed successfully");
        }
        
        [Test]
        public void TerrainGenerationAuthoring_DefaultTerraceHeights_AreValid()
        {
            // Test with default terrace heights from authoring component
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.IsTrue(terraceConfig.TerraceCount > 0, "Default terrace heights should not be empty");
            Assert.IsTrue(terraceConfig.TerraceHeights.IsCreated);
            
            // Verify heights are in ascending order and within valid range
            for (int i = 0; i < terraceConfig.TerraceCount; i++)
            {
                var height = terraceConfig.TerraceHeights.Value[i];
                Assert.IsTrue(height >= 0f, $"Terrace height {i} should be non-negative");
                Assert.IsTrue(height <= 10f, $"Terrace height {i} should be within max height");
                
                if (i > 0)
                {
                    var previousHeight = terraceConfig.TerraceHeights.Value[i - 1];
                    Assert.IsTrue(height >= previousHeight, 
                        $"Terrace height {i} should be >= previous height");
                }
            }
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ExtremeBounds_HandledCorrectly()
        {
            // Test with extreme height bounds
            SetPrivateField(authoring, "minHeight", -100f);
            SetPrivateField(authoring, "maxHeight", 1000f);
            var relativeHeights = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            SetPrivateField(authoring, "relativeTerraceHeights", relativeHeights);
            
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.AreEqual(relativeHeights.Length, terraceConfig.TerraceCount);
            
            // Verify extreme bounds conversion
            var heightDelta = 1000f - (-100f); // 1100f
            for (int i = 0; i < relativeHeights.Length; i++)
            {
                var expectedHeight = -100f + relativeHeights[i] * heightDelta;
                var actualHeight = terraceConfig.TerraceHeights.Value[i];
                Assert.AreEqual(expectedHeight, actualHeight, 0.001f);
            }
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_ZeroHeightDelta_HandledCorrectly()
        {
            // Test when min and max height are the same
            SetPrivateField(authoring, "minHeight", 5f);
            SetPrivateField(authoring, "maxHeight", 5f);
            var relativeHeights = new float[] { 0f, 0.5f, 1f };
            SetPrivateField(authoring, "relativeTerraceHeights", relativeHeights);
            
            var terraceConfig = authoring.ToTerraceConfigData();
            
            Assert.AreEqual(relativeHeights.Length, terraceConfig.TerraceCount);
            
            // All heights should be the same (5f) when delta is zero
            for (int i = 0; i < relativeHeights.Length; i++)
            {
                Assert.AreEqual(5f, terraceConfig.TerraceHeights.Value[i], 0.001f);
            }
            
            // Clean up
            terraceConfig.TerraceHeights.Dispose();
        }
        
        [Test]
        public void TerrainGenerationAuthoring_DataConsistency_AcrossMultipleCalls()
        {
            // Verify that multiple calls to conversion methods return consistent data
            SetPrivateField(authoring, "terrainType", TerrainType.Planar);
            SetPrivateField(authoring, "sides", (ushort)6);
            SetPrivateField(authoring, "radius", 12f);
            
            var data1 = authoring.ToTerrainGenerationData();
            var data2 = authoring.ToTerrainGenerationData();
            
            Assert.AreEqual(data1.TerrainType, data2.TerrainType);
            Assert.AreEqual(data1.Sides, data2.Sides);
            Assert.AreEqual(data1.Radius, data2.Radius, 0.001f);
            Assert.AreEqual(data1.Seed, data2.Seed);
            
            var request1 = authoring.ToTerrainGenerationRequest();
            var request2 = authoring.ToTerrainGenerationRequest();
            
            Assert.AreEqual(request1.UseAsyncGeneration, request2.UseAsyncGeneration);
        }
        
        /// <summary>
        /// Helper method to set fields on the authoring component using reflection.
        /// </summary>
        private void SetPrivateField<T>(TerrainGenerationAuthoring target, string fieldName, T value)
        {
            var field = typeof(TerrainGenerationAuthoring).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found");
            field.SetValue(target, value);
        }
    }
}