using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Xamarin.Android.AssemblyStore
{
    public class AssemblyStoreExplorer
    {
        AssemblyStoreReader? indexStore;
        AssemblyStoreManifestReader? manifest;
        int numberOfStores = 0;
        Action<AssemblyStoreExplorerLogLevel, string>? logger;
        bool keepStoreInMemory;

        public IDictionary<string, AssemblyStoreAssembly> AssembliesByName { get; } = new ConcurrentDictionary<string, AssemblyStoreAssembly>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<uint, AssemblyStoreAssembly> AssembliesByHash32 { get; } = new ConcurrentDictionary<uint, AssemblyStoreAssembly>();
        public IDictionary<ulong, AssemblyStoreAssembly> AssembliesByHash64 { get; } = new ConcurrentDictionary<ulong, AssemblyStoreAssembly>();
        public List<AssemblyStoreAssembly> Assemblies { get; } = new List<AssemblyStoreAssembly>();
        public IDictionary<uint, List<AssemblyStoreReader>> Stores { get; } = new ConcurrentDictionary<uint, List<AssemblyStoreReader>>();
        public string StorePath { get; }
        public string StoreSetName { get; }
        public bool HasErrors { get; private set; }
        public bool HasWarnings { get; private set; }

        public bool IsCompleteSet => indexStore != null && manifest != null;
        public int NumberOfStores => numberOfStores;

        public AssemblyStoreExplorer(string storePath, Action<AssemblyStoreExplorerLogLevel, string>? customLogger = null, bool keepStoreInMemory = false)
        {
            if (String.IsNullOrEmpty(storePath))
            {
                throw new ArgumentException("must not be null or empty", nameof(storePath));
            }

            if (Directory.Exists(storePath))
            {
                throw new ArgumentException($"'{storePath}' points to a directory", nameof(storePath));
            }

            logger = customLogger;
            this.keepStoreInMemory = keepStoreInMemory;
            StorePath = storePath;
            string? extension = Path.GetExtension(storePath);
            string? baseName = null;

            if (String.IsNullOrEmpty(extension))
            {
                baseName = GetBaseNameNoExtension(storePath);
            }
            else
            {
                baseName = GetBaseNameHaveExtension(storePath, extension);
            }

            if (String.IsNullOrEmpty(baseName))
            {
                throw new InvalidOperationException($"Unable to determine base name of a store set from path '{storePath}'");
            }

            StoreSetName = baseName;
            if (!IsAndroidArchive(extension))
            {
                Logger(AssemblyStoreExplorerLogLevel.Info, $"{storePath} is not an Android archive, reading from filesystem");
                string? directoryName = Path.GetDirectoryName(storePath);
                if (String.IsNullOrEmpty(directoryName))
                {
                    directoryName = ".";
                }

                ReadStoreSetFromFilesystem(baseName, directoryName).Wait();
            }
            else
            {
                Logger(AssemblyStoreExplorerLogLevel.Info, $"{storePath} is an Android archive");
                ReadStoreSetFromArchive(baseName, storePath, extension).Wait();
            }

            ProcessStores();
        }

        public AssemblyStoreExplorer(ZipArchive archive, string basePathInArchive, Action<AssemblyStoreExplorerLogLevel, string>? customLogger = null, bool keepStoreInMemory = false)
        {
            logger = customLogger;
            this.keepStoreInMemory = keepStoreInMemory;
            StorePath = "<in-memory-archive>";
            StoreSetName = StorePath;
            ReadStoreSetFromArchive(archive, basePathInArchive).Wait();

            ProcessStores();
        }

        void Logger(AssemblyStoreExplorerLogLevel level, string message)
        {
            if (level == AssemblyStoreExplorerLogLevel.Error)
            {
                HasErrors = true;
            }
            else if (level == AssemblyStoreExplorerLogLevel.Warning)
            {
                HasWarnings = true;
            }

            if (logger != null)
            {
                logger(level, message);
            }
            else
            {
                DefaultLogger(level, message);
            }
        }

        void DefaultLogger(AssemblyStoreExplorerLogLevel level, string message)
        {
            Console.WriteLine($"{level}: {message}");
        }

        void ProcessStores()
        {
            if (Stores.Count == 0 || indexStore == null)
            {
                return;
            }

            ProcessIndex(indexStore.GlobalIndex32, "32", (AssemblyStoreHashEntry he, AssemblyStoreAssembly assembly) =>
            {
                assembly.Hash32 = (uint)he.Hash;
                assembly.RuntimeIndex = he.MappingIndex;

                if (manifest != null && manifest.EntriesByHash32.TryGetValue(assembly.Hash32, out AssemblyStoreManifestEntry? me) && me != null)
                {
                    assembly.Name = me.Name;
                }

                if (!AssembliesByHash32.ContainsKey(assembly.Hash32))
                {
                    AssembliesByHash32.Add(assembly.Hash32, assembly);
                }
            });

            ProcessIndex(indexStore.GlobalIndex64, "64", (AssemblyStoreHashEntry he, AssemblyStoreAssembly assembly) =>
            {
                assembly.Hash64 = he.Hash;
                if (assembly.RuntimeIndex != he.MappingIndex)
                {
                    Logger(AssemblyStoreExplorerLogLevel.Warning, $"assembly with hashes 0x{assembly.Hash32} and 0x{assembly.Hash64} has a different 32-bit runtime index ({assembly.RuntimeIndex})");
                }

                if (manifest != null && manifest.EntriesByHash64.TryGetValue(assembly.Hash64, out AssemblyStoreManifestEntry? me) && me != null)
                {
                    if (String.IsNullOrEmpty(assembly.Name))
                    {
                        Logger(AssemblyStoreExplorerLogLevel.Warning, $"32-bit hash 0x{assembly.Hash32:x} did not match any assembly name in the manifest");
                        assembly.Name = me.Name;
                        if (String.IsNullOrEmpty(assembly.Name))
                        {
                            Logger(AssemblyStoreExplorerLogLevel.Warning, $"64-bit hash 0x{assembly.Hash64:x} did not match any assembly name in the manifest");
                        }
                    }
                    else if (String.Compare(assembly.Name, me.Name, StringComparison.Ordinal) != 0)
                    {
                        Logger(AssemblyStoreExplorerLogLevel.Warning, $"32-bit hash 0x{assembly.Hash32:x} maps to assembly name '{assembly.Name}', however 64-bit hash 0x{assembly.Hash64:x} for the same assembly maps to a different name '{me.Name}'");
                    }
                }

                if (!AssembliesByHash64.ContainsKey(assembly.Hash64))
                {
                    AssembliesByHash64.Add(assembly.Hash64, assembly);
                }
            });

            foreach (var kvp in Stores)
            {
                List<AssemblyStoreReader> list = kvp.Value;
                if (list.Count < 2)
                {
                    continue;
                }

                AssemblyStoreReader template = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    AssemblyStoreReader other = list[i];
                    if (!template.HasIdenticalContent(other))
                    {
                        Logger(AssemblyStoreExplorerLogLevel.Error, $"Store ID {template.StoreID} for architecture {other.Arch} is not identical to other stores with the same ID");
                    }
                }
            }

            void ProcessIndex(List<AssemblyStoreHashEntry> index, string bitness, Action<AssemblyStoreHashEntry, AssemblyStoreAssembly> assemblyHandler)
            {
                foreach (AssemblyStoreHashEntry he in index)
                {
                    if (!Stores.TryGetValue(he.StoreID, out List<AssemblyStoreReader>? storeList) || storeList == null)
                    {
                        Logger(AssemblyStoreExplorerLogLevel.Warning, $"store with id {he.StoreID} not part of the set");
                        continue;
                    }

                    foreach (AssemblyStoreReader store in storeList)
                    {
                        if (he.LocalStoreIndex >= (uint)store.Assemblies.Count)
                        {
                            Logger(AssemblyStoreExplorerLogLevel.Warning, $"{bitness}-bit index entry with hash 0x{he.Hash:x} has invalid store {store.StoreID} index {he.LocalStoreIndex} (maximum {store.Assemblies.Count - 1})");
                            continue;
                        }

                        AssemblyStoreAssembly assembly = store.Assemblies[(int)he.LocalStoreIndex];
                        assemblyHandler(he, assembly);

                        if (!AssembliesByName.ContainsKey(assembly.Name))
                        {
                            AssembliesByName.Add(assembly.Name, assembly);
                        }
                    }
                }
            }
        }

        async Task ReadStoreSetFromArchive(string baseName, string archivePath, string extension)
        {
            string basePathInArchive;

            if (String.Compare(".aab", extension, StringComparison.OrdinalIgnoreCase) == 0)
            {
                basePathInArchive = "base/root/assemblies";
            }
            else if (String.Compare(".apk", extension, StringComparison.OrdinalIgnoreCase) == 0)
            {
                basePathInArchive = "assemblies";
            }
            else if (String.Compare(".zip", extension, StringComparison.OrdinalIgnoreCase) == 0)
            {
                basePathInArchive = "root/assemblies";
            }
            else
            {
                throw new InvalidOperationException($"Unrecognized archive extension '{extension}'");
            }

            basePathInArchive = $"{basePathInArchive}/{baseName}.";
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Read))
            {
                await ReadStoreSetFromArchive(archive, basePathInArchive);
            }
        }

        async Task ReadStoreSetFromArchive(ZipArchive archive, string basePathInArchive)
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(basePathInArchive, StringComparison.Ordinal))
                {
                    continue;
                }

                using var stream = new MemoryStream();
                using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
                entryStream.Close();
                stream.Seek(0, SeekOrigin.Begin);

                if (entry.FullName.EndsWith(".blob", StringComparison.Ordinal))
                {
                    AddStore(new AssemblyStoreReader(stream, GetStoreArch(entry.FullName), keepStoreInMemory));
                }
                else if (entry.FullName.EndsWith(".manifest", StringComparison.Ordinal))
                {
                    manifest = new AssemblyStoreManifestReader(stream);
                }
            }
        }

        void AddStore(AssemblyStoreReader reader)
        {
            if (reader.HasGlobalIndex)
            {
                indexStore = reader;
            }

            List<AssemblyStoreReader>? storeList;
            if (!Stores.TryGetValue(reader.StoreID, out storeList))
            {
                storeList = new List<AssemblyStoreReader>();
                Stores.Add(reader.StoreID, storeList);
            }
            storeList.Add(reader);

            Assemblies.AddRange(reader.Assemblies);
        }

        string? GetStoreArch(string path)
        {
            string? arch = Path.GetFileNameWithoutExtension(path);
            if (!String.IsNullOrEmpty(arch))
            {
                arch = Path.GetExtension(arch);
                if (!String.IsNullOrEmpty(arch))
                {
                    arch = arch.Substring(1);
                }
            }

            return arch;
        }

        async Task ReadStoreSetFromFilesystem(string baseName, string setPath)
        {
            foreach (string de in Directory.EnumerateFiles(setPath, $"{baseName}.*", SearchOption.TopDirectoryOnly))
            {
                string? extension = Path.GetExtension(de);
                if (String.IsNullOrEmpty(extension))
                {
                    continue;
                }

                if (String.Compare(".blob", extension, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AddStore(await ReadStore(de));
                }
                else if (String.Compare(".manifest", extension, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    manifest = await ReadManifest(de);
                }
            }

            async Task<AssemblyStoreReader> ReadStore(string filePath)
            {
                string? arch = GetStoreArch(filePath);
                using (var fs = File.OpenRead(filePath))
                {
                    return CreateStoreReader(fs, arch);
                }
            }

            async Task<AssemblyStoreManifestReader> ReadManifest(string filePath)
            {
                using (var fs = File.OpenRead(filePath))
                {
                    return new AssemblyStoreManifestReader(fs);
                }
            }
        }

        AssemblyStoreReader CreateStoreReader(Stream input, string? arch)
        {
            numberOfStores++;
            return new AssemblyStoreReader(input, arch, keepStoreInMemory);
        }

        bool IsAndroidArchive(string extension)
        {
            return
                String.Compare(".aab", extension, StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(".apk", extension, StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(".zip", extension, StringComparison.OrdinalIgnoreCase) == 0;
        }

        string GetBaseNameHaveExtension(string storePath, string extension)
        {
            if (IsAndroidArchive(extension))
            {
                return "assemblies";
            }

            string fileName = Path.GetFileNameWithoutExtension(storePath);
            int dot = fileName.IndexOf('.');
            if (dot >= 0)
            {
                return fileName.Substring(0, dot);
            }

            return fileName;
        }

        string GetBaseNameNoExtension(string storePath)
        {
            string fileName = Path.GetFileName(storePath);
            if (fileName.EndsWith("_assemblies", StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }
            return $"{fileName}_assemblies";
        }
    }
}