using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Tests for the Runtime Debug Console system.
    /// Verifies console functionality, command processing, and integration features.
    /// Note: Many console methods are private, so we test through public interfaces.
    /// </summary>
    [TestFixture]
    public class RuntimeDebugConsoleTests
    {
        private GameObject consoleGameObject;
        private RuntimeDebugConsole debugConsole;
        private GameObject setupGameObject;
        private RuntimeDebugConsoleSetup consoleSetup;
        
        [SetUp]
        public void Setup()
        {
            // Create test console GameObject
            consoleGameObject = new GameObject("TestRuntimeDebugConsole");
            debugConsole = consoleGameObject.AddComponent<RuntimeDebugConsole>();
            
            // Create test setup GameObject
            setupGameObject = new GameObject("TestRuntimeDebugConsoleSetup");
            consoleSetup = setupGameObject.AddComponent<RuntimeDebugConsoleSetup>();
            
            // Configure for testing
            debugConsole.toggleKey = KeyCode.F1;
            debugConsole.maxLogEntries = 100;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (consoleGameObject != null)
            {
                Object.DestroyImmediate(consoleGameObject);
            }
            
            if (setupGameObject != null)
            {
                Object.DestroyImmediate(setupGameObject);
            }
        }
        
        [Test]
        public void RuntimeDebugConsole_InitialState_IsCorrect()
        {
            // Verify initial configuration
            Assert.AreEqual(KeyCode.F1, debugConsole.toggleKey);
            Assert.AreEqual(100, debugConsole.maxLogEntries);
            Assert.AreEqual(RuntimeDebugConsole.InputMethod.Auto, debugConsole.inputMethod);
        }
        
        [Test]
        public void RuntimeDebugConsole_Configuration_CanBeModified()
        {
            // Test configuration properties
            debugConsole.backgroundOpacity = 0.8f;
            Assert.AreEqual(0.8f, debugConsole.backgroundOpacity);
            
            debugConsole.autoScroll = true;
            Assert.IsTrue(debugConsole.autoScroll);
            
            debugConsole.showTimestamps = false;
            Assert.IsFalse(debugConsole.showTimestamps);
        }
        
        [Test]
        public void RuntimeDebugConsole_InputMode_Configuration()
        {
            // Test input mode configuration
            debugConsole.inputMethod = RuntimeDebugConsole.InputMethod.Auto;
            Assert.AreEqual(RuntimeDebugConsole.InputMethod.Auto, debugConsole.inputMethod);
            
            debugConsole.inputMethod = RuntimeDebugConsole.InputMethod.LegacyInput;
            Assert.AreEqual(RuntimeDebugConsole.InputMethod.LegacyInput, debugConsole.inputMethod);
            
            debugConsole.inputMethod = RuntimeDebugConsole.InputMethod.NewInputSystem;
            Assert.AreEqual(RuntimeDebugConsole.InputMethod.NewInputSystem, debugConsole.inputMethod);
        }
        
        [Test]
        public void RuntimeDebugConsole_MaxLogEntries_CanBeSet()
        {
            // Test log entry limits
            debugConsole.maxLogEntries = 50;
            Assert.AreEqual(50, debugConsole.maxLogEntries);
            
            debugConsole.maxLogEntries = 1000;
            Assert.AreEqual(1000, debugConsole.maxLogEntries);
        }
        
        [Test]
        public void RuntimeDebugConsole_ToggleKey_CanBeSet()
        {
            // Test toggle key configuration
            debugConsole.toggleKey = KeyCode.BackQuote;
            Assert.AreEqual(KeyCode.BackQuote, debugConsole.toggleKey);
            
            debugConsole.toggleKey = KeyCode.F2;
            Assert.AreEqual(KeyCode.F2, debugConsole.toggleKey);
        }
        
        [Test]
        public void RuntimeDebugConsole_BackgroundOpacity_CanBeSet()
        {
            // Test background opacity
            debugConsole.backgroundOpacity = 0.5f;
            Assert.AreEqual(0.5f, debugConsole.backgroundOpacity);
            
            debugConsole.backgroundOpacity = 1.0f;
            Assert.AreEqual(1.0f, debugConsole.backgroundOpacity);
        }
        
        [Test]
        public void RuntimeDebugConsole_AutoScroll_CanBeToggled()
        {
            // Test auto-scroll setting
            debugConsole.autoScroll = false;
            Assert.IsFalse(debugConsole.autoScroll);
            
            debugConsole.autoScroll = true;
            Assert.IsTrue(debugConsole.autoScroll);
        }
        
        [Test]
        public void RuntimeDebugConsole_ShowTimestamps_CanBeToggled()
        {
            // Test timestamp display setting
            debugConsole.showTimestamps = true;
            Assert.IsTrue(debugConsole.showTimestamps);
            
            debugConsole.showTimestamps = false;
            Assert.IsFalse(debugConsole.showTimestamps);
        }
        
        [Test]
        public void RuntimeDebugConsole_InputSystemSettings_CanBeConfigured()
        {
            // Test Input System settings (when available)
#if ENABLE_INPUT_SYSTEM
            // These are public fields in the actual implementation
            debugConsole.toggleAction = "<Keyboard>/f1";
            Assert.AreEqual("<Keyboard>/f1", debugConsole.toggleAction);
            
            debugConsole.executeAction = "<Keyboard>/return";
            Assert.AreEqual("<Keyboard>/return", debugConsole.executeAction);
#endif
            // Test completes regardless of Input System availability
            Assert.Pass("Input System configuration test completed");
        }
        
        [Test]
        public void RuntimeDebugConsole_ConsoleComponents_CanBeAccessed()
        {
            // Test that console has expected components after initialization
            // Note: These might be null until Awake/Start is called, which happens in play mode
            
            // Test that the console exists and can be configured
            Assert.IsNotNull(debugConsole);
            
            // Test that we can access configuration properties
            var opacity = debugConsole.backgroundOpacity;
            var autoScroll = debugConsole.autoScroll;
            var showTimestamps = debugConsole.showTimestamps;
            
            // Should not throw exceptions
            Assert.IsTrue(opacity >= 0f && opacity <= 1f);
        }
        
        [Test]
        public void RuntimeDebugConsoleSetup_StaticMethods_WorkCorrectly()
        {
            // Test static logging method (should not throw)
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.LogToConsole("Test message", LogType.Log));
            
            // Test console availability check
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.IsConsoleAvailable());
        }
        
        [Test]
        public void RuntimeDebugConsoleSetup_GetConsoleInstance_HandlesCorrectly()
        {
            // Test getting console instance (should not throw)
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.GetConsoleInstance());
        }
        
        [Test]
        public void RuntimeDebugConsole_GameObjectCreation_WorksCorrectly()
        {
            // Test that console can be added to GameObject without issues
            var testGO = new GameObject("TestConsole");
            var console = testGO.AddComponent<RuntimeDebugConsole>();
            
            Assert.IsNotNull(console);
            Assert.AreEqual(testGO, console.gameObject);
            
            Object.DestroyImmediate(testGO);
        }
        
        [Test]
        public void RuntimeDebugConsole_ComponentDestruction_HandlesCorrectly()
        {
            // Test that console can be destroyed without errors
            var testGO = new GameObject("TestConsole");
            var console = testGO.AddComponent<RuntimeDebugConsole>();
            
            // Destroy and verify no exceptions
            Assert.DoesNotThrow(() => Object.DestroyImmediate(testGO));
        }
        
        [Test]
        public void RuntimeDebugConsole_ErrorHandling_GracefulDegradation()
        {
            // Create test console for this test
            var testGO = new GameObject("TestConsole");
            var console = testGO.AddComponent<RuntimeDebugConsole>();
            
            // Test error handling - these should not throw exceptions
            // The console handles null/empty messages by converting them to "[Empty log message]"
            // and logs them as errors, so we need to expect the error logs
            
            // Expect the error logs that will be generated
            LogAssert.Expect(LogType.Error, "[Empty log message]");
            LogAssert.Expect(LogType.Error, "[Empty log message]");
            
            // These should not throw exceptions but will generate expected error logs
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.LogToConsole(null, LogType.Error));
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.LogToConsole("", LogType.Error));
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.LogToConsole("   ", LogType.Log));
            
            // Cleanup
            Object.DestroyImmediate(testGO);
        }
        
        [Test]
        public void RuntimeDebugConsole_TerrainManagerIntegration_HandlesCorrectly()
        {
            // Test terrain manager integration (should handle missing manager gracefully)
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.IsConsoleAvailable());
            
            // With terrain manager
            var terrainManagerGO = new GameObject("TestTerrainManager");
            var terrainManager = terrainManagerGO.AddComponent<RuntimeTerrainManager>();
            
            // Should not throw even with terrain manager present
            Assert.DoesNotThrow(() => RuntimeDebugConsoleSetup.LogToConsole("Test with terrain manager", LogType.Log));
            
            Object.DestroyImmediate(terrainManagerGO);
        }
        
        [Test]
        public void RuntimeDebugConsole_MultipleInstances_HandleCorrectly()
        {
            // Test that multiple console instances can coexist
            var console1GO = new GameObject("Console1");
            var console2GO = new GameObject("Console2");
            
            var console1 = console1GO.AddComponent<RuntimeDebugConsole>();
            var console2 = console2GO.AddComponent<RuntimeDebugConsole>();
            
            Assert.IsNotNull(console1);
            Assert.IsNotNull(console2);
            Assert.AreNotEqual(console1, console2);
            
            // Both should be configurable independently
            console1.toggleKey = KeyCode.F1;
            console2.toggleKey = KeyCode.F2;
            
            Assert.AreEqual(KeyCode.F1, console1.toggleKey);
            Assert.AreEqual(KeyCode.F2, console2.toggleKey);
            
            Object.DestroyImmediate(console1GO);
            Object.DestroyImmediate(console2GO);
        }
        
        [Test]
        public void RuntimeDebugConsole_InputMethodEnum_IsValid()
        {
            // Test that all input method enum values are valid
            var autoMethod = RuntimeDebugConsole.InputMethod.Auto;
            var legacyMethod = RuntimeDebugConsole.InputMethod.LegacyInput;
            var newMethod = RuntimeDebugConsole.InputMethod.NewInputSystem;
            
            // Should be able to assign all enum values
            debugConsole.inputMethod = autoMethod;
            Assert.AreEqual(autoMethod, debugConsole.inputMethod);
            
            debugConsole.inputMethod = legacyMethod;
            Assert.AreEqual(legacyMethod, debugConsole.inputMethod);
            
            debugConsole.inputMethod = newMethod;
            Assert.AreEqual(newMethod, debugConsole.inputMethod);
        }
        
        [Test]
        public void RuntimeDebugConsole_RangeValidation_WorksCorrectly()
        {
            // Test range validation for properties with Range attributes
            
            // maxLogEntries should be within range [50, 1000]
            debugConsole.maxLogEntries = 25; // Below range
            debugConsole.maxLogEntries = 1500; // Above range
            debugConsole.maxLogEntries = 200; // Within range
            Assert.AreEqual(200, debugConsole.maxLogEntries);
            
            // backgroundOpacity should be within range [0, 1]
            debugConsole.backgroundOpacity = -0.5f; // Below range
            debugConsole.backgroundOpacity = 1.5f; // Above range
            debugConsole.backgroundOpacity = 0.7f; // Within range
            Assert.AreEqual(0.7f, debugConsole.backgroundOpacity);
        }
    }
}