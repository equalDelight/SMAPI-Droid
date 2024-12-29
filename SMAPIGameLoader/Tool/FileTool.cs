﻿using Android.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

internal static class FileTool
{
    public static string ExternalFilesDir => Application.Context.GetExternalFilesDir(null).AbsolutePath;
    public static string InternalFilesDir => Application.Context.FilesDir.AbsolutePath;
    public static string SafePath(string path)
    {
        path = path.Replace("\\", "/");
        path = path.Replace("//", "/");
        return path;
    }
    public static float ConvertBytesToMB(long bytes)
    {
        return bytes / (1024f * 1024f);
    }

    internal static void MakeSureDirectory(string dir)
    {
        Directory.CreateDirectory(dir);
    }
    internal static void MakeSureFilePath(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        MakeSureDirectory(dir);
    }

    internal static void ClearCache()
    {
        try
        {

            var dir = Application.Context.ExternalCacheDir;
            if (dir.Exists())
                dir.Delete();

            dir = Application.Context.CacheDir;
            if (dir.Exists())
                dir.Delete();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    internal static void Delete(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    internal static string GetFirstDirNameFromFilePath(string fileName)
    {
        string fileDirPath = Path.GetDirectoryName(fileName);
        string[] dirNames = fileDirPath.Split(Path.DirectorySeparatorChar);
        return dirNames.FirstOrDefault();
    }

    //beware don't use entry.Name, it's not works if it dir entry
    internal static bool IsEntryDirectory(ZipArchiveEntry entry) => entry.FullName.EndsWith("/");
    internal static bool IsEntryFile(ZipArchiveEntry entry) => !IsEntryDirectory(entry);
}
