using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Main system that orchestrates the terrain generation pipeline using ECS.
    /// Processes only one phase per Update call to allow frame-by-frame progression.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TerrainGenerationSystem : SystemBase
    {
        private EntityQuery cleanupQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            // Create query for entities that need cleanup
            cleanupQuery = GetEntityQuery(ComponentType.ReadOnly<MeshDataComponent>());
        }
        
        private bool ProcessNewRequests()
        {
            bool processedAny = false;
            
            Entities
                .WithNone<TerrainGenerationState>()
                .ForEach((Entity entity, in TerrainGenerationRequest request, in TerrainGenerationData terrainData, in TerraceConfigData terraceConfig) =>
                {
                    var generationState = new TerrainGenerationState
                    {
                        CurrentPhase = GenerationPhase.ShapeGeneration,
                        IsComplete = false,
                        HasError = false,
                        ResultMeshEntity = Entity.Null
                    };
                    
                    EntityManager.AddComponentData(entity, generationState);
                    EntityManager.RemoveComponent<TerrainGenerationRequest>(entity);
                    processedAny = true;
                }).WithStructuralChanges().Run();
                
            return processedAny;
        }
        
        private bool ProcessShapeGeneration()
        {
            bool processedAny = false;
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<TerrainGenerationState>()
                .WithNone<MeshDataComponent>()
                .ForEach((Entity entity, in TerrainGenerationState generationState, in TerrainGenerationData terrainData) =>
                {
                    if (generationState.CurrentPhase != GenerationPhase.ShapeGeneration)
                        return;
                    
                    // Generate mesh data using CORRECTED algorithms
                    MeshDataComponent meshData;
                    
                    if (terrainData.TerrainType == TerrainType.Planar)
                    {
                        meshData = GenerateCorrectPlanarShape(terrainData);
                    }
                    else
                    {
                        meshData = GenerateCorrectSphericalShape(terrainData);
                    }
                    
                    ecb.AddComponent(entity, meshData);
                    
                    // Advance to next phase
                    var nextState = generationState;
                    nextState.CurrentPhase = GenerationPhase.Fragmentation;
                    ecb.SetComponent(entity, nextState);
                    processedAny = true;
                    
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return processedAny;
        }
        
        private bool ProcessFragmentation()
        {
            bool processedAny = false;
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in TerrainGenerationState generationState, in TerrainGenerationData terrainData, in MeshDataComponent meshData) =>
                {
                    if (generationState.CurrentPhase != GenerationPhase.Fragmentation)
                        return;
                    
                    // Apply CORRECTED fragmentation/subdivision to the mesh data
                    var fragmentedMeshData = ApplyCorrectMeshFragmentation(meshData, terrainData.Depth, terrainData.TerrainType);
                    
                    // Update mesh data
                    ecb.SetComponent(entity, fragmentedMeshData);
                    
                    // Advance to next phase
                    var nextState = generationState;
                    nextState.CurrentPhase = GenerationPhase.Sculpting;
                    ecb.SetComponent(entity, nextState);
                    processedAny = true;
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return processedAny;
        }
        
        private bool ProcessSculpting()
        {
            bool processedAny = false;
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in TerrainGenerationState generationState, in TerrainGenerationData terrainData, in MeshDataComponent meshData) =>
                {
                    if (generationState.CurrentPhase != GenerationPhase.Sculpting)
                        return;
                    
                    // Apply noise-based sculpting to vertices
                    var sculptedMeshData = ApplyNoiseSculpting(meshData, terrainData);
                    
                    // Update mesh data
                    ecb.SetComponent(entity, sculptedMeshData);
                    
                    // Advance to next phase
                    var nextState = generationState;
                    nextState.CurrentPhase = GenerationPhase.Terracing;
                    ecb.SetComponent(entity, nextState);
                    processedAny = true;
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return processedAny;
        }
        
        private bool ProcessTerracing()
        {
            bool processedAny = false;
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in TerrainGenerationState generationState, in TerrainGenerationData terrainData, in TerraceConfigData terraceConfig, in MeshDataComponent meshData) =>
                {
                    if (generationState.CurrentPhase != GenerationPhase.Terracing)
                        return;
                    
                    // Apply PROPER triangle slicing terracing with per-terrace organization
                    var terracedMeshData = ApplyTriangleSlicingTerracingWithSubmeshes(meshData, terraceConfig, terrainData);
                    
                    // Update mesh data
                    ecb.SetComponent(entity, terracedMeshData);
                    
                    // Advance to next phase
                    var nextState = generationState;
                    nextState.CurrentPhase = GenerationPhase.MeshCreation;
                    ecb.SetComponent(entity, nextState);
                    processedAny = true;
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return processedAny;
        }
        
        protected override void OnUpdate()
        {
            // Process only one phase per update to ensure frame-by-frame progression
            // Order matters - process in sequence of pipeline phases
            
            if (ProcessNewRequests()) return;
            if (ProcessShapeGeneration()) return;
            if (ProcessFragmentation()) return;
            if (ProcessSculpting()) return;
            if (ProcessTerracing()) return;
            ProcessMeshCreation();
        }
        
        private void ForceImmediateCleanup()
        {
            // Force garbage collection of any unreferenced blob assets
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
        
        private static void DisposeMeshDataBlobAssets(MeshDataComponent meshData)
        {
            try
            {
                if (meshData.Vertices.IsCreated)
                    meshData.Vertices.Dispose();
            }
            catch (System.InvalidOperationException)
            {
                // Already disposed - safe to ignore
            }
            
            try
            {
                if (meshData.Indices.IsCreated)
                    meshData.Indices.Dispose();
            }
            catch (System.InvalidOperationException)
            {
                // Already disposed - safe to ignore
            }
        }
        
        private void CleanupBlobAssets()
        {
            // Clean up all mesh data blob assets
            Entities
                .WithAll<MeshDataComponent>()
                .ForEach((Entity entity, in MeshDataComponent meshData) =>
                {
                    try
                    {
                        if (meshData.Vertices.IsCreated)
                            meshData.Vertices.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        // Blob asset already disposed - safe to ignore
                    }
                    
                    try
                    {
                        if (meshData.Indices.IsCreated)
                            meshData.Indices.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        // Blob asset already disposed - safe to ignore
                    }
                }).WithoutBurst().Run();
                
            // Clean up all terrace config blob assets
            Entities
                .WithAll<TerraceConfigData>()
                .ForEach((Entity entity, in TerraceConfigData terraceConfig) =>
                {
                    try
                    {
                        if (terraceConfig.TerraceHeights.IsCreated)
                            terraceConfig.TerraceHeights.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        // Blob asset already disposed - safe to ignore
                    }
                }).WithoutBurst().Run();
                
            // Clean up material data blob assets
            Entities
                .WithAll<TerrainMaterialData>()
                .ForEach((Entity entity, in TerrainMaterialData materialData) =>
                {
                    try
                    {
                        if (materialData.MaterialInstanceIDs.IsCreated)
                            materialData.MaterialInstanceIDs.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        // Blob asset already disposed - safe to ignore
                    }
                }).WithoutBurst().Run();
        }
        
        private void ProcessMeshCreation()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithAll<MeshDataComponent, TerrainGenerationState>()
                .ForEach((Entity entity, in TerrainGenerationState generationState, in MeshDataComponent meshData) =>
                {
                    if (generationState.CurrentPhase != GenerationPhase.MeshCreation)
                        return;
                    
                    // Mark as complete - actual Unity mesh creation handled in MeshCreationSystem
                    var nextState = generationState;
                    nextState.CurrentPhase = GenerationPhase.Complete;
                    nextState.IsComplete = true;
                    ecb.SetComponent(entity, nextState);
                }).WithoutBurst().Run();
                
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        
        // CORRECTED terrain generation algorithms based on Lazy Squirrel Labs implementation
        
        private static MeshDataComponent GenerateCorrectPlanarShape(TerrainGenerationData terrainData)
        {
            // IMPROVED: Better parameter validation
            var sides = math.max(3, terrainData.Sides);
            var radius = math.max(0.1f, math.abs(terrainData.Radius)); // Handle negative radius
            
            // Create polygon vertices in a ring (no center vertex)
            var vertices = new NativeArray<float3>(sides, Allocator.Temp);
            var indices = new NativeArray<int>((sides - 2) * 3, Allocator.Temp); // Triangle fan from vertex 0
            
            // Generate vertices in a circle
            for (int i = 0; i < sides; i++)
            {
                float angle = (2f * math.PI * i) / sides;
                vertices[i] = new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);
            }
            
            // Generate triangle fan indices (all triangles share vertex 0)
            int indexIndex = 0;
            for (int i = 1; i < sides - 1; i++)
            {
                indices[indexIndex] = 0;
                indices[indexIndex + 1] = i;
                indices[indexIndex + 2] = i + 1;
                indexIndex += 3;
            }
            
            var meshData = CreateMeshDataComponent(vertices, indices);
            vertices.Dispose();
            indices.Dispose();
            return meshData;
        }
        
        private static MeshDataComponent GenerateCorrectSphericalShape(TerrainGenerationData terrainData)
        {
            // IMPROVED: Better parameter validation for spherical terrain
            var radius = math.max(0.1f, terrainData.MinHeight);
            
            // Use icosahedron instead of octahedron for correct vertex count (12 vertices, not 6)
            var vertices = new NativeArray<float3>(12, Allocator.Temp);
            
            // Icosahedron vertices (from SphereGenerator) - exact coordinates
            vertices[0] = new float3(0.8506508f, 0.5257311f, 0f);
            vertices[1] = new float3(0.000000101405476f, 0.8506507f, -0.525731f);
            vertices[2] = new float3(0.000000101405476f, 0.8506506f, 0.525731f);
            vertices[3] = new float3(0.5257309f, -0.00000006267203f, -0.85065067f);
            vertices[4] = new float3(0.52573115f, -0.00000006267203f, 0.85065067f);
            vertices[5] = new float3(0.8506508f, -0.5257311f, 0f);
            vertices[6] = new float3(-0.52573115f, 0.00000006267203f, -0.85065067f);
            vertices[7] = new float3(-0.8506508f, 0.5257311f, 0f);
            vertices[8] = new float3(-0.5257309f, 0.00000006267203f, 0.85065067f);
            vertices[9] = new float3(-0.000000101405476f, -0.8506506f, -0.525731f);
            vertices[10] = new float3(-0.000000101405476f, -0.8506507f, 0.525731f);
            vertices[11] = new float3(-0.8506508f, -0.5257311f, 0f);
            
            // Scale vertices by radius
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = math.normalize(vertices[i]) * radius;
            }
            
            // Icosahedron faces (EXACT indices from SphereGenerator)
            var indices = new NativeArray<int>(60, Allocator.Temp);
            int[] faceIndices = {
                0, 1, 2,
                0, 3, 1,
                0, 2, 4,
                3, 0, 5,
                0, 4, 5,
                1, 3, 6,
                1, 7, 2,
                7, 1, 6,
                4, 2, 8,
                7, 8, 2,
                9, 3, 5,
                6, 3, 9,
                5, 4, 10,
                4, 8, 10,
                9, 5, 10,
                7, 6, 11,
                7, 11, 8,
                11, 6, 9,
                8, 11, 10,
                10, 11, 9
            };
            
            for (int i = 0; i < faceIndices.Length; i++)
            {
                indices[i] = faceIndices[i];
            }
            
            var meshData = CreateMeshDataComponent(vertices, indices);
            vertices.Dispose();
            indices.Dispose();
            return meshData;
        }
        
        private static MeshDataComponent ApplyCorrectMeshFragmentation(MeshDataComponent meshData, ushort depth, TerrainType terrainType)
        {
            if (depth <= 1)
                return meshData; // No fragmentation needed
            
            ref var originalVertices = ref meshData.Vertices.Value;
            ref var originalIndices = ref meshData.Indices.Value;
            
            var currentVertices = new NativeArray<float3>(originalVertices.Length, Allocator.Temp);
            var currentIndices = new NativeArray<int>(originalIndices.Length, Allocator.Temp);
            
            // Copy original data
            for (int i = 0; i < originalVertices.Length; i++)
            {
                currentVertices[i] = originalVertices[i];
            }
            for (int i = 0; i < originalIndices.Length; i++)
            {
                currentIndices[i] = originalIndices[i];
            }
            
            // IMPROVED: Apply subdivision iteratively with stricter bounds checking
            for (int d = 0; d < depth - 1; d++)
            {
                // Check if we're creating too much geometry
                int currentTriangleCount = currentIndices.Length / 3;
                int projectedTriangleCount = currentTriangleCount * 4;
                
                // STRICTER: Prevent excessive subdivision that causes exponential processing times
                if (projectedTriangleCount > 25000) // Reduced from 100,000 for better performance
                {
                    Debug.LogWarning($"Terrain fragmentation stopped at depth {d + 1} to prevent excessive processing time. Triangle count would be: {projectedTriangleCount}. Recommended: Use depth 4 or lower for optimal performance.");
                    break;
                }
                
                var (vertices, indices) = terrainType == TerrainType.Spherical ? 
                    SubdivideSphericalMesh(currentVertices, currentIndices) :
                    SubdividePlanarMesh(currentVertices, currentIndices);
                
                // Dispose old arrays
                currentVertices.Dispose();
                currentIndices.Dispose();
                
                // Use new data
                currentVertices = vertices;
                currentIndices = indices;
            }
            
            var fragmentedMeshData = CreateMeshDataComponent(currentVertices, currentIndices);
            currentVertices.Dispose();
            currentIndices.Dispose();
            return fragmentedMeshData;
        }
        
        private static (NativeArray<float3> vertices, NativeArray<int> indices) SubdivideSphericalMesh(
            NativeArray<float3> vertices, NativeArray<int> indices)
        {
            int triangleCount = indices.Length / 3;
            int newTriangleCount = triangleCount * 4;
            int newVertexCount = newTriangleCount * 3; // Simple approach: unique vertices per triangle
            
            var newVertices = new NativeArray<float3>(newVertexCount, Allocator.Temp);
            var newIndices = new NativeArray<int>(newTriangleCount * 3, Allocator.Temp);
            
            int vertexIndex = 0;
            int indexIndex = 0;
            
            // Get sphere radius from first vertex
            float sphereRadius = math.length(vertices[0]);
            
            // Process each original triangle
            for (int t = 0; t < triangleCount; t++)
            {
                var i0 = indices[t * 3];
                var i1 = indices[t * 3 + 1];
                var i2 = indices[t * 3 + 2];
                
                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];
                
                // Calculate midpoints and project them onto sphere using Vector3.Slerp-like functionality
                var v01 = math.normalize((v0 + v1) * 0.5f) * sphereRadius;
                var v12 = math.normalize((v1 + v2) * 0.5f) * sphereRadius;
                var v20 = math.normalize((v2 + v0) * 0.5f) * sphereRadius;
                
                // Create 4 new triangles following the exact pattern from SphericalMeshFragmenter
                // Triangle 1: v0, v01, v20
                newVertices[vertexIndex] = v0;
                newVertices[vertexIndex + 1] = v01;
                newVertices[vertexIndex + 2] = v20;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 2: v01, v1, v12
                newVertices[vertexIndex] = v01;
                newVertices[vertexIndex + 1] = v1;
                newVertices[vertexIndex + 2] = v12;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 3: v20, v12, v2
                newVertices[vertexIndex] = v20;
                newVertices[vertexIndex + 1] = v12;
                newVertices[vertexIndex + 2] = v2;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 4: v01, v12, v20 (center triangle)
                newVertices[vertexIndex] = v01;
                newVertices[vertexIndex + 1] = v12;
                newVertices[vertexIndex + 2] = v20;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
            }
            
            return (newVertices, newIndices);
        }
        
        private static (NativeArray<float3> vertices, NativeArray<int> indices) SubdividePlanarMesh(
            NativeArray<float3> vertices, NativeArray<int> indices)
        {
            int triangleCount = indices.Length / 3;
            int newTriangleCount = triangleCount * 4;
            int newVertexCount = newTriangleCount * 3; // Simple approach: unique vertices per triangle
            
            var newVertices = new NativeArray<float3>(newVertexCount, Allocator.Temp);
            var newIndices = new NativeArray<int>(newTriangleCount * 3, Allocator.Temp);
            
            int vertexIndex = 0;
            int indexIndex = 0;
            
            // Process each original triangle
            for (int t = 0; t < triangleCount; t++)
            {
                var i0 = indices[t * 3];
                var i1 = indices[t * 3 + 1];
                var i2 = indices[t * 3 + 2];
                
                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];
                
                // Calculate midpoints (simple linear interpolation for planar)
                var v01 = (v0 + v1) * 0.5f;
                var v12 = (v1 + v2) * 0.5f;
                var v20 = (v2 + v0) * 0.5f;
                
                // Create 4 new triangles
                // Triangle 1: v0, v01, v20
                newVertices[vertexIndex] = v0;
                newVertices[vertexIndex + 1] = v01;
                newVertices[vertexIndex + 2] = v20;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 2: v01, v1, v12
                newVertices[vertexIndex] = v01;
                newVertices[vertexIndex + 1] = v1;
                newVertices[vertexIndex + 2] = v12;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 3: v20, v12, v2
                newVertices[vertexIndex] = v20;
                newVertices[vertexIndex + 1] = v12;
                newVertices[vertexIndex + 2] = v2;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
                
                // Triangle 4: v01, v12, v20 (center triangle)
                newVertices[vertexIndex] = v01;
                newVertices[vertexIndex + 1] = v12;
                newVertices[vertexIndex + 2] = v20;
                newIndices[indexIndex] = vertexIndex;
                newIndices[indexIndex + 1] = vertexIndex + 1;
                newIndices[indexIndex + 2] = vertexIndex + 2;
                vertexIndex += 3;
                indexIndex += 3;
            }
            
            return (newVertices, newIndices);
        }
        
        private static MeshDataComponent ApplyNoiseSculpting(MeshDataComponent meshData, TerrainGenerationData terrainData)
        {
            ref var originalVertices = ref meshData.Vertices.Value;
            ref var originalIndices = ref meshData.Indices.Value;
            
            var newVertices = new NativeArray<float3>(originalVertices.Length, Allocator.Temp);
            var newIndices = new NativeArray<int>(originalIndices.Length, Allocator.Temp);
            
            // Copy indices (they don't change)
            for (int i = 0; i < originalIndices.Length; i++)
            {
                newIndices[i] = originalIndices[i];
            }
            
            // CRITICAL FIX: Ensure seed is never zero for Unity.Mathematics.Random
            uint validSeed = (uint)terrainData.Seed;
            if (validSeed == 0)
            {
                validSeed = 1; // Default fallback seed
                Debug.LogWarning($"Terrain generation seed was zero, using fallback seed: {validSeed}");
            }
            
            // Initialize random number generator with validated non-zero seed
            var random = new Unity.Mathematics.Random(validSeed);
            
            // Generate octave offsets for 3D noise (for spherical terrain)
            var offsets = new NativeArray<float3>((int)terrainData.Octaves, Allocator.Temp);
            for (int i = 0; i < terrainData.Octaves; i++)
            {
                offsets[i] = new float3(
                    random.NextFloat(-10000f, 10000f),
                    random.NextFloat(-10000f, 10000f),
                    random.NextFloat(-10000f, 10000f)
                );
            }
            
            float heightDelta = terrainData.MaxHeight - terrainData.MinHeight;
            
            // ADDITIONAL SAFETY: Handle edge cases for height delta
            if (heightDelta <= 0f)
            {
                Debug.LogWarning($"Invalid height range: MinHeight={terrainData.MinHeight}, MaxHeight={terrainData.MaxHeight}. Using default range.");
                heightDelta = 1f; // Default safe value
            }
            
            // Apply noise to vertices
            for (int i = 0; i < originalVertices.Length; i++)
            {
                var vertex = originalVertices[i];
                
                float noiseValue = 0f;
                float amplitude = 1f;
                float frequency = terrainData.BaseFrequency;
                
                // SAFETY: Ensure frequency is valid
                if (frequency <= 0f)
                {
                    frequency = 0.01f; // Default safe frequency
                }
                
                if (terrainData.TerrainType == TerrainType.Spherical)
                {
                    // For spherical terrain: use 3D Perlin noise like SphereSculptor
                    for (uint octave = 0; octave < terrainData.Octaves; octave++)
                    {
                        var offset = offsets[(int)octave];
                        var sampleX = vertex.x * frequency + offset.x;
                        var sampleY = vertex.y * frequency + offset.y;
                        var sampleZ = vertex.z * frequency + offset.z;
                        
                        // 3D Perlin noise implementation
                        float perlinNoise3D = GetPerlinNoise3D(sampleX, sampleY, sampleZ);
                        noiseValue += perlinNoise3D * amplitude;
                        
                        amplitude *= terrainData.Persistence;
                        frequency *= terrainData.Lacunarity;
                    }
                    
                    // Normalize and apply to spherical vertex
                    float relativeHeight = math.clamp(noiseValue * 0.5f + 0.5f, 0f, 1f); // Normalize to [0,1]
                    float finalHeight = terrainData.MinHeight + heightDelta * relativeHeight;
                    
                    // SAFETY: Ensure final height is positive for spherical terrain
                    finalHeight = math.max(0.01f, finalHeight);
                    vertex = math.normalize(vertex) * finalHeight;
                }
                else
                {
                    // For planar terrain: use 2D noise
                    for (uint octave = 0; octave < terrainData.Octaves; octave++)
                    {
                        var sampleX = vertex.x * frequency + validSeed;
                        var sampleZ = vertex.z * frequency + validSeed;
                        
                        noiseValue += noise.snoise(new float2(sampleX, sampleZ)) * amplitude;
                        
                        amplitude *= terrainData.Persistence;
                        frequency *= terrainData.Lacunarity;
                    }
                    
                    // Apply noise to height with proper scaling
                    float heightVariation = noiseValue * heightDelta * 0.3f; // Scale down for reasonable variation
                    vertex.y += heightVariation;
                }
                
                newVertices[i] = vertex;
            }
            
            offsets.Dispose();
            var sculptedMeshData = CreateMeshDataComponent(newVertices, newIndices);
            newVertices.Dispose();
            newIndices.Dispose();
            return sculptedMeshData;
        }
        
        private static float GetPerlinNoise3D(float x, float y, float z)
        {
            // 3D Perlin noise implementation (from SphereSculptor)
            var xy = noise.snoise(new float2(x, y));
            var xz = noise.snoise(new float2(x, z));
            var yz = noise.snoise(new float2(y, z));
            
            var yx = noise.snoise(new float2(y, x));
            var zx = noise.snoise(new float2(z, x));
            var zy = noise.snoise(new float2(z, y));
            
            var xyz = xy + xz + yz + yx + zx + zy;
            return xyz / 6f;
        }
        
        // MAJOR BLENDING FIX: Implement seamless vertex welding like original TerracedMeshBuilder
        // CORRECTED APPROACH: Proper terrace organization like original TerracedMeshBuilder
        private static MeshDataComponent ApplyTriangleSlicingTerracingWithSubmeshes(MeshDataComponent meshData, TerraceConfigData terraceConfig, TerrainGenerationData terrainData)
        {
            ref var originalVertices = ref meshData.Vertices.Value;
            ref var originalIndices = ref meshData.Indices.Value;
            
            int triangleCount = originalIndices.Length / 3;
            
            // FIXED: Use absolute heights directly from terraceConfig (no conversion needed)
            var terraceHeights = new NativeArray<float>(terraceConfig.TerraceHeights.Value.Length, Allocator.Temp);
            for (int i = 0; i < terraceHeights.Length; i++)
            {
                terraceHeights[i] = terraceConfig.TerraceHeights.Value[i];
            }
            
            // PROPER ARCHITECTURE: Separate collections for horizontal and vertical geometry like original
            var sharedVertices = new NativeParallelHashMap<float3, int>(triangleCount * 16, Allocator.Temp);
            var horizontalIndices = new NativeList<int>(triangleCount * 24, Allocator.Temp);  // Floor triangles
            var verticalIndices = new NativeList<int>(triangleCount * 24, Allocator.Temp);    // Wall triangles
            var nextVertexIndex = 0;
            
            // Process each triangle with proper horizontal/vertical separation
            for (int t = 0; t < triangleCount; t++)
            {
                var triangle = new TriangleECS(
                    originalVertices[originalIndices[t * 3]],
                    originalVertices[originalIndices[t * 3 + 1]],
                    originalVertices[originalIndices[t * 3 + 2]]
                );
                
                SliceAndAddTriangleProperlyOrganized(triangle, terraceHeights, terrainData, 
                    sharedVertices, horizontalIndices, verticalIndices, ref nextVertexIndex);
            }
            
            // Combine horizontal and vertical geometry into final mesh
            var finalVertices = new NativeArray<float3>(sharedVertices.Count(), Allocator.Temp);
            var finalIndices = new NativeArray<int>(horizontalIndices.Length + verticalIndices.Length, Allocator.Temp);
            
            // Fill vertices array from shared vertex map
            foreach (var kvp in sharedVertices)
            {
                finalVertices[kvp.Value] = kvp.Key;
            }
            
            // Copy horizontal indices first (floors)
            for (int i = 0; i < horizontalIndices.Length; i++)
            {
                finalIndices[i] = horizontalIndices[i];
            }
            
            // Copy vertical indices after (walls)
            for (int i = 0; i < verticalIndices.Length; i++)
            {
                finalIndices[horizontalIndices.Length + i] = verticalIndices[i];
            }
            
            var terracedMeshData = CreateMeshDataComponent(finalVertices, finalIndices);
            
            // Cleanup
            terraceHeights.Dispose();
            sharedVertices.Dispose();
            horizontalIndices.Dispose();
            verticalIndices.Dispose();
            finalVertices.Dispose();
            finalIndices.Dispose();
            
            return terracedMeshData;
        }
        
        private static void SliceAndAddTriangleProperlyOrganized(TriangleECS triangle, NativeArray<float> terraceHeights, TerrainGenerationData terrainData,
            NativeParallelHashMap<float3, int> sharedVertices, NativeList<int> horizontalIndices, NativeList<int> verticalIndices, ref int nextVertexIndex)
        {
            // CRITICAL FIX: Handle empty terrace heights array
            if (terraceHeights.Length == 0)
            {
                // No terracing - add triangle as-is to horizontal indices
                var v1Idx = GetOrAddProperlySharedVertex(triangle.V1, sharedVertices, ref nextVertexIndex);
                var v2Idx = GetOrAddProperlySharedVertex(triangle.V2, sharedVertices, ref nextVertexIndex);
                var v3Idx = GetOrAddProperlySharedVertex(triangle.V3, sharedVertices, ref nextVertexIndex);
                
                horizontalIndices.Add(v1Idx);
                horizontalIndices.Add(v2Idx);
                horizontalIndices.Add(v3Idx);
                return;
            }
            
            var t = triangle;
            bool added = false;
            float previousHeight = terraceHeights[0];
            
            // Process each terrace plane with proper organization
            for (int terraceIx = 0; terraceIx < terraceHeights.Length - 1; terraceIx++)
            {
                float terraceHeight = terraceHeights[terraceIx + 1];
                
                // Rearrange triangle based on vertices above/below plane
                var (rearrangedTriangle, pointsAbove) = RearrangeTriangleByPlane(t, terraceHeight, terrainData);
                t = rearrangedTriangle;
                
                switch (pointsAbove)
                {
                    case 0 when !added:
                        // Triangle is completely below plane - add as floor at previous height
                        AddWholeTriangleToHorizontal(t, previousHeight, terrainData, sharedVertices, horizontalIndices, ref nextVertexIndex);
                        added = true;
                        return;
                    case 0:
                        return; // Already added
                    case 1:
                        // 1 vertex above - slice and add floor + wall separately
                        if (!added)
                        {
                            AddWholeTriangleToHorizontal(t, previousHeight, terrainData, sharedVertices, horizontalIndices, ref nextVertexIndex);
                            added = true;
                        }
                        AddSlicedTriangle1AboveOrganized(t, terraceHeight, previousHeight, terrainData, 
                            sharedVertices, horizontalIndices, verticalIndices, ref nextVertexIndex);
                        break;
                    case 2:
                        // 2 vertices above - slice and add floor + wall separately
                        if (!added)
                        {
                            AddWholeTriangleToHorizontal(t, previousHeight, terrainData, sharedVertices, horizontalIndices, ref nextVertexIndex);
                            added = true;
                        }
                        AddSlicedTriangle2AboveOrganized(t, terraceHeight, previousHeight, terrainData,
                            sharedVertices, horizontalIndices, verticalIndices, ref nextVertexIndex);
                        break;
                    case 3:
                        // All vertices above - continue to next plane
                        break;
                }
                
                previousHeight = terraceHeight;
            }
            
            // Handle final terrace - add at the last terrace height
            if (!added)
            {
                float lastHeight = terraceHeights[^1];
                AddWholeTriangleToHorizontal(t, lastHeight, terrainData, sharedVertices, horizontalIndices, ref nextVertexIndex);
            }
        }
        
        private static void AddWholeTriangleToHorizontal(TriangleECS triangle, float height, TerrainGenerationData terrainData,
            NativeParallelHashMap<float3, int> sharedVertices, NativeList<int> horizontalIndices, ref int nextVertexIndex)
        {
            // Set all vertices to the specified terrace height and add to horizontal collection
            var v1 = SetVertexHeightECS(triangle.V1, height, terrainData);
            var v2 = SetVertexHeightECS(triangle.V2, height, terrainData);
            var v3 = SetVertexHeightECS(triangle.V3, height, terrainData);
            
            var idx1 = GetOrAddProperlySharedVertex(v1, sharedVertices, ref nextVertexIndex);
            var idx2 = GetOrAddProperlySharedVertex(v2, sharedVertices, ref nextVertexIndex);
            var idx3 = GetOrAddProperlySharedVertex(v3, sharedVertices, ref nextVertexIndex);
            
            // Add to horizontal (floor) collection
            horizontalIndices.Add(idx1);
            horizontalIndices.Add(idx2);
            horizontalIndices.Add(idx3);
        }
        
        private static void AddSlicedTriangle1AboveOrganized(TriangleECS triangle, float plane, float previousPlane, TerrainGenerationData terrainData,
            NativeParallelHashMap<float3, int> sharedVertices, NativeList<int> horizontalIndices, NativeList<int> verticalIndices, ref int nextVertexIndex)
        {
            // Add floor triangle to horizontal collection
            var floor1 = GetPlanePointECS(triangle.V1, triangle.V3, plane, terrainData);
            var floor2 = GetPlanePointECS(triangle.V2, triangle.V3, plane, terrainData);
            var floor3 = SetVertexHeightECS(triangle.V3, plane, terrainData);
            
            var floorIdx1 = GetOrAddProperlySharedVertex(floor1, sharedVertices, ref nextVertexIndex);
            var floorIdx2 = GetOrAddProperlySharedVertex(floor2, sharedVertices, ref nextVertexIndex);
            var floorIdx3 = GetOrAddProperlySharedVertex(floor3, sharedVertices, ref nextVertexIndex);
            
            horizontalIndices.Add(floorIdx1);
            horizontalIndices.Add(floorIdx2);
            horizontalIndices.Add(floorIdx3);
            
            // Add wall quadrilateral to vertical collection - PROPERLY CONNECTING to previous level
            var wallTop1 = floor2;
            var wallTop2 = floor1;
            var wallBottom1 = SetVertexHeightECS(floor1, previousPlane, terrainData);
            var wallBottom2 = SetVertexHeightECS(floor2, previousPlane, terrainData);
            
            // These wall vertices should connect to the floor below
            var wallTopIdx1 = floorIdx2;  // Reuse floor vertex indices for connection
            var wallTopIdx2 = floorIdx1;  // Reuse floor vertex indices for connection
            var wallBottomIdx1 = GetOrAddProperlySharedVertex(wallBottom1, sharedVertices, ref nextVertexIndex);
            var wallBottomIdx2 = GetOrAddProperlySharedVertex(wallBottom2, sharedVertices, ref nextVertexIndex);
            
            // Add wall to vertical collection with proper winding
            AddQuadrilateralToVertical(wallTopIdx1, wallTopIdx2, wallBottomIdx1, wallBottomIdx2, verticalIndices);
        }
        
        private static void AddSlicedTriangle2AboveOrganized(TriangleECS triangle, float plane, float previousPlane, TerrainGenerationData terrainData,
            NativeParallelHashMap<float3, int> sharedVertices, NativeList<int> horizontalIndices, NativeList<int> verticalIndices, ref int nextVertexIndex)
        {
            // Add floor quadrilateral to horizontal collection
            var floor1 = GetPlanePointECS(triangle.V1, triangle.V3, plane, terrainData);
            var floor2 = GetPlanePointECS(triangle.V2, triangle.V3, plane, terrainData);
            var floor3 = SetVertexHeightECS(triangle.V1, plane, terrainData);
            var floor4 = SetVertexHeightECS(triangle.V2, plane, terrainData);
            
            var floorIdx1 = GetOrAddProperlySharedVertex(floor1, sharedVertices, ref nextVertexIndex);
            var floorIdx2 = GetOrAddProperlySharedVertex(floor2, sharedVertices, ref nextVertexIndex);
            var floorIdx3 = GetOrAddProperlySharedVertex(floor3, sharedVertices, ref nextVertexIndex);
            var floorIdx4 = GetOrAddProperlySharedVertex(floor4, sharedVertices, ref nextVertexIndex);
            
            AddQuadrilateralToHorizontal(floorIdx1, floorIdx3, floorIdx4, floorIdx2, horizontalIndices);
            
            // Add wall quadrilateral to vertical collection - PROPERLY CONNECTING to previous level
            var wallTop1 = floor1;
            var wallTop2 = floor2;
            var wallBottom1 = SetVertexHeightECS(floor1, previousPlane, terrainData);
            var wallBottom2 = SetVertexHeightECS(floor2, previousPlane, terrainData);
            
            // These wall vertices should connect to the floor below
            var wallTopIdx1 = floorIdx1;  // Reuse floor vertex indices for connection
            var wallTopIdx2 = floorIdx2;  // Reuse floor vertex indices for connection
            var wallBottomIdx1 = GetOrAddProperlySharedVertex(wallBottom1, sharedVertices, ref nextVertexIndex);
            var wallBottomIdx2 = GetOrAddProperlySharedVertex(wallBottom2, sharedVertices, ref nextVertexIndex);
            
            // Add wall to vertical collection with proper winding
            AddQuadrilateralToVertical(wallTopIdx1, wallTopIdx2, wallBottomIdx2, wallBottomIdx1, verticalIndices);
        }
        
        private static void AddQuadrilateralToHorizontal(int idx1, int idx2, int idx3, int idx4, NativeList<int> horizontalIndices)
        {
            // Add quadrilateral to horizontal (floor) collection
            horizontalIndices.Add(idx1);
            horizontalIndices.Add(idx2);
            horizontalIndices.Add(idx4);
            
            horizontalIndices.Add(idx2);
            horizontalIndices.Add(idx3);
            horizontalIndices.Add(idx4);
        }
        
        private static void AddQuadrilateralToVertical(int idx1, int idx2, int idx3, int idx4, NativeList<int> verticalIndices)
        {
            // Add quadrilateral to vertical (wall) collection with proper winding
            verticalIndices.Add(idx1);
            verticalIndices.Add(idx2);
            verticalIndices.Add(idx4);
            
            verticalIndices.Add(idx2);
            verticalIndices.Add(idx3);
            verticalIndices.Add(idx4);
        }
        
        private static int GetOrAddProperlySharedVertex(float3 vertex, NativeParallelHashMap<float3, int> sharedVertices, ref int nextVertexIndex)
        {
            // PROPER VERTEX SHARING: Use appropriate epsilon for terrain scale
            const float epsilon = 0.0001f; // Good balance between precision and performance
            
            // Efficient spatial comparison with early exit
            foreach (var kvp in sharedVertices)
            {
                var existingVertex = kvp.Key;
                
                // Fast distance check - if any component is too far apart, skip
                if (math.abs(existingVertex.x - vertex.x) > epsilon ||
                    math.abs(existingVertex.y - vertex.y) > epsilon ||
                    math.abs(existingVertex.z - vertex.z) > epsilon)
                {
                    continue;
                }
                
                // Precise distance check for nearby vertices
                if (math.distancesq(existingVertex, vertex) < epsilon * epsilon)
                {
                    return kvp.Value;  // Reuse existing vertex for proper sharing
                }
            }
            
            // Vertex doesn't exist, add it
            var index = nextVertexIndex++;
            sharedVertices.TryAdd(vertex, index);
            return index;
        }
        
        private static (TriangleECS, int) RearrangeTriangleByPlane(TriangleECS triangle, float planeHeight, TerrainGenerationData terrainData)
        {
            var v1 = triangle.V1;
            var v2 = triangle.V2;
            var v3 = triangle.V3;
            
            var height1 = GetVertexHeightECS(v1, terrainData);
            var height2 = GetVertexHeightECS(v2, terrainData);
            var height3 = GetVertexHeightECS(v3, terrainData);
            
            var v1Below = height1 < planeHeight;
            var v2Below = height2 < planeHeight;
            var v3Below = height3 < planeHeight;
            
            // Rearrange triangle to simplify slicing (exact algorithm from Terracer.cs)
            if (v1Below)
            {
                if (v2Below)
                {
                    return height3 < planeHeight ? (triangle, 0) : (triangle, 1);
                }
                
                if (v3Below)
                {
                    triangle = new TriangleECS(v3, v1, v2);
                    return (triangle, 1);
                }
                
                triangle = new TriangleECS(v2, v3, v1);
                return (triangle, 2);
            }
            
            if (!v2Below)
            {
                return v3Below ? (triangle, 2) : (triangle, 3);
            }
            
            if (v3Below)
            {
                triangle = new TriangleECS(v2, v3, v1);
                return (triangle, 1);
            }
            
            triangle = new TriangleECS(v3, v1, v2);
            return (triangle, 2);
        }
        
        private static float3 GetPlanePointECS(float3 lower, float3 higher, float height, TerrainGenerationData terrainData)
        {
            var lowerHeight = GetVertexHeightECS(lower, terrainData);
            var higherHeight = GetVertexHeightECS(higher, terrainData);
            
            // Prevent division by zero
            if (math.abs(higherHeight - lowerHeight) < 0.0001f)
            {
                return math.lerp(lower, higher, 0.5f); // Return midpoint if heights are too close
            }
            
            var t = (height - lowerHeight) / (higherHeight - lowerHeight);
            t = math.clamp(t, 0f, 1f); // Ensure t is in valid range
            return math.lerp(lower, higher, t);
        }
        
        private static float GetVertexHeightECS(float3 vertex, TerrainGenerationData terrainData)
        {
            return terrainData.TerrainType == TerrainType.Spherical ? 
                math.length(vertex) : vertex.y;
        }
        
        private static float3 SetVertexHeightECS(float3 vertex, float height, TerrainGenerationData terrainData)
        {
            if (terrainData.TerrainType == TerrainType.Spherical)
            {
                return math.normalize(vertex) * height;
            }
            else
            {
                vertex.y = height;
                return vertex;
            }
        }
        
        private static MeshDataComponent CreateMeshDataComponent(NativeArray<float3> vertices, NativeArray<int> indices)
        {
            // Create blob assets
            using var vertexBuilder = new BlobBuilder(Allocator.Temp);
            ref var vertexArray = ref vertexBuilder.ConstructRoot<BlobArray<float3>>();
            var vertexArrayBuilder = vertexBuilder.Allocate(ref vertexArray, vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexArrayBuilder[i] = vertices[i];
            }
            var vertexBlob = vertexBuilder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
            
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
    }
    
    // Helper struct for triangle operations in ECS
    internal readonly struct TriangleECS
    {
        public readonly float3 V1;
        public readonly float3 V2;
        public readonly float3 V3;
        
        public TriangleECS(float3 v1, float3 v2, float3 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }
    }
}