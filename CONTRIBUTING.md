# Contributing to TTG Terrain Generation

Thank you for your interest in contributing to the TTG (Tiny Walnut Games) Terrain Generation project! This document provides comprehensive guidelines for contributing to our Unity DOTS/ECS terrain generation system.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Guidelines](#contributing-guidelines)
- [Code Standards](#code-standards)
- [Testing Requirements](#testing-requirements)
- [Documentation Standards](#documentation-standards)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Community Guidelines](#community-guidelines)
- [Development Resources](#development-resources)

## Getting Started

### Prerequisites

Before contributing, ensure you have:

- **Unity 2022.3 LTS** or newer
- **Unity DOTS packages** installed (Entities, Mathematics, Collections, Transforms, Burst)
- **Git** for version control
- **Visual Studio 2022** or compatible IDE with C# support
- Basic knowledge of **Unity ECS/DOTS** architecture
- Understanding of **C# .NET Framework 4.7.1**
- Familiarity with terrain generation concepts and mesh manipulation

### Project Overview

TTG Terrain Generation is a production-ready Unity DOTS/ECS implementation converted from Lazy Squirrel Labs' Terraced Terrain Generator. Key features:

- ? **Pure ECS Implementation** with frame-by-frame processing
- ? **URP/BRP Compatibility** with automatic shader fallbacks
- ? **154+ Tests** with 100% pass rate and complete coverage
- ? **Runtime Loading System** for editor/build parity
- ? **Advanced Debug Console** with 20+ runtime commands
- ? **Comprehensive Memory Management** preventing blob asset leaks

### Project Structure

```
TinyWalnutGames.TTG/
??? Runtime/
?   ??? ECS/
?   ?   ??? Systems/           # Core ECS systems (TerrainGenerationSystem, MeshCreationSystem, etc.)
?   ?   ??? Components/        # ECS components and data structures
?   ?   ??? Utils/            # Runtime utilities (RuntimeTerrainManager, DebugConsole, etc.)
?   ??? Documentation/        # Setup guides, runtime guides, development log
??? Tests/
?   ??? Unit/                 # Component and basic functionality tests
?   ??? Integration/          # Multi-system workflow tests
?   ??? Performance/          # Benchmarking and performance tests
?   ??? EdgeCase/            # Error handling and boundary condition tests
?   ??? Documentation/       # Testing guides and manifests
??? Documentation/            # Project-wide documentation
```

## Development Setup

### 1. Clone and Setup

```bash
# Clone the repository
git clone <repository-url>
cd ttg-terrain-generation

# Open in Unity 2022.3 LTS or newer
# The project will automatically import required DOTS packages
```

### 2. Verify Installation

Run this verification test to ensure proper setup:

```csharp
[Test]
public void VerifySetup()
{
    // Should pass if ECS packages are correctly installed
    var world = new World("Test World");
    var entityManager = world.EntityManager;
    var entity = entityManager.CreateEntity();
    
    Assert.IsTrue(entity != Entity.Null);
    world.Dispose();
}
```

### 3. Run Existing Tests

```bash
# In Unity: Window > General > Test Runner
# Run EditMode tests (fast): ~3 minutes
# Run PlayMode tests (integration): ~4 minutes
# All 154 tests should pass with 100% success rate
```

### 4. Explore Debug Console

```csharp
// Add RuntimeDebugConsoleSetup to any GameObject
var console = gameObject.AddComponent<RuntimeDebugConsoleSetup>();
console.toggleKey = KeyCode.F1;

// In builds, press F1 and try commands:
// > terrain.spherical 15 4
// > system.memory
// > debug.performance
```

## Contributing Guidelines

### Types of Contributions

We welcome contributions in the following areas:

#### ?? **Code Contributions**
- Bug fixes and performance improvements
- New terrain generation features (noise algorithms, terracing methods)
- ECS system optimizations and architectural improvements
- Cross-platform compatibility enhancements
- Memory management and blob asset optimizations
- URP/BRP shader compatibility improvements

#### ?? **Documentation**
- Setup guides and integration tutorials
- API documentation and code examples
- Troubleshooting guides and FAQ sections
- Performance optimization guides
- Best practices documentation

#### ?? **Testing**
- New test cases for edge conditions and error scenarios
- Performance benchmark improvements and regression tests
- Cross-platform testing (Windows, Mac, Linux, Android, iOS)
- Test infrastructure enhancements and utilities

#### ?? **Issue Reporting**
- Bug reports with detailed reproduction steps
- Feature requests with technical specifications
- Performance issues with profiling data and analysis
- Documentation gaps, errors, or improvement suggestions

### Contribution Workflow

1. **Fork** the repository to your GitHub account
2. **Create** a feature branch (`git checkout -b feature/amazing-terrain-feature`)
3. **Develop** your changes following our coding standards
4. **Test** thoroughly (all 154 existing tests must continue to pass)
5. **Document** your changes and update relevant guides
6. **Commit** with clear, descriptive messages
7. **Push** to your fork (`git push origin feature/amazing-terrain-feature`)
8. **Submit** a pull request with detailed description

## Code Standards

### Naming Conventions

Follow the established TTG naming conventions outlined in DEVELOPMENT_LOG.md:

```csharp
// Systems: PascalCase ending with "System"
public partial class TerrainGenerationSystem : SystemBase { }
public partial class MeshCreationSystem : SystemBase { }

// Components: PascalCase ending with "Data" or "Component"
public struct TerrainGenerationData : IComponentData { }
public struct MeshDataComponent : IComponentData { }

// Request Components: PascalCase ending with "Request"
public struct TerrainGenerationRequest : IComponentData { }

// Variables: camelCase
float terrainRadius = 10f;
int fragmentationDepth = 3;
uint noiseOctaves = 4;

// Constants: UPPER_CASE
const int MAX_TERRACE_COUNT = 32;
const float MIN_TERRAIN_RADIUS = 0.1f;
```

### ECS Architecture Patterns

```csharp
// ? Correct ECS System Structure
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class TerrainGenerationSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate<TerrainGenerationRequest>();
    }
    
    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        Entities
            .WithAll<TerrainGenerationRequest>()
            .ForEach((Entity entity, int entityInQueryIndex, 
                in TerrainGenerationData data) =>
            {
                // Process terrain generation phase by phase
                // Use entityInQueryIndex for parallel safety
            }).ScheduleParallel();
        
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### Memory Management Requirements

Always implement proper cleanup for blob assets:

```csharp
// ? Proper blob asset disposal pattern
public void CleanupBlobAssets(Entity entity)
{
    if (EntityManager.HasComponent<MeshDataComponent>(entity))
    {
        var meshData = EntityManager.GetComponentData<MeshDataComponent>(entity);
        
        if (meshData.VertexBlob.IsCreated)
        {
            meshData.VertexBlob.Dispose();
        }
        if (meshData.IndexBlob.IsCreated)
        {
            meshData.IndexBlob.Dispose();
        }
    }
}

// ? Use try-finally for guaranteed cleanup
BlobBuilder blobBuilder = default;
try
{
    blobBuilder = new BlobBuilder(Allocator.Temp);
    // Build blob asset
    var blobAsset = blobBuilder.CreateBlobAssetReference<YourBlobType>(Allocator.Persistent);
    return blobAsset;
}
finally
{
    if (blobBuilder.IsCreated)
        blobBuilder.Dispose();
}
```

### URP/BRP Compatibility Standards

Ensure render pipeline compatibility with proper fallback chains:

```csharp
// ? Shader priority system for URP/BRP compatibility
private Material CreateFallbackMaterial()
{
    // URP Priority: Try URP shaders first for Unity 6/URP compatibility
    var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                 Shader.Find("Universal Render Pipeline/Unlit") ??
                 Shader.Find("Standard") ??
                 Shader.Find("Legacy Shaders/Diffuse") ??
                 Shader.Find("Hidden/InternalErrorShader");
                 
    var material = new Material(shader);
    
    // Configure URP-specific properties for optimal visibility
    if (shader.name.Contains("Universal Render Pipeline"))
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.2f, 1.0f));
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.1f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.0f);
    }
    
    return material;
}
```

## Testing Requirements

### Mandatory Testing Standards

**All contributions must include comprehensive tests:**

- ? **Unit Tests** for individual components and data structures
- ? **System Tests** for ECS system functionality and behavior
- ? **Integration Tests** for multi-system workflows
- ? **Edge Case Tests** for error conditions and boundary values
- ? **Performance Tests** for memory usage and execution time
- ? **Cross-Platform Tests** when applicable

### Test Structure Template

```csharp
[TestFixture]
public class YourFeatureTests : ECSTestsFixture
{
    private YourSystem yourSystem;
    
    [SetUp]
    public override void Setup()
    {
        base.Setup();
        yourSystem = GetOrCreateSystem<YourSystem>();
    }
    
    [Test]
    public void YourFeature_SpecificBehavior_ExpectedResult()
    {
        // Arrange: Setup test conditions
        var entity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(entity, new YourComponent
        {
            SomeProperty = 42,
            AnotherProperty = true
        });
        EntityManager.AddComponent<YourRequestComponent>(entity);
        
        // Act: Execute the behavior
        yourSystem.Update();
        CompleteAllJobs();
        
        // Assert: Verify results
        Assert.IsTrue(EntityManager.HasComponent<ExpectedComponent>(entity));
        var result = EntityManager.GetComponentData<ExpectedComponent>(entity);
        Assert.AreEqual(expectedValue, result.Value);
        
        // Cleanup: Always dispose resources
        CleanupEntityMeshData(entity);
    }
    
    [TearDown]
    public override void TearDown()
    {
        // Ensure all blob assets are cleaned up
        var entities = EntityManager.GetAllEntities();
        foreach (var entity in entities)
        {
            CleanupEntityMeshData(entity);
        }
        entities.Dispose();
        
        base.TearDown();
    }
}
```

### Performance Testing Requirements

Include performance benchmarks for significant changes:

```csharp
[Test]
public void YourFeature_PerformanceBaseline_WithinThresholds()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var initialMemory = System.GC.GetTotalMemory(false);
    
    // Execute feature multiple times for reliable measurements
    for (int i = 0; i < 10; i++)
    {
        ExecuteYourFeature();
    }
    
    stopwatch.Stop();
    var finalMemory = System.GC.GetTotalMemory(true);
    var memoryUsed = finalMemory - initialMemory;
    
    // Assert performance thresholds
    Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Feature should complete 10 iterations within 1 second");
    Assert.Less(memoryUsed, 10 * 1024 * 1024, "Feature should use less than 10 MB memory");
}
```

### Test Coverage Requirements

- ? **100% pass rate** on all existing 154 tests
- ? **New functionality** must have comprehensive test coverage
- ? **Edge cases** and error conditions thoroughly tested
- ? **Memory leaks** prevented and validated
- ? **Cross-platform** compatibility verified when applicable
- ? **Performance regressions** detected and prevented

## Documentation Standards

### Code Documentation Requirements

```csharp
/// <summary>
/// @SystemType: ECS
/// @Domain: Terrain
/// @Role: Generation
/// 
/// Generates terrain meshes using Unity DOTS/ECS architecture.
/// Supports both planar and spherical terrain with terracing.
/// 
/// Processing Flow:
/// 1. Shape Generation - Creates base geometry
/// 2. Fragmentation - Subdivides mesh for detail
/// 3. Sculpting - Applies noise-based height variation
/// 4. Terracing - Creates stepped terrain layers
/// 5. Mesh Creation - Converts to Unity Mesh objects
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class TerrainGenerationSystem : SystemBase
{
    /// <summary>
    /// Processes terrain generation requests frame-by-frame.
    /// Only processes one phase per update to maintain performance.
    /// </summary>
    protected override void OnUpdate()
    {
        // Implementation with detailed comments
    }
    
    /// <summary>
    /// Generates correct planar terrain base shape.
    /// Fixes issues from original implementation's polygon generation.
    /// </summary>
    /// <param name="terrainData">Terrain parameters including sides, radius</param>
    /// <returns>MeshDataComponent with vertex and index blob assets</returns>
    private MeshDataComponent GenerateCorrectPlanarShape(TerrainGenerationData terrainData)
    {
        // Implementation
    }
}
```

### Documentation Update Requirements

Update relevant documentation for all changes:

- ? **README.md** - Main project documentation and usage examples
- ? **RUNTIME_SETUP_GUIDE.md** - Runtime configuration instructions
- ? **RUNTIME_DEBUG_CONSOLE_GUIDE.md** - Debug console commands and usage
- ? **TESTING_MANIFEST.md** - Test coverage tracking and execution
- ? **DEVELOPMENT_LOG.md** - Development history and architectural decisions

## Pull Request Process

### Before Submitting a PR

Ensure your contribution meets these requirements:

1. ? **All tests pass** (154/154 tests with 100% success rate)
2. ? **No compilation errors** or warnings in any build configuration
3. ? **Code follows** established naming conventions and patterns
4. ? **Documentation updated** for all user-facing changes
5. ? **Performance** benchmarks within acceptable thresholds
6. ? **Memory leaks** prevented and tested
7. ? **URP/BRP compatibility** maintained with proper fallbacks

### PR Description Template

```markdown
## Description
Brief description of changes and motivation for the contribution.

