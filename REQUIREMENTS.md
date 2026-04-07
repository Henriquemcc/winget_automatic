# Requirements

Description: A Windows service that runs in the background automatically updating installed applications on Windows using [WinGet](https://learn.microsoft.com/windows/package-manager/winget/).

## Functional Requirements

- The program must run in the background and automatically installs updates.
- The updates must be installed silently with no interaction of the user.
- The program must detect whether the system is shutting down and safely finish the installation of the update before shutting down.
- The program must allow the user to specify the delay time between each update check.
- The program must allow the user to specify which packages to do not update.
- The program must allow the user to specify whether to reboot the system: never, when necessary, always.

## Non-Functional Requirements

- The program must be written in C# for DotNet Core.
- The program must use WinGet to check and install updates.
- All configuration must be stored as JSON in a folder inside 'Program Data'.
- There must be a CI/CD integration (with GitHub Actions) that checks the program and generates the artifact whan a new tag is created. The artifact is:
    - A .MSI installation package for Windows.

