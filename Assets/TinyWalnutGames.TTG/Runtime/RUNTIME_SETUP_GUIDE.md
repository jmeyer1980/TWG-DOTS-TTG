# Runtime Subscene Loading Setup Guide

## Overview

This guide explains how to set up the TTG Terrain Generation system for proper runtime loading in builds. The Unity Editor uses live linking to automatically load subscenes, but builds require explicit setup.

## Problem

- **Editor**: Works perfectly with live linking automatically loading ECS entities
- **Builds**: Subscenes don't auto-load, causing missing entities and broken terrain generation

## Solution

The TTG Runtime Loading System provides multiple approaches to ensure your terrain entities are available in runtime builds.

## Quick Setup

### 1. Add RuntimeTerrainManager to Your Main Scene

1. In your main scene, create an empty GameObject
2. Name it "Runtime Terrain Manager" 
3. Add the `RuntimeTerrainManager` component
4. Configure the settings (see Configuration section below)

### 2. Basic Configuration

**Required Settings:**
- ? `Auto Initialize On Start` = true (recommended)
- ? `Create Default Terrain Entity` = true (for testing)
- ? `Enable Debug Logs` = true (during setup)

**Optional Settings:**
- `Use Unity Scene Loading` = true (if you have terrain entities in separate scenes)
- `Terrain Scene Name` = "YourTerrainScene" (if using separate scenes)
- `Initialization Delay` = 0.5s (allows ECS systems to initialize)

### 3. Test Your Setup

1. **Build and Run** your project
2. Check the Console for debug messages:
   ```
   RuntimeTerrainManager: Initializing terrain system for runtime build...
   RuntimeEntityLoaderSystem: Processing scene loading requests for runtime build...
   No terrain entities found. Creating default terrain entity...
   Successfully created default terrain entity with basic configuration.
   ```
3. Verify terrain generation works in the build

## Configuration Options

### Basic Runtime Settings

| Setting | Description | Recommended |
|---------|-------------|-------------|
| `Auto Initialize On Start` | Automatically start initialization on Start() | ? true |
| `Enable Debug Logs` | Show detailed debug information | ? true (during setup) |
| `Initialization Delay` | Wait time before creating entities | 0.5s |

### Entity Creation

| Setting | Description | Use Case |
|---------|-------------|----------|
| `Create Default Terrain Entity` | Create basic terrain if none exist | Testing & demos |
| `Default Terrain Data` | Parameters for default terrain | Customize for your needs |

### Scene Loading

| Setting | Description | Use Case |
|---------|-------------|----------|
| `Use Unity Scene Loading` | Load additional scenes with terrain entities | Multi-scene projects |
| `Terrain Scene Name` | Name of scene containing terrain entities | When using separate scenes |
| `Terrain Scene Index` | Build index of terrain scene | Alternative to scene name |

## Advanced Setup

### Option 1: Default Entity Creation (Recommended for Testing)

```csharp
// RuntimeTerrainManager settings
createDefaultTerrainEntity = true;
defaultTerrainData = new TerrainGenerationData
{
    TerrainType = TerrainType.Planar,
    Sides = 6,
    Radius = 10f,
    MinHeight = 0f,
    MaxHeight = 5f,
    Depth = 3,
    // ... other parameters
};
```

**Pros:** 
- ? Works immediately in any build
- ? No additional scene setup required
- ? Perfect for testing and prototyping

**Cons:**
- ? Limited to one default terrain configuration
- ? Can't leverage complex authoring workflows

### Option 2: Separate Terrain Scene

1. Create a new scene called "TerrainGenerationScene"
2. Add your terrain generation entities (with authoring components)
3. Add the scene to Build Settings
4. Configure RuntimeTerrainManager:
   ```csharp
   useUnitySceneLoading = true;
   terrainSceneName = "TerrainGenerationScene";
   createDefaultTerrainEntity = false;
   ```

**Pros:**
- ? Supports complex terrain configurations
- ? Can use existing authoring workflows
- ? Multiple terrain types per scene

**Cons:**
- ? Requires additional scene setup
- ? More complex build configuration

### Option 3: Hybrid Approach

```csharp
// Use both approaches for maximum flexibility
useUnitySceneLoading = true;           // Load scene if available
createDefaultTerrainEntity = true;     // Fallback if scene fails
terrainSceneName = "TerrainGenerationScene";
```

**Pros:**
- ? Robust fallback system
- ? Works even if scene loading fails
- ? Best of both approaches

## Monitoring & Debugging

### Runtime Status Monitoring

The RuntimeTerrainManager provides several ways to monitor system status:

```csharp
// Get the RuntimeTerrainManager component
var terrainManager = FindObjectOfType<RuntimeTerrainManager>();

// Check status
if (terrainManager.IsTerrainSystemReady)
{
    Debug.Log($"Terrain system ready with {terrainManager.TerrainEntityCount} entities");
}

// Get detailed status
Debug.Log(terrainManager.GetSystemStatusInfo());
```

