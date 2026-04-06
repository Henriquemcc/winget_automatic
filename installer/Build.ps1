param(
    # Version sent by GitHub Actions workflow.
    [string]$Version = "0.0.0"
)

# Removes the prefix 'v' from the tag name (eg: v1.2.3 -> 1.2.3)
$ProductVersion = $Version.TrimStart('v')

# Building dotnet service
dotnet build --self-contained --configuration Release --arch x86 --os win ..
dotnet build --self-contained --configuration Release --arch x64 --os win ..

# Preparing installer files
$installerStage = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition), "installer-stage")
New-Item -Path $installerStage -ItemType Directory -Force
Copy-Item -Path ([System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)), "bin", "Release", "net10.0", "win-x86", "winget_automatic.exe")) -Destination ([System.IO.Path]::Combine($installerStage, "winget_automatic_x86.exe"))
Copy-Item -Path ([System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)), "bin", "Release", "net10.0", "win-x64", "winget_automatic.exe")) -Destination ([System.IO.Path]::Combine($installerStage, "winget_automatic_x64.exe"))

# Building MSI Package
dotnet build /p:ProductVersion=$ProductVersion /p:DefineConstants="ProductVersion=$ProductVersion"