using Android.App;
using Android.OS;
using Newtonsoft.Json;
using SMAPIGameLoader.Game.Rewriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static SMAPIGameLoader.Launcher.GameCloner;

namespace SMAPIGameLoader.Launcher;

// Class responsible for cloning the game and managing its state
internal static class GameCloner
{
    public const string ClonerStateFileName = "cloner_state.json";
    public static string ClonerStateFilePath => Path.Combine(FileTool.ExternalFilesDir, ClonerStateFileName);

    // Class representing the state of the game cloner
    public sealed class ClonerState
    {
        // Properties to track the last launcher build code and game version
        public int LastLauncherBuildCode { get; set; } = 0;
        public string LastGameVersionString { get; set; } = "0.0.0.0";

        // Save the current state to a file
        public void SaveToFile()
        {
            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ClonerStateFilePath, jsonString);
            Console.WriteLine("Saved ClonerState to file");
        }

        // Mark the game cloning process as done
        public void MarkCloneGameDone()
        {
            this.LastLauncherBuildCode = ApkTool.LauncherBuildCode;
            this.LastGameVersionString = StardewApkTool.CurrentGameVersion.ToString();
        }

        // Check if the game needs to be cloned again
        public bool needsToClone()
        {
            // Check if the launcher build code has changed
            Console.WriteLine("Last build code: " + LastLauncherBuildCode);
            if (ApkTool.LauncherBuildCode != LastLauncherBuildCode)
                return true;

            // Check if there is a new game update
            if (StardewApkTool.CurrentGameVersion != new Version(LastGameVersionString))
                return true;

            // No need to clone the game again
            return false;
        }
    }

    // Retrieve the current cloner state from the file
    public static ClonerState GetClonerState()
    {
        try
        {
            var jsonString = File.ReadAllText(ClonerStateFilePath);
            var clonerState = JsonConvert.DeserializeObject<ClonerState>(jsonString);
            return clonerState ?? new ClonerState();
        }
        catch (Exception ex)
        {
            // If an exception occurs, recreate the cloner state
            return new ClonerState();
        }
    }

    // Set up the game cloner and verify assets and assemblies
    public static void Setup()
    {
        ClonerState clonerState = GetClonerState();
        TaskTool.NewLine("Asserting game clone");

        bool isCloneGameNeeded = clonerState.needsToClone();

        if (isCloneGameNeeded)
        {
            TaskTool.NewLine("Starting game asset cloning");
            // Verify game assets
            GameAssetManager.VerifyAssets();
            TaskTool.NewLine("Assets verified");
            // Verify game assemblies
            GameAssemblyManager.VerifyAssemblies();
            TaskTool.NewLine("Assemblies verified");
            // Verify game libraries
            GameAssemblyManager.VerifyLibs();
            TaskTool.NewLine("Libraries verified");
        }

        // Load MonoGame.Framework.dll into reference
        GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameDLLFileName);

        // Rewrite StardewValley.dll if needed
        if (isCloneGameNeeded)
        {
            TaskTool.NewLine("Rewriting StardewValley.dll");
            using (var stardewAssemblyStream = File.Open(GameAssemblyManager.StardewValleyFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                TaskTool.NewLine("Starting StardewValley rewriter...");
                var stardewAssemblyDef = StardewGameRewriter.ReadAssembly(stardewAssemblyStream);
                StardewGameRewriter.Rewrite(stardewAssemblyDef);
                StardewAudioRewriter.Rewrite(stardewAssemblyDef);

                TaskTool.NewLine("Saving rewritten StardewValley.dll to file...");
                stardewAssemblyDef.Write();
                TaskTool.NewLine("Successfully rewrote StardewValley.dll");
            }

            // Do not load StardewValley assembly here; load it in SMAPIActivity
        }

        // Finalize the setup
        if (isCloneGameNeeded)
        {
            // Mark the cloning process as done and save the state
            clonerState.MarkCloneGameDone();
            clonerState.SaveToFile();
            TaskTool.NewLine("Saved cloner state to file");
        }

        TaskTool.NewLine("Game clone assertion complete");
    }
}
