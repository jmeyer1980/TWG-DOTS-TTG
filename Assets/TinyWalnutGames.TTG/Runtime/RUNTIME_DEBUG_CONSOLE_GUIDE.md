# Runtime Debug Console Guide

## Overview

The TTG Runtime Debug Console provides a comprehensive debugging and command execution interface for runtime builds where Unity's editor console isn't available. This system integrates seamlessly with the TTG Terrain Generation system and provides both output logging and interactive command capabilities.

## Key Features

? **Runtime Logging**: Capture and display Unity debug logs in builds  
? **Command System**: Execute terrain generation and system commands  
? **TTG Integration**: Direct integration with RuntimeTerrainManager  
? **Memory Monitoring**: Real-time memory usage and performance tracking  
? **Entity Inspection**: ECS entity information and manipulation  
? **Auto-Complete**: Tab completion for commands  
? **Command History**: Navigate previous commands with arrow keys  
? **Persistent**: Console persists across scene changes  

## Quick Setup

### Option 1: Automatic Setup (Recommended)

1. **Add Setup Component**: 
   - Create empty GameObject in your main scene
   - Add `RuntimeDebugConsoleSetup` component
   - Configure settings in Inspector
   - Console auto-creates when scene starts

2. **Configure Settings**:
   ```csharp
   Toggle Key: F1 (default)
   Max Log Entries: 200
   Background Opacity: 0.8
   Auto Scroll: true
   Show Timestamps: true
   ```

3. **Test Console**:
   - Build and run your project
   - Press F1 to toggle console
   - Type `help` to see available commands

### Option 2: Manual Setup

1. **Create Console GameObject**:
   ```csharp
   var consoleGO = new GameObject("Runtime Debug Console");
   var console = consoleGO.AddComponent<RuntimeDebugConsole>();
   DontDestroyOnLoad(consoleGO);
   ```

2. **Configure Properties**:
   ```csharp
   console.toggleKey = KeyCode.F1;
   console.maxLogEntries = 200;
   console.backgroundOpacity = 0.8f;
   // ... other settings
   ```

## Console Interface

### Layout

```
???????????????????????????????????????????????????????????
?                    TTG Debug Console                   ?
???????????????????????????????????????????????????????????
?                         ?        === TTG STATUS ===    ?
?     Log Output Area     ?        Status: Complete       ?
?                         ?        Entities: 1            ?
?  [Timestamped logs      ?        Ready: True            ?
?   with color coding]    ?                               ?
?                         ?        === MEMORY ===         ?
?                         ?        Used: 45.2 MB          ?
?                         ?        FPS: 60                ?
?                         ?                               ?
?                         ?        === CONTROLS ===       ?
?                         ?        Toggle: F1             ?
?                         ?        History: ??            ?
?                         ?        Complete: Tab          ?
?                         ?        Execute: Enter         ?
???????????????????????????????????????????????????????????
? > command input         ? [Execute]                     ?
???????????????????????????????????????????????????????????
```

### Controls

| Key | Action |
|-----|--------|
| **F1** (configurable) | Toggle console visibility |
| **Enter** | Execute command |
| **? / ?** | Navigate command history |
| **Tab** | Auto-complete command |
| **Escape** | Clear input field |

## Available Commands

### General Commands

| Command | Description | Usage |
|---------|-------------|--------|
| `help` | Show all available commands | `help` |
| `clear` | Clear the console log | `clear` |
| `status` | Show TTG system status | `status` |
| `quit` | Quit the application | `quit` |

### Terrain Generation Commands

| Command | Description | Usage |
|---------|-------------|--------|
| `terrain.regenerate` | Regenerate current terrain | `terrain.regenerate` |
| `terrain.create` | Create new terrain with default parameters | `terrain.create` |
| `terrain.spherical` | Create spherical terrain | `terrain.spherical [radius] [depth]` |
| `terrain.planar` | Create planar terrain | `terrain.planar [sides] [radius] [depth]` |
| `terrain.delete` | Delete all terrain entities | `terrain.delete` |
| `terrain.count` | Show terrain entity count | `terrain.count` |

