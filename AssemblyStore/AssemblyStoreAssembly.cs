using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Text;

namespace Xamarin.Android.AssemblyStore
{
    public class AssemblyStoreAssembly
    {
        public uint DataOffset { get; }
        public uint DataSize { get; }
        public uint DebugDataOffset { get; }
        public uint DebugDataSize { get; }
        public uint ConfigDataOffset { get; }
        public uint ConfigDataSize { get; }
        public uint Hash32 { get; set; }
        public ulong Hash64 { get; set; }
        public string Name { get; set; } = string.Empty;
        public uint RuntimeIndex { get; set; }
        public AssemblyStoreReader Store { get; }

        public string DllName => MakeFileName("dll");
        public string PdbName => MakeFileName("pdb");
        public string ConfigName => MakeFileName("dll.config");

        internal AssemblyStoreAssembly(BinaryReader reader, AssemblyStoreReader store)
        {
            Store = store;
            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            DebugDataOffset = reader.ReadUInt32();
            DebugDataSize = reader.ReadUInt32();
            ConfigDataOffset = reader.ReadUInt32();
            ConfigDataSize = reader.ReadUInt32();
        }

        public void Extract(string outputDirPath, string extension, string? fileName = null, bool decompress = false)
        {
            var outputFilePath = MakeOutputFilePath(outputDirPath, extension, fileName);
            Store.ExtractAssemblyImage(this, outputFilePath);
            if (decompress && extension == "dll")
            {
                DecompressDll(outputFilePath);
            }
        }

        public void Extract(Stream output, string extension)
        {
            switch (extension)
            {
                case "dll":
                    Store.ExtractAssemblyImage(this, output);
                    break;
                case "pdb":
                    Store.ExtractAssemblyDebugData(this, output);
                    break;
                case "dll.config":
                    Store.ExtractAssemblyConfig(this, output);
                    break;
            }
        }

        private static void DecompressDll(string path)
        {
            var compressedData = File.ReadAllBytes(path);
            if (Encoding.ASCII.GetString(compressedData[0..4]) != "XALZ")
                return;

            var unpackLength = BitConverter.ToInt32(compressedData[8..12]);
            var payload = compressedData[12..];
            var decompressedData = new byte[unpackLength];
            LZ4Codec.Decode(payload, decompressedData);
            File.WriteAllBytes(path, decompressedData);
        }

        private string MakeOutputFilePath(string outputDirPath, string extension, string? fileName)
        {
            return Path.Combine(outputDirPath, MakeFileName(extension, fileName));
        }

        private string MakeFileName(string extension, string? fileName = null)
        {
            fileName ??= Name;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"{Hash32:x}_{Hash64:x}";
            }
            return $"{fileName}.{extension}";
        }
    }
}