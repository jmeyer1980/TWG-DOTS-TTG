using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

// Conditional compilation for Input System support
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// @SystemType: MonoBehaviour
    /// @Domain: Debug
    /// @Role: Console
    /// 
    /// Runtime debug console for TTG Terrain Generation system.
    /// Provides both output logging and command execution capabilities.
    /// Perfect for runtime builds where Unity's console isn't available.
    /// 
    /// Key Features:
    /// - Toggle visibility with configurable key (default: BackQuote)
    /// - Display Unity debug logs in runtime builds
    /// - Execute basic system commands
    /// - Real-time system status monitoring
    /// - Memory usage tracking
    /// - Entity inspection
    /// - Supports both Legacy Input System and new Input System
    /// </summary>
    public class RuntimeDebugConsole : MonoBehaviour
    {
        [Header("Console Settings")]
        [Tooltip("Key to toggle console visibility")]
        public KeyCode toggleKey = KeyCode.BackQuote;
        
        [Tooltip("Maximum number of log entries to keep")]
        [Range(50, 1000)]
        public int maxLogEntries = 200;
        
        [Tooltip("Console background opacity")]
        [Range(0f, 1f)]
        public float backgroundOpacity = 0.8f;
        
        [Tooltip("Auto-scroll to bottom when new logs arrive")]
        public bool autoScroll = true;
        
        [Tooltip("Show timestamps in log entries")]
        public bool showTimestamps = true;
        
        [Header("Input System Settings")]
        [Tooltip("Input method to use for console controls")]
        public InputMethod inputMethod = InputMethod.Auto;
        
#if ENABLE_INPUT_SYSTEM
        [Tooltip("Input action for toggle console (Input System)")]
        public string toggleAction = "<Keyboard>/backquote";
        
        [Tooltip("Input action for execute command (Input System)")]
        public string executeAction = "<Keyboard>/enter";
        
        [Tooltip("Input action for command history up (Input System)")]
        public string historyUpAction = "<Keyboard>/upArrow";
        
        [Tooltip("Input action for command history down (Input System)")]
        public string historyDownAction = "<Keyboard>/downArrow";
        
        [Tooltip("Input action for auto-complete (Input System)")]
        public string autoCompleteAction = "<Keyboard>/tab";
#endif
        
        [Header("UI References")]
        [SerializeField] private Canvas consoleCanvas;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Text logText;
        [SerializeField] private InputField commandInput;
        [SerializeField] private Button executeButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Image backgroundImage;
        
        // Console state
        private readonly List<LogEntry> logEntries = new();
        private readonly Dictionary<string, ConsoleCommand> commands = new();
        private bool isVisible = false;
        private World ecsWorld;
        
        // Command history
        private readonly List<string> commandHistory = new();
        private int historyIndex = -1;
        
        // Auto-complete
        private List<string> availableCommands = new();
        
        // Input System support
#if ENABLE_INPUT_SYSTEM
        private InputAction toggleInputAction;
        private InputAction executeInputAction;
        private InputAction historyUpInputAction;
        private InputAction historyDownInputAction;
        private InputAction autoCompleteInputAction;
#endif
        
        public enum InputMethod
        {
            Auto,           // Automatically detect best available
            LegacyInput,    // Force Legacy Input Manager
            NewInputSystem  // Force new Input System
        }
        
        public struct LogEntry
        {
            public string message;
            public LogType type;
            public DateTime timestamp;
            
            public LogEntry(string message, LogType type)
            {
                this.message = message;
                this.type = type;
                this.timestamp = DateTime.Now;
            }
        }
        
        public delegate string ConsoleCommand(string[] args);
        
        private InputMethod activeInputMethod;
        
        private void Awake()
        {
            // Determine which input method to use
            DetermineInputMethod();
            
            // Create UI if not assigned
            if (consoleCanvas == null)
            {
                CreateConsoleUI();
            }
            
            // Initialize console
            InitializeConsole();
        }
        
        private void DetermineInputMethod()
        {
            switch (inputMethod)
            {
                case InputMethod.Auto:
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                    activeInputMethod = InputMethod.NewInputSystem;
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                    // When both are available, prefer Input System
                    activeInputMethod = InputMethod.NewInputSystem;
#else
                    activeInputMethod = InputMethod.LegacyInput;
#endif
                    break;
                case InputMethod.LegacyInput:
#if !ENABLE_LEGACY_INPUT_MANAGER
                    Debug.LogWarning("RuntimeDebugConsole: Legacy Input Manager is not enabled! Falling back to Input System.");
                    activeInputMethod = InputMethod.NewInputSystem;
#else
                    activeInputMethod = InputMethod.LegacyInput;
#endif
                    break;
                case InputMethod.NewInputSystem:
#if !ENABLE_INPUT_SYSTEM
                    Debug.LogWarning("RuntimeDebugConsole: Input System package is not installed! Falling back to Legacy Input.");
                    activeInputMethod = InputMethod.LegacyInput;
#else
                    activeInputMethod = InputMethod.NewInputSystem;
#endif
                    break;
            }
            
            Debug.Log($"RuntimeDebugConsole: Using {activeInputMethod} for input handling.");
        }
        
        private void Start()
        {
            // Initialize input system
            InitializeInputSystem();
            
            // Get ECS world
            ecsWorld = World.DefaultGameObjectInjectionWorld;
            
            // Register built-in commands
            RegisterCommands();
            
            // Hide console initially
            SetConsoleVisibility(false);
            
            // Hook into Unity's log system
            Application.logMessageReceived += OnLogMessageReceived;
            
            LogToConsole($"TTG Runtime Debug Console initialized ({activeInputMethod}). Press ` to toggle.", LogType.Log);
            LogToConsole($"Type 'help' to see available commands. Total commands: {commands.Count}", LogType.Log);
        }
        
        private void InitializeInputSystem()
        {
#if ENABLE_INPUT_SYSTEM
            if (activeInputMethod == InputMethod.NewInputSystem)
            {
                try
                {
                    // Create input actions
                    toggleInputAction = new InputAction("ToggleConsole", InputActionType.Button, toggleAction);
                    executeInputAction = new InputAction("ExecuteCommand", InputActionType.Button, executeAction);
                    historyUpInputAction = new InputAction("HistoryUp", InputActionType.Button, historyUpAction);
                    historyDownInputAction = new InputAction("HistoryDown", InputActionType.Button, historyDownAction);
                    autoCompleteInputAction = new InputAction("AutoComplete", InputActionType.Button, autoCompleteAction);
                    
                    // Enable actions
                    toggleInputAction.Enable();
                    executeInputAction.Enable();
                    historyUpInputAction.Enable();
                    historyDownInputAction.Enable();
                    autoCompleteInputAction.Enable();
                    
                    // Subscribe to events
                    toggleInputAction.performed += OnTogglePerformed;
                    executeInputAction.performed += OnExecutePerformed;
                    historyUpInputAction.performed += OnHistoryUpPerformed;
                    historyDownInputAction.performed += OnHistoryDownPerformed;
                    autoCompleteInputAction.performed += OnAutoCompletePerformed;
                    
                    Debug.Log("RuntimeDebugConsole: Input System actions initialized successfully.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RuntimeDebugConsole: Failed to initialize Input System: {ex.Message}. Falling back to Legacy Input.");
                    activeInputMethod = InputMethod.LegacyInput;
                }
            }
#endif
        }
        
#if ENABLE_INPUT_SYSTEM
        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ToggleConsole();
            }
        }
        
        private void OnExecutePerformed(InputAction.CallbackContext context)
        {
            if (context.performed && isVisible && commandInput != null)
            {
                ExecuteCommand();
            }
        }
        
        private void OnHistoryUpPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && isVisible && commandInput != null)
            {
                NavigateHistory(-1);
            }
        }
        
        private void OnHistoryDownPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && isVisible && commandInput != null)
            {
                NavigateHistory(1);
            }
        }
        
        private void OnAutoCompletePerformed(InputAction.CallbackContext context)
        {
            if (context.performed && isVisible && commandInput != null)
            {
                AutoComplete();
            }
        }