**Examples:**
```
> terrain.spherical 15 4
Created spherical terrain: radius=15, depth=4

> terrain.planar 8 12 3
Created planar terrain: sides=8, radius=12, depth=3

> terrain.delete
Deleted 2 terrain entities.
```

### Parameter Commands

| Command | Description | Usage |
|---------|-------------|--------|
| `set.seed` | Set terrain seed | `set.seed [value]` |
| `set.depth` | Set fragmentation depth | `set.depth [1-6]` |
| `set.height` | Set terrain height range | `set.height [min] [max]` |
| `set.noise` | Set noise parameters | `set.noise [frequency] [octaves] [persistence]` |

**Examples:**
```
> set.seed 42
Seed set to 42. Use terrain.regenerate to apply.

> set.height 0 10
Height range set to 0-10. Use terrain.regenerate to apply.

> set.noise 0.2 6 0.7
Noise parameters set: freq=0.2, octaves=6, persistence=0.7. Use terrain.regenerate to apply.
```

### System Commands

| Command | Description | Usage |
|---------|-------------|--------|
| `system.gc` | Force garbage collection | `system.gc` |
| `system.memory` | Show memory usage information | `system.memory` |
| `system.entities` | Show ECS entity information | `system.entities` |
| `system.reload` | Reload terrain scene | `system.reload` |

**Examples:**
```
> system.memory
Total Memory: 67.3 MB
System Memory: 8192 MB
Graphics Memory: 4096 MB
ECS Entities: 15/1024

> system.gc
Garbage collection completed. Freed 2.1 MB
```

### Debug Commands

| Command | Description | Usage |
|---------|-------------|--------|
| `debug.logs` | Toggle debug log capture | `debug.logs` |
| `debug.performance` | Show performance information | `debug.performance` |
| `debug.cleanup` | Force TTG cleanup systems | `debug.cleanup` |

**Examples:**
```
> debug.performance
FPS: 58.3
Frame Time: 17.2ms
Time Scale: 1.00
Fixed Delta: 0.020s
Platform: WindowsPlayer
Unity Version: 2023.2.1f1
```

## Integration with Your Code

### Logging to Console

```csharp
// Static method for easy logging
RuntimeDebugConsoleSetup.LogToConsole("Custom message", LogType.Log);
RuntimeDebugConsoleSetup.LogToConsole("Warning message", LogType.Warning);
RuntimeDebugConsoleSetup.LogToConsole("Error message", LogType.Error);

// Check if console is available
if (RuntimeDebugConsoleSetup.IsConsoleAvailable())
{
    Debug.Log("Console is ready for logging");
}
```

### Adding Custom Commands

```csharp
// Get console instance
var console = RuntimeDebugConsoleSetup.GetConsoleInstance();
if (console != null)
{
    // Add custom command (this would require extending the system)
    // console.RegisterCommand("mycommand", MyCommandHandler, "Description");
}
```

### Monitoring System Status

The console automatically displays real-time status information:

- **TTG Status**: RuntimeTerrainManager status and entity counts
- **Memory Usage**: Current memory consumption and GC information  
- **Performance**: FPS, frame time, and platform information
- **ECS Information**: Entity counts and world status

## Configuration Options

### Console Appearance

```csharp
[Header("Console Settings")]
public KeyCode toggleKey = KeyCode.F1;           // Toggle key
public int maxLogEntries = 200;                  // Log history size
public float backgroundOpacity = 0.8f;           // Background transparency
public bool autoScroll = true;                   // Auto-scroll to bottom
public bool showTimestamps = true;               // Show log timestamps
```

### TTG Integration

```csharp
[Header("TTG Integration")]
public RuntimeTerrainManager terrainManager;     // Auto-found if null
```

The console automatically finds and integrates with your `RuntimeTerrainManager` to provide:
- Real-time terrain system status
- Direct terrain generation commands
- Entity count monitoring
- System health checks

## Use Cases

### Development & Testing

```bash
# Quick terrain testing
> terrain.spherical 10 3
> terrain.planar 6 8 4
> terrain.delete

# Parameter experimentation
> set.seed 12345
> set.noise 0.15 5 0.6
> terrain.regenerate

# Performance monitoring
> system.memory
> debug.performance
> system.gc
```

### Production Debugging

