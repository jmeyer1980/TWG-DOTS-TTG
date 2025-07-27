using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Terrain generation type enumeration.
    /// </summary>
    public enum TerrainType
    {
        Planar,
        Spherical
    }
    
    /// <summary>
    /// Generation phase enumeration for tracking terrain generation progress.
    /// </summary>
    public enum GenerationPhase
    {
        NotStarted,
        ShapeGeneration,
        Fragmentation,
        Sculpting,
        Terracing,
        MeshCreation,
        Complete
    }
    
    /// <summary>
    /// Core terrain generation parameters for ECS-based terrain generation.
    /// </summary>
    public struct TerrainGenerationData : IComponentData
    {
        public float MinHeight;
        public float MaxHeight;
        public ushort Depth;
        public TerrainType TerrainType;
        
        // Planar-specific fields
        public ushort Sides;
        public float Radius;
        
        // Sculpting parameters
        public int Seed;
        public float BaseFrequency;
        public uint Octaves;
        public float Persistence;
        public float Lacunarity;
        
        /// <summary>
        /// Convenience property for accessing Depth as FragmentationDepth
        /// </summary>
        public readonly ushort FragmentationDepth => Depth;
        
        /// <summary>
        /// Convenience property for noise scale (mapped from BaseFrequency)
        /// </summary>
        public readonly float NoiseScale => BaseFrequency;
        
        /// <summary>
        /// Convenience property for noise octaves
        /// </summary>
        public readonly uint NoiseOctaves => Octaves;
        
        /// <summary>
        /// Convenience property for noise persistence
        /// </summary>
        public readonly float NoisePersistence => Persistence;
        
        /// <summary>
        /// Convenience property for noise amplitude (derived from lacunarity)
        /// </summary>
        public readonly float NoiseAmplitude => Lacunarity;
    }
    
    /// <summary>
    /// Terrace configuration data stored as a blob asset.
    /// </summary>
    public struct TerraceConfigData : IComponentData
    {
        public int TerraceCount;
        public BlobAssetReference<BlobArray<float>> TerraceHeights;
    }
    
    /// <summary>
    /// Material configuration data for terrain rendering.
    /// Each terrace gets its own material assigned by the user.
    /// </summary>
    public struct TerrainMaterialData : IComponentData
    {
        public int MaterialCount;
        public BlobAssetReference<BlobArray<int>> MaterialInstanceIDs; // GameObject instance IDs for materials
    }
    
    /// <summary>
    /// State tracking for terrain generation process.
    /// </summary>
    public struct TerrainGenerationState : IComponentData
    {
        public GenerationPhase CurrentPhase;
        public bool IsComplete;
        public bool HasError;
        public Entity ResultMeshEntity;
    }
    
    /// <summary>
    /// Request component to trigger terrain generation.
    /// </summary>
    public struct TerrainGenerationRequest : IComponentData
    {
        public bool UseAsyncGeneration;
    }
    
    /// <summary>
    /// Component that holds mesh data during terrain generation pipeline.
    /// Uses blob assets for efficient data storage and transfer.
    /// 
    /// NOW ENABLEABLE: After mesh creation, this component is disabled rather than removed,
    /// allowing tests to verify its presence while preventing reprocessing by systems.
    /// 
    /// This solves the timing issue where tests expect the component to exist for validation
    /// but systems need to mark it as "processed" to prevent reprocessing.
    /// </summary>
    public struct MeshDataComponent : IComponentData, IEnableableComponent
    {
        public BlobAssetReference<BlobArray<float3>> Vertices;
        public BlobAssetReference<BlobArray<int>> Indices;
        public int VertexCount;
        public int IndexCount;
    }
    
    /// <summary>
    /// Tag component to mark entities with generated terrain meshes.
    /// </summary>
    public struct GeneratedTerrainMeshTag : IComponentData
    {
    }
    
    /// <summary>
    /// Component that references the actual Unity GameObject containing the mesh.
    /// </summary>
    public struct MeshGameObjectReference : IComponentData
    {
        public int GameObjectInstanceID;
        public int MeshInstanceID;
    }
    
    /// <summary>
    /// Tag component to mark entities that need their mesh data cleaned up.
    /// Used to schedule cleanup after Unity Mesh creation.
    /// </summary>
    public struct CleanupMeshDataTag : IComponentData
    {
    }
}