## Type of Change
- [ ] Bug fix (non-breaking change fixing an issue)
- [ ] New feature (non-breaking change adding functionality)
- [ ] Breaking change (fix or feature causing existing functionality to change)
- [ ] Performance improvement
- [ ] Documentation update
- [ ] Test coverage improvement

## Related Issues
Fixes #(issue number) or addresses #(issue number)

## Testing
- [ ] All existing tests pass (154/154)
- [ ] New tests added for new functionality
- [ ] Edge cases and error conditions covered
- [ ] Performance benchmarks within thresholds
- [ ] Memory leak tests pass
- [ ] Cross-platform compatibility verified

## Documentation
- [ ] Code comments updated with XML documentation
- [ ] README.md updated (if needed)
- [ ] Setup guides updated (if needed)
- [ ] API examples provided for new features

## Technical Checklist
- [ ] Code follows TTG naming conventions
- [ ] ECS patterns correctly implemented
- [ ] Blob assets properly disposed
- [ ] URP/BRP compatibility maintained
- [ ] Frame-by-frame processing maintained
- [ ] Error handling includes graceful degradation

## Screenshots/Evidence
Include screenshots, profiler data, or console logs if applicable.
```

### Review Process

1. **Automated Checks** - All tests and build verification run automatically
2. **Code Review** - Maintainers review for architecture, patterns, and quality
3. **Testing Verification** - Multi-platform testing and performance analysis
4. **Documentation Review** - Clarity, completeness, and accuracy verification
5. **Integration Testing** - Full system testing with other components

## Issue Reporting

### Bug Report Template

```markdown
**Bug Description**
A clear and concise description of the bug.

