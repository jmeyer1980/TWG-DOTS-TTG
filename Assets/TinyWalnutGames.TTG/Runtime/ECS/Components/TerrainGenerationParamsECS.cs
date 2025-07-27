using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Data-only component for terrain generation parameters in ECS.
    /// </summary>
    public struct TerrainGenerationParams : IComponentData
    {
        public float Interval;
        public bool Async;
        public float LastGeneration;
    }

    /// <summary>
    /// Authoring MonoBehaviour for terrain generation parameters.
    /// Note: Manual conversion required in systems or through other means.
    /// </summary>
    public class TerrainGenerationParamsAuthoring : MonoBehaviour
    {
        public float interval = 5f;
        public bool async = true;

        /// <summary>
        /// Helper method to create the component data from authoring values.
        /// </summary>
        /// <returns>TerrainGenerationParams component data</returns>
        public TerrainGenerationParams ToComponentData()
        {
            return new TerrainGenerationParams
            {
                Interval = interval,
                Async = async,
                LastGeneration = 0f
            };
        }
    }
}