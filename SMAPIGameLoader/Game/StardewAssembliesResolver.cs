extern alias MonoCecilAlias;

using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SMAPIGameLoader;

public class StardewAssembliesResolver : DefaultAssemblyResolver
{
    // Singleton instance of the resolver
    public static StardewAssembliesResolver Instance { get; } = new();

    // Constructor to initialize the resolver and add search directories
    public StardewAssembliesResolver() : base()
    {
        AddSearchDirectory(GameAssemblyManager.AssembliesDirPath);
    }
}
