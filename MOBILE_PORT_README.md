# Lost Artifacts — Mobile Port 1.3.0.0

This source tree is a mobile-targeted adaptation of Lost Artifacts 1.1.1.1 for the supplied Hollow Knight port 1.3.0.0.

## Changes

- Removed Randomizer 4 / RandomizerCore / RandoSettingsManager integration.
- Removed DebugMod integration.
- Removed the MenuChanger-based settings screen.
- Kept the core Artifact + ItemChanger functionality.
- Kept the SFCore inventory page integration.
- Added guards around the AssetBundle and inventory FSM so a changed mobile UI fails gracefully instead of crashing the mod during initialization.
- Repointed the project references to the supplied port Managed DLLs and the supplied SFCore/Satchel/ItemChanger DLLs.
- Build version is `1.1.1.2` and target is labeled `Mobile 1.3.0.0`.

## Important limitation

The source has been adapted, but this environment does not contain a C# compiler/MSBuild, so I cannot honestly claim that a final `LostArtifacts.dll` was compiled and tested here. The package contains the modified source and all reference DLLs needed for a local build.

## Build

Open `LostArtifacts.sln` in an environment with .NET Framework 4.7.2/Visual Studio support, or build the project with MSBuild. The project expects:

- `PortReferences/` — the supplied mobile `Managed` DLLs
- `ModDependencies/` — the supplied `SFCore.dll`, `Satchel.dll`, and `ItemChanger.dll`

The resulting DLL is written under `LostArtifacts/bin/Release/`.
