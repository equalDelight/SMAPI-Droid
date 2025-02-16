using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace SMAPIGameLoader.Launcher;

public class ModItemView
{
    public string NameText { get; private set; } = "Unknown";
    public string VersionText { get; private set; } = "Unknown";
    public string FolderPathText { get; private set; } = "Unknown";

    public readonly string modName = "unknown";
    public readonly string modVersion = "unknown";
    public readonly string modFolderPath = "unknown";

    public ModItemView(string manifestFilePath, int modListIndex)
    {
        try
        {
            // Read and parse the manifest file
            var manifestText = File.ReadAllText(manifestFilePath);
            var manifest = JObject.Parse(manifestText);

            modName = manifest["Name"].ToString();
            modVersion = manifest["Version"].ToString();

            NameText = $"[{modListIndex + 1}]: {modName}";
            VersionText = $"Version: {modVersion}";

            modFolderPath = Path.GetDirectoryName(manifestFilePath);
            var relativeModDir = modFolderPath.Substring(modFolderPath.IndexOf("/Mods") + 6);
            FolderPathText = $"Folder: {relativeModDir}";
        }
        catch (Exception ex)
        {
            // Handle exceptions and set folder path
            modFolderPath = Path.GetDirectoryName(manifestFilePath);
            FolderPathText = modFolderPath;
            ErrorDialogTool.Show(ex, "Error trying to parse mod folder path: " + modFolderPath);
        }

        // Ensure NameText and VersionText are set even if an exception occurs
        NameText = $"[{modListIndex + 1}]: {modName}";
        VersionText = $"Version: {modVersion}";
    }
}
