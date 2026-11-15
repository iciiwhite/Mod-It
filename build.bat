@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo  Building Mod it! single-file executable
echo ============================================

dotnet publish ModIt.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:IncludeAllContentForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishTrimmed=false ^
  -o publish

if %errorlevel% neq 0 (
    echo Build failed.
    pause
    exit /b %errorlevel%
)

echo.
echo Build complete. Output: publish\ModIt.exe
echo Rename ModIt.exe to "Mod it!.exe" if you prefer.
pause