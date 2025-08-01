using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

namespace TinyWalnutGames.TTG.TerrainGeneration.Editor
{
    /// <summary>
    /// Processes CSProject files to ensure they use the correct C# language version.
    /// </summary>
    /// <remarks>
    /// This script updates the LangVersion in .csproj files to 10.0 if it is set to 9.0. 
    /// </remarks>

public class CSProjectProcessor : AssetPostprocessor
{
    static void OnGeneratedCSProjectFiles()
    {
        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        foreach (var file in files)
        {
            string text = File.ReadAllText(file);
            if (!text.Contains("<LangVersion>10.0</LangVersion>"))
            {
                text = text.Replace("<LangVersion>9.0</LangVersion>", "<LangVersion>10.0</LangVersion>");
                File.WriteAllText(file, text);
                UnityEngine.Debug.Log($"Updated LangVersion in {Path.GetFileName(file)}");
            }
        }
    }
}
}