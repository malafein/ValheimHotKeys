#!/bin/bash

# Configuration
PROJECT_NAME="ValheimHotKeys"
DLL_PATH="bin/Debug/net462/$PROJECT_NAME.dll"
STAGING_DIR="staging"
RELEASES_DIR="Releases"

# Get version from csproj
VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "$PROJECT_NAME.csproj")
ZIP_NAME="${PROJECT_NAME}_v${VERSION}.zip"

echo "Releasing $PROJECT_NAME v$VERSION..."

# 1. Build project
echo "Building project..."
dotnet build -c Debug || { echo "Build failed!"; exit 1; }

# 2. Update staging directory
echo "Staging files..."
mkdir -p "$STAGING_DIR"
cp "$DLL_PATH" "$STAGING_DIR/"
cp README.md "$STAGING_DIR/"
cp CHANGELOG.md "$STAGING_DIR/"
cp manifest.json "$STAGING_DIR/"
cp Assets/icon.png "$STAGING_DIR/"

# 3. Create release zip
echo "Creating zip: $ZIP_NAME..."
mkdir -p "$RELEASES_DIR"
cd "$STAGING_DIR" || exit 1
zip -r "../$RELEASES_DIR/$ZIP_NAME" .
cd ..

# 4. Local Deployment (optional sync)
PLUGINS_DIR="/home/malafein/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/ValheimHotKeys"
echo "Deploying to local plugins folder: $PLUGINS_DIR..."
mkdir -p "$PLUGINS_DIR"
cp -r "$STAGING_DIR"/* "$PLUGINS_DIR/"

echo "Done! Release created at $RELEASES_DIR/$ZIP_NAME and deployed to local plugins."
