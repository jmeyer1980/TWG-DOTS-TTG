using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: MonoBehaviour
    /// @Domain: Debug
    /// @Role: Setup
    /// 
    /// Simple setup component for the Runtime Debug Console.
    /// Add this to any GameObject in your scene to automatically
    /// create and configure the debug console system.
    /// 
    /// This component will automatically:
    /// - Create the RuntimeDebugConsole if it doesn't exist
    /// - Configure default settings
    /// - Handle singleton behavior (only one console per scene)
    /// </summary>
    [AddComponentMenu("TTG/Runtime Debug Console Setup")]
    public class RuntimeDebugConsoleSetup : MonoBehaviour
    {
        [Header("Console Configuration")]
        [Tooltip("Key to toggle console visibility")]
        public KeyCode toggleKey = KeyCode.BackQuote;
        
        [Tooltip("Maximum number of log entries to keep in console")]
        [Range(50, 1000)]
        public int maxLogEntries = 200;
        
        [Tooltip("Console background opacity")]
        [Range(0f, 1f)]
        public float backgroundOpacity = 0.8f;
        
        [Tooltip("Auto-scroll console to bottom when new logs arrive")]
        public bool autoScroll = true;
        
        [Tooltip("Show timestamps in log entries")]
        public bool showTimestamps = true;
        
        [Header("Status")]
        [SerializeField, Tooltip("Current console instance (Read-only)")]
        private RuntimeDebugConsole consoleInstance;
        
        private static RuntimeDebugConsole globalConsoleInstance;
        
        private void Awake()
        {
            // Ensure only one console exists globally
            if (globalConsoleInstance != null)
            {
                Debug.LogWarning("RuntimeDebugConsole already exists! Destroying duplicate setup component.");
                Destroy(this);
                return;
            }
            
            CreateConsole();
        }
        
        private void CreateConsole()
        {
            // Create console GameObject
            var consoleGO = new GameObject("Runtime Debug Console");
            consoleInstance = consoleGO.AddComponent<RuntimeDebugConsole>();
            
            // Configure console with our settings
            consoleInstance.toggleKey = toggleKey;
            consoleInstance.maxLogEntries = maxLogEntries;
            consoleInstance.backgroundOpacity = backgroundOpacity;
            consoleInstance.autoScroll = autoScroll;
            consoleInstance.showTimestamps = showTimestamps;
            
            // Make console persistent across scenes
            DontDestroyOnLoad(consoleGO);
            
            // Set global reference
            globalConsoleInstance = consoleInstance;
            
            Debug.Log($"Runtime Debug Console initialized. Press {toggleKey} to toggle visibility.");
            Debug.Log("Console will automatically find and integrate with RuntimeTerrainManager if present.");
        }
        
        /// <summary>
        /// Get the global console instance.
        /// </summary>
        public static RuntimeDebugConsole GetConsoleInstance()
        {
            return globalConsoleInstance;
        }
        
        /// <summary>
        /// Check if console is available.
        /// </summary>
        public static bool IsConsoleAvailable()
        {
            return globalConsoleInstance != null;
        }
        
        /// <summary>
        /// Log a message to the console if available.
        /// </summary>
        public static void LogToConsole(string message, LogType logType = LogType.Log)
        {
            if (IsConsoleAvailable())
            {
                // SAFETY: Handle null or empty messages gracefully
                if (string.IsNullOrEmpty(message))
                {
                    message = "[Empty log message]";
                }
                
                // The console automatically captures Unity logs, so just use Debug.Log
                switch (logType)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        Debug.LogError(message);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(message);
                        break;
                    default:
                        Debug.Log(message);
                        break;
                }
            }
        }
        
        private void OnValidate()
        {
            // Update console settings if it exists
            if (consoleInstance != null)
            {
                consoleInstance.toggleKey = toggleKey;
                consoleInstance.maxLogEntries = maxLogEntries;
                consoleInstance.backgroundOpacity = backgroundOpacity;
                consoleInstance.autoScroll = autoScroll;
                consoleInstance.showTimestamps = showTimestamps;
            }
        }
        
        private void OnDestroy()
        {
            // Clear global reference if this was the active setup
            if (globalConsoleInstance == consoleInstance)
            {
                globalConsoleInstance = null;
            }
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Create Console Now")]
        private void CreateConsoleManually()
        {
            if (consoleInstance == null)
            {
                CreateConsole();
            }
            else
            {
                Debug.LogWarning("Console already exists!");
            }
        }
        
        [ContextMenu("Test Console Logging")]
        private void TestConsoleLogging()
        {
            LogToConsole("Test log message", LogType.Log);
            LogToConsole("Test warning message", LogType.Warning);
            LogToConsole("Test error message", LogType.Error);
        }
        #endif
    }
}