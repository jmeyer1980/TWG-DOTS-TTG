using UnityEngine;

namespace TinyWalnutGames.TTG.TerrainGeneration
{
    /// <summary>
    /// Simple test script to verify the runtime debug console is working properly.
    /// Add this to any GameObject to generate test messages and verify console functionality.
    /// </summary>
    [AddComponentMenu("TTG/Runtime Debug Console Tester")]
    public class RuntimeDebugConsoleTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Generate test messages every X seconds")]
        [Range(1f, 10f)]
        public float testMessageInterval = 3f;
        
        [Tooltip("Test the console toggle functionality")]
        public bool testToggle = true;
        
        [Tooltip("Generate various types of log messages")]
        public bool generateTestLogs = true;
        
        private float nextTestTime = 0f;
        private int messageCount = 0;
        
        private void Start()
        {
            if (generateTestLogs)
            {
                Debug.Log("RuntimeDebugConsoleTester: Starting console tests...");
                Debug.Log("Console should be toggleable with the backtick/tilde key (~)");
                
                // Test different log types
                Debug.Log("This is a normal log message");
                Debug.LogWarning("This is a warning message");
                Debug.LogError("This is an error message");
            }
        }
        
        private void Update()
        {
            // Generate periodic test messages
            if (generateTestLogs && Time.time >= nextTestTime)
            {
                nextTestTime = Time.time + testMessageInterval;
                messageCount++;
                
                switch (messageCount % 4)
                {
                    case 0:
                        Debug.Log($"Test message #{messageCount} - Normal log");
                        break;
                    case 1:
                        Debug.LogWarning($"Test message #{messageCount} - Warning");
                        break;
                    case 2:
                        Debug.LogError($"Test message #{messageCount} - Error");
                        break;
                    case 3:
                        RuntimeDebugConsoleSetup.LogToConsole($"Test message #{messageCount} - Direct console log", LogType.Log);
                        break;
                }
            }
            
            // Test console toggle
            if (testToggle && Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("Manual toggle test - pressing T should log this message");
                RuntimeDebugConsoleSetup.LogToConsole("Console toggle test successful!", LogType.Log);
            }
        }
        
        [ContextMenu("Test Console Commands")]
        private void TestConsoleCommands()
        {
            Debug.Log("Testing console commands...");
            Debug.Log("Open console and try these commands:");
            Debug.Log("- help");
            Debug.Log("- status");
            Debug.Log("- system.memory");
            Debug.Log("- terrain.count");
            Debug.Log("- clear");
        }
        
        [ContextMenu("Force Console Visible")]
        private void ForceConsoleVisible()
        {
            var console = RuntimeDebugConsoleSetup.GetConsoleInstance();
            if (console != null)
            {
                // Use reflection to force visibility for debugging
                var setVisibilityMethod = console.GetType().GetMethod("SetConsoleVisibility", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setVisibilityMethod != null)
                {
                    setVisibilityMethod.Invoke(console, new object[] { true });
                    Debug.Log("Forced console to be visible");
                }
            }
            else
            {
                Debug.LogError("Console instance not found!");
            }
        }
        
        [ContextMenu("Check Console Status")]
        private void CheckConsoleStatus()
        {
            Debug.Log($"Console Available: {RuntimeDebugConsoleSetup.IsConsoleAvailable()}");
            
            var console = RuntimeDebugConsoleSetup.GetConsoleInstance();
            if (console != null)
            {
                Debug.Log($"Console GameObject: {console.gameObject.name}");
                Debug.Log($"Console Active: {console.gameObject.activeInHierarchy}");
                Debug.Log($"Toggle Key: {console.toggleKey}");
            }
            else
            {
                Debug.LogError("Console instance is null!");
            }
        }
    }
}