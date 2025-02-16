using System;
using System.Globalization;

namespace Xamarin.Android.AssemblyStore
{
    class AssemblyStoreManifestEntry
    {
        // Constants representing the number of fields and their indices in the input array
        const int NumberOfFields = 5;
        const int Hash32FieldIndex = 0;
        const int Hash64FieldIndex = 1;
        const int StoreIDFieldIndex = 2;
        const int StoreIndexFieldIndex = 3;
        const int NameFieldIndex = 4;

        // Properties to store the parsed values
        public uint Hash32 { get; }
        public ulong Hash64 { get; }
        public uint StoreID { get; }
        public uint IndexInStore { get; }
        public string Name { get; }

        // Constructor to initialize the AssemblyStoreManifestEntry object
        public AssemblyStoreManifestEntry(string[] fields)
        {
            if (fields.Length != NumberOfFields)
            {
                throw new ArgumentOutOfRangeException(nameof(fields), "Invalid number of fields");
            }

            // Parse and assign values from the fields array
            Hash32 = GetUInt32(fields[Hash32FieldIndex]);
            Hash64 = GetUInt64(fields[Hash64FieldIndex]);
            StoreID = GetUInt32(fields[StoreIDFieldIndex]);
            IndexInStore = GetUInt32(fields[StoreIndexFieldIndex]);
            Name = fields[NameFieldIndex].Trim();
        }

        // Method to convert a string to a 32-bit unsigned integer
        uint GetUInt32(string value)
        {
            if (UInt32.TryParse(PrepHexValue(value), NumberStyles.HexNumber, null, out uint result))
            {
                return result;
            }

            throw new FormatException($"Invalid 32-bit unsigned integer value: {value}");
        }

        // Method to convert a string to a 64-bit unsigned integer
        ulong GetUInt64(string value)
        {
            if (UInt64.TryParse(PrepHexValue(value), NumberStyles.HexNumber, null, out ulong result))
            {
                return result;
            }

            throw new FormatException($"Invalid 64-bit unsigned integer value: {value}");
        }

        // Method to prepare a hexadecimal string value by removing the "0x" prefix if present
        string PrepHexValue(string value)
        {
            if (value.StartsWith("0x", StringComparison.Ordinal))
            {
                return value.Substring(2);
            }

            return value;
        }
    }
}
