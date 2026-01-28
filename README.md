# Social Sentry PC

A powerful Windows native application designed for digital well-being, parental control, and comprehensive system monitoring. Social Sentry PC helps users track screen time, manage app usage, and enforce digital boundaries with advanced blocking and filtering capabilities.

## 📥 Download
Download the latest version from our **[GitHub Releases](https://github.com/mkshaonexe/Social-Sentry-PC-/releases/latest)** page.

### 🚀 Release Notes v1.0.1.0
- **Unified Branding**: Updated application logo across all components (Installer, Taskbar, App).
- **UI Enhancements**: Improved Dark Mode consistency and Settings UI.
- **Performance**: Optimized Dashboard charts and data visualization.
- **Fixes**: Resolved minor UI glitches in the "Limit" section.
- **Maintenance**: Fixed build errors (ambiguous references) for smoother development.

## ✨ Features

### 🔍 Real-Time Activity Tracking
- **Granular Monitoring**: Tracks every second of activity across applications and browsers.
- **Smart Detection**: Captures Process Name, Window Title, and specific URLs (Chrome, Edge, Firefox, Brave).
- **Idle Detection**: Automatically pauses tracking when the user is inactive to ensure data accuracy.
- **Privacy Mode Support**: Detects and respects private/incognito windows.

### 🛡️ Advanced Blocking & Filtering
- **Application Blocking**: Prevent specific applications from running (e.g., games during work hours).
- **URL Filtering**: Block access to specific websites or sub-URLs (e.g., block `/reels/` but allow `facebook.com`).
- **Keyword Filtering**: Block content based on window titles or generic keywords.
- **Persistent Adult Blocker**:
    - **Dual-Layer Protection**: Base protection blocks over 150,000 domains from `adult_blocklist.txt` and `adult_hosts.txt` regardless of settings.
    - **Smart Toggle**: The "Adult Blocker" specific toggle controls strict Keyword and Title detection.
    - **Auto-Lock**: If the Adult Blocker is disabled, it automatically re-enables itself after 5 minutes to prevent leaving protections off.
- **Time Limits**: Set daily usage limits for categories or specific apps.

### 📊 Analytics & Insights
- **Dashboard**: Visual breakdown of your daily habits with charts and graphs.
- **Detailed Logs**: View raw activity logs to see exactly where time is spent.
- **Intelligent Categorization**: 
    - **Smart Logic**: Automatically detects "Doom Scrolling" (Shorts/Reels) separate from general "Entertainment".
    - **Context Awareness**: Identifies "Study" sessions based on window titles (e.g., "Math Lecture").
    - **Modern UI**: Revamped Categories view with Hero Cards and sleek animations.
- **Enhanced Dashboard**:
    - **Historical Views**: Analyze screen time trends by Day, Week, or Month.
    - **Interactive Charts**: Drill down into hourly or daily usage with precision.
    - **Data Integrity**: Self-healing statistics engine ensures accurate graphs even after crashes.
    - **Session Logic**: Improved session coalescing (merges short breaks) and buffering (ignores accidental clicks).
- **Categorization**: Group apps and sites into categories (e.g., Productivity, Social Media, Games).
- **Branding**: Unified high-resolution application logo across all platforms (Windows App, Taskbar, Installer, and Browser Extension).
- **🏆 Ranking System**: 
    - **No-Fap / Discipline Tracker**: Tracks streak days and awards badges from "Clown" to "Absolute Giga Chad".
    - **Visual Progress**: Dynamic dashboard showing current rank and progress to next level.
- **💬 Community & Social**:
    - **Global Chat**: Real-time chat with other users to share progress and motivation.
    - **Cloud Sync**: Powered by Supabase for instant message delivery and profile synchronization.
- **👑 Prime Mode**:
    - **Strict Enforcement**: Hardcore mode that prevents exiting the app or opening distractions.
    - **Focus Lock**: Maximizes productivity by eliminating all escape routes.
- **🤖 AI Hakari & Rich Notifications**:
    - **Waifu Personality**: "Hakari" - an AI companion that reacts to your screen time habits.
    - **Smart Check-ins**: Hourly notifications with varied personalities (Gentle to Tsundere) based on usage intensity.
    - **Rich Toast Notifications**: Native Windows toast notifications with actions and visual stats summaries.
    - **Roast & Praise**: Gets stricter as you waste more time, or praises you for staying productive.

## 🛠️ How It Works (Technical Implementation)

Social Sentry PC leverages low-level Windows APIs to provide robust and low-overhead monitoring.

### 1. Window Event Hooking (`SetWinEventHook`)
The core tracking engine uses the Windows API `SetWinEventHook` to listen for system-level events:
- `EVENT_SYSTEM_FOREGROUND`: Detects when the user switches to a different window.
- `EVENT_OBJECT_NAMECHANGE`: Detects when a window's title changes (essential for browser tab tracking).

This event-driven approach is far more efficient than constant polling, reducing CPU usage to a minimum.

### 2. Browser URL Extraction (`UIAutomation`)
To get granular data from modern web browsers without extensions, we use the **Microsoft UI Automation API**.
- When a known browser is active, the app inspects the UI tree to find the "Address Bar" or "Omnibox" element.
- It securely reads the current URL to log specific site usage.
- **Supported Browsers**: Google Chrome, Microsoft Edge, Mozilla Firefox, Brave.

### 3. Local Database (SQLite)
All data is stored locally on the user's machine to ensure privacy.
- **Technology**: `Microsoft.Data.Sqlite`
- **Schema**:
    - `ActivityLog`: Stores raw second-by-second events.
    - `DailyStats`: Aggregated data for fast dashboard rendering.
    - `Rules`: Configuration for blocking and limits.
    - `Settings`: Encrypted application settings.

### 4. Self-Protection (Watchdog)
(Planned/In-Progress)
A watchdog service ensures the core monitoring process cannot be easily terminated by unauthorized users, providing parental control-grade security.

## 📂 Project Structure

- **`Social Sentry/`**: Main WPF application.
    - **`Services/`**: Core logic providers.
        - `ActivityTracker.cs`: Handles low-level hooks and monitoring.
        - `BlockerService.cs`: Enforces block rules.
        - `DatabaseService.cs`: Manages SQLite connections and schema.
    - **`Views/`**: UI components (MVVM pattern).
    - **`Resources/`**: Styles, themes, and assets.
- **`mastersqlalinhere/`**: Contains the `mastersqlalinhere.txt` backup SQL file for database recovery.

## 📦 Creating the Installer (The Easy Way)

To create a shareable `.msi` installer for your friends or customers:

1.  Right-click `build_installer.ps1` in the project root.
2.  Select **"Run with PowerShell"**.
3.  Wait for the script to finish (it will look for WiX and build the app).
4.  The installer will appear in `Installer/SocialSentry.msi`.

## 🚀 Getting Started

### Prerequisites
- Windows 10 or 11
- .NET 8 Desktop Runtime
- Visual Studio 2022 (for development)

### Installation
1. Clone the repository.
   ```bash
   git clone https://github.com/yourusername/Social-Sentry-PC.git
   ```
2. Open `Social Sentry.sln` in Visual Studio.
3. Restore NuGet packages.
4. Build and Run.

## 🤝 API Usage

Currently, Social Sentry PC operates as a standalone desktop client. However, the `DatabaseService` can be queried externally if needed for custom status dashboards.

**Example: Querying Usage Time**
```sql
SELECT ProcessName, SUM(DurationSeconds) as TotalTime 
FROM ActivityLog 
WHERE Timestamp >= date('now') 
GROUP BY ProcessName 
ORDER BY TotalTime DESC;
```

## 📄 License
All rights reserved.
