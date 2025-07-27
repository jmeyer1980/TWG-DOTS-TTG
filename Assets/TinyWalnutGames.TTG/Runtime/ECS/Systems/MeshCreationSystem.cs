using System.Collections.Generic;
using System.Diagnostics;
using TinyWalnutGames.TTG.TerrainGeneration;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Mesh;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// System for creating Unity Meshes from mesh data on the main thread.
    /// Handles URP/BRP material compatibility and uses enableable components for test-friendly memory management.
    /// 
    /// ENABLEABLE COMPONENT SOLUTION:
    /// Instead of removing MeshDataComponent after mesh creation, we disable it.
    /// This allows tests to verify the component exists while preventing reprocessing.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MeshCreationSystem : SystemBase
    {
        private Material _defaultMaterial;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            // Create a default material that supports URP and BRP
            _defaultMaterial = CreateFallbackMaterial();
        }
        
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Process entities that need mesh creation
            // NOTE: Only processes ENABLED MeshDataComponent - disabled ones are "processed"
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .WithNone<GeneratedTerrainMeshTag>()
                .ForEach((Entity entity, ref TerrainGenerationState generationState, in MeshDataComponent meshData) =>
                {
                    // Only process entities that have completed terrain generation but haven't had their mesh created yet
                    if (generationState.CurrentPhase != GenerationPhase.Complete)
                        return;

// Create Unity Mesh from mesh data
var mesh = CreateUnityMesh(meshData);

// Get materials for the entity
var materials = GetMaterialsForEntity(entity);

// Create a GameObject with the mesh (traditional Unity rendering)
var gameObject = CreateMeshGameObject(mesh, $"GeneratedTerrain_{entity.Index}", materials);

// Create a companion entity to track the GameObject
var meshEntity = CreateMeshEntity(ecb, gameObject);

// Update the generation state with the result mesh entity
generationState.IsComplete = true;
generationState.ResultMeshEntity = meshEntity;

// Add tag to prevent reprocessing
ecb.AddComponent<GeneratedTerrainMeshTag>(entity);

// ENABLEABLE COMPONENT SOLUTION: Disable instead of remove
// This preserves the component for test verification while marking it as "processed"
ecb.SetComponentEnabled<MeshDataComponent>(entity, false);

UnityEngine.Debug.Log($"Terrain generation completed for entity {entity}. Created mesh entity {meshEntity} with GameObject '{gameObject.name}' containing {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
UnityEngine.Debug.Log($"ENABLEABLE SOLUTION: Disabled MeshDataComponent for entity {entity.Index}:{entity.Version} - component preserved for tests but marked as processed");
                }).WithStructuralChanges().Run();

ecb.Playback(EntityManager);
ecb.Dispose();
        }
        
    Material[] GetMaterialsForEntity(Entity entity)
        {
            // Check if entity has material data
            if (EntityManager.HasComponent<TerrainMaterialData>(entity))
            {
                var materialData = EntityManager.GetComponentData<TerrainMaterialData>(entity);
                var materials = new Material[materialData.MaterialCount];

                for (int i = 0; i < materialData.MaterialCount; i++)
                {
                    int instanceID = materialData.MaterialInstanceIDs.Value[i];
                    if (instanceID != 0)
                    {
#if UNITY_EDITOR
                        var material = EditorUtility.InstanceIDToObject(instanceID) as Material;
                        materials[i] = material != null ? material : GetDefaultMaterial();
#else
                        // In builds, we can't use EditorUtility, so we need a different approach
                        // Try to find materials by name from Resources or a material registry
                        var runtimeMaterial = GetMaterialFromRegistry(instanceID);
                        if (runtimeMaterial != null)
                            materials[i] = runtimeMaterial;
                        else
                            materials[i] = GetDefaultMaterial();
#endif
                    }
                    else
                    {
                        materials[i] = GetDefaultMaterial();
                    }
                }

                return materials;
            }

            // Fallback to single default material
            return new Material[] { GetDefaultMaterial() };
        }

