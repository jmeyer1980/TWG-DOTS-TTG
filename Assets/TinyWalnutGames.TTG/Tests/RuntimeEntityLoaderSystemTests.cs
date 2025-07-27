using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the RuntimeEntityLoaderSystem to verify scene loading request processing.
    /// Ensures proper runtime initialization and scene loading coordination.
    /// </summary>
    [TestFixture]
    public class RuntimeEntityLoaderSystemTests : ECSTestsFixture
    {
        private RuntimeEntityLoaderSystem loaderSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            loaderSystem = GetOrCreateSystem<RuntimeEntityLoaderSystem>();
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_InitialState_HasCorrectDefaults()
        {
            // Verify initial state
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
            Assert.AreEqual(0, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_SceneLoadingRequest_ProcessesCorrectly()
        {
            // Create scene loading request entity
            var entity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = "TestScene",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity, request);
            
            // Verify pending request
            Assert.IsTrue(loaderSystem.HasPendingSceneRequests());
            
            // Process the request
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Verify request was processed
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity));
            Assert.AreEqual(1, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_MultipleRequests_ProcessesAll()
        {
            const int requestCount = 5;
            var entities = new Entity[requestCount];
            
            // Create multiple scene loading requests
            for (int i = 0; i < requestCount; i++)
            {
                entities[i] = CreateEntity(typeof(RuntimeSceneLoadingRequest));
                
                var request = new RuntimeSceneLoadingRequest
                {
                    SceneName = $"TestScene_{i}",
                    SceneIndex = i,
                    UseUnitySceneManager = true
                };
                Manager.SetComponentData(entities[i], request);
            }
            
            // Verify all pending
            Assert.IsTrue(loaderSystem.HasPendingSceneRequests());
            
            // Process all requests
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Verify all were processed
            for (int i = 0; i < requestCount; i++)
            {
                Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entities[i]));
            }
            
            Assert.AreEqual(requestCount, loaderSystem.GetProcessedSceneCount());
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_EmptySceneName_HandlesGracefully()
        {
            // Create request with empty scene name
            var entity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = "",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity, request);
            
            // Process request - should handle gracefully
            Assert.DoesNotThrow(() =>
            {
                loaderSystem.Update();
                CompleteAllJobs();
            });
            
            // Should still be marked as processed even if empty
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity));
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_InvalidSceneIndex_HandlesGracefully()
        {
            // Create request with invalid scene index
            var entity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = "ValidScene",
                SceneIndex = -999, // Invalid index
                UseUnitySceneManager = false
            };
            Manager.SetComponentData(entity, request);
            
            // Process request - should handle gracefully
            Assert.DoesNotThrow(() =>
            {
                loaderSystem.Update();
                CompleteAllJobs();
            });
            
            // Should still be marked as processed
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity));
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_DifferentLoadingMethods_ProcessesBoth()
        {
            // Create request using Unity Scene Manager
            var entity1 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request1 = new RuntimeSceneLoadingRequest
            {
                SceneName = "Scene1",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity1, request1);
            
            // Create request using scene index
            var entity2 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request2 = new RuntimeSceneLoadingRequest
            {
                SceneName = "Scene2",
                SceneIndex = 1,
                UseUnitySceneManager = false
            };
            Manager.SetComponentData(entity2, request2);
            
            // Process both requests
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Verify both were processed
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity1));
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity2));
            Assert.AreEqual(2, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_RepeatedUpdates_DoesNotReprocessRequests()
        {
            // Create scene loading request
            var entity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = "TestScene",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity, request);
            
            // Process first time
            loaderSystem.Update();
            CompleteAllJobs();
            
            var firstProcessedCount = loaderSystem.GetProcessedSceneCount();
            Assert.AreEqual(1, firstProcessedCount);
            
            // Process again
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Should not reprocess
            Assert.AreEqual(firstProcessedCount, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_NewRequestsAfterProcessing_ProcessesNewOnes()
        {
            // Create and process first request
            var entity1 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request1 = new RuntimeSceneLoadingRequest
            {
                SceneName = "Scene1",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity1, request1);
            
            loaderSystem.Update();
            CompleteAllJobs();
            
            Assert.AreEqual(1, loaderSystem.GetProcessedSceneCount());
            
            // Create second request
            var entity2 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request2 = new RuntimeSceneLoadingRequest
            {
                SceneName = "Scene2",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity2, request2);
            
            // Process again
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Should process the new request
            Assert.AreEqual(2, loaderSystem.GetProcessedSceneCount());
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(entity2));
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_LargeNumberOfRequests_HandlesEfficiently()
        {
            const int requestCount = 100;
            var entities = new Entity[requestCount];
            
            // Create many scene loading requests
            for (int i = 0; i < requestCount; i++)
            {
                entities[i] = CreateEntity(typeof(RuntimeSceneLoadingRequest));
                var request = new RuntimeSceneLoadingRequest
                {
                    SceneName = $"Scene_{i}",
                    SceneIndex = i,
                    UseUnitySceneManager = true
                };
                Manager.SetComponentData(entities[i], request);
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Process all requests
            loaderSystem.Update();
            CompleteAllJobs();
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (1 second for 100 requests)
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Processing should complete efficiently");
            
            // Verify all were processed
            Assert.AreEqual(requestCount, loaderSystem.GetProcessedSceneCount());
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_EntityDestruction_UpdatesCountsCorrectly()
        {
            // Create scene loading request
            var entity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var request = new RuntimeSceneLoadingRequest
            {
                SceneName = "TestScene",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(entity, request);
            
            // Process the request
            loaderSystem.Update();
            CompleteAllJobs();
            
            Assert.AreEqual(1, loaderSystem.GetProcessedSceneCount());
            
            // Destroy the entity
            Manager.DestroyEntity(entity);
            CompleteAllJobs();
            
            // Update system again
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Counts should be updated to reflect destroyed entity
            Assert.AreEqual(0, loaderSystem.GetProcessedSceneCount());
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_MixedEntities_ProcessesOnlySceneRequests()
        {
            // Create scene loading request
            var sceneEntity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var sceneRequest = new RuntimeSceneLoadingRequest
            {
                SceneName = "TestScene",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            Manager.SetComponentData(sceneEntity, sceneRequest);
            
            // Create unrelated entities
            var terrainEntity = CreateEntity(typeof(TerrainGenerationData));
            var meshEntity = CreateEntity(typeof(MeshDataComponent));
            
            // Process system
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Only scene request should be processed
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(sceneEntity));
            Assert.IsFalse(Manager.HasComponent<RuntimeSceneLoadingProcessed>(terrainEntity));
            Assert.IsFalse(Manager.HasComponent<RuntimeSceneLoadingProcessed>(meshEntity));
            
            Assert.AreEqual(1, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_StatusMonitoring_ReflectsCurrentState()
        {
            // Initially no pending requests
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
            Assert.AreEqual(0, loaderSystem.GetProcessedSceneCount());
            
            // Create requests
            var entity1 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var entity2 = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var request1 = new RuntimeSceneLoadingRequest { SceneName = "Scene1", UseUnitySceneManager = true };
            var request2 = new RuntimeSceneLoadingRequest { SceneName = "Scene2", UseUnitySceneManager = true };
            
            Manager.SetComponentData(entity1, request1);
            Manager.SetComponentData(entity2, request2);
            
            // Should show pending requests
            Assert.IsTrue(loaderSystem.HasPendingSceneRequests());
            Assert.AreEqual(0, loaderSystem.GetProcessedSceneCount());
            
            // Process requests
            loaderSystem.Update();
            CompleteAllJobs();
            
            // Should show processed requests
            Assert.IsFalse(loaderSystem.HasPendingSceneRequests());
            Assert.AreEqual(2, loaderSystem.GetProcessedSceneCount());
        }
        
        [Test]
        public void RuntimeEntityLoaderSystem_ErrorHandling_ContinuesProcessingOtherRequests()
        {
            // Create mix of valid and potentially problematic requests
            var validEntity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            var problematicEntity = CreateEntity(typeof(RuntimeSceneLoadingRequest));
            
            var validRequest = new RuntimeSceneLoadingRequest
            {
                SceneName = "ValidScene",
                SceneIndex = -1,
                UseUnitySceneManager = true
            };
            
            var problematicRequest = new RuntimeSceneLoadingRequest
            {
                SceneName = "", // Empty name
                SceneIndex = -999, // Invalid index
                UseUnitySceneManager = false
            };
            
            Manager.SetComponentData(validEntity, validRequest);
            Manager.SetComponentData(problematicEntity, problematicRequest);
            
            // Process should handle both without throwing
            Assert.DoesNotThrow(() =>
            {
                loaderSystem.Update();
                CompleteAllJobs();
            });
            
            // Both should be marked as processed
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(validEntity));
            Assert.IsTrue(Manager.HasComponent<RuntimeSceneLoadingProcessed>(problematicEntity));
            Assert.AreEqual(2, loaderSystem.GetProcessedSceneCount());
        }
    }
}