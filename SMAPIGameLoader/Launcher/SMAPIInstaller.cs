using Octokit;
using SMAPIGameLoader.Tool;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class SMAPIInstaller
{
    public const string GithubOwner = "equalDelight";
    public const string GithubRepoName = "SMAPI-Droid";

    // Get the build code of the installed SMAPI
    public static long GetBuildCode()
    {
        try
        {
            if (!IsInstalled)
            {
                return 0;
            }

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(stream);
            var SMAPIAndroidBuild = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.Mobile.SMAPIAndroidBuild");
            string buildString = SMAPIAndroidBuild.Fields.Single(p => p.Name == "BuildCode").Constant as string;
            return long.Parse(buildString);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    // Get the current version of the installed SMAPI
    public static Version GetCurrentVersion()
    {
        try
        {
            if (!IsInstalled)
            {
                return null;
            }

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(stream);
            var constantsType = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.EarlyConstants");
            var RawApiVersionForAndroidField = constantsType.Fields.Single(p => p.Name == "RawApiVersionForAndroid");
            string version = RawApiVersionForAndroidField.Constant as string;
            return new Version(version);
        }
        catch
        {
            return new Version(0, 0, 0, 0);
        }
    }

#if false
    // Method to install SMAPI online using GitHub releases
    public static async void OnClickInstallSMAPIOnline()
    {
        try
        {
            TaskTool.Run(ActivityTool.CurrentActivity, async () =>
            {
                try
                {
                    TaskTool.SetTitle("Install SMAPI Online");
                    var github = new GitHubClient(new ProductHeaderValue("SMPAI-Installer"));
                    TaskTool.NewLine("Fetching all releases...");
                    var releases = await github.Repository.Release.GetAll(GithubOwner, GithubRepoName);
                    TaskTool.NewLine("Found release count: " + releases.Count);
                    var latestRelease = releases.FirstOrDefault();
                    if (latestRelease == null)
                    {
                        ErrorDialogTool.Show(new Exception("Failed to install SMAPI, no release files found"));
                        return;
                    }

                    var smapiAssetFile = latestRelease.Assets.FirstOrDefault(
                        asset => asset.Name.StartsWith("SMAPI-") && asset.Name.EndsWith(".zip"));

                    if (smapiAssetFile != null)
                    {
                        TaskTool.NewLine("Found latest SMAPI file: " + smapiAssetFile.Name);
                        var smapiZipFilePath = Path.Combine(FileTool.ExternalFilesDir, smapiAssetFile.Name);
                        TaskTool.NewLine("Starting download and installation");
                        TaskTool.NewLine($"File size: {FileTool.ConvertBytesToMB(smapiAssetFile.Size):F2} MB");

                        using (var netClient = new HttpClient())
                        {
                            Console.WriteLine($"Retrieving {smapiAssetFile.Name}");
                            var fileData = await netClient.GetByteArrayAsync(smapiAssetFile.BrowserDownloadUrl);
                            File.WriteAllBytes(smapiZipFilePath, fileData);
                            Console.WriteLine("Saved zip file at: " + smapiZipFilePath + ", file size: " + fileData.Length);
                        }

                        InstallSMAPIFromZipFile(smapiZipFilePath);

                        TaskTool.NewLine("Successfully installed SMAPI: " + smapiAssetFile.Name);
                        DialogTool.Show("Successfully Installed SMAPI", $"Installed zip: {smapiAssetFile.Name}.\nYou can close this.");
                    }
                    else
                    {
                        TaskTool.NewLine("No SMAPI files found");
                    }

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    ErrorDialogTool.Show(ex);
                    Console.WriteLine("Error trying to install SMAPI Zip: " + ex);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
            Console.WriteLine("Error trying to install SMAPI Zip: " + ex);
        }
    }
#endif

    // Check if the picked file is a valid SMAPI zip for Android
    static bool IsSMAPIZipFromPickFile(FileResult pick)
    {
        var fileName = pick.FileName;

        if (!fileName.EndsWith(".zip"))
            return false;

        if (fileName.StartsWith("SMAPI-") || fileName.StartsWith("SMAPI_"))
        {
            // Check file size, should be less than 30MB for Android
            var fileInfo = new FileInfo(pick.FullPath);
            return FileTool.ConvertBytesToMB(fileInfo.Length) <= 30;
        }

        return false;
    }

    public static Action OnInstalledSMAPI;

    // Method to install SMAPI from a picked zip file
    public static async void OnClickInstallSMAPIZip(object sender, EventArgs eventArgs)
    {
        try
        {
            var pick = await FilePickerTool.PickZipFile("Please Pick File SMAPI-4.x.x.xxxx.zip Android");
            if (pick == null)
                return;

            // Assert that the selected file is a valid SMAPI zip for Android
            if (!IsSMAPIZipFromPickFile(pick))
            {
                DialogTool.Show("SMAPI Installer Error", "Please select file SMAPI-4.x.x.xxxx.zip for Android");
                return;
            }

            InstallSMAPIFromZipFile(pick.FullPath);

            DialogTool.Show("Successfully Installed SMAPI", "Installed SMAPI from zip file: " + pick.FileName);
            OnInstalledSMAPI?.Invoke();
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
            Console.WriteLine(ex);
        }
    }

    // Method to install SMAPI from the specified zip file path
    static void InstallSMAPIFromZipFile(string smapiZipFilePath)
    {
        using (var zip = ZipFile.OpenRead(smapiZipFilePath))
        {
            var stardewDir = GameAssemblyManager.AssembliesDirPath;
            foreach (var entry in zip.Entries)
            {
                // Remove the first directory name from the entry
                string entryDirName = Path.GetDirectoryName(entry.FullName);
                string[] directoryNames = entryDirName.Split(Path.DirectorySeparatorChar);
                var rootDirName = directoryNames[0];
                var newEntryFileName = entry.FullName.Remove(0, rootDirName.Length + 1);
                var destExtractFilePath = Path.Combine(stardewDir, newEntryFileName);
                ZipFileTool.Extract(entry, destExtractFilePath);
            }
        }

        FileTool.ClearCache();
    }

    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";
    public static string GetInstallFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName);
    public static bool IsInstalled => File.Exists(GetInstallFilePath);
}