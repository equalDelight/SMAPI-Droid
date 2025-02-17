using Mono.Cecil;
using System;

namespace SMAPIGameLoader.Game.Rewriter.Audios
{
    internal static class StardewAudioRewriter
    {
        // Method to rewrite the Stardew Assembly for audio modifications
        internal static void Rewrite(AssemblyDefinition stardewAssemblyDef)
        {
            if (stardewAssemblyDef == null)
                throw new ArgumentNullException(nameof(stardewAssemblyDef), "Assembly definition cannot be null.");

            try
            {
                // Add your code here to modify the assembly for audio purposes
                // Example: Modify methods, fields, properties, etc.
            }
            catch (Exception e)
            {
                // Show the error dialog and rethrow the exception for further handling
                ErrorDialogTool.Show(e, nameof(StardewAudioRewriter));
                throw;
            }
        }
    }
}