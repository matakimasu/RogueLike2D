// Assets/Editor/ExportAllScriptsSnapshot.cs
using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ExportAllScriptsSnapshot
{
    [MenuItem("Tools/Export All C# Scripts Snapshot")]
    public static void Export()
    {
        try
        {
            var projectPath = Directory.GetCurrentDirectory();
            var assetsPath = Path.Combine(projectPath, "Assets");
            var outDir = Path.Combine(assetsPath, "ScriptSnapshots");
            Directory.CreateDirectory(outDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outPath = Path.Combine(outDir, "ScriptSnapshot_" + timestamp + ".txt");

            var csFiles = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories)
                                   .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                                   .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("// Snapshot generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("// Project: " + new DirectoryInfo(projectPath).Name);
            sb.AppendLine(new string('=', 80));

            foreach (var file in csFiles)
            {
                string rel = file.Replace(projectPath + Path.DirectorySeparatorChar, string.Empty);
                sb.AppendLine();
                sb.AppendLine("=== " + rel + " ===");
                sb.AppendLine();

                string code = File.ReadAllText(file, new UTF8Encoding(true));
                sb.AppendLine(code);
                sb.AppendLine();
                sb.AppendLine(new string('-', 80));
            }

            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
            Debug.Log("[ExportAllScriptsSnapshot] Wrote " + csFiles.Length + " files -> " + outPath);
            EditorUtility.RevealInFinder(outPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("ExportAllScriptsSnapshot failed: " + ex);
        }
    }
}
