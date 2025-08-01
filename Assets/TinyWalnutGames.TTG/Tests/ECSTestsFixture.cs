using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Base fixture for ECS tests that provides World and EntityManager setup/teardown.
    /// </summary>
    public abstract class ECSTestsFixture
    {
        protected World World { get; private set; }
        protected EntityManager Manager { get; private set; }
        private World _previousWorld;

        [SetUp]
        public virtual void Setup()
        {
            // Create a test world
            World = new World("Test World");
            Manager = World.EntityManager;

            // Set up the test world to be used by default systems
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            World.DefaultGameObjectInjectionWorld = World;
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up any remaining entities
            if (World != null && World.IsCreated)
            {
                // Dispose all entities and their components
                Manager.DestroyEntity(Manager.UniversalQuery);
                
                // Dispose the world
                World.Dispose();
                World = null;
            }

            // Restore previous default world
            World.DefaultGameObjectInjectionWorld = _previousWorld;

            // Clean up any lingering GameObjects created during tests
            var testGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.Contains("GeneratedTerrain") || go.name.Contains("TestTerrain"))
                .ToArray();

            foreach (var go in testGameObjects)
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Create an entity in the test world.
        /// </summary>
        protected Entity CreateEntity()
        {
            return Manager.CreateEntity();
        }

        /// <summary>
        /// Create an entity with the specified component types.
        /// </summary>
        protected Entity CreateEntity(params ComponentType[] componentTypes)
        {
            return Manager.CreateEntity(componentTypes);
        }

        /// <summary>
        /// Get or create a system in the test world.
        /// </summary>
        protected T GetOrCreateSystem<T>() where T : ComponentSystemBase
        {
            return World.GetOrCreateSystemManaged<T>();
        }

        /// <summary>
        /// Helper to complete any pending jobs before assertions.
        /// </summary>
        protected void CompleteAllJobs()
        {
            World.EntityManager.CompleteAllTrackedJobs();
        }
        
        /// <summary>
        /// Helper to clean up mesh data blob assets from an entity.
        /// </summary>
        protected void CleanupEntityMeshData(Entity entity)
        {
            if (Manager.HasComponent<MeshDataComponent>(entity))
            {
                var meshData = Manager.GetComponentData<MeshDataComponent>(entity);

                if (meshData.Vertices.IsCreated)
                {
                    meshData.Vertices.Dispose();
                    meshData.Vertices = default; // Prevent double-dispose
                }

                if (meshData.Indices.IsCreated)
                {
                    meshData.Indices.Dispose();
                    meshData.Indices = default; // Prevent double-dispose
                }

                // Write back the updated struct to the entity
                Manager.SetComponentData(entity, meshData);
            }

            if (Manager.HasComponent<TerraceConfigData>(entity))
            {
                var terraceConfig = Manager.GetComponentData<TerraceConfigData>(entity);

                if (terraceConfig.TerraceHeights.IsCreated)
                {
                    terraceConfig.TerraceHeights.Dispose();
                    terraceConfig.TerraceHeights = default; // Prevent double-dispose
                }

                // Write back the updated struct to the entity
                Manager.SetComponentData(entity, terraceConfig);
            }
        }
    }
}