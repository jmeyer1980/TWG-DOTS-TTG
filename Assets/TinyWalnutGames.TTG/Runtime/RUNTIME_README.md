# TTG Terrain Generation - ECS Unity Package

## Overview

**TTG Terrain Generation** is a Unity DOTS/ECS implementation of procedural terraced terrain generation, converted from the original concept by **Lazy Squirrel Labs**. This package provides a complete Entity Component System (ECS) workflow for generating highly detailed, performance-optimized terraced terrain suitable for both planar and spherical worlds.

### Key Features

- ? **Pure ECS Architecture** - Built entirely on Unity DOTS for maximum performance
- ? **Universal Render Pipeline (URP) Support** - Automatic shader detection and fallback system
- ? **Runtime Generation** - Create terrain at runtime with full build support
- ? **Advanced Debug Console** - 20+ debugging commands for comprehensive terrain analysis
- ? **Memory Optimized** - Ultra-aggressive cleanup prevents memory leaks
- ? **Dual Input System Support** - Compatible with both Legacy Input Manager and new Input System
- ? **Production Ready** - Comprehensive error handling, monitoring, and documentation

### Credits & Attribution

This implementation is based on the **Terraced Terrain Generator** concept and algorithms originally developed by **Lazy Squirrel Labs**. While the core geometric algorithms maintain compatibility with the original implementation, this package represents a complete architectural overhaul to support Unity's Entity Component System (ECS) and Data-Oriented Technology Stack (DOTS).

