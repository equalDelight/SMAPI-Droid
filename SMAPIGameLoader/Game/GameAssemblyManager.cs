using Android.App;
using MonoGame.Framework.Utilities;
using SMAPIGameLoader.Game;
using SMAPIGameLoader.Launcher;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Xamarin.Android.AssemblyStore;

namespace SMAPIGameLoader;

internal class GameAssemblyManager
{
    public const string AssembliesDirName = "Stardew Assemblies";
    public static string AssembliesDirPath => Path.Combine(FileTool.ExternalFilesDir, AssembliesDirName);
    public const string StardewDllName = "StardewValley.dll";
    public const string MonoGameDLLFileName = "MonoGame.Framework.dll";
    public static string StardewValleyFilePath => Path.Combine(AssembliesDirPath, StardewDllName);

    public static void VerifyAssemblies()
    {
        Console.WriteLine("Verifying Assemblies");
        var assembliesOutputDirPath = AssembliesDirPath;
        Directory.CreateDirectory(assembliesOutputDirPath);

        // Clone Stardew Valley assemblies
        Console.WriteLine("Cloning Stardew Valley assemblies");
        var stardewStore = new AssemblyStoreExplorer(StardewApkTool.BaseApkPath, keepStoreInMemory: true);
        foreach (var asm in stardewStore.Assemblies)
        {
            asm.ExtractImage(assembliesOutputDirPath);
        }
        Console.WriteLine("Finished cloning Stardew Valley assemblies");

        // Clone SMAPI Game Loader assemblies
        Console.WriteLine("Cloning SMAPI Game Loader assemblies");
        var appInfo = Application.Context.ApplicationInfo;
        var smapiStore = new AssemblyStoreExplorer(appInfo.PublicSourceDir, keepStoreInMemory: true);
        foreach (var asm in smapiStore.Assemblies)
        {
            asm.ExtractImage(assembliesOutputDirPath);
        }
        Console.WriteLine("Finished cloning SMAPI Game Loader assemblies");
    }

    public static Assembly LoadAssembly(string dllFileName)
    {
        return Assembly.LoadFrom(Path.Combine(AssembliesDirPath, dllFileName));
    }

    static string LibDirPath => Path.Combine(FileTool.ExternalFilesDir, "lib");

    internal static void VerifyLibs()
    {
        Console.WriteLine("Setting up libraries");

        // Clean up the library directory
        Console.WriteLine("Cleaning up library directory");
        if (Directory.Exists(LibDirPath))
        {
            Directory.Delete(LibDirPath, true);
        }
        Console.WriteLine("Finished setting up libraries");
    }
}
