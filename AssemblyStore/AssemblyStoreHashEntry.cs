using System;
using System.IO;

namespace Xamarin.Android.AssemblyStore
{
    public class AssemblyStoreHashEntry
    {
        public bool Is32Bit { get; }

        public ulong Hash { get; }
        public uint MappingIndex { get; }
        public uint LocalStoreIndex { get; }
        public uint StoreID { get; }

        internal AssemblyStoreHashEntry(BinaryReader reader, bool is32Bit)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            Is32Bit = is32Bit;
            
            // Read the hash value from the binary reader
            Hash = reader.ReadUInt64();

            // Read the mapping index from the binary reader
            MappingIndex = reader.ReadUInt32();

            // Read the local store index from the binary reader
            LocalStoreIndex = reader.ReadUInt32();

            // Read the store ID from the binary reader
            StoreID = reader.ReadUInt32();
        }
    }
}
