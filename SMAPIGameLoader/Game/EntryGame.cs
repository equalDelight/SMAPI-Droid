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
        TaskTool.Run(launcherActivity, async () =>
        {
            await LaunchGameActivityInternalAsync(launcherActivity);
        });
    }

    static async Task LaunchGameActivityInternalAsync(Activity launcherActivity)
    {
        //ToastNotifyTool.Notify("Starting Game..");
        //check game it's can launch with version

        try
        {
            if (!StardewApkTool.IsGameVersionSupport)
            {
                ToastNotifyTool.Notify("Your current game version (" + StardewApkTool.CurrentGameVersion + ") is not supported, please update the game to the latest version.");
                return;
            }

            if (!SMAPIInstaller.IsInstalled)
            {
                ToastNotifyTool.Notify("Please install SMAPI.");
                return;
            }

            GameCloner.Setup();

#if DEBUG
            ToastNotifyTool.Notify("Error can't start the game on Debug Mode");
            return;
#endif
            StartSMAPIActivity(launcherActivity);
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error: LaunchGameActivity: " + ex.ToString());
        }
    }

    static void StartSMAPIActivity(Activity launcherActivity)
    {
        var intent = new Intent(launcherActivity, typeof(SMAPIActivity));
        intent.AddFlags(ActivityFlags.ClearTask);
        intent.AddFlags(ActivityFlags.NewTask);
        launcherActivity.StartActivity(intent);
        launcherActivity.Finish();
    }
}