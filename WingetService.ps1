# Block shutdown and reboot
$registryPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
$registryName = "ShutdownWithoutLogon"
$registryValue = 0
Set-ItemProperty -Path $registryPath -Name $registryName -Value $registryValue

# Disable shutdown via Group Policy (Requires Admin Privileges)
$policyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"
$policyName = "NoClose"
$policyValue = 1
New-Item -Path $policyPath -Force | Out-Null
Set-ItemProperty -Path $policyPath -Name $policyName -Value $policyValue

# Refresh policies
Invoke-Expression "gpupdate /force"

# Apply updates with winget
Write-Output "Applying updates with winget..."
winget update --all --silent --accept-source-agreements --accept-package-agreements

# Wait for updates to complete
Write-Output "Waiting for updates to finish..."
Start-Sleep -Seconds 60

# Unblock shutdown and reboot
Set-ItemProperty -Path $registryPath -Name $registryName -Value 1
Set-ItemProperty -Path $policyPath -Name $policyName -Value 0

# Refresh policies again
Invoke-Expression "gpupdate /force"

Write-Output "Updates applied. Shutdown and reboot are now allowed."