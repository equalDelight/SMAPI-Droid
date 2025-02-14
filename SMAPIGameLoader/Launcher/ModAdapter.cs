using Android.App;
using Android.Widget;
using System.Collections.Generic;

namespace SMAPIGameLoader.Launcher;

// Adapter class for displaying mod items in a ListView
public class ModAdapter : BaseAdapter<ModItemView>
{
    private readonly Activity context;
    private readonly List<ModItemView> items;

    // Constructor to initialize context and items list
    public ModAdapter(Activity context, List<ModItemView> items)
    {
        this.context = context;
        this.items = items;
    }

    // Indexer to get the item at a specific position
    public override ModItemView this[int position] => items[position];

    // Property to get the count of items in the list
    public override int Count => items.Count;

    // Method to get the item ID at a specific position
    public override long GetItemId(int position) => position;

    // Method to get the view for each item in the list
    public override Android.Views.View GetView(int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
    {
        var item = items[position];
        var view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.ModItemViewLayout, null);

        // Set the text for mod name, version, and folder path
        view.FindViewById<TextView>(Resource.Id.modName).Text = item.NameText;
        view.FindViewById<TextView>(Resource.Id.version).Text = item.VersionText;
        view.FindViewById<TextView>(Resource.Id.folderPath).Text = item.FolderPathText;

        return view;
    }

    // Method to refresh the mod list and notify the adapter
    public void RefreshMods()
    {
        NotifyDataSetChanged();
    }

    // Method to get the mod item on click event
    public ModItemView GetModOnClick(AdapterView.ItemClickEventArgs click)
    {
        if (items.Count == 0 || click.Position >= items.Count)
            return null;

        return items[click.Position];
    }
}