// Enhanced material registry system for runtime builds
private static Dictionary<int, Material> materialRegistry;
private static bool materialRegistryInitialized = false;

        private Material GetMaterialFromRegistry(int instanceID)
        {
            if (!materialRegistryInitialized)
            {
                InitializeMaterialRegistry();
            }

            Material material = null;
            materialRegistry?.TryGetValue(instanceID, out material);
            if (material == null)
                return GetDefaultMaterial();
            return material;
        }

private void InitializeMaterialRegistry()
{
    materialRegistryInitialized = true;
    materialRegistry = new Dictionary<int, Material>();

    // Load all materials from Resources folders
    var allMaterials = Resources.LoadAll<Material>("");
    foreach (var material in allMaterials)
    {
        if (material != null)
        {
            materialRegistry[material.GetInstanceID()] = material;
        }
    }

    // Also register any materials found in the scene using the newer API
    var sceneObjects = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
    foreach (var renderer in sceneObjects)
    {
        if (renderer.materials != null)
        {
            foreach (var material in renderer.materials)
            {
                if (material != null && !materialRegistry.ContainsKey(material.GetInstanceID()))
                {
                    materialRegistry[material.GetInstanceID()] = material;
                }
            }
        }
    }

    UnityEngine.Debug.Log($"MeshCreationSystem: Initialized material registry with {materialRegistry.Count} materials for runtime builds");
}

private GameObject CreateMeshGameObject(Mesh mesh, string name, Material[] materials)
{
    // Create a new GameObject with MeshRenderer and MeshFilter
    var gameObject = new GameObject(name);

    // Add and configure MeshFilter
    var meshFilter = gameObject.AddComponent<MeshFilter>();
    meshFilter.mesh = mesh;

    // Add and configure MeshRenderer with materials
    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
    meshRenderer.materials = materials;

    return gameObject;
}

        private Material GetDefaultMaterial()
        {
            // Return the pre-created material to avoid shader finding issues
            if (_defaultMaterial != null)
                return _defaultMaterial;

            // Fallback creation if somehow the material is null
            _defaultMaterial = CreateFallbackMaterial();
            return _defaultMaterial;
        }

Material CreateFallbackMaterial()
{
    // URP Priority: Try URP shaders first (Unity 6 / URP compatible)
    var shader = Shader.Find("Universal Render Pipeline/Lit");

    // If URP Lit not found, try URP Unlit (more basic but still URP)
    if (shader == null)
        shader = Shader.Find("Universal Render Pipeline/Unlit");

    // If no URP shaders, try standard legacy shaders (for non-URP projects)
    if (shader == null)
        shader = Shader.Find("Standard");
    if (shader == null)
        shader = Shader.Find("Legacy Shaders/Diffuse");
    if (shader == null)
        shader = Shader.Find("Unlit/Color");
    if (shader == null)
        shader = Shader.Find("Sprites/Default");

    // Final fallback: Use any available shader
    if (shader == null)
    {
        // Create a very basic unlit material with error shader
        var fallbackShader = Shader.Find("Hidden/InternalErrorShader");
        if (fallbackShader == null)
            fallbackShader = Shader.Find("GUI/Text Shader");

        var material = new Material(fallbackShader)
        {
            color = new Color(0.0f, 1.0f, 0.0f, 1.0f), // Bright green for visibility
            name = "TTG_URPFallbackMaterial"
        };

        Debug.LogWarning("MeshCreationSystem: Using emergency fallback material. Ensure URP shaders are available!");
        return material;
    }

    // Create material with found shader
    var defaultMaterial = new Material(shader)
    {
        color = new Color(0.2f, 0.8f, 0.2f, 1.0f), // Green for terrain visibility
        name = "TTG_TerrainDefaultMaterial"
    };

    // Configure URP-specific properties if using URP Lit shader
    if (shader.name.Contains("Universal Render Pipeline/Lit"))
    {
        // Set standard URP properties for better visibility
        if (defaultMaterial.HasProperty("_BaseColor"))
            defaultMaterial.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.2f, 1.0f));
        if (defaultMaterial.HasProperty("_Smoothness"))
            defaultMaterial.SetFloat("_Smoothness", 0.1f);
        if (defaultMaterial.HasProperty("_Metallic"))
            defaultMaterial.SetFloat("_Metallic", 0.0f);

        Debug.Log("MeshCreationSystem: Created URP Lit default material for terrain rendering.");
    }
    else if (shader.name.Contains("Universal Render Pipeline/Unlit"))
    {
        // Set URP Unlit properties
        if (defaultMaterial.HasProperty("_BaseColor"))
            defaultMaterial.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.2f, 1.0f));

        Debug.Log("MeshCreationSystem: Created URP Unlit default material for terrain rendering.");
    }
    else
    {
        Debug.Log($"MeshCreationSystem: Created default material using shader: {shader.name}");
    }

    return defaultMaterial;
}

