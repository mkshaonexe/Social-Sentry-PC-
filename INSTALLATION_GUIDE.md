# Social Sentry PC - Production Installation Guide

This guide explains how to package `Social Sentry PC` for end-users using the **WiX Toolset**.

## Prerequisites
*   Visual Studio 2022 with "WiX Toolset Visual Studio Extension" installed.
*   .NET 8 SDK.
*   A **Code Signing Certificate** (Required for avoidance of "Windows SmartScreen" warnings).

## Step 1: Prepare the Binaries
1.  Open `Social Sentry.sln` in Visual Studio.
2.  Select **Release** configuration.
3.  Right-click `Social Sentry` Project -> **Publish**.
    *   Target: **Folder**
    *   Configuration: **Release | x64**
    *   Deployment Mode: **Self-Contained** (Include .NET Runtime so users don't need to install it).
    *   *Output Path*: `bin\Release\net8.0-windows\publish\`

## Step 2: Configure WiX Installer
The `Installer` project contains `Product.wxs` (or `Package.wxs`).
1.  **Update GUIDs**: Generate new GUIDs for `ProductCode` and `UpgradeCode` in `Package.wxs`.
2.  **File Harvesting**: Use `heat.exe` or manually reference all files from the `publish` folder in Step 1.
    *   *Critical*: Ensure `Social Sentry.exe` and `SocialSentry.Watchdog.exe` are included.
3.  **Shortcuts**: Ensure the installer creates:
    *   Desktop Shortcut
    *   Start Menu Shortcut
    *   Startup Folder Shortcut ( Registry Run Key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) -> **Essential for auto-start**.

## Step 3: Build the MSI
1.  Right-click `Installer` project -> **Build**.
2.  Output: `SocialSentryInstaller.msi`.

## Step 4: Code Signing (Crucial for Sales)
If you sell this, you represent a business. Unsigned EXEs are treated as malware by Windows.
1.  Purchase a standard Code Signing Certificate (Sectigo, DigiCert, etc.) ~ $200-$400/year.
2.  Sign the MSI:
    ```powershell
    signtool sign /f "YourCert.pfx" /p "password" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "SocialSentryInstaller.msi"
    ```

## Step 5: Distribution
*   Host the `.msi` on your sales website.
*   Or store in an AWS S3 bucket / GitHub Releases.
