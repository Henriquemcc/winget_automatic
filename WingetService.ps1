# Getting Winget location
$winget = (Get-Command -Name winget).Source
if ($null -eq $winget)
{
    $winget = Get-ChildItem -Path "C:\Program Files\WindowsApps" -Filter "winget.exe" -Recurse
    $winget = [System.IO.Path]::Combine($winget.Directory.FullName, $winget.Name)
}

# Apply updates with winget
Write-Output "Applying updates with winget..."
& $winget update --all --silent --accept-source-agreements --accept-package-agreements

Write-Output "Updates applied."