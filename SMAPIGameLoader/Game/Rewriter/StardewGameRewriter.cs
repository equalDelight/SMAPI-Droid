using System;
using System.Linq;
using Mono.Cecil;
using System.IO;

namespace SMAPIGameLoader.Game.Rewriter;

internal static class StardewGameRewriter
{
    // Find a type in the module definition by its full name
    public static TypeReference FindType(ModuleDefinition moduleDefinition, Type type)
    {
        return moduleDefinition.Types.First(t => t.FullName == type.FullName);
    }

    // Configuration for Mono.Cecil reader
    public static ReaderParameters MonoCecilReaderConfig = new()
    {
        AssemblyResolver = StardewAssembliesResolver.Instance,
    };

    // Read a module from a stream
    public static ModuleDefinition ReadModule(Stream stream)
    {
        return ModuleDefinition.ReadModule(stream, MonoCecilReaderConfig);
    }

    // Read an assembly from a stream
    public static AssemblyDefinition ReadAssembly(Stream stream)
    {
        return AssemblyDefinition.ReadAssembly(stream, MonoCecilReaderConfig);
    }

    // Add InternalsVisibleTo attribute to the module
    public static void AddInternalsVisibleTo(ModuleDefinition module, string visibleTo)
    {
        var assembly = module.Assembly;
        var attributeConstructor = assembly.MainModule.ImportReference(
            typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })
        );
        var customAttribute = new CustomAttribute(attributeConstructor);
        customAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, visibleTo)
        );

        assembly.CustomAttributes.Add(customAttribute);
        Console.WriteLine("Added InternalsVisibleTo with: " + visibleTo);
    }

    // Rewrite the assembly definition
    public static void Rewrite(AssemblyDefinition assemblyDefinition)
    {
        try
        {
            StardewRewriterTool.Init(assemblyDefinition);
            var stardewModule = assemblyDefinition.MainModule;
            var mainActivityTypeDef = stardewModule.Types.First(t => t.Name == "MainActivity");
            var instanceFieldDef = mainActivityTypeDef.Fields.First(f => f.Name == "instance");

            // Change field type from MainActivity to SMAPIActivity
            instanceFieldDef.FieldType = stardewModule.ImportReference(typeof(SMAPIActivity));
            TaskTool.NewLine("Changed field type from MainActivity to SMAPIActivity");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
        }
    }
}
