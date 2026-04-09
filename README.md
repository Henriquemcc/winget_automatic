# Winget Automatic

![GitHub License](https://img.shields.io/github/license/Henriquemcc/winget_automatic)
![GitHub Release Workflow Status](https://img.shields.io/github/actions/workflow/status/Henriquemcc/winget_automatic/release.yml)
![GitHub release (latest by severity)](https://img.shields.io/github/v/release/Henriquemcc/winget_automatic)

A Windows service that runs in the background automatically updating installed applications on Windows using [WinGet](https://learn.microsoft.com/windows/package-manager/winget/).

## Table of contents

- [Features](#features)
- [Download and Installation](#download-and-installation)
- [Quick Start](#quick-start)
- [License](#license)

## Features

- Windows service: Background execution of WinGet to automatically update installed applications with no user interaction.
- Shutdown detection: The background service detects that the computer is shutting down and safely finishes the installation of the update before shutting down.
- Customized scheduling: Individual configuration of the delay time between each update check.
- Package excluding: Packages can be excluded from update by adding their ID.
- Reboot policy: Is possible to define whether to reboot when necessary, always or never.
- Hashing and malware scanning configuration: Is possible to disable hashing and malware scanning.
- Proxy configuration: Is possible to define proxy settings to download packages.

## Download and Installation

To install this program, go to the [release page](https://github.com/Henriquemcc/winget_automatic/releases) and download the MSI installation package for Windows.

## Quick Start

1. [Download and install Winget Automatic](#download-and-installation).
2. On the folder ```C:\ProgramData\WingetAutomatic``` open the file ```config.json``` and change the desired configuration.
3. Restart Winget Automatic Service:
    - With Windows Service Manager:
        - Open ```services.msc```.
        - Open the service ```Winget Automatic```.
        - Click on the buttons ```Stop``` and then ```Start```.
    - With PowerShell:
        - On PowerShell, as administrator, type: ```Restart-Service -Name WingetAutomaticService```.


## License

This program is licensed under [European Union Public License 1.2](LICENSE).