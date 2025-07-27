using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the Unity mesh creation system and entity management with URP material support.
    /// Updated to test URP material compatibility and runtime material registry.
    /// </summary>
    [TestFixture]
    public class MeshCreationSystemTests : ECSTestsFixture
    {
        private MeshCreationSystem meshCreationSystem;
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            meshCreationSystem = GetOrCreateSystem<MeshCreationSystem>();
        }
        
        [Test]
        public void MeshCreationSystem_CompleteTerrain_CreatesMeshAndGameObject()
        {
            // Create entity with complete mesh data
            var entity = CreateEntityWithCompleteMeshData();
            
            // Verify initial state
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            var initialState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(Entity.Null, initialState.ResultMeshEntity);
            Assert.IsFalse(initialState.IsComplete);
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh creation completed
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            var finalState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(finalState.IsComplete);
            Assert.AreNotEqual(Entity.Null, finalState.ResultMeshEntity);
            
            // Verify mesh entity was created
            var meshEntity = finalState.ResultMeshEntity;
            Assert.IsTrue(Manager.Exists(meshEntity));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(meshEntity));
            Assert.IsTrue(Manager.HasComponent<MeshGameObjectReference>(meshEntity));
            Assert.IsTrue(Manager.HasComponent<LocalTransform>(meshEntity));
            
            // Verify GameObject was created
            var meshReference = Manager.GetComponentData<MeshGameObjectReference>(meshEntity);
#if UNITY_EDITOR
            var gameObject = EditorUtility.InstanceIDToObject(meshReference.GameObjectInstanceID) as GameObject;
            Assert.IsNotNull(gameObject);
            Assert.IsTrue(gameObject.name.Contains("GeneratedTerrain"));
            
            // Verify GameObject components
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            Assert.IsNotNull(meshFilter);
            Assert.IsNotNull(meshRenderer);
            Assert.IsNotNull(meshFilter.mesh);
            Assert.IsNotNull(meshRenderer.material);
            
            // Verify mesh data
            var mesh = meshFilter.mesh;
            Assert.AreEqual(3, mesh.vertexCount);
            Assert.AreEqual(3, mesh.triangles.Length);
            
            // Clean up
            Object.DestroyImmediate(gameObject);
