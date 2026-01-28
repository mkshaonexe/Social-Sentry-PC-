# To-Do List: Categorization Logic Upgrade

This document tracks the tasks required to implement the new Categorization Logic PRD.

## Phase 1: Database Updates (Seeding)
We need to update the SQLite seeding logic or the `ClassificationService` initialization to include these new rules.

- [x] **Study Category (Bangla & English)**
    - [x] Add 50 English Keywords (e.g., `syllabus`, `calculus`, `thesis`, `coursera`...).
    - [x] Add 50 Bangla/Banglish Keywords (e.g., `porashona`, `gonit`, `biggan`, `shikkha`...).
    - [x] Verify Bangla script support in SQLite (UTF-8 encoding).

- [x] **Productive Category (AI & IDEs)**
    - [x] Add Top 20 AI Tools (e.g., `chatgpt`, `claude`, `gemini`, `antigravity`, `midjourney`...).
    - [x] Add Top 20 IDEs/Tools (e.g., `cursor`, `windsurf`, `android studio`, `figma`...).
    - [x] Ensure specific tools like `Antigravity` and `Cursor` are present.

- [x] **Entertainment Category**
    - [x] Add Social Media platforms (`reddit`, `pinterest`, `discord`*). *Manage priority vs Communication*.
    - [x] Add Gaming Platforms (`steam`, `epic games`, `battle.net`).

## Phase 2: Logic Implementation ("Browser Guard")
Modify `ClassificationService.cs` or `UsageTrackerService.cs` to handle intelligent classification.

- [x] **Create `BrowserHelpers`**
    - [x] Implement `IsBrowserProcess(string processName)` returning `true` for `chrome`, `msedge`, `firefox`, `brave`, `opera`, `vivaldi`.
    
- [x] **Update `DetermineCategory` Logic**
    - [x] **Before** checking database rules, check for "Doom Scrolling" keywords (`shorts`, `reels`, `tiktok`).
    - [x] **Condition**: `IF (Title contains "shorts") AND (IsBrowserProcess(process) == FALSE) -> DO NOT classify as Doom Scrolling.`
    - [x] **Fallback**: Let it fall through to standard rules (where "code" or "studio" will pick it up as Productive).

## Phase 3: Testing & Verification
- [x] **Test Case 1: The "Programmer" Test**
    - [x] Open VS Code with a file named `shorts_generator.py`.
    - [x] **Expectation**: Categorized as **Productive**.
    - [x] **Failure**: Categorized as Doom Scrolling.

- [x] **Test Case 2: The "Student" Test**
    - [x] Open a PDF named `Gonit_Homework.pdf` or a browser tab "HSC Biggan Class".
    - [x] **Expectation**: Categorized as **Study**.

- [x] **Test Case 3: The "Gamer" Test**
    - [x] Launch `Valorant` or `Steam`.
    - [x] **Expectation**: Categorized as **Entertainment**.

- [x] **Test Case 4: The "Browser" Test**
    - [x] Open Chrome to `youtube.com/shorts/...`.
    - [x] **Expectation**: Categorized as **Doom Scrolling**.

## Future Improvements
- [ ] Add API integration for IGDB to fetch game executable names dynamically.
- [ ] Implement User Override (Right-click -> "Always classify X as Y").