### Common Debug Messages

**Success Messages:**
```
RuntimeTerrainManager: Initialization complete in 0.523s
Successfully created default terrain entity with basic configuration.
Terrain entity count updated: 1
```

**Warning Messages:**
```
RuntimeTerrainManager already initialized!
Found 2 existing terrain entities. Skipping default creation.
World not initialized! Call InitializeRuntimeLoading() first.
```

**Error Messages:**
```
Default ECS World not found! Make sure Unity.Entities is properly set up.
Failed to create default terrain entity: [exception details]
RuntimeTerrainManager initialization failed: [exception details]
```

### Context Menu Actions

The RuntimeTerrainManager provides useful context menu actions in the Inspector:

- **Create Terrain Generation Request** - Manually create terrain entities
- **Reload Using Unity SceneManager** - Reload terrain scenes  
- **Update Terrain Entity Count** - Refresh entity count
- **Show System Status** - Display detailed status information

## Build Settings

### Required Steps

1. **Add Scenes to Build**: Include your main scene (and terrain scene if using Option 2)
2. **ECS Settings**: Ensure Unity.Entities package is properly configured
3. **Build Configuration**: Test with Development Build enabled first

### Build Testing Checklist

- [ ] Main scene contains RuntimeTerrainManager component
- [ ] RuntimeTerrainManager is properly configured  
- [ ] Console shows initialization success messages
- [ ] Terrain entities are created/loaded successfully
- [ ] Terrain generation pipeline works as expected
- [ ] Performance is acceptable in build vs editor

## Troubleshooting

### Issue: No terrain entities in build

**Symptoms:** Empty scene, no terrain generation occurs
**Solutions:**
1. Ensure `createDefaultTerrainEntity = true`
2. Check console for initialization error messages
3. Verify ECS World is properly initialized
4. Try increasing `initializationDelay`

### Issue: Scene loading fails

**Symptoms:** Scene not found errors, missing entities
**Solutions:**
1. Verify scene name exactly matches Build Settings
2. Ensure scene is added to Build Settings
3. Check scene index if using `terrainSceneIndex`
4. Enable `createDefaultTerrainEntity` as fallback

### Issue: Performance problems in build

**Symptoms:** Slow initialization, frame drops
**Solutions:**
1. Reduce `defaultTerrainData.Depth` (try 2-3 instead of 4+)
2. Increase `initializationDelay` to spread initialization over time
3. Consider async terrain generation
4. Profile with Unity Profiler to identify bottlenecks

### Issue: Memory leaks in builds

**Symptoms:** Increasing memory usage over time
**Solutions:**
1. Ensure TerrainCleanupSystem is working properly
2. Check blob asset disposal in cleanup systems
3. Monitor with Memory Profiler
4. Verify proper entity lifecycle management

## Best Practices

### Development

1. **Start Simple**: Use default entity creation for initial testing
2. **Enable Logging**: Keep debug logs enabled during development
3. **Test Early**: Build and test frequently, don't rely only on editor testing
4. **Monitor Status**: Use the monitoring APIs to track system health

### Production

1. **Disable Debug Logs**: Set `enableDebugLogs = false` for release builds
2. **Optimize Delay**: Fine-tune `initializationDelay` for your target platform
3. **Error Handling**: Implement fallback strategies for failed initialization
4. **Performance**: Profile initialization cost and optimize terrain parameters

### Multi-Platform

1. **Platform Testing**: Test on all target platforms (mobile, console, etc.)
2. **Memory Constraints**: Adjust terrain complexity for mobile platforms
3. **Loading Times**: Consider platform-specific initialization delays
4. **Build Sizes**: Monitor impact on build size from additional scenes

## Integration Examples

### With Loading Screen

```csharp
public class LoadingScreen : MonoBehaviour
{
    private RuntimeTerrainManager terrainManager;
    
    void Start()
    {
        terrainManager = FindObjectOfType<RuntimeTerrainManager>();
    }
    
    void Update()
    {
        if (terrainManager != null && terrainManager.IsTerrainSystemReady)
        {
            // Hide loading screen, show main game
            gameObject.SetActive(false);
        }
    }
}
```

### With Game State Management

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private RuntimeTerrainManager terrainManager;
    
    public bool CanStartGame()
    {
        return terrainManager != null && 
               terrainManager.IsTerrainSystemReady && 
               terrainManager.TerrainEntityCount > 0;
    }
}
```

## Summary

The TTG Runtime Loading System provides a robust solution for ensuring terrain entities are available in runtime builds. Start with the default entity creation approach for immediate results, then gradually move to more complex scene-based setups as needed.

**Key Points:**
- ? Always include RuntimeTerrainManager in your main scene
- ? Start with default entity creation for testing
- ? Use debug logs to verify proper initialization
- ? Test builds early and often
- ? Monitor system status for production applications

For questions or issues, refer to the troubleshooting section or check the detailed logs provided by the system.