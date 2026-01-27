# To-Do List: Category Intelligence & Modern UI

## Phase 1: Logic Implementation
- [ ] **Classification Service Update**
    - [ ] Update `SeedDefaultRules` in `ClassificationService.cs` with comprehensive Productivity apps (VS Code, Office, etc.).
    - [ ] Update `SeedDefaultRules` with comprehensive Communication apps (Discord, Teams, Slack, etc.).
    - [ ] Add explicit rules for "Doom Scrolling" (YouTube Shorts, Reels, TikTok).
    - [ ] Add explicit rules for "Study" context.

- [ ] **Usage Tracker Enhancement**
    - [ ] Modify `UsageTrackerService.cs` -> `GetSmartProcessName` (and native tracking logic).
    - [ ] Implement URL detection for "Doom Scrolling":
        - [ ] Detect `youtube.com/shorts` -> Return "YouTube Shorts".
        - [ ] Detect `facebook.com/reel` -> Return "Facebook Reels".
        - [ ] Detect `instagram.com/reels` -> Return "Instagram Reels".
    - [ ] Implement Keyword detection for "Study":
        - [ ] Define keywords: "study", "lecture", "tutorial", "course", "assignment", "thesis", "research".
        - [ ] Check Window Title against keywords.
        - [ ] If match -> Return "[App Name] (Study)".

## Phase 2: UI Redesign (WPF)
- [ ] **CategoryViewModel Update**
    - [ ] Ensure `Categories` collection supports new data structure if needed.
    - [ ] Add properties for Total Time Study, Total Time Doom Scrolling (for quick access if needed outside list).
    - [ ] Add `LoadAnimation` trigger property.

- [ ] **CategoryView.xaml Overhaul**
    - [ ] Clear existing "List" UI.
    - [ ] Create `Grid` layout with `ScrollViewer`.
    - [ ] Design **"Hero Card"** for the top category (Most used).
    - [ ] Design **"Category Cards"** for others:
        - [ ] Modern Card Style: White/Dark background, CornerRadius 15, DropShadow.
        - [ ] Icon: Large, colorful.
        - [ ] Progress Bar: Sleek, rounded.
        - [ ] Context Menu: "View details".
    - [ ] Implement Animations:
        - [ ] `EventTrigger` `RoutedEvent="Loaded"`.
        - [ ] `DoubleAnimation` for Opacity (0 -> 1).
        - [ ] `ThicknessAnimation` for Margin (Slide Up effect).

## Phase 3: Verification & Cleanup
- [ ] **Testing**
    - [ ] manual test: Open YouTube Shorts -> Verify "Doom Scrolling" category increases.
    - [ ] manual test: Open "Math Lecture" video -> Verify "Study" category increases.
- [ ] **Git Operations**
    - [ ] `git add .`
    - [ ] `git commit -m "feat: revamp categories logic and UI with doom scrolling detection"`
    - [ ] `git push`