private Entity CreateMeshEntity(EntityCommandBuffer ecb, GameObject meshGameObject)
{
    // Create a new entity to represent the mesh
    var meshEntity = EntityManager.CreateEntity();

    // Add transform components
    ecb.AddComponent(meshEntity, new LocalTransform
    {
        Position = float3.zero,
        Rotation = quaternion.identity,
        Scale = 1f
    });

    // Add a component to hold the GameObject reference
    ecb.AddComponent(meshEntity, new MeshGameObjectReference
    {
        GameObjectInstanceID = meshGameObject.GetInstanceID(),
        MeshInstanceID = meshGameObject.GetComponent<MeshFilter>().mesh.GetInstanceID()
    });

    // Add components for rendering (if needed)
    // e.g. MeshRenderer, Material, etc.

    return meshEntity;
}

private Mesh CreateUnityMesh(MeshDataComponent meshData)
{
    var mesh = new Mesh
    {
        name = "TTG_GeneratedTerrain"
    };

    // Convert vertices to Vector3 array
    var vertices = new Vector3[meshData.VertexCount];
    for (int i = 0; i < meshData.VertexCount; i++)
    {
        var vertex = meshData.Vertices.Value[i];
        vertices[i] = new Vector3(vertex.x, vertex.y, vertex.z);
    }

    // Convert indices to int array
    var indices = new int[meshData.IndexCount];
    for (int i = 0; i < meshData.IndexCount; i++)
    {
        indices[i] = meshData.Indices.Value[i];
    }

    mesh.vertices = vertices;
    mesh.triangles = indices;
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();

    return mesh;
}

protected override void OnDestroy()
{
    // Clean up any created GameObjects and their meshes
    Entities
        .WithAll<MeshGameObjectReference>()
        .ForEach((Entity entity, in MeshGameObjectReference meshRef) =>
        {
#if UNITY_EDITOR
                    try
                    {
                        var gameObject = UnityEditor.EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
                        if (gameObject != null)
                        {
                            var meshFilter = gameObject.GetComponent<MeshFilter>();
                            if (meshFilter != null && meshFilter.mesh != null)
                            {
                                // Clean up the mesh
                                if (Application.isPlaying)
                                    Object.Destroy(meshFilter.mesh);
                                else
                                    Object.DestroyImmediate(meshFilter.mesh);
                            }
                            
                            // Clean up the GameObject
                            if (Application.isPlaying)
                                Object.Destroy(gameObject);
                            else
                                Object.DestroyImmediate(gameObject);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Failed to cleanup GameObject for entity {entity}: {ex.Message}");
                    }
#endif
        }).WithoutBurst().Run();
            // Clean up the default material
            
            if (_defaultMaterial != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_defaultMaterial);
                else
                    Object.DestroyImmediate(_defaultMaterial);
            }
            base.OnDestroy();
        }
    }
}