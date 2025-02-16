using HarmonyLib;
using StardewValley;
using System;

namespace SMAPIGameLoader;

internal class MainActivityPatcher
{
    // Prefix method to bypass CheckStorageMigration
    public static bool PrefixCheckStorageMigration(ref bool __result)
    {
        Console.WriteLine("Bypassing CheckStorageMigration");
        __result = false;
        return false;
    }

    // Apply the Harmony patch
    internal static void Apply()
    {
        var harmony = new Harmony("SMAPIGameLoader");
        var prefixMethod = AccessTools.Method(typeof(MainActivityPatcher), nameof(PrefixCheckStorageMigration));
        var targetMethod = AccessTools.Method(typeof(MainActivity), nameof(MainActivity.CheckStorageMigration));
        harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
        Console.WriteLine("MainActivityPatcher.Apply() completed");
    }
}