**Reproduction Steps**
1. Go to '...'
2. Create terrain with parameters '...'
3. Execute command '...'
4. See error

**Expected Behavior**
What you expected to happen.

**Actual Behavior**
What actually happened instead.

**Environment**
- Unity Version: [e.g. 2022.3.12f1]
- Platform: [e.g. Windows 10, Android API 30, iOS 15]
- Render Pipeline: [URP 14.0.9, BRP]
- TTG Version: [commit hash or release version]
- Hardware: [CPU, GPU, RAM for performance issues]

**Console Logs**
```
Paste relevant console logs here
```

**Additional Context**
- Performance profiler data (if performance issue)
- Screenshots or videos
- Related terrain parameters or configurations
```

### Feature Request Template

```markdown
**Feature Description**
Clear description of the proposed feature.

**Use Case**
Why is this feature needed? What problem does it solve?
What terrain generation scenarios would benefit?

**Proposed Implementation**
How do you envision this working technically?
- ECS system changes needed
- New components required
- Integration with existing pipeline

**Alternatives Considered**
Other approaches you've considered and why this is preferred.

**Performance Considerations**
Expected impact on generation time, memory usage, etc.

**Testing Strategy**
How would this feature be tested comprehensively?

**Additional Context**
Links to research, reference implementations, or examples.
```

### Performance Issue Template

```markdown
**Performance Issue**
Description of the performance problem and its impact.