**Original Work**: [Lazy Squirrel Labs - Terraced Terrain Generator](https://github.com/LazySquirrelLabs)  
**ECS Conversion**: Tiny Walnut Games

---

## Quick Start

### Prerequisites

- Unity 2022.3 LTS or newer
- Unity DOTS packages (Entities, Mathematics, Collections)
- Universal Render Pipeline (recommended) or Built-in Render Pipeline

### Basic Setup

1. **Add Runtime Manager**
   ```csharp
   // Add RuntimeTerrainManager to your main scene
   var manager = gameObject.AddComponent<RuntimeTerrainManager>();
   manager.autoInitialize = true;
   manager.createDefaultTerrainEntity = true;
   ```

2. **Configure Settings** (Optional)
   ```csharp
   // Customize default terrain parameters
   manager.defaultTerrainData = new TerrainGenerationData
   {
       Width = 8,
       Height = 8, 
       Depth = 3,
       // ... other parameters
   };
   ```

3. **Build and Run** - The system automatically handles initialization and terrain creation

### Runtime Console Access

Press **F1** in your build to open the debug console:

```bash
# Generate different terrain types
> terrain.spherical 10 3        # Spherical terrain, radius 10, depth 3
> terrain.planar 8 8 4          # Planar terrain, 8x8 grid, depth 4

# Debug visibility issues
> terrain.visibility            # Comprehensive visibility analysis
> debug.materials               # Check material assignments

# Monitor system health
> status                        # Overall system status
> system.memory                 # Memory usage information
```

---

## Architecture Overview

### ECS Components

| Component | Purpose | Usage |
|-----------|---------|-------|
| `TerrainGenerationData` | Core terrain parameters (size, depth, noise) | Configure terrain generation |
| `TerrainGenerationRequest` | Trigger terrain generation | Request new terrain creation |
| `TerraceConfigData` | Height configuration for terraces | Define terrace levels |
| `MeshDataComponent` | Generated mesh data storage | Internal ECS mesh representation |
| `TerrainGenerationState` | Pipeline phase tracking | Monitor generation progress |

### ECS Systems

| System | Purpose | Update Group |
|--------|---------|--------------|
| `TerrainGenerationSystem` | Main generation pipeline | `SimulationSystemGroup` |
| `MeshCreationSystem` | Unity Mesh creation with URP support | `PresentationSystemGroup` |
| `TerrainCleanupSystem` | Memory management and disposal | `SimulationSystemGroup` |
| `RuntimeEntityLoaderSystem` | Runtime scene loading | `InitializationSystemGroup` |

### Runtime Utilities

| Utility | Purpose | Integration |
|---------|---------|-------------|
| `RuntimeTerrainManager` | MonoBehaviour interface for initialization | Add to main scene GameObject |
| `RuntimeDebugConsole` | Advanced debugging and monitoring | Automatic integration |
| `TerrainMemoryManager` | Memory cleanup utilities | Internal system support |

---

## Usage Examples

### Basic Terrain Generation

```csharp
using Unity.Entities;
using TinyWalnutGames.TTG.TerrainGeneration;

public class TerrainExample : MonoBehaviour
{
    void Start()
    {
        // Get the default ECS World
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        
        // Create terrain generation request
        var entity = entityManager.CreateEntity();
        entityManager.AddComponentData(entity, new TerrainGenerationData
        {
            Width = 10,
            Height = 10,
            Depth = 4,
            TerrainType = TerrainType.Planar,
            // Configure other parameters as needed
        });
        
        entityManager.AddComponent<TerrainGenerationRequest>(entity);
    }
}
```

### Advanced Configuration

```csharp
// Configure detailed terrain parameters
var terrainData = new TerrainGenerationData
{
    Width = 12,
    Height = 12,
    Depth = 5,
    TerrainType = TerrainType.Spherical,
    
    // Noise settings
    NoiseStrength = 0.15f,
    NoiseScale = 5f,
    NoiseOctaves = 3,
    NoisePersistence = 0.6f,
    
    // Terracing
    TerraceCount = 8,
    TerraceHeight = 0.5f,
    
    // Mesh fragmentation
    FragmentationDepth = 3,
    UseSphericalFragmentation = true
};

// Create terrace configuration
var terraceConfig = TerraceConfigData.Create(new float[] 
{
    0.0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f
});

// Apply to entity
entityManager.AddComponentData(entity, terrainData);
entityManager.AddComponentData(entity, terraceConfig);
```

### Runtime Monitoring

```csharp
// Monitor terrain generation progress
var terrainManager = FindObjectOfType<RuntimeTerrainManager>();

if (terrainManager.IsTerrainSystemReady)
{
    Debug.Log($"System ready with {terrainManager.TerrainEntityCount} terrain entities");
    
    // Get detailed status
    string statusInfo = terrainManager.GetSystemStatusInfo();
    Debug.Log(statusInfo);
}

// Check initialization status
switch (terrainManager.Status)
{
    case RuntimeTerrainManager.InitializationStatus.Complete:
        // System ready for terrain generation
        break;
    case RuntimeTerrainManager.InitializationStatus.Failed:
        Debug.LogError("Terrain system initialization failed!");
        break;
    case RuntimeTerrainManager.InitializationStatus.InProgress:
        // Still initializing, wait for completion
        break;
}
```

---

## Advanced Features

### Debug Console Commands

The runtime debug console provides extensive debugging capabilities:

#### Terrain Commands
```bash
terrain.spherical <radius> <depth>     # Generate spherical terrain
terrain.planar <width> <height> <depth> # Generate planar terrain  
terrain.regenerate                     # Regenerate current terrain
terrain.delete                         # Delete all terrain entities
terrain.count                          # Show terrain entity count
terrain.materials                      # Show material assignments
terrain.visibility                     # Advanced visibility debugging
```

#### System Commands
```bash
system.memory                          # Show memory usage and GC info
system.entities                        # Show ECS entity counts
system.gc                             # Force garbage collection
system.worlds                         # Show ECS world information
system.time                           # Show system timing data
```

#### Debug Commands  
```bash
debug.camera                          # Camera frustum and position info
debug.renderers                       # Scene renderer analysis
debug.materials                       # Material system status
debug.performance                     # Performance metrics (FPS, frame time)
debug.bounds                          # Mesh bounds intersection testing
debug.layers                          # Layer culling mask analysis
```

### Material System

The package includes an advanced material system with automatic render pipeline detection:

```csharp
// URP projects automatically get URP-compatible materials
// BRP projects automatically get legacy shader materials
// No configuration required - system detects render pipeline automatically

// Manual material assignment (optional)
var materialData = TerrainMaterialData.Create(new Material[] 
{
    myFloorMaterial,    // Material for horizontal surfaces
    myWallMaterial      // Material for vertical surfaces
});

entityManager.AddComponentData(entity, materialData);
```

### Memory Management

The system includes ultra-aggressive memory management:

```csharp
// Memory cleanup is automatic, but you can force it:
TerrainMemoryManager.ForceCleanup();

// Monitor memory usage
var memoryInfo = TerrainMemoryManager.GetMemoryUsage();
Debug.Log($"Managed Memory: {memoryInfo.managedMemory} MB");
Debug.Log($"Native Memory: {memoryInfo.nativeMemory} MB");
```

---

## Configuration Options

### RuntimeTerrainManager Settings

```csharp
[Header("Initialization")]
public bool autoInitialize = true;           // Auto-start on Awake()
public float initializationDelay = 0.1f;     // Delay before initialization
public bool createDefaultTerrainEntity = true; // Create default terrain if none exist

[Header("Scene Loading")]
public string terrainSceneName = "";         // Optional terrain scene name
public int terrainSceneIndex = -1;           // Optional terrain scene index
public bool useUnitySceneManager = false;    // Use traditional scene loading

[Header("Debug")]
public bool enableDebugLogs = true;          // Enable detailed logging
public bool showInitializationStatus = true; // Show status in inspector

[Header("Default Terrain")]
public TerrainGenerationData defaultTerrainData; // Default terrain configuration
```

### Console Configuration

```csharp
[Header("Console Settings")]
public KeyCode toggleKey = KeyCode.F1;       // Console toggle key
public int maxLogEntries = 200;              // Maximum log history
public float backgroundOpacity = 0.8f;       // Background transparency
public bool autoScroll = true;               // Auto-scroll to latest logs
public bool showTimestamps = true;           // Show log timestamps

[Header("Input System")]
public InputMode inputMode = InputMode.Auto; // Auto-detect input system
```

---

## Performance Considerations

### Recommended Settings

For optimal performance across different platforms:

```csharp
// Mobile/Low-end devices
terrainData.Depth = 2;                    // Reduce subdivision depth
terrainData.FragmentationDepth = 2;       // Reduce fragmentation
terrainData.TerraceCount = 4;             // Fewer terraces

// Desktop/High-end devices  
terrainData.Depth = 4;                    // Higher detail
terrainData.FragmentationDepth = 3;       // More fragmentation
terrainData.TerraceCount = 8;             // More terraces

// VR/High-performance requirements
terrainData.Depth = 5;                    // Maximum detail
terrainData.FragmentationDepth = 4;       // Maximum fragmentation
terrainData.TerraceCount = 12;            // Maximum terraces
```

### Memory Optimization

```csharp
// Enable aggressive cleanup for memory-constrained environments
RuntimeTerrainManager.ForceAggressiveCleanup = true;

// Monitor memory usage
var usage = TerrainMemoryManager.GetMemoryUsage();
if (usage.managedMemory > 100) // MB threshold
{
    TerrainMemoryManager.ForceCleanup();
}
```

### Build Optimization

```csharp
// Production builds should disable debug features
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
    RuntimeDebugConsole.EnableConsole = false;
    RuntimeTerrainManager.EnableDebugLogs = false;
#endif
```

---

## Troubleshooting

### Common Issues

#### Issue: Terrain not visible in builds
**Symptoms:** Terrain generates but appears invisible
**Solutions:**
1. Check render pipeline compatibility - enable URP if using URP
2. Use debug console: `terrain.visibility` for detailed analysis
3. Verify materials: `debug.materials` to check assignments
4. Check camera position: `debug.camera` for frustum analysis

#### Issue: Memory leaks during terrain generation
**Symptoms:** Increasing memory usage over time
**Solutions:**
1. Ensure cleanup systems are running: `system.entities`
2. Force cleanup: `system.gc` or `debug.cleanup`
3. Monitor with: `system.memory`
4. Check blob asset disposal in custom code

#### Issue: Console not responding to input
**Symptoms:** F1 key not opening console
**Solutions:**
1. Check Input System: Use `InputMode.LegacyInput` or `InputMode.NewInput`
2. Verify key binding in RuntimeDebugConsole settings
3. Ensure console GameObject is active in scene
4. Check for conflicting input handlers

#### Issue: Poor performance during generation
**Symptoms:** Frame drops, stuttering during terrain creation
**Solutions:**
1. Reduce `Depth` and `FragmentationDepth` parameters
2. Increase `initializationDelay` to spread load over time
3. Generate terrain asynchronously in smaller chunks
4. Use console: `debug.performance` to identify bottlenecks

### Debug Console Workflow

For systematic debugging:

```bash
# 1. Check overall system status
> status

# 2. Verify terrain entities exist
> terrain.count
> system.entities

# 3. Check visibility if terrain invisible
> terrain.visibility
> debug.camera
> debug.materials

# 4. Monitor performance
> debug.performance
> system.memory

# 5. Force cleanup if memory issues
> system.gc
> debug.cleanup
```

---

## Integration with Other Systems

### Custom ECS Systems

```csharp
[UpdateAfter(typeof(TerrainGenerationSystem))]
public partial class CustomTerrainSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Process completed terrain entities
        Entities
            .WithAll<GeneratedTerrainMeshTag>()
            .ForEach((Entity entity, in TerrainGenerationState state) =>
            {
                if (state.IsComplete)
                {
                    // Your custom terrain processing
                    Debug.Log($"Terrain {entity} ready for custom processing");
                }
            }).WithoutBurst().Run();
    }
}
```

### Event System Integration

```csharp
public class TerrainEventManager : MonoBehaviour
{
    public static event System.Action<Entity> OnTerrainGenerated;
    
    void Update()
    {
        // Check for newly completed terrain
        var world = World.DefaultGameObjectInjectionWorld;
        var query = world.EntityManager.CreateEntityQuery(
            typeof(GeneratedTerrainMeshTag),
            typeof(TerrainGenerationState)
        );
        
        var entities = query.ToEntityArray(Allocator.TempJob);
        foreach (var entity in entities)
        {
            var state = world.EntityManager.GetComponentData<TerrainGenerationState>(entity);
            if (state.IsComplete)
            {
                OnTerrainGenerated?.Invoke(entity);
            }
        }
        entities.Dispose();
    }
}
```

---

## API Reference

### Core Components

#### TerrainGenerationData
```csharp
public struct TerrainGenerationData : IComponentData
{
    public int Width;                     // Terrain width (planar) or segments (spherical)
    public int Height;                    // Terrain height (planar only)
    public int Depth;                     // Subdivision depth
    public TerrainType TerrainType;       // Planar or Spherical
    public float NoiseStrength;           // Noise amplitude
    public float NoiseScale;              // Noise frequency
    public int NoiseOctaves;              // Noise detail levels
    public float NoisePersistence;        // Noise octave falloff
    public int TerraceCount;              // Number of terrace levels
    public float TerraceHeight;           // Height between terraces
    public int FragmentationDepth;        // Mesh subdivision depth
    public bool UseSphericalFragmentation; // Use spherical subdivision
}
```

#### TerrainGenerationState
```csharp
public struct TerrainGenerationState : IComponentData
{
    public GenerationPhase CurrentPhase;  // Current pipeline phase
    public bool IsComplete;               // Generation completed flag
    public Entity ResultMeshEntity;       // Generated mesh entity reference
    public float3 TerrainCenter;          // Terrain center position
    public float TerrainRadius;           // Terrain bounding radius
}
```

### Runtime Manager API

#### RuntimeTerrainManager
```csharp
public class RuntimeTerrainManager : MonoBehaviour
{
    // Status Properties
    public InitializationStatus Status { get; }
    public int TerrainEntityCount { get; }
    public bool IsTerrainSystemReady { get; }
    
    // Control Methods
    public void InitializeRuntimeLoading();
    public void CreateDefaultTerrainEntity();
    public void ForceUpdateTerrainEntityCount();
    public string GetSystemStatusInfo();
    
    // Event System
    public static event System.Action<InitializationStatus> OnStatusChanged;
    public static event System.Action<int> OnTerrainEntityCountChanged;
}
```

### Debug Console API

#### RuntimeDebugConsole
```csharp
public class RuntimeDebugConsole : MonoBehaviour
{
    // Console Control
    public static bool EnableConsole { get; set; }
    public static void ToggleConsole();
    public static void LogMessage(string message, LogType logType);
    
    // Command Registration
    public void RegisterCommand(string command, System.Action<string[]> handler, string description);
    public void UnregisterCommand(string command);
    
    // Status Properties
    public bool IsVisible { get; }
    public int LogEntryCount { get; }
}
```

---

## Best Practices

### Development Workflow

1. **Start Simple**: Begin with basic planar terrain and low depth values
2. **Incremental Testing**: Test each parameter change in builds, not just editor
3. **Performance Profiling**: Use Unity Profiler with the debug console for monitoring
4. **Memory Monitoring**: Watch memory usage during development and implement limits
5. **Error Handling**: Implement proper error handling for terrain generation failures

### Production Deployment

```csharp
// Production configuration example
var productionConfig = new TerrainGenerationData
{
    Width = 8,                    // Conservative size
    Height = 8,
    Depth = 3,                    // Balanced detail/performance
    TerrainType = TerrainType.Planar,
    NoiseStrength = 0.1f,         // Subtle height variation
    NoiseScale = 4f,
    NoiseOctaves = 2,             // Reduced complexity
    NoisePersistence = 0.5f,
    TerraceCount = 6,             // Good visual detail
    TerraceHeight = 0.4f,
    FragmentationDepth = 2,       // Conservative subdivision
    UseSphericalFragmentation = false
};

// Production manager setup
var manager = RuntimeTerrainManager.Instance;
manager.autoInitialize = true;
manager.createDefaultTerrainEntity = true;
manager.initializationDelay = 0.2f;        // Spread initialization load
manager.enableDebugLogs = false;           // Disable debug in production
```

### Code Organization

```csharp
// Organize terrain systems in logical groups
namespace YourProject.Terrain
{
    using TinyWalnutGames.TTG.TerrainGeneration;
    
    // Custom terrain behaviors
    public partial class CustomTerrainBehaviorSystem : SystemBase { }
    
    // Terrain interaction systems  
    public partial class TerrainCollisionSystem : SystemBase { }
    
    // Terrain modification systems
    public partial class TerrainDeformationSystem : SystemBase { }
}
```

---

## Support & Contributing

### Getting Help

1. **Debug Console**: Use `help` command for real-time assistance
2. **Documentation**: Check `RUNTIME_SETUP_GUIDE.md` for detailed setup instructions
3. **Development Log**: Review `DEVELOPMENT_LOG.md` for known issues and solutions
4. **Community**: Reach out to the Unity DOTS community for ECS-specific questions

### Reporting Issues

When reporting issues, please include:
- Unity version and render pipeline (URP/BRP)
- Platform and target (Windows, Android, etc.)
- Debug console output (`status`, `system.memory`, `debug.performance`)
- Terrain configuration parameters used
- Steps to reproduce the issue

### Contributing

This package is part of the Tiny Walnut Games TTG ecosystem. Contributions should:
- Follow the established ECS patterns and naming conventions
- Include comprehensive unit tests for new features
- Update documentation for any public API changes
- Maintain compatibility with both URP and BRP render pipelines
- Preserve memory optimization and performance characteristics

---

## License & Attribution

**ECS Implementation**: © 2025 Tiny Walnut Games  
**Original Concept**: © Lazy Squirrel Labs - Terraced Terrain Generator

This package is based on the terraced terrain generation algorithms and concepts originally developed by Lazy Squirrel Labs. The ECS implementation represents a complete architectural overhaul while maintaining compatibility with the core geometric algorithms.

Special thanks to the Unity DOTS team for providing the Entity Component System framework that makes this high-performance terrain generation possible.

---

*Generated terrain awaits! Happy building! ??*