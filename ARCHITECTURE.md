# Social Sentry - Technical Overview & Architecture

This document details the internal working of the **Social Sentry** Windows application. It explains how the code detects activity, monitors URLs, blocks content, and stores data.

## 1. High-Level Architecture
The application is built on **.NET 8** using **WPF (Windows Presentation Foundation)** for the UI. It relies heavily on **Win32 APIs** and **Microsoft UI Automation** to interact with the Windows Operating System.

### Core Components
*   **UI Layer (`MainWindow.xaml`)**: Displays the dashboard and receives events.
*   **Service Layer**: Background classes that perform the heavy lifting.
    *   `ActivityTracker`: The "Heartbeat" of the system.
    *   `BrowserMonitor`: The "Eyes", looking into browsers.
    *   `BlockerService`: The "Enforcer", blocking restricted content.
    *   `NativeMethods`: The "Hands", interacting with low-level Windows functions.
*   **Data Layer**: Uses **SQLite** to persist activity logs.

---

## 2. Detailed Data Flow

### Step 1: The Heartbeat (`ActivityTracker.cs`)
*   **How it works**: A `System.Timers.Timer` triggers constantly (every 1 second).
*   **Action**: It calls `NativeMethods.GetForegroundWindow()` to find the "handle" (ID) of the window the user is currently looking at.
*   **Result**: It extracts the **Process Name** (e.g., "chrome.exe") and **Window Title** (e.g., "Facebook - Google Chrome").
*   **Trigger**: If the window has changed or time has passed, it fires an `OnActivityChanged` event.

### Step 2: Inspection (`BrowserMonitor.cs`)
*   **Trigger**: The `MainWindow` receives the activity event. It checks if the app is a browser (Chrome, Edge, Firefox).
*   **How it works**:
    *   It uses **Microsoft UI Automation** (`AutomationElement.FromHandle`).
    *   It scans the visual tree of the browser window looking for the **Address Bar** (ControlType.Edit).
    *   It reads the text value of the address bar to get the **URL**.
*   **Performance**: This is resource-intensive, so we only do it when the user is actually in a browser.

### Step 3: Enforcement (`BlockerService.cs`)
*   **Input**: Receives Process Name, Title, and URL.
*   **Logic**: It checks these against a list of "Blacklisted" keywords (e.g., "porn", "Reels", "Shorts").
*   **If Blocked**:
    1.  **Input Simulation**: It uses `NativeMethods.SendInput` to simulate the physical keyboard press of **Ctrl + W**.
    2.  **Visual Feedback**: It immediately shows a `BlockOverlayWindow` (a red "Access Denied" screen) on top of everything.
    3.  **Result**: The browser tab closes instantly, and the user sees the warning.

### Step 4: Storage (`DatabaseService.cs`)
*   **Database**: A local SQLite file `sentry.db` stored in `%LOCALAPPDATA%\SocialSentry\`.
*   **Logging**: Valid (non-blocked) activity is inserted into the `ActivityLog` table with a timestamp.
*   **Backup**: The schema is defined in `mastersqlalinhere.txt`.

---

## 3. Codebase Structure

### `/Services`
*   `NativeMethods.cs`: Contains `DllImport` definitions. This is the bridge between C# and Windows C++ APIs.
*   `ActivityTracker.cs`: Manages the main monitoring loop.
*   `BrowserMonitor.cs`: Handles complex UI Automation to read URLs.
*   `BlockerService.cs`: Contains the blocking rules and `SendInput` logic.

### `/Views`
*   `MainWindow.xaml`: The main control panel.
*   `BlockOverlayWindow.xaml`: The transparent red overlay window used for notifications.

### `/Data`
*   `DatabaseService.cs`: Handles all SQLite connections and queries.

---

## 4. How to Extend
*   **Add New Block Rules**: Edit `BlockerService.cs` (arrays `_blockedKeywords`, etc.) or load them from the Database.
*   **God Mode**: Currently, the app runs as a standard user process. To prevent termination, the logic needs to be moved to a **Windows Service** (Session 0) or use a "Watchdog" process (Phase 3).
