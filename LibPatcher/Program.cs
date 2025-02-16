using ELFSharp.Utilities;
using LibPatcher;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal class Program
{
    static void Main(string[] args)
    {
        // Ready to start patching process
        try
        {
            PatchLibX64.Start();
            PatchLibArm64.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        // Close the application
        Exit();
    }

    static void Exit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }
}

