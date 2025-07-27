using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Example controller for testing terrain generation in ECS.
    /// </summary>
    public class TerrainGenerationController : MonoBehaviour
    {
        [Header("Runtime Control")]
        [SerializeField] private bool generateOnUpdate = false;
        [SerializeField] private float generateInterval = 5f;
        
        private float lastGenerationTime;
        private EntityManager entityManager;
        private Entity terrainEntity;
        
        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Find the terrain entity (assuming it was created through authoring)
            var query = entityManager.CreateEntityQuery(
                typeof(TerrainGenerationData), 
                typeof(TerraceConfigData)
            );
            
            if (query.CalculateEntityCount() > 0)
            {
                terrainEntity = query.GetSingletonEntity();
            }
            
            query.Dispose();
        }
        
        private void Update()
        {
            if (!generateOnUpdate || terrainEntity == Entity.Null)
                return;
                
            if (Time.time - lastGenerationTime >= generateInterval)
            {
                TriggerTerrainGeneration();
                lastGenerationTime = Time.time;
            }
        }
        
        /// <summary>
        /// Manually trigger terrain generation.
        /// </summary>
        public void TriggerTerrainGeneration()
        {
            if (terrainEntity == Entity.Null)
                return;
                
            // Add generation request component to trigger generation
            var request = new TerrainGenerationRequest
            {
                UseAsyncGeneration = true
            };
            
            entityManager.AddComponentData(terrainEntity, request);
            
            Debug.Log("Terrain generation requested");
        }
        
        /// <summary>
        /// Check if terrain generation is in progress.
        /// </summary>
        public bool IsGenerating()
        {
            if (terrainEntity == Entity.Null)
                return false;
                
            if (!entityManager.HasComponent<TerrainGenerationState>(terrainEntity))
                return false;
                
            var state = entityManager.GetComponentData<TerrainGenerationState>(terrainEntity);
            return !state.IsComplete && !state.HasError;
        }
        
        /// <summary>
        /// Get current generation phase.
        /// </summary>
        public GenerationPhase GetCurrentPhase()
        {
            if (terrainEntity == Entity.Null || 
                !entityManager.HasComponent<TerrainGenerationState>(terrainEntity))
                return GenerationPhase.NotStarted;
                
            var state = entityManager.GetComponentData<TerrainGenerationState>(terrainEntity);
            return state.CurrentPhase;
        }
    }
}