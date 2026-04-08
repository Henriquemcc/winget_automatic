param(
    # Version sent by GitHub Actions workflow.
    [string]$Version = "0.0.0"
)

# Removes the prefix 'v' from the tag name (eg: v1.2.3 -> 1.2.3)
$ProductVersion = $Version.TrimStart('v')

# Publishing dotnet service as a single file to ensure all dependencies are inside the EXE
dotnet publish -c Release -r win-x86 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ..\app
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ..\app

# Preparing installer files
$installerStage = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition), "installer-stage")
New-Item -Path $installerStage -ItemType Directory -Force
Copy-Item -Path ([System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)), "app", "bin", "Release", "net10.0", "win-x86", "publish", "winget_automatic.exe")) -Destination ([System.IO.Path]::Combine($installerStage, "winget_automatic_x86.exe"))
Copy-Item -Path ([System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)), "app", "bin", "Release", "net10.0", "win-x64", "publish", "winget_automatic.exe")) -Destination ([System.IO.Path]::Combine($installerStage, "winget_automatic_x64.exe"))

# Building MSI Package
dotnet build /p:ProductVersion=$ProductVersion /p:DefineConstants="ProductVersion=$ProductVersion"