**Profiling Data**
Include Unity Profiler screenshots showing:
- CPU usage breakdown
- Memory allocation patterns
- ECS system performance
- Frame time analysis

**Environment**
- Hardware specifications (CPU, GPU, RAM)
- Unity version and project settings
- Terrain complexity (vertex count, terrace count, etc.)
- Build configuration (Development/Master, Burst enabled, etc.)

**Current Performance**
Actual measured performance with specific metrics.

**Expected Performance**
What performance level should be achieved and why.

**Reproduction Configuration**
Specific terrain generation parameters that reproduce the issue.
```

## Community Guidelines

### Communication Standards

- ? **Be respectful** and professional in all interactions
- ? **Ask questions** if implementation details are unclear
- ? **Provide context** when reporting issues or requesting features
- ? **Share knowledge** about Unity ECS/DOTS patterns and terrain generation
- ? **Help others** learn complex concepts and debugging techniques
- ? **Offer constructive feedback** focused on technical improvement

### Code of Conduct

All contributors must follow our [Code of Conduct](CODE_OF_CONDUCT.md). Key principles:

- **Respectful collaboration** focused on technical excellence
- **Constructive feedback** for code and architecture improvement
- **Knowledge sharing** about Unity DOTS/ECS concepts and terrain generation
- **Professional conduct** in all project interactions
- **Inclusive environment** welcoming to developers of all experience levels

### Getting Help

**For Development Questions:**
- Create a GitHub issue with the "question" label
- Join our Discord server: https://discord.gg/6FeBEk68ra (ask for @Bellok)
- Email maintainers: jmeyer1980@gmail.com

**For Documentation Issues:**
- Create a GitHub issue with the "documentation" label
- Suggest specific improvements or corrections
- Contribute documentation improvements via pull request

**For Technical Support:**
- Use the runtime debug console for troubleshooting
- Check existing issues for similar problems
- Provide detailed reproduction steps and environment info

## Development Resources

### Essential Unity Documentation

- **Unity DOTS Documentation**: https://docs.unity3d.com/Packages/com.unity.entities@latest
- **Unity ECS Samples**: https://github.com/Unity-Technologies/EntityComponentSystemSamples
- **Unity Mathematics**: https://docs.unity3d.com/Packages/com.unity.mathematics@latest
- **Unity Burst Compiler**: https://docs.unity3d.com/Packages/com.unity.burst@latest
- **Unity Collections**: https://docs.unity3d.com/Packages/com.unity.collections@latest

### TTG-Specific Learning Resources

**ECS Architecture Patterns:**
- Entity-Component-System design principles
- Job system and Burst compilation optimization
- Blob assets and memory management strategies
- System update groups and execution order
- EntityCommandBuffer usage and structural changes

**Terrain Generation Concepts:**
- Mesh generation algorithms and polygon mathematics
- Noise functions and procedural generation techniques
- Geometry subdivision and fragmentation strategies
- Triangle slicing algorithms for terracing
- Material assignment and rendering pipeline integration

### Debug Console Commands for Development

Use these commands during development and testing:

```bash
# Terrain Generation and Analysis
terrain.count                    # Show current terrain entity count
terrain.materials               # Display material assignments and fallbacks
terrain.visibility              # Advanced visibility debugging analysis
terrain.spherical 15 4          # Generate test spherical terrain
terrain.planar 8 12 3           # Generate test planar terrain
terrain.regenerate              # Regenerate all existing terrain

