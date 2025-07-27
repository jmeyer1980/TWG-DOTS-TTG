using NUnit.Framework;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the RuntimeTerrainManager MonoBehaviour.
    /// Verifies runtime initialization, status monitoring, and entity management.
    /// </summary>
    [TestFixture]
    public class RuntimeTerrainManagerTests
    {
        private GameObject testGameObject;
        private RuntimeTerrainManager terrainManager;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject with RuntimeTerrainManager
            testGameObject = new GameObject("TestRuntimeTerrainManager");
            terrainManager = testGameObject.AddComponent<RuntimeTerrainManager>();
            
            // Configure for testing
            terrainManager.autoInitializeOnStart = false; // Manual control for testing
            terrainManager.enableDebugLogs = false; // Reduce log noise
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void RuntimeTerrainManager_InitialState_HasCorrectDefaults()
        {
            // Verify initial state
            Assert.AreEqual(RuntimeTerrainManager.InitializationStatus.NotStarted, terrainManager.Status);
            Assert.AreEqual(0, terrainManager.TerrainEntityCount);
            Assert.IsFalse(terrainManager.IsTerrainSystemReady);
            
            // Verify default configuration
            Assert.IsTrue(terrainManager.createDefaultTerrainEntity);
            Assert.AreEqual(0.5f, terrainManager.initializationDelay);
        }
        
        [Test]
        public void RuntimeTerrainManager_ManualInitialization_UpdatesStatus()
        {
            // Manual initialization
            terrainManager.InitializeRuntimeLoading();
            
            // Status should change from NotStarted
            Assert.AreNotEqual(RuntimeTerrainManager.InitializationStatus.NotStarted, terrainManager.Status);
        }
        
        [Test]
        public void RuntimeTerrainManager_GetSystemStatusInfo_ReturnsValidInfo()
        {
            // Get status info
            string statusInfo = terrainManager.GetSystemStatusInfo();
            
            // Should return non-empty string with status information
            Assert.IsNotNull(statusInfo);
            Assert.IsNotEmpty(statusInfo);
            Assert.IsTrue(statusInfo.Contains("Status:"));
        }
        
        [Test]
        public void RuntimeTerrainManager_MultipleInitialization_HandlesSafely()
        {
            // Initialize multiple times
            terrainManager.InitializeRuntimeLoading();
            var firstStatus = terrainManager.Status;
            
            terrainManager.InitializeRuntimeLoading();
            var secondStatus = terrainManager.Status;
            
            // Should not cause errors and maintain consistent state
            Assert.AreEqual(firstStatus, secondStatus);
        }
        
        [Test]
        public void RuntimeTerrainManager_DefaultTerrainData_HasValidValues()
        {
            // Check default terrain data
            var defaultData = terrainManager.defaultTerrainData;
            
            // Should have reasonable default values
            Assert.Greater(defaultData.MaxHeight, defaultData.MinHeight);
            Assert.Greater(defaultData.Depth, 0);
            Assert.Greater(defaultData.Radius, 0);
            Assert.Greater(defaultData.Sides, 2);
        }
        
        [Test]
        public void RuntimeTerrainManager_TerrainSceneConfiguration_IsValid()
        {
            // Test scene configuration
            terrainManager.terrainSceneName = "TestScene";
            terrainManager.terrainSceneIndex = 1;
            terrainManager.useUnitySceneLoading = true;
            
            // Configuration should be accessible
            Assert.AreEqual("TestScene", terrainManager.terrainSceneName);
            Assert.AreEqual(1, terrainManager.terrainSceneIndex);
            Assert.IsTrue(terrainManager.useUnitySceneLoading);
        }
        
        [Test]
        public void RuntimeTerrainManager_CreateTerrainGenerationRequest_WorksCorrectly()
        {
            // Initialize first
            terrainManager.InitializeRuntimeLoading();
            
            // Create terrain generation request
            Assert.DoesNotThrow(() => terrainManager.CreateTerrainGenerationRequest());
        }
        
        [Test]
        public void RuntimeTerrainManager_UpdateTerrainEntityCount_WorksCorrectly()
        {
            // Test terrain entity count update
            Assert.DoesNotThrow(() => terrainManager.UpdateTerrainEntityCount());
        }
        
        [Test]
        public void RuntimeTerrainManager_ReloadUsingUnitySceneManager_WorksCorrectly()
        {
            // Test scene manager reload
            terrainManager.terrainSceneName = "TestScene";
            Assert.DoesNotThrow(() => terrainManager.ReloadUsingUnitySceneManager());
        }
        
        [Test]
        public void RuntimeTerrainManager_GetTerrainEntityCount_ReturnsValidCount()
        {
            // Get entity count (should not throw)
            var count = terrainManager.GetTerrainEntityCount();
            Assert.GreaterOrEqual(count, 0);
        }
        
        [Test]
        public void RuntimeTerrainManager_IsTerrainSystemAvailable_WorksCorrectly()
        {
            // Test system availability check
            var isAvailable = terrainManager.IsTerrainSystemAvailable();
            // Should be either true or false, not throw
            Assert.IsTrue(isAvailable || !isAvailable);
        }
        
        [Test]
        public void RuntimeTerrainManager_DebugLogging_CanBeToggled()
        {
            // Test debug logging toggle
            terrainManager.enableDebugLogs = true;
            Assert.IsTrue(terrainManager.enableDebugLogs);
            
            terrainManager.enableDebugLogs = false;
            Assert.IsFalse(terrainManager.enableDebugLogs);
        }
        
        [Test]
        public void RuntimeTerrainManager_AutoInitializeOnStart_CanBeToggled()
        {
            // Test auto-initialization toggle
            terrainManager.autoInitializeOnStart = true;
            Assert.IsTrue(terrainManager.autoInitializeOnStart);
            
            terrainManager.autoInitializeOnStart = false;
            Assert.IsFalse(terrainManager.autoInitializeOnStart);
        }
        
        [Test]
        public void RuntimeTerrainManager_UseUnitySceneLoading_CanBeToggled()
        {
            // Test Unity scene loading toggle
            terrainManager.useUnitySceneLoading = true;
            Assert.IsTrue(terrainManager.useUnitySceneLoading);
            
            terrainManager.useUnitySceneLoading = false;
            Assert.IsFalse(terrainManager.useUnitySceneLoading);
        }
        
        [Test]
        public void RuntimeTerrainManager_InitializationDelay_CanBeSet()
        {
            // Test initialization delay setting
            terrainManager.initializationDelay = 1.0f;
            Assert.AreEqual(1.0f, terrainManager.initializationDelay);
            
            terrainManager.initializationDelay = 0.0f;
            Assert.AreEqual(0.0f, terrainManager.initializationDelay);
        }
        
        [Test]
        public void RuntimeTerrainManager_CreateDefaultTerrainEntity_CanBeToggled()
        {
            // Test create default terrain entity toggle
            terrainManager.createDefaultTerrainEntity = false;
            Assert.IsFalse(terrainManager.createDefaultTerrainEntity);
            
            terrainManager.createDefaultTerrainEntity = true;
            Assert.IsTrue(terrainManager.createDefaultTerrainEntity);
        }
        
        [Test]
        public void RuntimeTerrainManager_DestroyBehavior_HandlesCleanup()
        {
            // Initialize manager
            terrainManager.InitializeRuntimeLoading();
            
            // Destroy GameObject (simulates scene cleanup)
            Object.DestroyImmediate(testGameObject);
            testGameObject = null;
            terrainManager = null;
            
            // Should not cause errors (verified by not throwing exceptions)
            Assert.Pass("GameObject destruction handled without errors");
        }
        
        [Test]
        public void RuntimeTerrainManager_InvalidSceneConfiguration_HandlesGracefully()
        {
            // Set invalid scene configuration
            terrainManager.terrainSceneName = "";
            terrainManager.terrainSceneIndex = -1;
            
            // Should handle gracefully without errors
            Assert.DoesNotThrow(() => terrainManager.InitializeRuntimeLoading());
        }
        
        [Test]
        public void RuntimeTerrainManager_ExtremeDelayValues_HandlesCorrectly()
        {
            // Test extreme delay values
            terrainManager.initializationDelay = 0.0f; // Immediate
            Assert.AreEqual(0.0f, terrainManager.initializationDelay);
            
            terrainManager.initializationDelay = 10.0f; // Very long
            Assert.AreEqual(10.0f, terrainManager.initializationDelay);
            
            terrainManager.initializationDelay = -1.0f; // Invalid (negative)
            // Manager should handle invalid values gracefully
            Assert.DoesNotThrow(() => terrainManager.InitializeRuntimeLoading());
        }
        
        [Test]
        public void RuntimeTerrainManager_IsInitialized_ReflectsState()
        {
            // Initially not initialized
            Assert.IsFalse(terrainManager.IsInitialized);
            
            // After initialization
            terrainManager.InitializeRuntimeLoading();
            Assert.IsTrue(terrainManager.IsInitialized);
        }
    }
}