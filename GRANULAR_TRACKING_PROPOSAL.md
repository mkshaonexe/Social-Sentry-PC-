# Granular Activity Tracking & Context Awareness Proposal

This document details the technical approach to transforming **Social Sentry** from a basic usage tracker into a context-aware productivity assistant.

## 1. Current State ("What We Have Now")
Currently, the application captures a linear stream of "Active Window" events.

*   **Data Points**:
    *   `Process Name` (e.g., `chrome.exe`, `devenv.exe`).
    *   `Window Title` (e.g., "Social Sentry - Cursor", "YouTube - Google Chrome").
    *   `URL` (Captured via UI Automation for Chrome/Edge/Firefox/Brave).
    *   `Timestamp` & `Idle State` (Active vs. Idle).
*   **Mechanism**:
    *   Uses `SetWinEventHook` to detect when the foreground window changes.
    *   Uses `UIAutomation` to scrape the address bar from browser windows.
    *   Uses `GetLastInputInfo` to detect global idleness.

## 2. The Goal: "Deep Context" & Categorization
The objective is to understand *what* the user is actually doing within these applications:
*   **Context**: Is the user *watching* a tutorial, *scrolling* a feed, *writing* code, or *AFK* (Away From Keyboard)?
*   **Categorization**: "Study", "Entertainment", "Productivity", "Gaming".
*   **Analytics**: "5 hours total: 60% Study, 30% Entertainment".

## 3. Technical Implementation Strategy

### A. Deep Context Extraction (The "How")

To go beyond simple titles, we need to leverage specific APIs and Heuristics.

#### 1. Media Playback Detection (Watching Videos/Movies)
To detect if a user is passively watching a video (Youtube, Netflix, local player) vs. reading:
*   **API**: `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`.
*   **Logic**:
    *   Request the current media session.
    *   Check `GetPlaybackInfo()`. If `PlaybackStatus == Playing`, the user is likely consuming media.
    *   **Context**: Match the media source (e.g., "Chrome") with the active window.
    *   **Result**: If `Process=chrome` AND `Title="Python Tutorial"` AND `Media=Playing`, then **Activity = Watching Video**.

#### 2. Scrolling & Interaction (Reading vs. Random Browsing)
*   **Heuristic**:
    *   **Reading/Studying**: consistent, slow scroll/input patterns (or static page with low idle time) + specific domains (StackOverflow, Documentation).
    *   **Social Scrolling**: frequent rapid scrolling + short dwell times + domain (Facebook, Instagram, Twitter/X).
*   **Implementation**: 
    *   Enhance `IdleDetector` or use global hooks (carefully for privacy) to detect "Scroll Wheel" intensity.
    *   *Simpler Alternative*: Categorize strictly by Domain + Path.
        *   `twitter.com/home` = **Doom Scrolling** (Entertainment).
        *   `twitter.com/some_useful_dev_thread` = **Research** (Productivity).

#### 3. Categorization Engine (The "Brain")
We need a robust classification system. This cannot be hardcoded strings; it needs a logic engine.

*   **Structure**: 
    *   **Category**: High-level (Productivity, Entertainment, Study, System).
    *   **Tag**: Specific (Coding, Social Media, Movie, Documentation).
*   **Logic Hierarchy**:
    1.  **Process Match**: `devenv.exe`, `Code.exe` -> **Productivity (Coding)**.
    2.  **Domain Match**: `github.com`, `stackoverflow.com` -> **Productivity (Dev)**.
    3.  **Title Keyword Match**: 
        *   Title contains "Tutorial" + Domain "youtube.com" -> **Study**.
        *   Title contains "Funny" + Domain "youtube.com" -> **Entertainment**.
    4.  **AI/Smart Classification** (Future): Send title/URL to a local LLM or small classification API to guess category.

### B. Updated Data Schema (Supabase/Local)

To support this, the database schema needs expansion.

*   **`activities_log`** (Existing + New Fields)
    *   `activity_type`: Enum (`ActiveWork`, `PassiveConsumption`, `Gaming`, `Idle`).
    *   `media_playing`: Boolean (Is audio/video playing?).
    *   `category_id`: Foreign Key.

*   **`categories`** (New Table)
    *   `id`: UUID.
    *   `name`: String ("Productivity", "Entertainment").
    *   `color_hex`: String (For dashboard UI).
    *   `is_productive`: Boolean.

*   **`classification_rules`** (New Table)
    *   `type`: Enum (`Process`, `Domain`, `TitleKeyword`).
    *   `match_string`: String ("youtube.com").
    *   `category_id`: UUID.
    *   `priority`: Integer.

> **Note**: As per project rules, any SQL changes will be documented in `mastersqlalinhere.txt`.

## 4. Dashboard & visual Analytics
**"Last 5 Hours Breakdown"**

*   **Data Aggregation**:
    *   Query `activities_log` for `timestamp > NOW() - INTERVAL '5 hours'`.
    *   Group by `category_id`.
    *   Sum `(next_event_time - current_event_time)` duration.
    *   Calculate Percentage: `(CategoryDuration / TotalDuration) * 100`.

*   **Visuals**:
    *   **Donut Chart**: Inner ring = Total Time, Segments = Categories (colored by `category.color_hex`).
    *   **Timeline Strip**: A horizontal bar showing the day's flow:
        `[Coding (Blue)] [======] [YouTube (Red)] [==] [Coding (Blue)]`
    *   **Productivity Score**: `(Time in 'is_productive' categories / Total Time) * 100`.

## 5. Implementation Roadmap

### Phase 1: Enhanced Detection (C#)
1.  **Add Media Listener**: Integrate `Windows.Media.Control` to detect "Video Watching" state.
2.  **Refine Browser Monitor**: Improve URL capture stability (already in progress).

### Phase 2: Classification System (Logic)
1.  Create the `RuleEngine` class.
2.  Load default rules (e.g., `youtube.com` -> Entertainment, `VS Code` -> Productivity).
3.  Process every `ActivityEvent` through `RuleEngine.Categorize(event)` before saving.

### Phase 3: Reporting (UI)
1.  Create `CategoryBreakdown` SQL view or logic.
2.  Build the "Analytics" Dashboard tab.
3.  Add "Productivity vs. Entertainment" charts.

---

## 6. Feasibility & Limitations

*   **Possibilities**:
    *   **High Accuracy**: For known apps (VS Code, Word) and specific websites.
    *   **Media Detection**: Very reliable on Windows 10/11 for modern browsers.
    *   **Privacy**: All logic runs locally; no data leaves the machine unless synced to user's own Supabase.
*   **Limitations**:
    *   **"Study" vs "Fun" on YouTube**: Hard to distinguish without specific title keywords. AI analysis of the title is the best bet here.
    *   **Incognito Mode**: Browser restrictions may prevent identifying *what* video is playing in Incognito, though we can still see the browser is active.
    *   **Scrolling vs Reading**: "Reading" looks exactly like "Idle" to the computer if the user doesn't scroll often. We assume "Active Window + Media Not Playing + Mouse Jiggle occasionally" = Reading.
