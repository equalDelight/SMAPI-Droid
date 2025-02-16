using Android.App;
using Android.Content;
using SMAPIGameLoader.Launcher;
using System;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

internal static class EntryGame
{
    public static void LaunchGameActivity(Activity launcherActivity)
    {
        // Run the game launch process asynchronously
        TaskTool.Run(launcherActivity, async () =>
        {
            await LaunchGameActivityInternalAsync(launcherActivity);
        });
    }

    static async Task LaunchGameActivityInternalAsync(Activity launcherActivity)
    {
        //ToastNotifyTool.Notify("Starting Game..");
        try
        {
            // Check if the game version is supported
            if (!StardewApkTool.IsGameVersionSupport)
            {
                ToastNotifyTool.Notify($"Your current game version ({StardewApkTool.CurrentGameVersion}) is not supported, please update the game to the latest version.");
                return;
            }

            // Check if SMAPI is installed
            if (!SMAPIInstaller.IsInstalled)
            {
                ToastNotifyTool.Notify("Please install SMAPI.");
                return;
            }

            // Setup game cloning
            GameCloner.Setup();

#if DEBUG
            // Notify and return if in Debug mode
            ToastNotifyTool.Notify("Error: Can't start the game in Debug Mode");
            return;
#endif
            // Start SMAPI activity
            StartSMAPIActivity(launcherActivity);
        }
        catch (Exception ex)
        {
            // Notify if an error occurs
            ToastNotifyTool.Notify($"Error: LaunchGameActivity: {ex}");
        }
    }

    static void StartSMAPIActivity(Activity launcherActivity)
    {
        // Create an intent to start the SMAPI activity
        var intent = new Intent(launcherActivity, typeof(SMAPIActivity));
        intent.AddFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
        launcherActivity.StartActivity(intent);
        launcherActivity.Finish();
    }
}