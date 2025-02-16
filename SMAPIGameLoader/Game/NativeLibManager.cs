using LWJGL;
using MonoGame.Framework.Utilities;
using System;
using System.IO;

namespace SMAPIGameLoader.Game;

internal static class NativeLibManager
{
    // Load the libLZ4 library
    static nint Load_libLZ4()
    {
        nint handle = FuncLoader.LoadLibrary("liblwjgl_lz4.so");
        if (handle == IntPtr.Zero)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string directoryName = Path.GetDirectoryName(folderPath);
            string libPath = Path.Combine(directoryName, "lib", "liblwjgl_lz4.so");
            handle = FuncLoader.LoadLibrary(libPath);
        }
        return handle;
    }

    // Load and setup native libraries
    public static void Loads()
    {
        try
        {
            // Example usage of LZ4 library
            int compressBound = LZ4.CompressBound(10);
            Console.WriteLine("Done setting up native libs");
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }
}
