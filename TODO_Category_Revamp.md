# To-Do List: Category Intelligence & Modern UI

## Phase 1: Logic Implementation
- [x] **Classification Service Update**
    - [x] Update `SeedDefaultRules` in `ClassificationService.cs` with comprehensive Productivity apps (VS Code, Office, etc.).
    - [x] Update `SeedDefaultRules` with comprehensive Communication apps (Discord, Teams, Slack, etc.).
    - [x] Add explicit rules for "Doom Scrolling" (YouTube Shorts, Reels, TikTok).
    - [x] Add explicit rules for "Study" context.

- [x] **Usage Tracker Enhancement**
    - [x] Modify `UsageTrackerService.cs` -> `GetSmartProcessName` (and native tracking logic).
    - [x] Implement URL detection for "Doom Scrolling":
        - [x] Detect `youtube.com/shorts` -> Return "YouTube Shorts".
        - [x] Detect `facebook.com/reel` -> Return "Facebook Reels".
        - [x] Detect `instagram.com/reels` -> Return "Instagram Reels".
    - [x] Implement Keyword detection for "Study":
        - [x] Define keywords: "study", "lecture", "tutorial", "course", "assignment", "thesis", "research".
        - [x] Check Window Title against keywords.
        - [x] If match -> Return "[App Name] (Study)".

## Phase 2: UI Redesign (WPF)
- [x] **CategoryViewModel Update**
    - [x] Ensure `Categories` collection supports new data structure if needed.
    - [x] Add properties for Total Time Study, Total Time Doom Scrolling (for quick access if needed outside list).
    - [x] Add `LoadAnimation` trigger property.

- [x] **CategoryView.xaml Overhaul**
    - [x] Clear existing "List" UI.
    - [x] Create `Grid` layout with `ScrollViewer`.
    - [x] Design **"Hero Card"** for the top category (Most used).
    - [x] Design **"Category Cards"** for others:
        - [x] Modern Card Style: White/Dark background, CornerRadius 15, DropShadow.
        - [x] Icon: Large, colorful.
        - [x] Progress Bar: Sleek, rounded.
        - [x] Context Menu: "View details".
    - [x] Implement Animations:
        - [x] `EventTrigger` `RoutedEvent="Loaded"`.
        - [x] `DoubleAnimation` for Opacity (0 -> 1).
        - [x] `ThicknessAnimation` for Margin (Slide Up effect).

## Phase 3: Verification & Cleanup
- [x] **Testing**
    - [x] manual test: Open YouTube Shorts -> Verify "Doom Scrolling" category increases.
    - [x] manual test: Open "Math Lecture" video -> Verify "Study" category increases.
- [x] **Git Operations**
    - [x] `git add .`
    - [x] `git commit -m "feat: revamp categories logic and UI with doom scrolling detection"`
    - [x] `git push`