#endif
        
        private void Update()
        {
            // Handle input based on active method
            if (activeInputMethod == InputMethod.LegacyInput)
            {
                HandleLegacyInput();
            }
            // Input System handles input via callbacks, so no Update needed for NewInputSystem
            
            // Update status display
            UpdateStatusDisplay();
        }
        
        private void HandleLegacyInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // Toggle console visibility - check for both configured key and backtick
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                ToggleConsole();
                Debug.Log($"Console toggle pressed. Current visibility: {isVisible}");
            }
            
            // Handle command input when console is visible
            if (isVisible && commandInput != null)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ExecuteCommand();
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    NavigateHistory(-1);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    NavigateHistory(1);
                }
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    AutoComplete();
                }
            }
#endif
        }
        
        private void CreateConsoleUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("Runtime Debug Console Canvas");
            consoleCanvas = canvasGO.AddComponent<Canvas>();
            consoleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            consoleCanvas.sortingOrder = 1000;
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create background panel
            var panelGO = new GameObject("Console Panel");
            panelGO.transform.SetParent(consoleCanvas.transform, false);
            
            var panel = panelGO.AddComponent<Image>();
            panel.color = new Color(0, 0, 0, backgroundOpacity);
            backgroundImage = panel;
            
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.3f);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Create scroll view for logs
            var scrollGO = new GameObject("Log Scroll View");
            scrollGO.transform.SetParent(panelGO.transform, false);
            
            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0.1f);
            scrollRectTransform.anchorMax = new Vector2(0.75f, 0.9f);
            scrollRectTransform.offsetMin = new Vector2(10, 10);
            scrollRectTransform.offsetMax = new Vector2(-10, -10);
            
            // Create log text
            var logGO = new GameObject("Log Text");
            logGO.transform.SetParent(scrollGO.transform, false);
            
            logText = logGO.AddComponent<Text>();
            logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logText.fontSize = 12;
            logText.color = Color.white;
            logText.alignment = TextAnchor.LowerLeft;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            
            var logRect = logGO.GetComponent<RectTransform>();
            logRect.anchorMin = Vector2.zero;
            logRect.anchorMax = Vector2.one;
            logRect.offsetMin = Vector2.zero;
            logRect.offsetMax = Vector2.zero;
            
            scrollRect.content = logRect;
            
            // Create status panel
            var statusGO = new GameObject("Status Panel");
            statusGO.transform.SetParent(panelGO.transform, false);
            
            statusText = statusGO.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 10;
            statusText.color = Color.yellow;
            statusText.alignment = TextAnchor.UpperLeft;
            
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.75f, 0.1f);
            statusRect.anchorMax = new Vector2(1f, 0.9f);
            statusRect.offsetMin = new Vector2(10, 10);
            statusRect.offsetMax = new Vector2(-10, -10);
            
            // Create input field
            var inputGO = new GameObject("Command Input");
            inputGO.transform.SetParent(panelGO.transform, false);
            
            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            commandInput = inputGO.AddComponent<InputField>();
            
            // Create the text component for the input field
            var inputTextGO = new GameObject("Input Text");
            inputTextGO.transform.SetParent(inputGO.transform, false);
            var inputText = inputTextGO.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.text = "";
            
            var inputTextRect = inputTextGO.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(5, 0);
            inputTextRect.offsetMax = new Vector2(-5, 0);
            
            // Create placeholder text
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(1f, 1f, 1f, 0.5f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.text = "Type command here...";
            
            var placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);
            
            // Configure the input field
            commandInput.textComponent = inputText;
            commandInput.placeholder = placeholderText;
            
            var inputRect = inputGO.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(0.75f, 0.1f);
            inputRect.offsetMin = new Vector2(10, 10);
            inputRect.offsetMax = new Vector2(-10, -10);
            
            // Create execute button
            var buttonGO = new GameObject("Execute Button");
            buttonGO.transform.SetParent(panelGO.transform, false);
            
            executeButton = buttonGO.AddComponent<Button>();
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.6f, 0.3f, 0.8f);
            
            var buttonText = new GameObject("Button Text").AddComponent<Text>();
            buttonText.transform.SetParent(buttonGO.transform, false);
            buttonText.text = "Execute";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            var buttonTextRect = buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.75f, 0);
            buttonRect.anchorMax = new Vector2(1f, 0.1f);
            buttonRect.offsetMin = new Vector2(10, 10);
            buttonRect.offsetMax = new Vector2(-10, -10);
            
            DontDestroyOnLoad(canvasGO);
        }
        
        private void InitializeConsole()
        {
            if (executeButton != null)
            {
                executeButton.onClick.AddListener(ExecuteCommand);
            }
            
            if (commandInput != null)
            {
                commandInput.onEndEdit.AddListener(OnCommandInputEndEdit);
            }
            
            if (backgroundImage != null)
            {
                var color = backgroundImage.color;
                color.a = backgroundOpacity;
                backgroundImage.color = color;
            }
        }
        
        private void RegisterCommands()
        {
            // General commands
            RegisterCommand("help", CmdHelp, "Show all available commands");
            RegisterCommand("clear", CmdClear, "Clear the console log");
            RegisterCommand("status", CmdStatus, "Show system status");
            RegisterCommand("quit", CmdQuit, "Quit the application");
            
            // Input system commands
            RegisterCommand("input.status", CmdInputStatus, "Show input system status");
            RegisterCommand("input.switch", CmdSwitchInput, "Switch input method (legacy/inputsystem)");
            
            // Terrain generation commands (via manager search)
            RegisterCommand("terrain.regenerate", CmdRegenerateTerrain, "Request terrain regeneration via manager");
            RegisterCommand("terrain.count", CmdTerrainCount, "Show terrain entity count");
            RegisterCommand("terrain.manager", CmdFindManager, "Find and show terrain manager status");
            
            // NEW: Advanced terrain debugging commands
            RegisterCommand("terrain.inspect", CmdInspectTerrain, "Detailed terrain inspection (mesh, materials, GameObjects)");
            RegisterCommand("terrain.meshes", CmdInspectMeshes, "Show detailed mesh information");
            RegisterCommand("terrain.materials", CmdInspectMaterials, "Show material assignments and validity");
            RegisterCommand("terrain.gameobjects", CmdInspectGameObjects, "Show GameObject hierarchy and components");
            RegisterCommand("terrain.visibility", CmdCheckVisibility, "Check visibility factors (bounds, culling, rendering)");
            RegisterCommand("terrain.camera", CmdCameraInfo, "Show camera position and view information");
            RegisterCommand("terrain.lights", CmdLightingInfo, "Show lighting setup information");
            
            // System commands
            RegisterCommand("system.gc", CmdForceGC, "Force garbage collection");
            RegisterCommand("system.memory", CmdMemoryInfo, "Show memory usage");
            RegisterCommand("system.entities", CmdEntityInfo, "Show ECS entity information");
            
            // Debug commands
            RegisterCommand("debug.performance", CmdPerformanceInfo, "Show performance information");
            RegisterCommand("debug.world", CmdWorldInfo, "Show ECS world information");
            
            // Build command list for auto-complete
            availableCommands = commands.Keys.ToList();
            availableCommands.Sort();
        }
        
        private void RegisterCommand(string name, ConsoleCommand command, string description)
        {
            commands[name] = command;
            // Store description in a way that can be retrieved for help
            commands[$"{name}.__desc"] = args => description;
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ExecuteCommand();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateHistory(1);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                AutoComplete();
            }
        }
        
        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0) return;
            
            historyIndex = Mathf.Clamp(historyIndex + direction, -1, commandHistory.Count - 1);
            
            if (historyIndex >= 0)
            {
                commandInput.text = commandHistory[historyIndex];
                commandInput.caretPosition = commandInput.text.Length;
            }
            else
            {
                commandInput.text = "";
            }
        }
        
        private void AutoComplete()
        {
            var input = commandInput.text.ToLower();
            if (string.IsNullOrEmpty(input)) return;
            
            var matches = availableCommands.Where(cmd => cmd.StartsWith(input)).ToList();
            
            if (matches.Count == 1)
            {
                commandInput.text = matches[0];
                commandInput.caretPosition = commandInput.text.Length;
            }
            else if (matches.Count > 1)
            {
                LogToConsole($"Possible completions: {string.Join(", ", matches)}", LogType.Log);
            }
        }
        
        private void OnCommandInputEndEdit(string command)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (activeInputMethod == InputMethod.LegacyInput)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ExecuteCommand();
                }
            }
