using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class ModTool
{
    // Directory name for mods
    const string ModsDirName = "Mods";

    // Property to get the full path to the mods directory
    public static string ModsDir { get; } = Path.Combine(FileTool.ExternalFilesDir, ModsDirName);

    // Manifest file name
    public static string ManifiestFileName = "manifest.json";

    // Method to find manifest files in the specified directory and its subdirectories
    public static void FindManifestFile(string rootDirPath, List<string> manifestFiles)
    {
        try
        {
            // Check if the root directory exists
            if (!Directory.Exists(rootDirPath))
                return;

            // Search for the manifest file in the current directory
            var manifestFilePath = Path.Combine(rootDirPath, ManifiestFileName);

            // If the current directory is not the mods directory, add the manifest file to the list if it exists
            if (rootDirPath != ModsDir)
            {
                if (File.Exists(manifestFilePath))
                {
                    manifestFiles.Add(manifestFilePath);
                    return;
                }
            }

            // Recursively search for manifest files in subdirectories
            var folders = Directory.GetDirectories(rootDirPath);
            foreach (var folderPath in folders)
            {
                FindManifestFile(folderPath, manifestFiles);
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur
            Console.WriteLine(ex);
        }
    }
}