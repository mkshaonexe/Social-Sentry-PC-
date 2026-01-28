# Social Sentry: Deep Dive into Categorization Logic

This document answers the question: **"How does the system know that 'Reels' is 'Doom Scrolling' and 'VS Code' is 'Productive'?"**

It details the **exact logic path** from the moment you switch windows to when it appears on the dashboard, along with the rules, pitfalls, and improvement plans.

---

## 1. The Logic Flow (The "Golden Path")

When you switch between windows or tabs, the data flows through 4 distinct stages:

### Stage 1: The Sensor (Detecting *What* You Are Doing)
**File:** `ActivityTracker.cs`
The system is always listening.
1.  **Window Switch**: You click on a window. `SetWinEventHook` fires.
2.  **Raw Data Capture**:
    *   **Process Name**: `chrome`
    *   **Window Title**: `YouTube - Calculus II Lecture 1 - Google Chrome`
    *   **Extension Data** (If active): `URL: youtube.com/watch?v=123`, `Category: Video`, `ContentType: "Video"`

### Stage 2: Context Enrichment (Making It "Smart")
**File:** `UsageTrackerService.cs` -> `DetermineCategory()`
Before looking up rules, the system checks for **Extension Data** which acts as a "Super Override".

**The Logic:**
```csharp
if (ContentType == "Shorts" OR ContentType == "Reels") 
    -> RETURN "Doom Scrolling" (IMMEDIATELY)

if (Title contains "Study" OR "Lecture" OR "Thesis") 
    -> RETURN "Study"

if (URL contains "youtube.com" AND NOT Study) 
    -> RETURN "Entertainment"
```
*   **Why this matters**: This is why a YouTube video can be "Study" while YouTube Shorts is always "Doom Scrolling". The logic splits the same website into two different categories based on the *content*.

### Stage 3: The Rule Engine (The "Fallback Brain")
**File:** `ClassificationService.cs`
If the Extension didn't already force a category, we check the **Local Database Rules**. The system takes your window title and process name, combines them, and runs a pattern match.

> **Input String**: `"code visual studio code - myproject"`

**The Match Loop:**
1.  It checks every rule in the database.
2.  Highest **Priority** wins.

| Pattern | Category | Match Type | Priority | Result |
| :--- | :--- | :--- | :--- | :--- |
| `visual studio` | **Productive** | Contains | 20 | ✅ Match! |
| `code` | **Productive** | Contains | 15 | (Ignored, lower priority) |

**Result**: The category is **"Productive"**.

### Stage 4: The UI Grouper (Showing It to You)
**File:** `CategoryViewModel.cs`
The dashboard doesn't just show every random category found in the database. It enforces a strict **Presentation Order** for the main buckets:

1.  **Entertainment**
2.  **Productive**
3.  **Study**
4.  **Doom Scrolling**
5.  **Communication**

If an app falls into "Uncategorized" or a custom category (e.g., "Finance"), it is added to the end of the list.

---

## 2. The Hardcoded "Truth" (Default Rules)

These are the rules currently baked into the system (`ClassificationService.cs`). If you reset the database, these are what load.

### 🔴 Doom Scrolling (Priority: 100 - Highest)
*These rules are designed to catch distractions immediately.*
*   `shorts`
*   `reels`
*   `tiktok`

### 🟣 Study (Priority: 80-90)
*   `study`, `lecture`, `tutorial`, `course`, `assignment`, `thesis`, `research`
*   `math`, `physics`, `chemistry`

### 🔵 Productive (Priority: 15-20)
*   `visual studio`, `code`, `word`, `excel`, `powerpoint`, `notion`, `obsidian`, `trello`, `jira`, `slack`

### 🟢 Communication (Priority: 15)
*   `discord`, `whatsapp`, `telegram`, `messenger`, `skype`, `zoom`, `teams`, `outlook`

### 🟠 Entertainment (Priority: 5-10)
*   `youtube`, `netflix`, `steam`, `game`
*   **General Catch-All**: `(Media)` context detection.

---

## 3. Where It Can Go Wrong (Mistakes & Limitations)

1.  **The "Gaming" Blindspot**:
    *   **Issue**: Most games are just `.exe` files with random names (e.g., `eldenring.exe`). If the rule "eldenring" isn't in the database, it shows as `Uncategorized`.
    *   **Impact**: Your gaming time might not count as "Entertainment".

2.  **The "Productive" YouTube Problem**:
    *   **Issue**: If you watch a coding tutorial but the title is "Learn React Fast" (no "tutorial" keyword), it defaults to **Entertainment** (YouTube rule).
    *   **Impact**: Productive learning time is counted as wasted entertainment time.

3.  **False "Doom Scrolling"**:
    *   **Issue**: If you work on a project with the file name `shorts_generator.py`, the filename contains "shorts".
    *   **Result**: VS Code might incorrectly be flagged as "Doom Scrolling".
    *   **Fix**: The "Productive" rule for `visual studio` needs higher priority than `shorts`, OR matching should be smarter (Process Name vs Window Title).

4.  **Browser Variance**:
    *   **Issue**: If the extension is disabled, we rely *only* on Window Titles. "Instagram" titles often don't say "Reels", so it might just show as generic "Instagram" (which might default to Communication or Entertainment, missing the "Doom Scrolling" nuance).

---

## 4. How We Can Improve It (Roadmap)

To fix the mistakes above, here is the technical roadmap:

### ✅ Improvement 1: Smart Priority Adjustments
*   **Action**: Ensure application-specific rules (VS Code) always have higher priority than generic keywords (Shorts).
*   **Logic**:
    ```csharp
    // VS Code Rule
    { Pattern: "visual studio", Priority: 200 } 
    // Shorts Rule
    { Pattern: "shorts", Priority: 100 }
    ```
    *Result*: `shorts_generator.py` in VS Code is now **Productive** properly.

### ✅ Improvement 2: Cloud Game Database
*   **Action**: Instead of hardcoding 5 games, query the [IGDB API](https://www.igdb.com/api) or a community list of common game Executables (`steam.exe`, `csgo.exe`, `valorant.exe`).
*   **Benefit**: "Valorant" automatically becomes "Entertainment" without you adding a rule.

### ✅ Improvement 3: User Categorization UI
*   **Action**: Right-click an activity in "Recent Usage" -> "Always categorize 'Blender' as 'Productive'".
*   **Code**: This would insert a new rule into the local SQLite database with `Priority: 1000` (User Override).

### ✅ Improvement 4: AI Text Classification (The "Brain")
*   **Action**: Use a local ML model (like ML.NET or a small BERT model) to classify text.
*   **Example**:
    *   Input: *"How to center a div - Stack Overflow"*
    *   Regex: No match.
    *   AI: Detecs "Technical/Dev" -> **Productive**.

---

## Summary
The system is **Deterministic**: It always follows the rules.
1.  **Extension** overrides everything (100% confidence).
2.  **High Priority Keywords** override low ones.
3.  **Database Rules** fill in the rest.

If something is categorized wrong, it is because **a rule is missing** or **priority is wrong**.
