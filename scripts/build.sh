# scripts/build.sh
#!/bin/bash
set -e

DIST_DIR="./dist"
PROJECT="./src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj"

echo "🚀 Building executables into $DIST_DIR ..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

dotnet publish "$PROJECT" -c Release -r osx-arm64 --self-contained true \
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true \
  --output "$DIST_DIR/osx-arm64"

dotnet publish "$PROJECT" -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true \
  --output "$DIST_DIR/win-x64"

dotnet publish "$PROJECT" -c Release -r linux-x64 --self-contained true \
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true \
  --output "$DIST_DIR/linux-x64"


echo "✅ Build complete!"