# System Monitoring and Performance
system.memory                   # Monitor memory usage and allocation
system.entities                 # Track ECS entity counts across worlds
system.gc                      # Force garbage collection
debug.performance              # Show performance metrics and FPS
debug.cleanup                  # Force TTG cleanup systems execution

# Development and Debugging
debug.camera                   # Camera and frustum information
debug.renderers               # Scene renderer analysis
debug.gameobjects             # GameObject hierarchy inspection
debug.bounds                  # Mesh bounds checking and validation
help                          # Show all available commands
```

## Troubleshooting Common Issues

### Build and Compilation Issues

**Tests Failing to Compile:**
1. Ensure all Unity DOTS packages are updated to compatible versions
2. Check assembly references in .asmdef files
3. Verify test assembly references include Unity.Entities.Tests
4. Use only public APIs in test code, avoid internal/editor-only APIs

**Missing References:**
1. Update Unity packages: Entities, Mathematics, Collections, Transforms, Burst
2. Verify assembly definition references
3. Check for circular assembly dependencies
4. Ensure .NET Framework 4.7.1 compatibility

### ECS System Issues

**Entity Queries Not Working:**
1. Verify component types in query match actual components
2. Check RequireForUpdate conditions in OnCreate
3. Ensure entities have all required components
4. Use EntityQuery.CalculateEntityCount() to debug empty queries

**System Update Order Problems:**
1. Check UpdateInGroup attributes on systems
2. Verify system dependencies and update groups
3. Use [UpdateBefore] and [UpdateAfter] for specific ordering
4. Check World system list: World.Systems

### Memory and Performance Issues

**Memory Leaks in Tests:**
1. Always dispose blob assets in try-finally blocks
2. Use CleanupEntityMeshData() in test teardown
3. Call CompleteAllJobs() before disposal operations
4. Monitor memory usage with System.GC.GetTotalMemory()

**Performance Bottlenecks:**
1. Use Unity Profiler to identify expensive operations
2. Check Burst compilation is enabled for jobs
3. Verify parallel job scheduling where appropriate
4. Monitor blob asset allocation patterns

### Render Pipeline Issues

**Invisible Terrain in Builds:**
1. Verify URP/BRP shader compatibility
2. Check material property assignments
3. Test with both Development and Master builds
4. Use debug console visibility commands for analysis

**Material Assignment Problems:**
1. Test material registry fallback system
2. Verify shader availability in target platform
3. Check material instance ID resolution
4. Use runtime material debugging commands

### Testing and Development Workflow

**Test Isolation Issues:**
1. Ensure each test creates its own entities
2. Clean up all resources in [TearDown] methods
3. Use separate World instances when needed
4. Avoid dependencies between test methods

**Cross-Platform Compatibility:**
1. Test on target platforms early and frequently
2. Check platform-specific conditional compilation
3. Verify input system compatibility (Legacy vs New)
4. Test both editor and build environments

---

## Summary

Contributing to TTG Terrain Generation requires attention to:

- **ECS Architecture** - Following Unity DOTS patterns and performance best practices
- **Memory Management** - Proper blob asset lifecycle and cleanup procedures
- **Testing Standards** - Comprehensive coverage with all 154 tests passing
- **Documentation** - Clear, accurate, and up-to-date technical documentation
- **Performance** - Maintaining frame-by-frame processing and optimization
- **Compatibility** - Supporting URP/BRP render pipelines and multiple platforms

Your contributions help advance Unity DOTS/ECS terrain generation technology and support the broader Unity development community. Thank you for helping make TTG better!

For questions about this guide or the contribution process, please create an issue or contact the maintainers through the channels listed above.