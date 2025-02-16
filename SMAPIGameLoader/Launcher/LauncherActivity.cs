using System;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using SMAPIGameLoader.Tool;
using Xamarin.Essentials;
using AndroidX.AppCompat.App;
using System.Text;

namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "SMAPI Launcher",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorPortrait
)]
public class LauncherActivity : AppCompatActivity
{
    public static LauncherActivity Instance { get; private set; }

    private static bool IsDeviceSupport => IntPtr.Size == 8;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);

        SetContentView(ResourceConstant.Layout.LauncherLayout);

        Platform.Init(this, savedInstanceState);
        ActivityTool.Init(this);

        // Assert requirements
        AssertRequirement();

        // Setup layout and UI
        OnReadyToSetupLayoutPage();
        SetDarkMode();

        // Run utility scripts
        ProcessAdbExtras();
    }

    private void SetDarkMode()
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
    }

    /// <summary>
    /// Process ADB extras to handle specific actions
    /// </summary>
    private void ProcessAdbExtras()
    {
        if (AdbExtraTool.IsClickStartGame(this))
        {
            OnClickStartGame();
        }
    }

    private static bool AssetGameVerify()
    {
        try
        {
            if (!StardewApkTool.IsInstalled)
            {
                var currentPackage = StardewApkTool.CurrentPackageInfo;
                if (currentPackage != null)
                {
                    switch (currentPackage.PackageName)
                    {
                        case StardewApkTool.GamePlayStorePackageName:
                            ToastNotifyTool.Notify("Please download the game from Play Store");
                            break;
                        case StardewApkTool.GameGalaxyStorePackageName:
                            ToastNotifyTool.Notify("Please download the game from Galaxy Store");
                            break;
                    }
                }
                else
                {
                    ToastNotifyTool.Notify("Please download the game from Play Store or Galaxy Store");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error: " + ex);
            return false;
        }

        return true;
    }

    private void AssertRequirement()
    {
        // Check if the device is 32-bit, which is not supported
        if (!IsDeviceSupport)
        {
            ToastNotifyTool.Notify("Not supported on 32-bit devices");
            Finish();
            return;
        }

        // Assert game requirements
        if (!AssetGameVerify())
        {
            Finish();
            return;
        }
    }

    private void OnReadyToSetupLayoutPage()
    {
        // Setup and bind UI events
        try
        {
            FindViewById<Button>(ResourceConstant.Id.InstallSMAPIZip).Click += SMAPIInstaller.OnClickInstallSMAPIZip;
            FindViewById<Button>(ResourceConstant.Id.UploadLog).Click += LogParser.OnClickUploadLog;
            // Work In Progress
            /// FindViewById<Button>(ResourceConstant.Id.SaveImportFromSavesZip).Click += SaveManager.OnClickImportSaveZip;

            var startGameBtn = FindViewById<Button>(ResourceConstant.Id.StartGame);
            startGameBtn.Click += (sender, e) => OnClickStartGame();
            var modManagerBtn = FindViewById<Button>(ResourceConstant.Id.ModManagerBtn);
            modManagerBtn.Click += (sender, e) => ActivityTool.SwapActivity<ModManagerActivity>(this, false);

            SMAPIInstaller.OnInstalledSMAPI += NotifyInstalledSMAPIInfo;
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error: Failed to setup UI event bindings");
            ErrorDialogTool.Show(ex);
            return;
        }

        // Set launcher text info
        try
        {
            var launcherInfoLines = new StringBuilder();
            // Set app version
            launcherInfoLines.AppendLine("Launcher Version: " + AppInfo.VersionString);

            var buildDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(int.Parse(AppInfo.BuildString));
            var localDateTimeOffset = buildDateTimeOffset.ToLocalTime();
            var localDateTimeString = localDateTimeOffset.ToString("HH:mm:ss dd/MM/yyyy");
            launcherInfoLines.AppendLine($"Build: {localDateTimeString} (d/m/y)");

            // Set supported game version
            launcherInfoLines.AppendLine($"Supported Game Version: {StardewApkTool.GameVersionSupport} or later");
            launcherInfoLines.AppendLine("Your Game Version: " + StardewApkTool.CurrentGameVersion);
            /// launcherInfoLines.AppendLine("Discord: Stardew SMAPI Thailand");
            /// launcherInfoLines.AppendLine("Owner: NRTnarathip");

            FindViewById<TextView>(ResourceConstant.Id.launcherInfoTextView).Text = launcherInfoLines.ToString();
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error: Failed to setup app text info: " + ex);
            ErrorDialogTool.Show(ex);
        }

        // Initialize UI info
        NotifyInstalledSMAPIInfo();
    }

    private void NotifyInstalledSMAPIInfo()
    {
        var smapiInstallInfo = FindViewById<TextView>(ResourceConstant.Id.SMAPIInstallInfoTextView);
        if (!SMAPIInstaller.IsInstalled)
        {
            smapiInstallInfo.Text = "Please install SMAPI!!";
            return;
        }

        var lines = new StringBuilder();
        lines.AppendLine($"SMAPI Version: {SMAPIInstaller.GetCurrentVersion()}");
        lines.AppendLine($"SMAPI Build: {SMAPIInstaller.GetBuildCode()}");
        smapiInstallInfo.Text = lines.ToString();
    }

    private void OnClickStartGame()
    {
        Console.WriteLine("On click start game");
        EntryGame.LaunchGameActivity(this);
        Console.WriteLine("Done continuing UI runner");
    }
}