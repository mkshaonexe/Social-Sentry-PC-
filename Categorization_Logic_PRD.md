# Product Requirements Document (PRD): Social Sentry Categorization Logic Update

## 1. Executive Summary
The goal of this update is to refine the `Social Sentry` desktop application's categorization engine to be more culturally relevant (specifically for Bangladeshi students), technically accurate (recognizing modern AI and Dev tools), and smarter about false positives (preventing file names from triggering "Doom Scrolling").

## 2. Problem Statement
*   **Cultural Gap**: The current system lacks keywords relevant to Bangladeshi students (e.g., "Gonit", "Porashona"), leading to "Uncategorized" study sessions.
*   **Technological Gap**: New AI tools (e.g., "Cursor", "Antigravity", "ChatGPT") are not recognized as "Productive" by default.
*   **False Positives**: Developers working on files like `youtube_shorts_bot.py` or `reels_downloader.cpp` in VS Code are incorrectly flagged as "Doom Scrolling", punishing productivity.
*   **Gaming Blindspot**: Many popular games are not detected.

## 3. Scope of Work

### 3.1. Keyword Expansion (Database Update)
We need to inject 100+ new high-priority rules into the `ClassificationService` database.

#### 3.1.1. Study Category (Priority: 80-90)
*   **Target Audience**: Bangladeshi Students.
*   **Requirement**: Support both English and Transliterated Bangla (Banglish), and Bangla Script if possible.
*   **Keywords**:
    *   **Bangla**: `porashona`, `gonit` (Math), `biggan` (Science), `itihaas`, `bhugol`, `sahitya`, `boi`, `patho`, `shikkha`.
    *   **English**: `thesis`, `research`, `syllabus`, `assignment`, `lecture`, `tutorial`.
    *   **Quantity**: ~50 Bangla/Banglish terms + ~50 English terms.

#### 3.1.2. Productive Category (Priority: 15-20)
*   **Target**: Modern Developers & Knowledge Workers.
*   **Requirement**: Recognize the latest AI agents and IDEs.
*   **Keywords**:
    *   **AI**: `chatgpt`, `claude`, `gemini`, `copilot`, `midjourney`, `antigravity`, `perplexity`.
    *   **IDEs**: `cursor`, `windsurf`, `visual studio`, `vscode`, `jetbrains`, `xcode`, `android studio`.
    *   **Quantity**: Top 20 AI tools + Top 20 IDEs.

#### 3.1.3. Entertainment Category (Priority: 5-10)
*   **Target**: General Users.
*   **Keywords**:
    *   **Social**: `facebook`, `instagram`, `reddit`, `twitter`, `tiktok`.
    *   **Gaming**: `steam`, `epic games`, `valorant`, `league of legends`, `minecraft`.

### 3.2. Logic Functionality (Code Update)

#### 3.2.1. Browser-Only Doom Scrolling
*   **Current Logic**: If Window Title contains "Shorts" -> Doom Scrolling.
*   **New Logic**: If Window Title contains "Shorts" **AND** Process Name is a **Web Browser** -> Doom Scrolling.
*   **Browser List**: `chrome`, `msedge`, `firefox`, `brave`, `opera`, `vivaldi`, `arc`, `safari`, `iexplore`.
*   **Edge Case**: If user is watching "Shorts" in `Code.exe` (VS Code), it relies on the VS Code rule (Priority 20) vs Shorts rule (Priority 100). The Code Logic must explicitly **IGNORE** distraction keywords if the process is NOT a browser.

## 4. Technical Implementation Details

### 4.1. The "Browser Guard" check
Pseudocode Algorithm for `DetermineCategory(process, title)`:

```csharp
bool IsBrowser = ["chrome", "msedge", "firefox", ...].Contains(process.ToLower());

// 1. Check Extension Data (High Trust)
if (ExtensionData != null) return ExtensionData.Category;

// 2. Check Doom Scrolling (Distractions)
if (IsBrowser) {
    if (Title.Contains("Shorts") || Title.Contains("Reels")) {
        return "Doom Scrolling";
    }
}

// 3. Fallback to Database Rules
Match match = FindBestMatch(title + " " + process);
return match.Category;
```

### 4.2. Database Schema
No schema changes required. Just data insertion (Seeding).

## 5. Success Metrics
*   **Accuracy**: `shorts_generator.py` in VS Code is categorized as **Productive**.
*   **Coverage**: A window titled "Gonit Lecture 1" is categorized as **Study**.
*   **Coverage**: A window titled "Chat with Antigravity" is categorized as **Productive**.
