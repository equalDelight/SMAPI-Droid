using Android.App;
using Java.Lang.Ref;
using Newtonsoft.Json.Linq;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class ModInstaller
{
    // Directory where mods are stored
    public static string ModDir = Path.Combine(FileTool.ExternalFilesDir, "Mods");

    // Get the minimum game version required for a mod
    public static Version GetMinGameVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumGameVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }

    // Get the minimum SMAPI version required for a mod
    public static Version GetMinSMAPIVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumApiVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }

    // Assert if the mod is supported by checking game and SMAPI versions
    public static bool AssertModISupport(JObject manifest)
    {
        if (!SMAPIInstaller.IsInstalled)
        {
            ToastNotifyTool.Notify("Cannot check mod, please install SMAPI first!");
            return false;
        }

        // Check if game version is 1.6 or higher
        var minGameVersion = GetMinGameVersion(manifest);
        if (minGameVersion != null && minGameVersion < new Version(1, 6, 0))
        {
            ToastNotifyTool.Notify("Not supported for game version 1.6");
            return false;
        }

        // Check if SMAPI version is 4.0.0 or higher
        var minSMAPIVersion = GetMinSMAPIVersion(manifest);
        if (minSMAPIVersion != null && minSMAPIVersion < new Version(4, 0, 0))
        {
            ToastNotifyTool.Notify("Not supported for game version 1.6");
            return false;
        }

        // Check if it is a content pack
        bool isContentPack = manifest.ContainsKey("ContentPackFor");
        return true;
    }

    // Install a mod pack from a zip file
    public static void InstallModPackZip(string zipFilePath, ZipArchive zip)
    {
        // Extract the mod zip file
        ExtractModZipFile(zipFilePath, zip, ModTool.ModsDir);

        // Log the installed mods
        var entries = zip.Entries;
        var manifestEntries = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
        var logBuilder = new StringBuilder();
        var fileInfo = new FileInfo(zipFilePath);
        logBuilder.AppendLine("Mod zip: " + fileInfo.Name);
        logBuilder.AppendLine("");
        logBuilder.AppendLine("List of mods: " + manifestEntries.Length);
        for (int i = 0; i < manifestEntries.Length; i++)
        {
            var manifestEntry = manifestEntries[i];
            var modDir = manifestEntry.FullName.Replace($"/{ModTool.ManifiestFileName}", "");
            var dirInfo = new DirectoryInfo(modDir);
            logBuilder.AppendLine($"[{i + 1}]: {dirInfo.Name}");
        }

        // Show the installed mod pack
        DialogTool.Show("Installed Mod Pack", logBuilder.ToString());
    }

    // Handle the mod installation process
    public static async void OnClickInstallMod(Action OnInstalledCallback = null)
    {
        try
        {
            var pickFile = await FilePickerTool.PickZipFile();
            if (pickFile == null)
                return;

            using var zip = ZipFile.OpenRead(pickFile.FullPath);
            var entries = zip.Entries;
            var manifestEntries = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
            if (manifestEntries.Length == 0)
            {
                ToastNotifyTool.Notify("Manifest.json not found");
                return;
            }

            bool isModPack = manifestEntries.Length != 1;
            if (isModPack)
            {
                InstallModPackZip(pickFile.FullPath, zip);
                OnInstalledCallback?.Invoke();
                return;
            }

            // Extract and install the mod
            var manifestText = ReadManifest(manifestEntries[0]);
            var manifestJson = JObject.Parse(manifestText);
            string modName = manifestJson["Name"].ToString();

            ExtractModZipFile(pickFile.FileName, zip, Path.Combine(ModDir));
            zip.Dispose();

            // Log the installed mod details
            var modVersion = manifestJson["Version"].ToString();
            var author = manifestJson["Author"].ToString();
            var modLogBuilder = new StringBuilder();
            modLogBuilder.AppendLine($"Name: {modName}");
            modLogBuilder.AppendLine($"Version: {modVersion}");
            modLogBuilder.AppendLine($"Author: {author}");

            var minGameVersion = GetMinGameVersion(manifestJson);
            if (minGameVersion != null)
                modLogBuilder.AppendLine($"Minimum Game Version: " + minGameVersion);

            var minSMAPIVersion = GetMinSMAPIVersion(manifestJson);
            if (minSMAPIVersion != null)
                modLogBuilder.AppendLine($"Minimum SMAPI Version: " + minSMAPIVersion);

            // Show the installed mod details
            DialogTool.Show("Installed Mod", modLogBuilder.ToString());
            OnInstalledCallback?.Invoke();

            // Clear cache
            FileTool.ClearCache();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    // Extract a mod zip file to the specified output directory
    public static void ExtractModZipFile(string zipFilePath, ZipArchive zip, string outputDir)
    {
        // Fix bug if a file with the same name as the zip file (without extension) exists
        var fileNameNoExtens = new FileInfo(zipFilePath).Name.Replace(".zip", "");
        var checkFileExist = Path.Combine(outputDir, fileNameNoExtens);
        if (File.Exists(checkFileExist))
            File.Delete(checkFileExist);

        // Extract the zip file
        zip.ExtractToDirectory(outputDir, true);
    }

    // Read the manifest file from the zip entry
    public static string ReadManifest(ZipArchiveEntry entry)
    {
        using (StreamReader reader = new StreamReader(entry.Open()))
        {
            return reader.ReadToEnd();
        }
    }

    // Try to delete a mod and optionally clean up the parent folder
    internal static bool TryDeleteMod(string folderPath, bool cleanupParentFolder)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return false;

            Directory.Delete(folderPath, true);

            // Clean up the parent folder if needed
            if (cleanupParentFolder)
            {
                var parentDir = Directory.GetParent(folderPath).FullName;
                if (parentDir != ModDir)
                {
                    var dirs = Directory.GetDirectories(parentDir);
                    if (dirs.Length == 0)
                        Directory.Delete(parentDir, true);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
            return true;
        }
    }
}