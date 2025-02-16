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

internal static class SaveManager
{
    const string SavesDirName = "Saves";
    const string SaveGameInfo_FileName = "SaveGameInfo";
    const string EMERGENCY_SAVE_FileName = "EMERGENCY_SAVE";

    // Get the directory path for saves
    public static string GetSavesDir() => Path.Combine(FileTool.ExternalFilesDir, SavesDirName);

    // Handle the import save zip button click event
    public static void OnClickImportSaveZip(object sender, EventArgs args)
    {
        try
        {
            ImportSaveZipInternal();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    // Import save zip file internally
    static async void ImportSaveZipInternal()
    {
        var pick = await FilePickerTool.PickZipFile();
        if (pick is null)
            return;

        if (pick.FileName != "Saves.zip")
        {
            DialogTool.Show("Save Importer Error", "Please pick the file Saves.zip!!");
            return;
        }

        // Extract the zip file
        HashSet<string> savesImportHashset = new();

        using (var zip = ZipFile.OpenRead(pick.FullPath))
        {
            var savesDirPath = GetSavesDir();
            const string SkipEntrySavesName = SavesDirName + "/";
            var entries = zip.Entries.ToArray();
            foreach (var entry in entries)
            {
                // Skip entry name folder 'Saves/'
                if (entry.FullName == SkipEntrySavesName)
                    continue;

                if (FileTool.IsEntryDirectory(entry))
                    continue;

                var firstDirName = FileTool.GetFirstDirNameFromFilePath(entry.FullName);
                var currentEntryFilePath = entry.FullName;
                if (firstDirName != null && firstDirName == SavesDirName)
                    currentEntryFilePath = currentEntryFilePath.Remove(0, SkipEntrySavesName.Length);

                var extractFilePath = Path.Combine(savesDirPath, currentEntryFilePath);
                ZipFileTool.Extract(entry, extractFilePath);
                var fileInfo = new FileInfo(extractFilePath);
                if (fileInfo.Name == SaveGameInfo_FileName)
                {
                    var parentDir = fileInfo.Directory;
                    savesImportHashset.Add(parentDir.Name);
                }
            }

            // Remove emergency save file for safety
            RemoveEmergencySaveFile();
        }

        // Clean up cache after picking the file
        FileTool.ClearCache();

        // Build the success message
        StringBuilder sbSaveImported = new();
        sbSaveImported.AppendLine($"Successfully imported {savesImportHashset.Count} saves");

        int saveIndexCounter = 0;
        foreach (var saveDirName in savesImportHashset)
        {
            saveIndexCounter++;
            sbSaveImported.AppendLine($"[{saveIndexCounter}] {saveDirName}");
        }

        DialogTool.Show("Save Importer", sbSaveImported.ToString());
    }

    // Import save zip file
    public static bool ImportSaveZip(FileResult pick)
    {
        if (!pick.FileName.EndsWith(".zip"))
            return false;

        return true;
    }

    // Remove the emergency save file
    public static void RemoveEmergencySaveFile()
    {
        string filePath = Path.Combine(GetSavesDir(), EMERGENCY_SAVE_FileName);
        FileTool.Delete(filePath);
    }
}
