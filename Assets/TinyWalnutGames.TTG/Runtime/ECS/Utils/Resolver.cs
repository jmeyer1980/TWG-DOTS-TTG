using System.Collections.Generic;
using Unity.Entities;

// For C# 9+ record/record struct support on .NET Framework 4.7.1
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Placeholder for mesh blob data. Replace with actual implementation as needed.
    /// </summary>
    public struct MeshBlob { }

    /// <summary>
    /// Generic resolver interface for mapping keys to values.
    /// </summary>
    public interface IResolver<TKey, TValue>
        where TKey : struct
        where TValue : struct
    {
        void Register(TKey key, TValue value);
        bool TryResolve(TKey key, out TValue value);
    }

    /// <summary>
    /// Example mesh key for identifying mesh data in ECS systems.
    /// </summary>
    public readonly record struct MeshKey(int LOD, int ChunkX, int ChunkY);

    /// <summary>
    /// Example mesh value for storing mesh data and related ECS entities.
    /// </summary>
    public readonly record struct MeshValue(
        BlobAssetReference<MeshBlob> Data,
        Entity ColliderEntity
    );

    /// <summary>
    /// Mesh resolver using record structs for DOTS-friendly value semantics.
    /// </summary>
    public class MeshResolver : IResolver<MeshKey, MeshValue>
    {
        private readonly Dictionary<MeshKey, MeshValue> _map = new();

        public void Register(MeshKey key, MeshValue value) => _map[key] = value;

        public bool TryResolve(MeshKey key, out MeshValue value) => _map.TryGetValue(key, out value);
    }
}
