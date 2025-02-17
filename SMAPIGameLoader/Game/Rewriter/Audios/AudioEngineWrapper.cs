using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using System;

namespace SMAPIGameLoader.Game.Rewriter.Audios
{
    public static class AudioEngineWrapperMethods
    {
        // Method to get the index of an audio category by its name
        public static int GetCategoryIndex(object audioEngine, string name)
        {
            if (audioEngine == null) 
                throw new ArgumentNullException(nameof(audioEngine));
            
            if (string.IsNullOrWhiteSpace(name)) 
                throw new ArgumentException("Category name cannot be null or whitespace.", nameof(name));

            // Access the private _categories field from the AudioEngine class using Harmony
            var categoriesField = AccessTools.Field(typeof(AudioEngine), "_categories");
            var categories = categoriesField.GetValue(audioEngine) as AudioCategory[];

            if (categories == null)
                throw new InvalidOperationException("Unable to retrieve audio categories.");

            // Search for the category by name
            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i].Name == name)
                {
                    return i;
                }
            }

            return -1; // Return -1 if the category is not found
        }
    }
}