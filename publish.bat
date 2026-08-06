@echo off
echo Publication de Metal Bayala Gestion...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
echo.
echo Publication terminee dans le dossier ./publish
pause
