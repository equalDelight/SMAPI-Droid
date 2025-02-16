using HarmonyLib;
using StardewValley;
using System;

namespace SMAPIGameLoader;

[Harmony]
internal class Log
{
    // Log a message to the console
    public static void It(string message)
    {
        Console.WriteLine(message);
    }

    // Setup Harmony patching for the DefaultLogger
    public static void Setup()
    {
        var harmony = new Harmony("SMAPIGameLoader");
        var defaultLoggerType = typeof(MainActivity).Assembly.GetType("StardewValley.Logging.DefaultLogger");
        var logImplMethod = AccessTools.Method(defaultLoggerType, "LogImpl");
        harmony.Patch(logImplMethod, prefix: AccessTools.Method(typeof(Log), nameof(PrefixLogImpl)));
    }

    // Prefix method to intercept LogImpl calls
    static void PrefixLogImpl(string level, string message, Exception exception = null)
    {
        Console.WriteLine($"LogImpl(level: {level}, msg: {message})");
    }
}
