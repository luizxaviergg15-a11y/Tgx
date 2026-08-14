#!/usr/bin/env bash
set -e
if command -v msbuild >/dev/null 2>&1; then
  msbuild LostArtifacts.sln /t:Build /p:Configuration=Release
elif command -v xbuild >/dev/null 2>&1; then
  xbuild LostArtifacts.sln /p:Configuration=Release
else
  echo "MSBuild/xbuild not found. Build on a machine with a .NET Framework-compatible C# build environment." >&2
  exit 1
fi
