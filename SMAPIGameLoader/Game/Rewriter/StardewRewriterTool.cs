using Mono.Cecil;
using System;
using System.Linq;

namespace SMAPIGameLoader.Game.Rewriter;

internal static class StardewRewriterTool
{
    private static AssemblyDefinition assemblyDefinition;
    private static ModuleDefinition mainModule;

    // Initialize the rewriter tool with the given assembly definition
    internal static void Init(AssemblyDefinition stardewAsmDef)
    {
        assemblyDefinition = stardewAsmDef;
        mainModule = stardewAsmDef.MainModule;
    }

    // Find a type in the main module by its full name
    internal static TypeDefinition FindType(string fullName)
    {
        return mainModule.Types.Single(t => t.FullName == fullName);
    }
}