#endif
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_URPMaterialFallback_CreatesValidMaterial()
        {
            // Create entity with complete mesh data but no custom materials
            var entity = CreateEntityWithCompleteMeshData();
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh creation completed
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            var finalState = Manager.GetComponentData<TerrainGenerationState>(entity);
            var meshEntity = finalState.ResultMeshEntity;
            var meshReference = Manager.GetComponentData<MeshGameObjectReference>(meshEntity);
            
#if UNITY_EDITOR
            var gameObject = EditorUtility.InstanceIDToObject(meshReference.GameObjectInstanceID) as GameObject;
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            
            // Verify material was created (should be fallback material)
            Assert.IsNotNull(meshRenderer.material);
            
            // Verify material properties (should be URP compatible or legacy fallback)
            var material = meshRenderer.material;
            Assert.IsNotNull(material.shader);
            
            // Material should either be URP shader or legacy fallback
            bool isURPShader = material.shader.name.Contains("Universal Render Pipeline");
            bool isLegacyShader = material.shader.name.Contains("Standard") || 
                                 material.shader.name.Contains("Diffuse") || 
                                 material.shader.name.Contains("Unlit");
            
            Assert.IsTrue(isURPShader || isLegacyShader, 
                $"Material shader should be URP or legacy compatible. Found: {material.shader.name}");
            
            // Clean up
            Object.DestroyImmediate(gameObject);
#endif
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_WithCustomMaterials_AppliesCorrectly()
        {
            // Create entity with custom material data
            var entity = CreateEntityWithMaterialData();
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh creation completed
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            var finalState = Manager.GetComponentData<TerrainGenerationState>(entity);
            var meshEntity = finalState.ResultMeshEntity;
            
#if UNITY_EDITOR
            var meshReference = Manager.GetComponentData<MeshGameObjectReference>(meshEntity);
            var gameObject = EditorUtility.InstanceIDToObject(meshReference.GameObjectInstanceID) as GameObject;
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            
            // Verify materials were applied
            Assert.IsNotNull(meshRenderer.materials);
            Assert.GreaterOrEqual(meshRenderer.materials.Length, 1);
            
            // Clean up
            Object.DestroyImmediate(gameObject);
#endif
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_BlobAssetCleanup_DisposesCorrectly()
        {
            // Create entity with complete mesh data
            var entity = CreateEntityWithCompleteMeshData();
            
            // Get initial mesh data reference
            var initialMeshData = Manager.GetComponentData<MeshDataComponent>(entity);
            Assert.IsTrue(initialMeshData.Vertices.IsCreated);
            Assert.IsTrue(initialMeshData.Indices.IsCreated);
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify mesh data component was removed (blob assets disposed)
            Assert.IsFalse(Manager.HasComponent<MeshDataComponent>(entity));
            
            // Verify terrain generation completed
            var finalState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.IsTrue(finalState.IsComplete);
            
#if UNITY_EDITOR
            // Clean up GameObject
            var meshReference = Manager.GetComponentData<MeshGameObjectReference>(finalState.ResultMeshEntity);
            var gameObject = EditorUtility.InstanceIDToObject(meshReference.GameObjectInstanceID) as GameObject;
            if (gameObject != null) Object.DestroyImmediate(gameObject);
#endif
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_IncompleteGeneration_DoesNotProcess()
        {
            // Create entity with incomplete generation
            var entity = CreateEntityWithIncompleteMeshData();
            
            // Run mesh creation system
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify no processing occurred
            Assert.IsFalse(Manager.HasComponent<GeneratedTerrainMeshTag>(entity));
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(Entity.Null, state.ResultMeshEntity);
            Assert.IsFalse(state.IsComplete);
            
            // Clean up
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_AlreadyProcessed_DoesNotReprocess()
        {
            // Create entity with complete mesh data
            var entity = CreateEntityWithCompleteMeshData();
            
            // First processing
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            var firstState = Manager.GetComponentData<TerrainGenerationState>(entity);
            var firstMeshEntity = firstState.ResultMeshEntity;
            
            // Second processing attempt
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify no reprocessing occurred
            var secondState = Manager.GetComponentData<TerrainGenerationState>(entity);
            Assert.AreEqual(firstMeshEntity, secondState.ResultMeshEntity);
            
            // Clean up
            CleanupEntityMeshData(entity);
#if UNITY_EDITOR
            if (Manager.Exists(firstMeshEntity))
            {
                var meshRef = Manager.GetComponentData<MeshGameObjectReference>(firstMeshEntity);
                var go = EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
                if (go != null) Object.DestroyImmediate(go);
            }
#endif
        }
        
        [Test]
        public void MeshCreationSystem_MultipleEntities_ProcessesAll()
        {
            // Create multiple entities
            var entity1 = CreateEntityWithCompleteMeshData(vertices: CreateTriangleVertices());
            var entity2 = CreateEntityWithCompleteMeshData(vertices: CreateQuadVertices());
            var entity3 = CreateEntityWithCompleteMeshData(vertices: CreateTriangleVertices());
            
            // Process all entities
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Verify all entities were processed
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity1));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity2));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(entity3));
            
            var state1 = Manager.GetComponentData<TerrainGenerationState>(entity1);
            var state2 = Manager.GetComponentData<TerrainGenerationState>(entity2);
            var state3 = Manager.GetComponentData<TerrainGenerationState>(entity3);
            
            Assert.IsTrue(state1.IsComplete);
            Assert.IsTrue(state2.IsComplete);
            Assert.IsTrue(state3.IsComplete);
            
            Assert.AreNotEqual(Entity.Null, state1.ResultMeshEntity);
            Assert.AreNotEqual(Entity.Null, state2.ResultMeshEntity);
            Assert.AreNotEqual(Entity.Null, state3.ResultMeshEntity);
            
            // Verify all mesh entities are different
            Assert.AreNotEqual(state1.ResultMeshEntity, state2.ResultMeshEntity);
            Assert.AreNotEqual(state1.ResultMeshEntity, state3.ResultMeshEntity);
            Assert.AreNotEqual(state2.ResultMeshEntity, state3.ResultMeshEntity);
            
            // Clean up
            CleanupMultipleEntities(entity1, entity2, entity3);
        }
        
        [Test]
        public void MeshCreationSystem_MeshEntity_HasCorrectComponents()
        {
            var entity = CreateEntityWithCompleteMeshData();
            
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            var meshEntity = state.ResultMeshEntity;
            
            // Verify mesh entity components
            Assert.IsTrue(Manager.HasComponent<LocalTransform>(meshEntity));
            Assert.IsTrue(Manager.HasComponent<MeshGameObjectReference>(meshEntity));
            Assert.IsTrue(Manager.HasComponent<GeneratedTerrainMeshTag>(meshEntity));
            
            // Verify transform defaults
            var transform = Manager.GetComponentData<LocalTransform>(meshEntity);
            Assert.AreEqual(float3.zero, transform.Position);
            Assert.AreEqual(quaternion.identity, transform.Rotation);
            Assert.AreEqual(1f, transform.Scale);
            
            // Clean up
            CleanupEntityMeshData(entity);
#if UNITY_EDITOR
            var meshRef = Manager.GetComponentData<MeshGameObjectReference>(meshEntity);
            var go = EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
            if (go != null) Object.DestroyImmediate(go);
#endif
        }
        
        [Test]
        public void MeshCreationSystem_CreatedMesh_HasValidGeometry()
        {
            // Create entity with specific vertex data
            var vertices = new float3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0.5f, 1, 0),
                new(0.5f, 0, 1)
            };
            var indices = new int[] { 0, 1, 2, 0, 2, 3 };
            
            var entity = CreateEntityWithCompleteMeshData(vertices, indices);
            
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            var state = Manager.GetComponentData<TerrainGenerationState>(entity);
            var meshEntity = state.ResultMeshEntity;
            var meshRef = Manager.GetComponentData<MeshGameObjectReference>(meshEntity);
            
