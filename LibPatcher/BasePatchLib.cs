using ELFSharp.ELF.Sections;
using ELFSharp.ELF;
using System.Security.Cryptography;
using System.Text;

namespace LibPatcher;

public enum PlatformEnum
{
    Arm64, X64
}

internal class PatchData
{
    public string ExportFunctionName { get; }
    public int Offset { get; }
    public byte[] PatchBytes { get; }

    public PatchData(string exportFuncName, int offset, byte[] patchBytes)
    {
        ExportFunctionName = exportFuncName;
        Offset = offset;
        PatchBytes = patchBytes;
    }
}

internal abstract class BasePatchLib
{
    public readonly PlatformEnum platform;
    public readonly string PackageDirPath;
    public const string LibSrcFileName = "libmonosgen-2.0.so";
    public readonly string LibSrcPath;

    public const string LibOriginalBackupFileName = "libmonosgen-2.0-original.so";
    public readonly string LibOriginalBackupFilePath;

    public readonly string LibModifyOutputFileName;
    public string LibModifyOutputFilePath => LibModifyOutputFileName;

    public ISymbolTable dynamicSymbolTable;
    public IELF LibReader;
    public Dictionary<string, SymbolEntry<UInt64>> monoMethodMap = new();
    public FileStream LibWriter = null;

    internal BasePatchLib(PlatformEnum platform)
    {
        this.platform = platform;

        // Set paths based on platform
        if (platform == PlatformEnum.Arm64)
        {
            PackageDirPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64";
            LibModifyOutputFileName = "libmonosgen-2.0-modify-arm64.so";
            LibSrcPath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-arm64\native", LibSrcFileName);
        }
        else
        {
            PackageDirPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-x64";
            LibModifyOutputFileName = "libmonosgen-2.0-modify-x64.so";
            LibSrcPath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-x64\native", LibSrcFileName);
        }

        LibOriginalBackupFilePath = LibSrcPath.Replace(LibSrcFileName, LibOriginalBackupFileName);

        // Start patching process
        Run();

        // Post build actions
        PostBuild();

        Console.WriteLine("Successfully patched library on: " + GetType());
    }

    void Run()
    {
        Console.WriteLine("Start running patch library on: " + GetType());

        // Clone original file if backup does not exist
        if (!File.Exists(LibOriginalBackupFilePath))
            File.Copy(LibSrcPath, LibOriginalBackupFilePath);

        // Verify original file hash
        var fileHash = ComputeSHA256(LibOriginalBackupFilePath);
        var hashTarget = GetLibHashTarget();
        if (fileHash != hashTarget)
        {
            Console.WriteLine("File hash does not match: " + hashTarget);
            Exit();
        }

        // Clone into local app
        File.Copy(LibOriginalBackupFilePath, LibModifyOutputFileName, true);

        Console.WriteLine("Original hash: " + ComputeSHA256(LibOriginalBackupFilePath));

        // Setup ELF reader
        LibReader = ELFReader.Load(LibOriginalBackupFilePath);
        dynamicSymbolTable = LibReader.GetSection(".dynsym") as ISymbolTable;
        foreach (SymbolEntry<UInt64> item in dynamicSymbolTable.Entries)
        {
            if (item.Type == SymbolType.Function && item.Name.StartsWith("mono_"))
            {
                monoMethodMap[item.Name] = item;
            }
        }

        // Setup writer
        LibWriter = File.Open(LibModifyOutputFileName, FileMode.Open, FileAccess.ReadWrite);

        // Ready to patch all
        PatchData[] patches = {
            Patch_FieldAccessException(),
            Patch_MethodAccessException(),
            Patch_mono_class_from_mono_type_internalCrashFix(),
        };
        foreach (var patchData in patches)
        {
            var funcVAFile = (long)GetFunctionOffsetVAFile(patchData.ExportFunctionName);
            var patchFileOffset = funcVAFile + patchData.Offset;

            Console.WriteLine($"Trying to patch: {patchData.ExportFunctionName}" +
                $" file offset: 0x{patchData.Offset:X}" +
                $" byte length: {patchData.PatchBytes.Length}" +
                $" patch file offset: 0x{patchFileOffset:X}");
            WriteByteArray(patchFileOffset, patchData.PatchBytes);
        }

        // Cleanup
        LibWriter.Close();
        LibReader.Dispose();

        Console.WriteLine($"New modified {LibModifyOutputFileName} hash: {ComputeSHA256(LibModifyOutputFileName)}");
    }

    void PostBuild()
    {
        // Copy modified library into package directory
        Console.WriteLine("Starting post build...");
        Console.WriteLine($"Trying to copy {LibModifyOutputFilePath} to {LibSrcPath}");
        File.Copy(LibModifyOutputFilePath, LibSrcPath, true);
        Console.WriteLine("Post build completed");
    }

    protected abstract PatchData Patch_FieldAccessException();
    protected abstract PatchData Patch_MethodAccessException();
    protected abstract PatchData Patch_mono_class_from_mono_type_internalCrashFix();
    protected abstract string GetLibHashTarget();

    internal void Exit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.Read();
        Environment.Exit(0);
    }

    internal ulong GetFunctionOffsetVASection(string name) => GetFunctionOffsetVASection(GetFunction(name));
    internal ulong GetFunctionOffsetVASection(SymbolEntry<UInt64> func)
    {
        return func.Value - func.PointedSection.Offset;
    }

    internal ulong GetFunctionOffsetVAFile(string name)
    {
        var func = GetFunction(name);
        var section = func.PointedSection;
        var offsetOnSection = GetFunctionOffsetVASection(func);

        return section.Offset + offsetOnSection;
    }

    internal byte[] ReadByteArrayFromFunction(SymbolEntry<UInt64> func, int readOffset, int readCount)
    {
        var section = func.PointedSection;
        var funcOffset = GetFunctionOffsetVASection(func);
        byte[] sectionData = section.GetContents();

        byte[] result = new byte[readCount];
        Array.Copy(sectionData, (int)funcOffset + readOffset, result, 0, readCount);
        return result;
    }

    internal SymbolEntry<UInt64> GetFunction(string name) => monoMethodMap[name];

    internal void WriteByteArray(long start, byte[] bytes)
    {
        LibWriter.Seek(start, SeekOrigin.Begin);
        LibWriter.Write(bytes);
    }

    internal void ReadByteArray(byte[] bytes, long start)
    {
        LibWriter.Seek(start, SeekOrigin.Begin);
        LibWriter.Read(bytes);
    }

    internal void DumpHex(byte[] bytes, int start, int length)
    {
        byte[] crop = new byte[length];
        Array.Copy(bytes, start, crop, 0, length);
        DumpHex(crop);
    }

    internal void DumpHex(int start, int length)
    {
        byte[] bytes = new byte[length];
        ReadByteArray(bytes, start);
        DumpHex(bytes);
    }

    internal void DumpHex(byte[] bytes, int dumpRowLength = 4)
    {
        Console.WriteLine("===== Dump Memory =====");
        int x = 0;
        StringBuilder sb = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            byte value = bytes[i];

            // New line
            if (x == dumpRowLength)
            {
                sb.Append("\n");
                x = 0;
            }

            sb.Append($"{value:X2} ");
            x++;
        }
        Console.WriteLine(sb.ToString());
        Console.WriteLine("===== End Dump Memory =====");
    }

    internal string ComputeSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        // Compute hash
        byte[] hashBytes = sha256.ComputeHash(fileStream);

        // Convert hash to hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}