using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Authoring component for terrain generation in ECS.
    /// </summary>
    public class TerrainGenerationAuthoring : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private TerrainType terrainType = TerrainType.Planar;
        
        [Tooltip("Minimum height/radius for spherical terrain (must be > 0), or minimum Y-height for planar terrain")]
        [SerializeField] private float minHeight = 0f;
        
        [Tooltip("Maximum height/radius for spherical terrain, or maximum Y-height for planar terrain")]
        [SerializeField] private float maxHeight = 10f;
        
        [Tooltip("Mesh subdivision level (1=no subdivision, 2=4x triangles, 3=16x triangles, 4=64x triangles). Higher values = more detail but exponentially slower processing. Recommended: 3-4 max.")]
        [Range(1, 6)]
        [SerializeField] private ushort depth = 3;
        
        [Header("Planar Settings")]
        [Tooltip("Number of sides for planar polygon (3=triangle, 6=hexagon, 8=octagon)")]
        [Range(3, 10)]
        [SerializeField] private ushort sides = 6;
        
        [Tooltip("Radius of the planar polygon")]
        [SerializeField] private float radius = 10f;
        
        [Header("Noise Sculpting - Creates Hills and Valleys")]
        [Tooltip("Random seed for reproducible terrain generation. Same seed = same terrain.")]
        [SerializeField] private int seed = 12345;
        
        [Tooltip("Base Frequency: Controls density of large terrain features. Lower = larger hills/valleys, Higher = more frequent smaller features. (0.01 = very large features, 0.5 = very small features)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float baseFrequency = 0.1f;
        
        [Tooltip("Octaves: Number of noise layers combined together. More octaves = more detail but slower processing. Each octave adds finer detail on top of previous layers. 1 = smooth terrain, 4+ = highly detailed terrain.")]
        [Range(1, 8)]
        [SerializeField] private uint octaves = 4;
        
        [Tooltip("Persistence: How much each octave contributes to the final result (0.1 = mostly base layer, 0.9 = lots of fine detail). Controls how 'rough' vs 'smooth' the terrain appears.")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float persistence = 0.5f;
        
        [Tooltip("Lacunarity: How much the frequency increases between octaves (detail multiplier). 2.0 = each octave has 2x more detail than previous. Higher = more dramatic detail differences between layers.")]
        [Range(1.1f, 4f)]
        [SerializeField] private float lacunarity = 2f;
        
        [Header("Terraces - Stepped Height Levels")]
        [Tooltip("Relative terrace heights (0.0 to 1.0). Each value creates a flat terrace level at that percentage of the terrain's total height. Example: [0.2, 0.5, 0.8] creates terraces at 20%, 50%, and 80% of max height.")]
        [SerializeField] private float[] relativeTerraceHeights = { 0.2f, 0.4f, 0.6f, 0.8f };
        
        [Header("Materials")]
        [Tooltip("Materials for each terrace level. Array size automatically matches terrace count.")]
        [SerializeField] private Material[] terraceMaterials = new Material[4];
        
        [Header("Generation")]
        [Tooltip("Automatically generate terrain when the scene starts")]
        public bool generateOnStart = true;
        
        [Tooltip("Use async generation to prevent frame drops (recommended for complex terrains)")]
        public bool useAsyncGeneration = true;
        
        private void OnValidate()
        {
            // Validate terrain type specific constraints
            if (terrainType == TerrainType.Spherical)
            {
                // For spherical terrain, minimum height must be greater than 0
                if (minHeight <= 0f)
                {
                    minHeight = 1f;
                    Debug.LogWarning("TerrainGenerationAuthoring: Minimum height for spherical terrain must be greater than 0. Setting to 1.");
                }
                
                // Ensure max height is greater than min height
                if (maxHeight <= minHeight)
                {
                    maxHeight = minHeight + 5f;
                    Debug.LogWarning("TerrainGenerationAuthoring: Maximum height must be greater than minimum height for spherical terrain.");
                }
            }
            else if (terrainType == TerrainType.Planar)
            {
                // For planar terrain, ensure valid polygon settings
                if (sides < 3)
                {
                    sides = 3;
                    Debug.LogWarning("TerrainGenerationAuthoring: Planar terrain must have at least 3 sides.");
                }
                
                if (radius <= 0f)
                {
                    radius = 1f;
                    Debug.LogWarning("TerrainGenerationAuthoring: Planar terrain radius must be greater than 0.");
                }
            }
            
            // Validate general parameters
            if (depth == 0)
            {
                depth = 1;
                Debug.LogWarning("TerrainGenerationAuthoring: Depth must be at least 1.");
            }
            
            if (baseFrequency <= 0f)
            {
                baseFrequency = 0.01f;
                Debug.LogWarning("TerrainGenerationAuthoring: Base frequency must be greater than 0.");
            }
            
            if (octaves == 0)
            {
                octaves = 1;
                Debug.LogWarning("TerrainGenerationAuthoring: Octaves must be at least 1.");
            }
            
            if (octaves > 1)
            {
                if (persistence <= 0f || persistence >= 1f)
                {
                    persistence = 0.5f;
                    Debug.LogWarning("TerrainGenerationAuthoring: Persistence must be between 0 and 1 when using multiple octaves.");
                }
                
                if (lacunarity <= 1f)
                {
                    lacunarity = 2f;
                    Debug.LogWarning("TerrainGenerationAuthoring: Lacunarity must be greater than 1 when using multiple octaves.");
                }
            }
            
            // Validate and normalize terrace heights
            if (relativeTerraceHeights != null && relativeTerraceHeights.Length > 0)
            {
                for (int i = 0; i < relativeTerraceHeights.Length; i++)
                {
                    relativeTerraceHeights[i] = Mathf.Clamp01(relativeTerraceHeights[i]);
                }
                
                // Sort terrace heights to ensure they're in ascending order
                System.Array.Sort(relativeTerraceHeights);
            }
            else
            {
                relativeTerraceHeights = new float[] { 0.2f, 0.4f, 0.6f, 0.8f };
                Debug.LogWarning("TerrainGenerationAuthoring: No terrace heights defined. Using default values.");
            }
            
            // Automatically resize materials array to match terrace count
            if (terraceMaterials == null || terraceMaterials.Length != relativeTerraceHeights.Length)
            {
                var oldMaterials = terraceMaterials ?? new Material[0];
                terraceMaterials = new Material[relativeTerraceHeights.Length];
                
                // Copy existing materials
                for (int i = 0; i < Mathf.Min(oldMaterials.Length, terraceMaterials.Length); i++)
                {
                    terraceMaterials[i] = oldMaterials[i];
                }
                
                // Fill missing materials with the last valid material if available
                Material lastMaterial = null;
                for (int i = oldMaterials.Length - 1; i >= 0; i--)
                {
                    if (oldMaterials[i] != null)
                    {
                        lastMaterial = oldMaterials[i];
                        break;
                    }
                }
                
                if (lastMaterial != null)
                {
                    for (int i = oldMaterials.Length; i < terraceMaterials.Length; i++)
                    {
                        terraceMaterials[i] = lastMaterial;
                    }
                }
            }
        }
        
        public TerrainGenerationData ToTerrainGenerationData()
        {
            return new TerrainGenerationData
            {
                TerrainType = terrainType,
                MinHeight = minHeight,
                MaxHeight = maxHeight,
                Depth = depth,
                Sides = sides,
                Radius = radius,
                Seed = seed,
                BaseFrequency = baseFrequency,
                Octaves = octaves,
                Persistence = persistence,
                Lacunarity = lacunarity
            };
        }
        
        public TerraceConfigData ToTerraceConfigData()
        {
            // FIXED: Convert relative heights to absolute heights as expected by tests
            // The authoring component should handle the conversion from relative to absolute heights
            float heightDelta = maxHeight - minHeight;
            
            // Create blob asset for terrace heights (absolute)
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var heightArray = ref builder.ConstructRoot<BlobArray<float>>();
            var heightArrayBuilder = builder.Allocate(ref heightArray, relativeTerraceHeights.Length);
            
            for (int i = 0; i < relativeTerraceHeights.Length; i++)
            {
                // Convert relative height (0-1) to absolute height
                float absoluteHeight = minHeight + relativeTerraceHeights[i] * heightDelta;
                heightArrayBuilder[i] = absoluteHeight;
            }
            
            var heightBlob = builder.CreateBlobAssetReference<BlobArray<float>>(Allocator.Persistent);
            
            return new TerraceConfigData
            {
                TerraceCount = relativeTerraceHeights.Length,
                TerraceHeights = heightBlob
            };
        }
        
        public TerrainMaterialData ToTerrainMaterialData()
        {
            // Create blob asset for material instance IDs
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var materialArray = ref builder.ConstructRoot<BlobArray<int>>();
            var materialArrayBuilder = builder.Allocate(ref materialArray, terraceMaterials.Length);
            
            for (int i = 0; i < terraceMaterials.Length; i++)
            {
                // Store the material's instance ID (0 if null)
                materialArrayBuilder[i] = terraceMaterials[i] != null ? terraceMaterials[i].GetInstanceID() : 0;
            }
            
            var materialBlob = builder.CreateBlobAssetReference<BlobArray<int>>(Allocator.Persistent);
            
            return new TerrainMaterialData
            {
                MaterialCount = terraceMaterials.Length,
                MaterialInstanceIDs = materialBlob
            };
        }
        
        public TerrainGenerationRequest ToTerrainGenerationRequest()
        {
            return new TerrainGenerationRequest
            {
                UseAsyncGeneration = useAsyncGeneration
            };
        }
    }
    
    /// <summary>
    /// Baker for converting terrain generation authoring to ECS components.
    /// </summary>
    public class TerrainGenerationBaker : Baker<TerrainGenerationAuthoring>
    {
        public override void Bake(TerrainGenerationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, authoring.ToTerrainGenerationData());
            AddComponent(entity, authoring.ToTerraceConfigData());
            AddComponent(entity, authoring.ToTerrainMaterialData());
            
            if (authoring.generateOnStart)
            {
                AddComponent(entity, authoring.ToTerrainGenerationRequest());
            }
        }
    }
}