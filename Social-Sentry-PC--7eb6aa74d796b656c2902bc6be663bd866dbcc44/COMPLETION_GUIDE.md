# Social Sentry PC - Completion Guide

This document outlines the steps required to bring Social Sentry PC from its current "Alpha/Prototype" state (approx. 60%) to a production-ready "Release Candidte".

## 1. Security & Zero Trust (Critical Priority)

### 1.1 Database Encryption Validation
Currently, the code leverages both **Column-Level Encryption** (via `EncryptionService`) and attempts **Full Database Encryption** (via `SQLCipher` password).
*   **Action**: Verify `SQLCipher` is actually active.
    *   *Test*: Try opening the `.db` file in "DB Browser for SQLite". It should **fail** to open without the password.
    *   *Optimization*: If Full DB Encryption works, you can optionally remove manual column encryption (`_encryptionService.Encrypt`) to improve performance (Dashboard loading speed), or keep it for "Defense in Depth".
*   **Code Location**: `Social Sentry/Data/DatabaseService.cs`

### 1.2 Anti-Tamper Mechanism (Watchdog)
The current `SocialSentry.Watchdog` only restarts the app if it closes. Smart users can kill the Watchdog first.
*   **Action**: Implement Access Control Lists (ACLs).
    *   Use `GetKernelObjectSecurity` / `SetKernelObjectSecurity`.
    *   Deny `PROCESS_TERMINATE` rights to the "Everyone" group for the `Social Sentry` process.
    *   *Note*: This requires the app to run as **Administrator**.
*   **Code Location**: `SocialSentry.Watchdog/Program.cs` needs to become a Windows Service or a high-privilege background task.

## 2. Browser Extension Integration (Functional Priority)

The PRD mentions the extension is "Partial".
*   **Action**: Ensure robust communication.
    *   The `LocalApiServer.cs` must robustly handle `POST` requests from the extension.
    *   Handle "Incognito" mode detection (manifest `incognito: "split"` or `"spanning"`).
    *   Implement "Heartbeat" to detect if the extension is disabled/removed.

## 3. UI/UX Polish (Digital Wellbeing)

*   **Action**: Improve the Dashboard.
    *   Current UI is likely basic. Implement Chart.js or LiveCharts for "Time per App".
    *   Add "Focus Mode": A button to instantly block all "Distracting" apps for X minutes.

## 4. Updates & Recovery
*   **Action**: Implement Auto-Update.
    *   Integrate `Velopack` or `Squirrel.Windows` to handle background updates from a GitHub Release.
