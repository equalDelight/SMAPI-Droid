using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xamarin.Android.AssemblyStore
{
    class AssemblyStoreManifestReader
    {
        // Character array used to split fields in the manifest
        static readonly char[] fieldSplit = new char[] { ' ' };

        // List to store all manifest entries
        public List<AssemblyStoreManifestEntry> Entries { get; } = new List<AssemblyStoreManifestEntry>();

        // Dictionary to store manifest entries by 32-bit hash
        public Dictionary<uint, AssemblyStoreManifestEntry> EntriesByHash32 { get; } = new Dictionary<uint, AssemblyStoreManifestEntry>();

        // Dictionary to store manifest entries by 64-bit hash
        public Dictionary<ulong, AssemblyStoreManifestEntry> EntriesByHash64 { get; } = new Dictionary<ulong, AssemblyStoreManifestEntry>();

        // Constructor to initialize the AssemblyStoreManifestReader object
        public AssemblyStoreManifestReader(Stream manifest)
        {
            // Ensure the stream is at the beginning
            manifest.Seek(0, SeekOrigin.Begin);

            // Use a StreamReader to read the manifest
            using (var sr = new StreamReader(manifest, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            {
                ReadManifest(sr);
            }
        }

        // Method to read the manifest from a StreamReader
        void ReadManifest(StreamReader reader)
        {
            // First line is ignored, it contains headers
            reader.ReadLine();

            // Each subsequent line consists of fields separated by any number of spaces
            while (!reader.EndOfStream)
            {
                // Split the line into fields
                string[]? fields = reader.ReadLine()?.Split(fieldSplit, StringSplitOptions.RemoveEmptyEntries);
                if (fields == null)
                {
                    continue;
                }

                // Create a new manifest entry from the fields
                var entry = new AssemblyStoreManifestEntry(fields);
                Entries.Add(entry);

                // Add the entry to the dictionary by 32-bit hash if it is not zero
                if (entry.Hash32 != 0)
                {
                    EntriesByHash32.Add(entry.Hash32, entry);
                }

                // Add the entry to the dictionary by 64-bit hash if it is not zero
                if (entry.Hash64 != 0)
                {
                    EntriesByHash64.Add(entry.Hash64, entry);
                }
            }
        }
    }
}
