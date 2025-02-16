using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using System;
using System.IO;
using System.IO.Compression;
using HarmonyLib;

namespace SMAPIGameLoader;

[HarmonyPatch]
static class GameAssetManager
{
    // Prefix method to intercept TitleContainer.OpenStream calls
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TitleContainer), nameof(TitleContainer.OpenStream))]
    static bool PrefixOpenStream(ref Stream __result, string name)
    {
        __result = FixOpenStream(name);
        return false;
    }

    public const string StardewAssetFolderName = "Stardew Assets";
    private static string _gameAssetDir = null;

    // Property to get the game assets directory
    static string GameAssetsDir
    {
        get
        {
            if (_gameAssetDir == null)
                _gameAssetDir = Path.Combine(FileTool.ExternalFilesDir, StardewAssetFolderName);
            return _gameAssetDir;
        }
    }

    public delegate Stream OnOpenStreamDelegate(string assetName);
    public static OnOpenStreamDelegate OnOpenStream;

    // Method to fix the OpenStream call
    static Stream FixOpenStream(string assetName)
    {
        try
        {
            // Example: Content\\BigCraftables
            assetName = FileTool.SafePath(assetName);

            // Load from other stream if hooked
            var hookOpenStream = OnOpenStream?.Invoke(assetName);
            if (hookOpenStream != null)
                return hookOpenStream;

            // Load vanilla asset
            string assetFullPath = Path.Combine(GameAssetsDir, assetName);
            return File.OpenRead(assetFullPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    // Method to verify and update game assets
    public static void VerifyAssets()
    {
        // Check and update game content
        var baseContentApk = StardewApkTool.ContentApkPath;
        using (FileStream apkFileStream = new FileStream(baseContentApk, FileMode.Open, FileAccess.Read))
        using (ZipArchive apkArchive = new ZipArchive(apkFileStream, ZipArchiveMode.Read))
        {
            var externalAssetsDir = Path.Combine(FileTool.ExternalFilesDir, StardewAssetFolderName);
            foreach (ZipArchiveEntry entry in apkArchive.Entries)
            {
                if (!entry.FullName.StartsWith("assets/Content"))
                    continue;

                var destFilePath = Path.Combine(externalAssetsDir, entry.FullName.Replace("assets/", ""));
                var destFolderFullPath = Path.GetDirectoryName(destFilePath);
                if (!Directory.Exists(destFolderFullPath))
                {
                    Directory.CreateDirectory(destFolderFullPath);
                }
                using var entryStream = entry.Open();
                using var destFileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.ReadWrite);
                entryStream.CopyTo(destFileStream);
            }
        }

        Console.WriteLine("Successfully verified and cloned assets");
    }
}
