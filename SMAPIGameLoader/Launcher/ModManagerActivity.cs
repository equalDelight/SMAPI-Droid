using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using Newtonsoft.Json.Linq;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "Mod Manager",
    Theme = "@style/AppTheme"
)]
internal class ModManagerActivity : AppCompatActivity
{
    ModAdapter modAdapter;
    List<ModItemView> mods = new();

    protected override void OnCreate(Bundle savedInstanceState)
    {
        // Initialize base activity
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);
        SetContentView(Resource.Layout.ModManagerLayout);

        // Initialize SDK
        ActivityTool.Init(this); // debug

        // Set up the page
        SetupPage();
    }

    // Set up the page elements and event handlers
    void SetupPage()
    {
        // Bind the ListView and set its adapter
        var modsListView = FindViewById<ListView>(Resource.Id.modsListViews);
        modsListView.Adapter = modAdapter = new ModAdapter(this, mods);

        // Handle item clicks in the ListView
        modsListView.ItemClick += (sender, e) => OnClickModItemView(e);

        // Set up the install mod button click handler
        var installModBtn = FindViewById<Button>(Resource.Id.InstallModBtn);
        installModBtn.Click += async (sender, e) =>
        {
            ModInstaller.OnClickInstallMod(OnInstalledCallback: () =>
            {
                RefreshMods();
            });
        };

        // Set up the open folder button click handler
        FindViewById<Button>(Resource.Id.OpenFolderModsBtn).Click += OnClick_OpenFolderMods;

        // Refresh the mods list
        RefreshMods();
    }

    // Open the mods folder
    void OnClick_OpenFolderMods(object sender, EventArgs e)
    {
        FileTool.OpenAppFilesExternalFilesDir("Mods");
    }

    // Refresh the list of mods by finding and loading manifest files
    void RefreshMods()
    {
        // Clear the current list of mods
        mods.Clear();

        try
        {
            var manifestFiles = new List<string>();
            Console.WriteLine("Start Refresh Mods...");
            ModTool.FindManifestFile(ModTool.ModsDir, manifestFiles);

            // Add each found manifest to the mods list
            for (int i = 0; i < manifestFiles.Count; i++)
            {
                var mod = new ModItemView(manifestFiles[i], i);
                mods.Add(mod);
            }
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }

        // Refresh the adapter to update the ListView
        modAdapter.RefreshMods();
        var foundModsText = FindViewById<TextView>(Resource.Id.foundModsText);
        foundModsText.Text = "Found Mods: " + mods.Count;
    }

    // Handle click events on mod items in the ListView
    void OnClickModItemView(AdapterView.ItemClickEventArgs e)
    {
        var mod = modAdapter.GetModOnClick(e);
        var text = new StringBuilder();
        text.AppendLine($"Mod: {mod.NameText}");
        text.AppendLine($"{mod.VersionText}");
        text.AppendLine();
        text.AppendLine("Are you sure you want to delete this mod?");

        // Show a dialog to confirm deletion
        DialogTool.Show(
            "❌ Delete: " + mod.NameText,
            text.ToString(),
            buttonOKName: "Yes, Delete It!",
            onClickYes: () =>
            {
                OnClickDeleteMod(mod);
            }
        );
    }

    // Handle the deletion of a mod
    void OnClickDeleteMod(ModItemView mod)
    {
        Console.WriteLine("Trying to delete mod: " + mod.modName);
        if (ModInstaller.TryDeleteMod(mod.modFolderPath, true))
        {
            ToastNotifyTool.Notify("Successfully deleted mod: " + mod.modName);
            RefreshMods();
        }
    }
}