```bash
# Check system health
> status
> system.entities
> terrain.count

# Memory investigation
> system.memory
> system.gc
> debug.cleanup

# Emergency recovery
> terrain.delete
> system.reload
> terrain.create
```

### User Support

The console can be left enabled in production builds to help users:
- Report system status information
- Attempt basic recovery operations
- Provide detailed error information
- Test different terrain configurations

## Best Practices

### Development

1. **Enable During Development**: Keep console enabled with debug logs
2. **Test Commands**: Verify all terrain commands work in your build
3. **Monitor Performance**: Use memory and performance commands regularly
4. **Custom Commands**: Add project-specific commands as needed

### Production

1. **Consider Security**: Disable sensitive commands in production if needed
2. **User Training**: Provide simple command guide for users
3. **Log Analysis**: Use console logs for support and debugging
4. **Performance Impact**: Monitor console overhead in production builds

### Memory Management

1. **Log Limits**: Configure appropriate `maxLogEntries` for your platform
2. **Regular Cleanup**: Use `system.gc` periodically if needed
3. **Memory Monitoring**: Watch `system.memory` output for leaks
4. **TTG Cleanup**: Use `debug.cleanup` to force blob asset cleanup

## Troubleshooting

### Console Not Appearing

**Check:**
- Toggle key is correct (default: F1)
- RuntimeDebugConsoleSetup component is in scene
- Console GameObject was created successfully
- No compilation errors preventing creation

**Solutions:**
```bash
# Check if console exists
> RuntimeDebugConsoleSetup.IsConsoleAvailable()

# Manually create console
# Add RuntimeDebugConsoleSetup component to any GameObject
```

### Commands Not Working

**Check:**
- RuntimeTerrainManager is found and linked
- ECS World is available and initialized
- Correct command syntax and parameters
- Console shows any error messages

**Solutions:**
```bash
# Check system status
> status

# Verify terrain manager
> terrain.count

# Check ECS world
> system.entities
```

### Performance Issues

**Check:**
- Log entry limit (`maxLogEntries`)
- Console update frequency
- Background opacity impact
- Command execution overhead

**Solutions:**
```bash
# Reduce log entries
maxLogEntries = 100

# Force garbage collection
> system.gc

# Check memory usage
> system.memory
```

### Memory Leaks

**Check:**
- TTG blob asset cleanup
- Console log accumulation
- Command history size
- Unity log capture overhead

**Solutions:**
```bash
# Force TTG cleanup
> debug.cleanup

# Force garbage collection
> system.gc

# Clear console logs
> clear

# Check current memory
> system.memory
```

## Advanced Features

### Command History

The console maintains a history of executed commands:
- Navigate with ?/? arrow keys
- Stores last 50 commands
- Persistent across console sessions
- Excludes duplicate consecutive commands

### Auto-Complete

Tab completion for commands:
- Partial command matching
- Shows multiple matches if ambiguous
- Case-insensitive matching
- Completes to longest common prefix

### Color-Coded Logs

Log entries are color-coded by type:
- **White**: Normal log messages
- **Yellow**: Warning messages  
- **Red**: Error and exception messages
- **Magenta**: Assert messages

### Real-Time Status

The status panel updates automatically showing:
- TTG system status and entity counts
- Memory usage and garbage collection stats
- Performance metrics (FPS, frame time)
- ECS world information
- Console controls reminder

## Summary

The TTG Runtime Debug Console provides a powerful debugging and command interface for runtime builds. Key benefits:

? **Complete Logging**: All Unity debug output captured and displayed  
? **Interactive Commands**: Direct terrain generation and system control  
? **Real-Time Monitoring**: Live system status and performance metrics  
? **User-Friendly**: Intuitive interface with history and auto-complete  
? **Production-Ready**: Suitable for both development and production builds  
? **TTG Integration**: Seamless integration with terrain generation system  

**Quick Start:**
1. Add `RuntimeDebugConsoleSetup` component to any GameObject
2. Build and run your project  
3. Press F1 to toggle console
4. Type `help` to explore available commands
5. Use `terrain.*` commands to control terrain generation

The console enhances your runtime debugging capabilities and provides users with powerful tools for terrain generation and system monitoring.