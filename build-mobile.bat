@echo off
setlocal
where msbuild >nul 2>nul
if errorlevel 1 (
  echo MSBuild was not found. Open this folder in Visual Studio with .NET Framework 4.7.2 build tools installed.
  exit /b 1
)
msbuild LostArtifacts.sln /t:Build /p:Configuration=Release
if errorlevel 1 exit /b %errorlevel%
echo.
echo Build complete. Check LostArtifacts\bin\Release\LostArtifacts.dll
