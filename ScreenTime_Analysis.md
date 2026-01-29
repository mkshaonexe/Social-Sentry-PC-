# Social Sentry: Screen Time Calculation & Categorization Logic

This document details the technical architecture behind the screen time tracking and automatic categorization system in **Social Sentry Desktop**. It analyzes the categorization algorithms, assesses the current system's strength, and proposes specific improvements.

---

## 1. How It Works: The Core Logic

The Social Sentry tracking engine operates on a hybrid model that combines **OS-level hooks** with **browser extension intelligence** to ensure high fidelity data tracking.

### A. Activity Capture (The "Sensors")
The system uses a multi-layered approach to capture user activity:

1.  **Native Window Hooks (`ActivityTracker.cs`)**:
    *   **Foreground Detection**: Uses `SetWinEventHook` with `EVENT_SYSTEM_FOREGROUND` to detect exactly when the active window changes.
    *   **Title Monitoring**: Uses `EVENT_OBJECT_NAMECHANGE` to detect when the title of the *current* window changes (e.g., switching tabs in Chrome changes the window title).
    *   **Debouncing**: A 250ms debounce timer prevents system overload during rapid window switching.
    *   **Idle Detection**: An `IdleDetector` pauses tracking if there is no mouse or keyboard input for a set duration, ensuring "Screen Time" equals "Active Time".

2.  **Browser Intelligence (`BrowserMonitor` & Extension)**:
    *   **URL Extraction**: For browsers, it attempts to extract the URL via UI Automation.
    *   **Extension Data**: If the user has the Social Sentry extension installed, it pushes "rich" data (exact URL, content type like "Shorts" or "Reels") to the desktop app. This allows the system to distinguish between *Productive YouTube* (Tutorials) and *Distracting YouTube* (Shorts).

### B. Time Calculation (`UsageTrackerService.cs`)
Time is not just a counter; it is calculated using a **Session-based** approach:

1.  **Duration Calculation**: `Duration = Now - LastSwitchTime`.
2.  **Noise Filtering (Buffering)**: Sessions shorter than **2 seconds** are discarded. This filters out accidental Alt-Tabs or passing glances at windows.
3.  **Session Coalescing**: If a user switches away from an app and returns within **5 seconds**, it is treated as a continuous session. This smooths out the data log.

### C. Automatic Categorization Logic (`ClassificationService.cs`)
This is the "Brain" of the system. It takes the raw data and assigns it a category (e.g., "Productive", "Doom Scrolling").

1.  **Input Normalization**:
    The system concatenates the Process Name, Window Title, and **URL** (if available):
    > `Input = (ProcessName + " " + WindowTitle + " " + URL).ToLower()`

2.  **Rule-Based Matching**:
    The input is checked against a database of **Classification Rules**. Each rule has a:
    *   **Pattern**: The keyword or Regex to look for (e.g., "visual studio", "shorts").
    *   **Category**: The target bucket (e.g., "Productive", "Doom Scrolling").
    *   **Priority**: A score (0-100) determining which rule wins.

3.  **Priority Resolution**:
    High-priority rules override low-priority ones.
    *   *Example*: A window titled "Visual Studio Code - Making a Game"
        *   Rule "game" (Entertainment) has Priority 5.
        *   Rule "code" (Productive) has Priority 15.
        *   **Result**: "Productive" wins.

4.  **Extension Overrides**:
    Data from the browser extension bypasses some text matching for 100% accuracy.
    *   If `ContentType == "Shorts"`, it is **forcefully** categorized as "Doom Scrolling".

---

## 2. Current Implementation Strength

The current implementation is **Robust and Production-Ready** for 95% of use cases. It is significantly more advanced than basic time trackers.

### Strengths
*   **Context-Aware**: Unlike basic trackers that just see "Chrome", Social Sentry sees "YouTube Shorts" vs. "Coursera". This is achieved by the "Smart Process Naming" logic in `UsageTrackerService` and **URL-based categorization**.
*   **High Accuracy**: The combination of `WinEventHook` (low latency) and Idle Detection eliminates "fake" screen time.
*   **Smart Defaults**: The seed rules cover the most common use cases immediately (VS Code, Office, Social Media, Games).
*   **Priority System**: The implementation of a priority integer allows for nuanced rules where specific contexts (Study) can correctly override general ones (Entertainment).
*   **Performance**: The use of `ConcurrentDictionary` and background database writes ensures the UI never freezes, even when processing rapid activity changes.

### Scale of "Smartness"
*   **Current Level**: **Level 3 (Rule-Based Heuristics)**.
    *   It knows that "VS Code" is productive.
    *   It knows that "YouTube" is entertainment *unless* the title contains "Lecture".
    *   It is *not* yet Level 4 (AI/Semantic Understanding), relying on keyword matches.

---

## 3. Proposed Improvements

To elevate the software from "Smart" to "Intelligent", the following improvements are recommended:

### A. Semantic Categorization (AI Integration)
**Limitation**: Currently, if a user watches a video titled "Advanced Thermodynamics", the system only knows it's "Study" if the word "Study" or "Lecture" is in the title.
**Solution**:
*   Implement a lightweight **local NLP classifier** (or an API call to a small model).
*   Instead of `input.Contains("keyword")`, send the title to the model: `Classify("Advanced Thermodynamics")` -> Returns `Education`.
*   **Benefit**: Catches thousands of productive tasks without needing thousands of regex rules.

### B. User Feedback Loop (Self-Correcting System)
**Limitation**: If the system gets it wrong (e.g., categorizing a specific game tool as "Productive"), the user cannot easily fix it in the current flow.
**Solution**:
*   Add a "Recategorize" option in the detailed activity log.
*   When a user manually changes "FooBar App" from *Uncategorized* to *Productive*, **write a new high-priority rule** to the local database.
*   The system learns the user's specific workflow over time.

### C. Cloud-Based Rule Syncing
**Limitation**: New apps and games come out every day. The local hardcoded list will get stale.
**Solution**:
*   Fetch a `global_classification_rules.json` from a Supabase/GitHub endpoint on startup.
*   This allows the developer to push updates (e.g., adding "Black Myth: Wukong" to the Game category) without requiring a user to update the software binary.

### D. Time-of-Day Context
**Limitation**: Browsing Reddit might be "Entertainment" at 8 PM, but "Doom Scrolling" (Distraction) at 10 AM during work hours.
**Solution**:
*   Add `TimeStart` and `TimeEnd` properties to `ClassificationRule`.
*   Allow rules that only apply during "Focus Hours".

### E. Granular "Distraction" Scoring
**Limitation**: Currently, a category is binary: Distracting or Productive.
**Solution**:
*   Implement a **Distraction Score (0-100)**.
    *   VS Code: 0
    *   YouTube (General): 50
    *   YouTube Shorts: 100
*   This allows for a "Focus Health" metric rather than just a raw time counter.
