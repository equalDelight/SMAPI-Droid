
# SMAPI Game Loader

The SMAPI Loader runs a Stardew Valley clone. The clone supports custom assets and DLLs without the need for game patches.


## How it work?

    1. Custom game assets are loaded from the directory Android/data/packagename/files/Stardew Assets.
    2. Custom DLLs are loaded from the directory Android/data/packagename/files, specifically files matching *.dll.
    3. These game assets and DLLs are cloned from the application's base APK (base.apk) and split content APK (split_content.apk).
    4. The game's asset loading process is modified by hooking the MonoGame.Framework.dll's asset loading method, redirecting it from internal resources to the external paths.
