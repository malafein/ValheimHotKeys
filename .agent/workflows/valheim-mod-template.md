---
description: Create a new Valheim mod project using the standard template (based on Shipwright's Touch)
---

# Valheim Mod Template Workflow

Use this workflow to set up a new BepInEx mod for Valheim with a standard build and release system.

## 1. Create Project File (.csproj)
Create a file named `[ProjectName].csproj` with the following content. Replace `[ProjectName]` and `[Description]`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
    <AssemblyName>[ProjectName]</AssemblyName>
    <Description>[Description]</Description>
    <Version>0.0.1</Version>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <LangVersion>latest</LangVersion>
    <RestoreAdditionalProjectSources>
      https://nuget.bepinex.dev/v3/index.json
    </RestoreAdditionalProjectSources>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BepInEx.Analyzers" Version="1.0.8" PrivateAssets="all" />
    <PackageReference Include="BepInEx.Core" Version="5.4.21" />
    <PackageReference Include="UnityEngine.Modules" Version="2019.4.31" IncludeAssets="compile" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="assembly_valheim">
      <HintPath>/home/malafein/.local/share/Steam/steamapps/common/Valheim/Valheim_Data/Managed/assembly_valheim.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="assembly_guiutils">
      <HintPath>/home/malafein/.local/share/Steam/steamapps/common/Valheim/Valheim_Data/Managed/assembly_guiutils.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="assembly_utils">
      <HintPath>/home/malafein/.local/share/Steam/steamapps/common/Valheim/Valheim_Data/Managed/assembly_utils.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

## 2. Initialize Plugin Base (Plugin.cs)
Initialize the BepInEx plugin class.

```csharp
using BepInPlugin;
using BepInEx;
using HarmonyLib;

namespace [ProjectName]
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.malafein.[projectname]";
        public const string ModName = "[Project Name]";
        public const string ModVersion = "0.0.1";

        private readonly Harmony harmony = new Harmony(ModGUID);

        private void Awake()
        {
            Logger.LogInfo($"{ModName} {ModVersion} is loading...");
            harmony.PatchAll();
            Logger.LogInfo($"{ModName} loaded!");
        }
    }
}
```

## 3. Create manifest.json
Required for Thunderstore / Mod managers.

```json
{
    "name": "[ProjectName]",
    "version_number": "0.0.1",
    "website_url": "https://github.com/malafein/[ProjectName]",
    "description": "[Description]",
    "dependencies": [
        "denikson-BepInExPack_Valheim-5.4.2100"
    ]
}
```

## 4. Set up Release Script (release.sh)
Create `release.sh` and run `chmod +x release.sh`.

```bash
#!/bin/bash
PROJECT_NAME="[ProjectName]"
DLL_PATH="bin/Debug/net462/$PROJECT_NAME.dll"
STAGING_DIR="staging"
RELEASES_DIR="Releases"

VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "$PROJECT_NAME.csproj")
ZIP_NAME="${PROJECT_NAME}_v${VERSION}.zip"

dotnet build -c Debug || exit 1
mkdir -p "$STAGING_DIR"
cp "$DLL_PATH" README.md CHANGELOG.md manifest.json "$STAGING_DIR/"
mkdir -p "$RELEASES_DIR"
cd "$STAGING_DIR" && zip -r "../$RELEASES_DIR/$ZIP_NAME" . && cd ..
```

## 5. Metadata Files
Create `README.md`, `CHANGELOG.md`, and `.gitignore`.