#if UNITY_EDITOR
            var gameObject = EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
            var mesh = gameObject.GetComponent<MeshFilter>().mesh;
            
            // Verify mesh geometry
            Assert.AreEqual(4, mesh.vertexCount);
            Assert.AreEqual(6, mesh.triangles.Length);
            
            // Verify vertex positions
            var meshVertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                var expected = vertices[i];
                var actual = meshVertices[i];
                Assert.AreEqual(expected.x, actual.x, 0.001f);
                Assert.AreEqual(expected.y, actual.y, 0.001f);
                Assert.AreEqual(expected.z, actual.z, 0.001f);
            }
            
            // Verify indices
            var meshIndices = mesh.triangles;
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.AreEqual(indices[i], meshIndices[i]);
            }
            
            // Clean up
            Object.DestroyImmediate(gameObject);
#endif
            
            CleanupEntityMeshData(entity);
        }
        
        [Test]
        public void MeshCreationSystem_GameObjectNaming_IsConsistent()
        {
            var entity1 = CreateEntityWithCompleteMeshData();
            var entity2 = CreateEntityWithCompleteMeshData();
            
            meshCreationSystem.Update();
            CompleteAllJobs();
            
            // Get GameObjects
            var state1 = Manager.GetComponentData<TerrainGenerationState>(entity1);
            var state2 = Manager.GetComponentData<TerrainGenerationState>(entity2);
            
            var meshRef1 = Manager.GetComponentData<MeshGameObjectReference>(state1.ResultMeshEntity);
            var meshRef2 = Manager.GetComponentData<MeshGameObjectReference>(state2.ResultMeshEntity);
            
#if UNITY_EDITOR
            var go1 = EditorUtility.InstanceIDToObject(meshRef1.GameObjectInstanceID) as GameObject;
            var go2 = EditorUtility.InstanceIDToObject(meshRef2.GameObjectInstanceID) as GameObject;
            
            // Verify naming pattern
            Assert.IsTrue(go1.name.StartsWith("GeneratedTerrain_"));
            Assert.IsTrue(go2.name.StartsWith("GeneratedTerrain_"));
            Assert.AreNotEqual(go1.name, go2.name); // Should have different entity indices
            
            // Clean up
            Object.DestroyImmediate(go1);
            Object.DestroyImmediate(go2);
#endif
            
            CleanupEntityMeshData(entity1);
            CleanupEntityMeshData(entity2);
        }
        
        #region Helper Methods
        
        private Entity CreateEntityWithCompleteMeshData(float3[] vertices = null, int[] indices = null)
        {
            vertices ??= CreateTriangleVertices();
            indices ??= new int[] { 0, 1, 2 };
            
            var entity = CreateEntity(typeof(MeshDataComponent), typeof(TerrainGenerationState));
            
            // Create mesh data
            var meshData = CreateMeshDataComponent(vertices, indices);
            Manager.SetComponentData(entity, meshData);
            
            // Set generation state to complete
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.Complete,
                IsComplete = false, // Will be set to true by the system
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.SetComponentData(entity, state);
            
            return entity;
        }
        
        private Entity CreateEntityWithMaterialData()
        {
            var entity = CreateEntityWithCompleteMeshData();
            
            // Add material data component
            var materialData = CreateTestMaterialData();
            Manager.AddComponentData(entity, materialData);
            
            return entity;
        }
        
        private TerrainMaterialData CreateTestMaterialData()
        {
            // Create test material instance IDs (in real scenario these would be from actual materials)
            var materialIds = new int[] { 1001, 1002 };
            
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var materialArray = ref builder.ConstructRoot<BlobArray<int>>();
            var materialArrayBuilder = builder.Allocate(ref materialArray, materialIds.Length);
            
            for (int i = 0; i < materialIds.Length; i++)
            {
                materialArrayBuilder[i] = materialIds[i];
            }
            
            var materialBlob = builder.CreateBlobAssetReference<BlobArray<int>>(Allocator.Persistent);
            
            return new TerrainMaterialData
            {
                MaterialCount = materialIds.Length,
                MaterialInstanceIDs = materialBlob
            };
        }
        
        private Entity CreateEntityWithIncompleteMeshData()
        {
            var entity = CreateEntity(typeof(MeshDataComponent), typeof(TerrainGenerationState));
            
            // Create mesh data
            var vertices = CreateTriangleVertices();
            var indices = new int[] { 0, 1, 2 };
            var meshData = CreateMeshDataComponent(vertices, indices);
            Manager.SetComponentData(entity, meshData);
            
            // Set generation state to incomplete
            var state = new TerrainGenerationState
            {
                CurrentPhase = GenerationPhase.Sculpting, // Not complete
                IsComplete = false,
                HasError = false,
                ResultMeshEntity = Entity.Null
            };
            Manager.SetComponentData(entity, state);
            
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
        
        private static float3[] CreateTriangleVertices()
        {
            return new float3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0.5f, 1, 0)
            };
        }
        
        private static float3[] CreateQuadVertices()
        {
            return new float3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 0, 1),
                new(0, 0, 1)
            };
        }
        
        private void CleanupMultipleEntities(params Entity[] entities)
        {
            foreach (var entity in entities)
            {
                CleanupEntityMeshData(entity);
                
                if (Manager.HasComponent<TerrainGenerationState>(entity))
                {
                    var state = Manager.GetComponentData<TerrainGenerationState>(entity);
                    if (state.ResultMeshEntity != Entity.Null && Manager.Exists(state.ResultMeshEntity))
                    {
#if UNITY_EDITOR
                        var meshRef = Manager.GetComponentData<MeshGameObjectReference>(state.ResultMeshEntity);
                        var go = EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
                        if (go != null) Object.DestroyImmediate(go);
#endif
                    }
                }
            }
        }
        
        #endregion
    }
}