#endif
        }
        
        private void ExecuteCommand()
        {
            var commandText = commandInput.text.Trim();
            if (string.IsNullOrEmpty(commandText)) return;
            
            // Add to history
            if (commandHistory.Count == 0 || commandHistory[^1] != commandText)
            {
                commandHistory.Add(commandText);
                if (commandHistory.Count > 50) // Limit history size
                {
                    commandHistory.RemoveAt(0);
                }
            }
            historyIndex = -1;
            
            // Log the command
            LogToConsole($"> {commandText}", LogType.Log);
            
            // Parse and execute
            var parts = commandText.Split(' ');
            var commandName = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();
            
            if (commands.ContainsKey(commandName))
            {
                try
                {
                    var result = commands[commandName](args);
                    if (!string.IsNullOrEmpty(result))
                    {
                        LogToConsole(result, LogType.Log);
                    }
                }
                catch (Exception ex)
                {
                    LogToConsole($"Command error: {ex.Message}", LogType.Error);
                }
            }
            else
            {
                LogToConsole($"Unknown command: {commandName}. Type 'help' for available commands.", LogType.Warning);
            }
            
            // Clear input
            commandInput.text = "";
            commandInput.ActivateInputField();
        }
        
        private void ToggleConsole()
        {
            Debug.Log($"ToggleConsole called. Current visibility: {isVisible}");
            SetConsoleVisibility(!isVisible);
        }
        
        private void SetConsoleVisibility(bool visible)
        {
            Debug.Log($"SetConsoleVisibility called with visible: {visible}");
            isVisible = visible;
            if (consoleCanvas != null)
            {
                consoleCanvas.gameObject.SetActive(visible);
                Debug.Log($"Console canvas active set to: {visible}");
            }
            
            if (visible && commandInput != null)
            {
                commandInput.ActivateInputField();
                commandInput.Select();
            }
        }
        
        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            LogToConsole(message, type);
        }
        
        private void LogToConsole(string message, LogType type)
        {
            var entry = new LogEntry(message, type);
            logEntries.Add(entry);
            
            // Trim old entries
            while (logEntries.Count > maxLogEntries)
            {
                logEntries.RemoveAt(0);
            }
            
            // Update display
            UpdateLogDisplay();
        }
        
        private void UpdateLogDisplay()
        {
            if (logText == null) return;
            
            var sb = new System.Text.StringBuilder();
            
            foreach (var entry in logEntries)
            {
                var color = GetLogColor(entry.type);
                var timestamp = showTimestamps ? $"[{entry.timestamp:HH:mm:ss}] " : "";
                
                sb.AppendLine($"<color={color}>{timestamp}{entry.message}</color>");
            }
            
            logText.text = sb.ToString();
            
            // Auto-scroll to bottom
            if (autoScroll && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        /// <summary>
        /// Update status display if console is visible and status text exists.
        /// </summary>
        private void UpdateStatusDisplay()
        {
            // UNT0008 FIX: Avoid null propagation with Unity objects
            if (!isVisible) return;
            if (statusText == null) return;
            
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== TTG STATUS ===");
            
            // Try to find RuntimeTerrainManager using reflection to avoid compilation issues
            var terrainManager = FindManagerComponent();
            if (terrainManager != null)
            {
                try
                {
                    var statusProperty = terrainManager.GetType().GetProperty("Status");
                    var entityCountProperty = terrainManager.GetType().GetProperty("TerrainEntityCount");
                    var readyProperty = terrainManager.GetType().GetProperty("IsTerrainSystemReady");
                    
                    if (statusProperty != null)
                        status.AppendLine($"Status: {statusProperty.GetValue(terrainManager)}");
                    if (entityCountProperty != null)
                        status.AppendLine($"Entities: {entityCountProperty.GetValue(terrainManager)}");
                    if (readyProperty != null)
                        status.AppendLine($"Ready: {readyProperty.GetValue(terrainManager)}");
                }
                catch
                {
                    status.AppendLine("Manager: Found but inaccessible");
                }
            }
            else
            {
                status.AppendLine("Manager: Not found");
            }
            
            if (ecsWorld != null && ecsWorld.IsCreated)
            {
                var entityCount = GetEntityCount();
                status.AppendLine($"ECS Entities: {entityCount}");
            }
            
            status.AppendLine();
            status.AppendLine("=== INPUT ===");
            status.AppendLine($"Method: {activeInputMethod}");
            status.AppendLine($"Toggle: {toggleKey} (~)");
            
            status.AppendLine();
            status.AppendLine("=== MEMORY ===");
            status.AppendLine($"Used: {GC.GetTotalMemory(false) / 1024 / 1024:F1} MB");
            status.AppendLine($"FPS: {1f / Time.unscaledDeltaTime:F0}");
            
            status.AppendLine();
            status.AppendLine("=== CONTROLS ===");
            status.AppendLine("History: ↑↓");
            status.AppendLine("Complete: Tab");
            status.AppendLine("Execute: Enter");
            
            statusText.text = status.ToString();
        }
        
        private string GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Error or LogType.Exception => "#ff4444",
                LogType.Warning => "#ffaa44",
                LogType.Assert => "#ff44ff",
                _ => "#ffffff",
            };
        }
        
        // Helper method to find RuntimeTerrainManager without direct type reference
        private MonoBehaviour FindManagerComponent()
        {
            try
            {
                var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var component in allComponents)
                {
                    if (component.GetType().Name == "RuntimeTerrainManager")
                    {
                        return component;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }
        
        // Helper method to get entity count safely
        private int GetEntityCount()
        {
            try
            {
                if (ecsWorld == null || !ecsWorld.IsCreated) return -1;
                
                var entityManager = ecsWorld.EntityManager;
                using var allEntitiesQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                });
                return allEntitiesQuery.CalculateEntityCount();
            }
            catch
            {
                return -1; // Return -1 if we can't get the count
            }
        }
        
        #region Command Implementations
        
        private string CmdHelp(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("Available Commands:");
            result.AppendLine("==================");
            
            var sortedCommands = commands.Keys.Where(k => !k.EndsWith(".__desc")).OrderBy(k => k);
            
            foreach (var cmd in sortedCommands)
            {
                var descKey = $"{cmd}.__desc";
                var description = commands.ContainsKey(descKey) ? commands[descKey](null) : "No description";
                result.AppendLine($"{cmd,-25} - {description}");
            }
            
            return result.ToString();
        }
        
        private string CmdClear(string[] args)
        {
            logEntries.Clear();
            UpdateLogDisplay();
            return "Console cleared.";
        }
        
        private string CmdStatus(string[] args)
        {
            var terrainManager = FindManagerComponent();
            if (terrainManager == null)
                return "RuntimeTerrainManager not found!";
            
            try
            {
                var statusMethod = terrainManager.GetType().GetMethod("GetSystemStatusInfo");
                if (statusMethod != null)
                {
                    return (string)statusMethod.Invoke(terrainManager, null);
                }
                else
                {
                    return "RuntimeTerrainManager found but GetSystemStatusInfo method not available.";
                }
            }
            catch (Exception ex)
            {
                return $"Error getting status: {ex.Message}";
            }
        }
        
        private string CmdQuit(string[] args)
        {
            LogToConsole("Quitting application...", LogType.Warning);
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
            
            return "Application quit requested.";
        }
        
        private string CmdRegenerateTerrain(string[] args)
        {
            var terrainManager = FindManagerComponent();
            if (terrainManager == null)
                return "RuntimeTerrainManager not found!";
            
            try
            {
                var method = terrainManager.GetType().GetMethod("CreateTerrainGenerationRequest");
                if (method != null)
                {
                    method.Invoke(terrainManager, null);
                    return "Terrain regeneration requested.";
                }
                else
                {
                    return "CreateTerrainGenerationRequest method not found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error requesting terrain regeneration: {ex.Message}";
            }
        }
        
        private string CmdTerrainCount(string[] args)
        {
            var terrainManager = FindManagerComponent();
            if (terrainManager == null)
                return "RuntimeTerrainManager not found!";
            
            try
            {
                var updateMethod = terrainManager.GetType().GetMethod("UpdateTerrainEntityCount");
                var countProperty = terrainManager.GetType().GetProperty("TerrainEntityCount");
                
                updateMethod?.Invoke(terrainManager, null);
                
                if (countProperty != null)
                {
                    var count = countProperty.GetValue(terrainManager);
                    return $"Terrain entity count: {count}";
                }
                else
                {
                    return "TerrainEntityCount property not found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error getting terrain count: {ex.Message}";
            }
        }
        
        private string CmdFindManager(string[] args)
        {
            var terrainManager = FindManagerComponent();
            if (terrainManager == null)
                return "RuntimeTerrainManager not found in scene!";
            
            return $"RuntimeTerrainManager found: {terrainManager.name} on GameObject {terrainManager.gameObject.name}";
        }
        
        private string CmdForceGC(string[] args)
        {
            var beforeMem = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var afterMem = GC.GetTotalMemory(false);
            
            var freed = (beforeMem - afterMem) / 1024f / 1024f;
            return $"Garbage collection completed. Freed {freed:F2} MB";
        }
        
        private string CmdMemoryInfo(string[] args)
        {
            var totalMem = GC.GetTotalMemory(false) / 1024f / 1024f;
            var result = new System.Text.StringBuilder();
            
            result.AppendLine($"Total Memory: {totalMem:F2} MB");
            result.AppendLine($"System Memory: {SystemInfo.systemMemorySize} MB");
            result.AppendLine($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            
            var entityCount = GetEntityCount();
            if (entityCount >= 0)
            {
                result.AppendLine($"ECS Entities: {entityCount}");
            }
            
            return result.ToString();
        }
        
        private string CmdEntityInfo(string[] args)
        {
            if (ecsWorld == null || !ecsWorld.IsCreated)
                return "ECS World not available!";
            
            try
            {
                var result = new System.Text.StringBuilder();
                
                var totalEntities = GetEntityCount();
                result.AppendLine($"Total Entities: {totalEntities}");
                result.AppendLine($"World: {ecsWorld.Name}");
                result.AppendLine($"World Created: {ecsWorld.IsCreated}");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Failed to get entity info: {ex.Message}";
            }
        }
        
        private string CmdPerformanceInfo(string[] args)
        {
            var result = new System.Text.StringBuilder();
            
            result.AppendLine($"FPS: {1f / Time.unscaledDeltaTime:F1}");
            result.AppendLine($"Frame Time: {Time.unscaledDeltaTime * 1000f:F1}ms");
            result.AppendLine($"Time Scale: {Time.timeScale:F2}");
            result.AppendLine($"Fixed Delta: {Time.fixedDeltaTime:F3}s");
            result.AppendLine($"Platform: {Application.platform}");
            result.AppendLine($"Unity Version: {Application.unityVersion}");
            
            return result.ToString();
        }
        
        private string CmdWorldInfo(string[] args)
        {
            if (ecsWorld == null || !ecsWorld.IsCreated)
                return "ECS World not available!";
            
            var result = new System.Text.StringBuilder();
            result.AppendLine($"World Name: {ecsWorld.Name}");
            result.AppendLine($"World Created: {ecsWorld.IsCreated}");
            result.AppendLine($"Entity Count: {GetEntityCount()}");
            
            return result.ToString();
        }
        
        private string CmdInputStatus(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== INPUT SYSTEM STATUS ===");
            result.AppendLine($"Current Method: {activeInputMethod}");
            result.AppendLine($"Configured Method: {inputMethod}");
            
#if ENABLE_INPUT_SYSTEM
            result.AppendLine($"Input System Available: Yes");
            if (activeInputMethod == InputMethod.NewInputSystem)
            {
                result.AppendLine($"Toggle Action: {toggleAction}");
                result.AppendLine($"Execute Action: {executeAction}");
                result.AppendLine($"History Up: {historyUpAction}");
                result.AppendLine($"History Down: {historyDownAction}");
                result.AppendLine($"Auto Complete: {autoCompleteAction}");
            }
#else
            result.AppendLine($"Input System Available: No");
#endif
            
#if ENABLE_LEGACY_INPUT_MANAGER
            result.AppendLine($"Legacy Input Available: Yes");
            if (activeInputMethod == InputMethod.LegacyInput)
            {
                result.AppendLine($"Toggle Key: {toggleKey}");
            }
#else
            result.AppendLine($"Legacy Input Available: No");
#endif
            
            return result.ToString();
        }
        
        private string CmdSwitchInput(string[] args)
        {
            if (args.Length == 0)
            {
                return "Usage: input.switch [legacy|inputsystem|auto]\nCurrent: " + activeInputMethod;
            }
            
            var targetMethod = args[0].ToLower();
            InputMethod newMethod;
            
            switch (targetMethod)
            {
                case "legacy":
                    newMethod = InputMethod.LegacyInput;
                    break;
                case "inputsystem":
                case "new":
                    newMethod = InputMethod.NewInputSystem;
                    break;
                case "auto":
                    newMethod = InputMethod.Auto;
                    break;
                default:
                    return $"Invalid input method: {targetMethod}. Use: legacy, inputsystem, or auto";
            }
            
            // Clean up current input system
#if ENABLE_INPUT_SYSTEM
            if (activeInputMethod == InputMethod.NewInputSystem)
            {
                toggleInputAction?.Disable();
                executeInputAction?.Disable();
                historyUpInputAction?.Disable();
                historyDownInputAction?.Disable();
                autoCompleteInputAction?.Disable();
            }
#endif
            
            // Switch to new method
            inputMethod = newMethod;
            DetermineInputMethod();
            
            if (activeInputMethod == InputMethod.NewInputSystem)
            {
                InitializeInputSystem();
            }
            
            return $"Switched input method to: {activeInputMethod}";
        }
        #endregion

        #region Advanced Terrain Debugging Commands

        private string CmdInspectTerrain(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== COMPREHENSIVE TERRAIN INSPECTION ===");
            
            // Find all terrain GameObjects
            var terrainObjects = FindAllTerrainGameObjects();
            result.AppendLine($"Found {terrainObjects.Count} terrain GameObjects:");
            
            if (terrainObjects.Count == 0)
            {
                result.AppendLine("❌ NO TERRAIN GAMEOBJECTS FOUND!");
                result.AppendLine("This is likely why terrain is not visible.");
                return result.ToString();
            }
            
            for (int i = 0; i < terrainObjects.Count; i++)
            {
                var go = terrainObjects[i];
                result.AppendLine($"\n--- Terrain GameObject {i + 1}: {go.name} ---");
                result.AppendLine($"Instance ID: {go.GetInstanceID()}");
                result.AppendLine($"Active: {go.activeInHierarchy} (Self: {go.activeSelf})");
                result.AppendLine($"Position: {go.transform.position}");
                result.AppendLine($"Scale: {go.transform.localScale}");
                result.AppendLine($"Rotation: {go.transform.rotation.eulerAngles}");
                
                // Check MeshFilter
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.mesh != null)
                {
                    var mesh = meshFilter.mesh;
                    result.AppendLine($"✅ Mesh: {mesh.name} ({mesh.vertexCount} verts, {mesh.triangles.Length/3} tris)");
                    result.AppendLine($"   Bounds: {mesh.bounds}");
                }
                else
                {
                    result.AppendLine("❌ No valid mesh found!");
                }
                
                // Check MeshRenderer
                if (go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    result.AppendLine($"✅ MeshRenderer: Enabled={meshRenderer.enabled}");
                    result.AppendLine($"   Materials: {meshRenderer.materials.Length}");
                    for (int m = 0; m < meshRenderer.materials.Length; m++)
                    {
                        var mat = meshRenderer.materials[m];
                        if (mat != null)
                        {
                            result.AppendLine($"     [{m}]: {mat.name} (Shader: {mat.shader.name})");
                        }
                        else
                        {
                            result.AppendLine($"     [{m}]: NULL MATERIAL ❌");
                        }
                    }
                }
                else
                {
                    result.AppendLine("❌ No MeshRenderer found!");
                }
                
                // Check if object is within camera view
                var camera = Camera.main;
                if (camera != null)
                {
                    var bounds = go.GetComponent<MeshRenderer>()?.bounds;
                    if (bounds.HasValue)
                    {
                        var visible = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), bounds.Value);
                        result.AppendLine($"Camera Visibility: {(visible ? "✅ IN VIEW" : "❌ OUT OF VIEW")}");
                        result.AppendLine($"Distance to Camera: {Vector3.Distance(camera.transform.position, go.transform.position):F2}");
                    }
                }
            }
            
            return result.ToString();
        }
        
        private string CmdInspectMeshes(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== MESH DETAILED INSPECTION ===");
            
            var terrainObjects = FindAllTerrainGameObjects();
            if (terrainObjects.Count == 0)
            {
                return "No terrain GameObjects found!";
            }
            
            foreach (var go in terrainObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter?.mesh == null)
                {
                    result.AppendLine($"❌ {go.name}: No mesh");
                    continue;
                }
                
                var mesh = meshFilter.mesh;
                result.AppendLine($"\n🔍 Mesh: {mesh.name} (GameObject: {go.name})");
                result.AppendLine($"   Vertices: {mesh.vertexCount}");
                result.AppendLine($"   Triangles: {mesh.triangles.Length / 3}");
                result.AppendLine($"   Submeshes: {mesh.subMeshCount}");
                result.AppendLine($"   Bounds: Center={mesh.bounds.center}, Size={mesh.bounds.size}");
                result.AppendLine($"   Bounds Volume: {mesh.bounds.size.x * mesh.bounds.size.y * mesh.bounds.size.z:F2}");
                
                // Check if mesh has degenerate triangles
                if (mesh.triangles.Length > 0)
                {
                    var vertices = mesh.vertices;
                    var triangles = mesh.triangles;
                    int degenerateCount = 0;
                    
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var v1 = vertices[triangles[i]];
                        var v2 = vertices[triangles[i + 1]];
                        var v3 = vertices[triangles[i + 2]];
                        
                        // Check if triangle is degenerate (zero area)
                        var cross = Vector3.Cross(v2 - v1, v3 - v1);
                        if (cross.magnitude < 0.0001f)
                        {
                            degenerateCount++;
                        }
                    }
                    
                    if (degenerateCount > 0)
                    {
                        result.AppendLine($"   ⚠️ Degenerate triangles: {degenerateCount}");
                    }
                    else
                    {
                        result.AppendLine($"   ✅ All triangles valid");
                    }
                }
                
                // Sample some vertices to check for reasonable values
                if (mesh.vertexCount > 0)
                {
                    var vertices = mesh.vertices;
                    var minVert = vertices[0];
                    var maxVert = vertices[0];
                    
                    foreach (var vert in vertices)
                    {
                        if (vert.x < minVert.x) minVert.x = vert.x;
                        if (vert.y < minVert.y) minVert.y = vert.y;
                        if (vert.z < minVert.z) minVert.z = vert.z;
                        if (vert.x > maxVert.x) maxVert.x = vert.x;
                        if (vert.y > maxVert.y) maxVert.y = vert.y;
                        if (vert.z > maxVert.z) maxVert.z = vert.z;
                    }
                    
                    result.AppendLine($"   Vertex Range: Min={minVert}, Max={maxVert}");
                    
                    // Check for extreme values that might indicate corruption
                    var maxDist = Mathf.Max(
                        Mathf.Abs(minVert.x), Mathf.Abs(minVert.y), Mathf.Abs(minVert.z),
                        Mathf.Abs(maxVert.x), Mathf.Abs(maxVert.y), Mathf.Abs(maxVert.z)
                    );
                    
                    if (maxDist > 10000f)
                    {
                        result.AppendLine($"   ⚠️ Extreme vertex values detected (max: {maxDist:F2})");
                    }
                }
            }
            
            return result.ToString();
        }
        
        private string CmdInspectMaterials(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== MATERIAL INSPECTION ===");
            
            var terrainObjects = FindAllTerrainGameObjects();
            if (terrainObjects.Count == 0)
            {
                return "No terrain GameObjects found!";
            }
            
            foreach (var go in terrainObjects)
            {
                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    result.AppendLine($"❌ {go.name}: No MeshRenderer");
                    continue;
                }
                
                result.AppendLine($"\n🎨 Materials for {go.name}:");
                result.AppendLine($"   Renderer Enabled: {meshRenderer.enabled}");
                result.AppendLine($"   Shadow Casting: {meshRenderer.shadowCastingMode}");
                result.AppendLine($"   Receive Shadows: {meshRenderer.receiveShadows}");
                result.AppendLine($"   Material Count: {meshRenderer.materials.Length}");
                
                for (int i = 0; i < meshRenderer.materials.Length; i++)
                {
                    var material = meshRenderer.materials[i];
                    if (material == null)
                    {
                        result.AppendLine($"     [{i}]: ❌ NULL MATERIAL");
                        continue;
                    }
                    
                    result.AppendLine($"     [{i}]: {material.name}");
                    result.AppendLine($"          Shader: {material.shader.name}");
                    
                    // Check shader validity
                    if (material.shader == null)
                    {
                        result.AppendLine($"          ❌ NULL SHADER");
                    }
                    else if (material.shader.name.Contains("Error") || material.shader.name.Contains("Hidden/InternalErrorShader"))
                    {
                        result.AppendLine($"          ❌ ERROR SHADER DETECTED");
                    }
                    else
                    {
                        result.AppendLine($"          ✅ Shader appears valid");
                    }
                    
                    // Check common material properties
                    if (material.HasProperty("_Color"))
                    {
                        result.AppendLine($"          Color: {material.color}");
                    }
                    if (material.HasProperty("_MainTex"))
                    {
                        var tex = material.mainTexture;
                        result.AppendLine($"          Texture: {(tex != null ? tex.name : "None")}");
                    }
                }
            }
            
            return result.ToString();
        }
        
        private string CmdInspectGameObjects(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== GAMEOBJECT HIERARCHY INSPECTION ===");
            
            var terrainObjects = FindAllTerrainGameObjects();
            if (terrainObjects.Count == 0)
            {
                return "No terrain GameObjects found!";
            }
            
            foreach (var go in terrainObjects)
            {
                result.AppendLine($"\n🎯 GameObject: {go.name}");
                result.AppendLine($"   Instance ID: {go.GetInstanceID()}");
                result.AppendLine($"   Active Self: {go.activeSelf}");
                result.AppendLine($"   Active in Hierarchy: {go.activeInHierarchy}");
                result.AppendLine($"   Layer: {go.layer} ({LayerMask.LayerToName(go.layer)})");
                result.AppendLine($"   Tag: {go.tag}");
                
                // Transform information
                var t = go.transform;
                result.AppendLine($"   Transform:");
                result.AppendLine($"     Position: {t.position}");
                result.AppendLine($"     Rotation: {t.rotation.eulerAngles}");
                result.AppendLine($"     Scale: {t.localScale}");
                result.AppendLine($"     Parent: {(t.parent != null ? t.parent.name : "None")}");
                result.AppendLine($"     Children: {t.childCount}");
                
                // Component list
                var components = go.GetComponents<Component>();
                result.AppendLine($"   Components ({components.Length}):");
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        var enabled = "";
                        if (comp is Behaviour behaviour)
                        {
                            enabled = behaviour.enabled ? " ✅" : " ❌";
                        }
                        result.AppendLine($"     - {comp.GetType().Name}{enabled}");
                    }
                    else
                    {
                        result.AppendLine($"     - ❌ NULL COMPONENT");
                    }
                }
            }
            
            return result.ToString();
        }
        
        private string CmdCheckVisibility(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== VISIBILITY FACTORS ANALYSIS ===");
            
            var camera = FindFirstObjectByType<Camera>();
            if (camera == null && FindObjectsByType<Camera>(FindObjectsSortMode.None).Length > 0)
            {
                camera = FindObjectsByType<Camera>(FindObjectsSortMode.None)[0];
            }
            if (camera == null)
            {
                result.AppendLine("❌ No camera found in scene!");
                return result.ToString();
            }
            
            result.AppendLine($"📷 Using Camera: {camera.name}");
            result.AppendLine($"   Position: {camera.transform.position}");
            result.AppendLine($"   Rotation: {camera.transform.rotation.eulerAngles}");
            result.AppendLine($"   FOV: {camera.fieldOfView}°");
            result.AppendLine($"   Near Clip: {camera.nearClipPlane}");
            result.AppendLine($"   Far Clip: {camera.farClipPlane}");
            result.AppendLine($"   Culling Mask: {Convert.ToString(camera.cullingMask, 2).PadLeft(32, '0')}");
            
            var terrainObjects = FindAllTerrainGameObjects();
            if (terrainObjects.Count == 0)
            {
                result.AppendLine("\n❌ No terrain GameObjects to check visibility for!");
                return result.ToString();
            }
            
            result.AppendLine($"\n🔍 Checking {terrainObjects.Count} terrain objects:");
            
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            
            foreach (var go in terrainObjects)
            {
                result.AppendLine($"\n--- {go.name} ---");
                
                // Check if object is active
                if (!go.activeInHierarchy)
                {
                    result.AppendLine("❌ GameObject is inactive");
                    continue;
                }
                
                // Check layer culling
                var layerMask = 1 << go.layer;
                if ((camera.cullingMask & layerMask) == 0)
                {
                    result.AppendLine($"❌ Layer {go.layer} is culled by camera");
                    continue;
                }
                
                // UNT0008 FIX: Avoid null propagation with Unity objects
                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    result.AppendLine("❌ No MeshRenderer component");
                    continue;
                }
                
                if (!meshRenderer.enabled)
                {
                    result.AppendLine("❌ MeshRenderer is disabled");
                    continue;
                }
                
                // Check bounds
                var bounds = meshRenderer.bounds;
                result.AppendLine($"📦 Bounds: {bounds}");
                
                // Check frustum culling
                var inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                result.AppendLine($"🎯 In Camera Frustum: {(inFrustum ? "✅ YES" : "❌ NO")}");
                
                // Calculate distance to camera
                var distance = Vector3.Distance(camera.transform.position, bounds.center);
                result.AppendLine($"📏 Distance to Camera: {distance:F2}");
                
                // Check if within camera range
                if (distance < camera.nearClipPlane)
                {
                    result.AppendLine("⚠️ Object is behind near clip plane");
                }
                else if (distance > camera.farClipPlane)
                {
                    result.AppendLine("⚠️ Object is beyond far clip plane");
                }
                
                // Check if object has valid materials
                var materials = meshRenderer.materials;
                var validMaterials = materials.Count(m => m != null);
                result.AppendLine($"🎨 Valid Materials: {validMaterials}/{materials.Length}");
                
                // Calculate overall visibility score
                var visibilityFactors = new List<string>();
                if (go.activeInHierarchy) visibilityFactors.Add("Active");
                if ((camera.cullingMask & layerMask) != 0) visibilityFactors.Add("Layer OK");
                if (meshRenderer != null && meshRenderer.enabled) visibilityFactors.Add("Renderer OK");
                if (inFrustum) visibilityFactors.Add("In Frustum");
                if (distance >= camera.nearClipPlane && distance <= camera.farClipPlane) visibilityFactors.Add("In Range");
                if (validMaterials > 0) visibilityFactors.Add("Has Materials");
                
                result.AppendLine($"✅ Visibility Factors: {visibilityFactors.Count}/6 ({string.Join(", ", visibilityFactors)})");
                
                if (visibilityFactors.Count == 6)
                {
                    result.AppendLine("🟢 SHOULD BE VISIBLE");
                }
                else
                {
                    result.AppendLine("🔴 VISIBILITY ISSUES DETECTED");
                }
            }
            
            return result.ToString();
        }
        
        private string CmdCameraInfo(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== CAMERA INFORMATION ===");
            
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length == 0)
            {
                return "❌ No cameras found in scene!";
            }
            
            result.AppendLine($"Found {cameras.Length} camera(s):");
            
            foreach (var camera in cameras)
            {
                result.AppendLine($"\n📷 Camera: {camera.name}");
                result.AppendLine($"   Enabled: {camera.enabled}");
                result.AppendLine($"   GameObject Active: {camera.gameObject.activeInHierarchy}");
                result.AppendLine($"   Position: {camera.transform.position}");
                result.AppendLine($"   Rotation: {camera.transform.rotation.eulerAngles}");
                result.AppendLine($"   Forward: {camera.transform.forward}");
                result.AppendLine($"   Field of View: {camera.fieldOfView}°");
                result.AppendLine($"   Orthographic: {camera.orthographic}");
                if (camera.orthographic)
                {
                    result.AppendLine($"   Orthographic Size: {camera.orthographicSize}");
                }
                result.AppendLine($"   Near Clip: {camera.nearClipPlane}");
                result.AppendLine($"   Far Clip: {camera.farClipPlane}");
                result.AppendLine($"   Culling Mask: {camera.cullingMask} (Binary: {Convert.ToString(camera.cullingMask, 2).PadLeft(8, '0')})");
                result.AppendLine($"   Clear Flags: {camera.clearFlags}");
                result.AppendLine($"   Background Color: {camera.backgroundColor}");
                result.AppendLine($"   Depth: {camera.depth}");
                result.AppendLine($"   Render Path: {camera.renderingPath}");
                
                if (camera == Camera.main)
                {
                    result.AppendLine($"   🌟 This is the MAIN CAMERA");
                }
            }
            
            return result.ToString();
        }
        
        private string CmdLightingInfo(string[] args)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== LIGHTING INFORMATION ===");
            
            // Ambient lighting
            result.AppendLine($"🌅 Ambient Lighting:");
            result.AppendLine($"   Mode: {RenderSettings.ambientMode}");
            result.AppendLine($"   Color: {RenderSettings.ambientLight}");
            result.AppendLine($"   Intensity: {RenderSettings.ambientIntensity}");
            
            // Fog
            result.AppendLine($"\n🌫️ Fog:");
            result.AppendLine($"   Enabled: {RenderSettings.fog}");
            if (RenderSettings.fog)
            {
                result.AppendLine($"   Color: {RenderSettings.fogColor}");
                result.AppendLine($"   Mode: {RenderSettings.fogMode}");
                result.AppendLine($"   Density: {RenderSettings.fogDensity}");
                result.AppendLine($"   Start: {RenderSettings.fogStartDistance}");
                result.AppendLine($"   End: {RenderSettings.fogEndDistance}");
            }
            
            // Skybox
            result.AppendLine($"\n🌌 Skybox:");
            if (RenderSettings.skybox != null)
            {
                result.AppendLine($"   Material: {RenderSettings.skybox.name}");
                result.AppendLine($"   Shader: {RenderSettings.skybox.shader.name}");
            }
            else
            {
                result.AppendLine($"   ❌ No skybox material assigned");
            }
            
            // Light sources
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            result.AppendLine($"\n💡 Lights ({lights.Length}):");
            
            if (lights.Length == 0)
            {
                result.AppendLine("   ❌ No lights found in scene!");
            }
            else
            {
                foreach (var light in lights)
                {
                    result.AppendLine($"   🔦 {light.name}: {light.type}");
                    result.AppendLine($"      Enabled: {light.enabled} (GameObject: {light.gameObject.activeInHierarchy})");
                    result.AppendLine($"      Color: {light.color}");
                    result.AppendLine($"      Intensity: {light.intensity}");
                    result.AppendLine($"      Range: {light.range}");
                    if (light.type == LightType.Directional)
                    {
                        result.AppendLine($"      Direction: {light.transform.forward}");
                    }
                    else
                    {
                        result.AppendLine($"      Position: {light.transform.position}");
                    }
                }
            }
            
            return result.ToString();
        }
        
        private List<GameObject> FindAllTerrainGameObjects()
        {
            var terrainObjects = new List<GameObject>();
            
            // Method 1: Find by name pattern
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGameObjects)
            {
                if (go.name.ToLower().Contains("terrain") || go.name.ToLower().Contains("generated"))
                {
                    // Check if it has mesh components
                    if (go.GetComponent<MeshFilter>() != null && go.GetComponent<MeshRenderer>() != null)
                    {
                        terrainObjects.Add(go);
                    }
                }
            }
            
            // Method 2: Look for ECS entity references (if we can find any)
            if (ecsWorld != null && ecsWorld.IsCreated)
            {
                try
                {
                    var entityManager = ecsWorld.EntityManager;
                    var query = entityManager.CreateEntityQuery(typeof(MeshGameObjectReference));
                    var entities = query.ToEntityArray(Allocator.TempJob);
                    
                    foreach (var entity in entities)
                    {
                        if (entityManager.HasComponent<MeshGameObjectReference>(entity))
                        {
                            var meshRef = entityManager.GetComponentData<MeshGameObjectReference>(entity);
                            
#if UNITY_EDITOR
                            var go = UnityEditor.EditorUtility.InstanceIDToObject(meshRef.GameObjectInstanceID) as GameObject;
                            if (go != null && !terrainObjects.Contains(go))
                            {
                                terrainObjects.Add(go);
                            }
#endif
                        }
                    }
                    
                    entities.Dispose();
                    query.Dispose();
                }
                catch (Exception ex)
                {
                    // Ignore ECS lookup errors
                    Debug.LogWarning($"Could not query ECS for terrain objects: {ex.Message}");
                }
            }
            
            return terrainObjects;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            
#if ENABLE_INPUT_SYSTEM
            // Clean up Input System actions
            if (activeInputMethod == InputMethod.NewInputSystem)
            {
                toggleInputAction?.Disable();
                executeInputAction?.Disable();
                historyUpInputAction?.Disable();
                historyDownInputAction?.Disable();
                autoCompleteInputAction?.Disable();
                
                toggleInputAction?.Dispose();
                executeInputAction?.Dispose();
                historyUpInputAction?.Dispose();
                historyDownInputAction?.Dispose();
                autoCompleteInputAction?.Dispose();
            }
#endif
        }
    }
}