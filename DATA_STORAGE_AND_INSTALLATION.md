# Social Sentry - Data Storage and Installation Details

This document outlines where **Social Sentry** stores user data, how it is installed, and where it resides on the user's system.

## 1. Application Installation Location

When compiled as a production-level EXE installer (using tools like Inno Setup, WiX, or Squirrel), the application binaries are typically installed in one of the following locations:

*   **Per-User Installation (Default for most modern apps):**
    *   Path: `%LocalAppData%\Programs\Social Sentry`
    *   Example: `C:\Users\JohnDoe\AppData\Local\Programs\Social Sentry`
    *   *Benefits:* Does not require Administrator privileges to install or update.

*   **Machine-Wide Installation:**
    *   Path: `%ProgramFiles%\Social Sentry` (or `%ProgramFiles(x86)%`)
    *   Example: `C:\Program Files\Social Sentry`
    *   *Requires Administrator privileges.*

**Current Native Build Output:**
When you build the source code directly, the EXE is located in the `bin` directory:
`...\Social Sentry\Social Sentry\bin\Debug\net8.0-windows\`

## 2. User Data Storage (The "Recorded Options")

The application stores user data in two specific hidden system folders created for the user. This separates the *application code* from the *user data*, ensuring data persists even if the app is updated.

### A. The Main Database (Activity Logs, Rules, Limits)
**Location:** `%LocalAppData%\SocialSentry`
*   **Full Path:** `C:\Users\<Username>\AppData\Local\SocialSentry\sentry.db`
*   **Format:** SQLite Database (`.db`)
*   **What is stored here:**
    *   **`ActivityLog` Table:** Detailed second-by-second records of every app and website visited.
        *   `Timestamp`: When it happened.
        *   `ProcessName`: Example: `chrome`, `winword`.
        *   `WindowTitle`: Example: `Facebook - Google Chrome`.
        *   `Url`: Example: `https://www.facebook.com/`.
        *   `DurationSeconds`: How long the user was active there.
    *   **`DailyStats` Table:** Aggregated summaries for dashboards (e.g., "2 hours on Social Media today").
    *   **`Rules` Table:** Blocking rules, prohibited keywords (e.g., "porn", "gambling"), and time limits.
    *   **`Settings` Table:** Internal configuration and encryption keys.

### B. User Preferences (Settings)
**Location:** `%AppData%\SocialSentry` (Roaming)
*   **Full Path:** `C:\Users\<Username>\AppData\Roaming\SocialSentry\settings.json`
*   **Format:** JSON Text File
*   **What is stored here:**
    *   UI preferences (e.g., "Start Minimized", "Dark Mode").
    *   Toggle states (e.g., "Start with Windows").

## 3. System Integration

### Start with Windows
To ensure the app runs automatically when the computer turns on, a Registry entry is created:
*   **Registry Key:** `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
*   **Value Name:** `SocialSentry`
*   **Value Data:** Path to the executable (e.g., `C:\...\Social Sentry.exe`)

## 4. Security & Encryption (Privacy First)

To ensure that strictly **only** the Social Sentry application can read your data, all sensitive information is encrypted.

### Database Encryption
The SQLite database stores activity logs. To respect user privacy, the following fields are encrypted using **AES-256** before being saved:
*   `ProcessName`
*   `WindowTitle`
*   `Url`
*   `Category`
*   `Rule.Value`

Even if someone copies the `sentry.db` file, they cannot see which websites or apps were used without the unique encryption key generated on your device.

### Settings Encryption
The `settings.json` file is also fully encrypted. It is no longer stored as plain text.

### Key Management
*   A unique **Master Key** is generated on the first run.
*   This key is protected using **Windows DPAPI (Data Protection API)** (`CurrentUser` scope).
*   This means the key file can only be decrypted by **your specific Windows User Account** on **your specific machine**. Copying the key to another computer will render it useless.

## Summary for Production

When you distribute the **Social Sentry Installer**:

1.  **Binaries** go to `%LocalAppData%\Programs\Social Sentry`.
2.  **Tracking Data** goes to `%LocalAppData%\SocialSentry\sentry.db` (**Encrypted**).
3.  **Config** goes to `%AppData%\SocialSentry\settings.json` (**Encrypted**).

**Note:** If the user uninstalls the application, standard uninstallers often *leave* the AppData folders behind (to preserve data for re-installation) unless explicitly script to delete them. To fully wipe all traces, the uninstaller must specifically delete the `SocialSentry` folders in both Local and Roaming AppData.
