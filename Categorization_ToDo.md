# To-Do List: Categorization Logic Upgrade

This document tracks the tasks required to implement the new Categorization Logic PRD.

## Phase 1: Database Updates (Seeding)
We need to update the SQLite seeding logic or the `ClassificationService` initialization to include these new rules.

- [ ] **Study Category (Bangla & English)**
    - [ ] Add 50 English Keywords (e.g., `syllabus`, `calculus`, `thesis`, `coursera`...).
    - [ ] Add 50 Bangla/Banglish Keywords (e.g., `porashona`, `gonit`, `biggan`, `shikkha`...).
    - [ ] Verify Bangla script support in SQLite (UTF-8 encoding).

- [ ] **Productive Category (AI & IDEs)**
    - [ ] Add Top 20 AI Tools (e.g., `chatgpt`, `claude`, `gemini`, `antigravity`, `midjourney`...).
    - [ ] Add Top 20 IDEs/Tools (e.g., `cursor`, `windsurf`, `android studio`, `figma`...).
    - [ ] Ensure specific tools like `Antigravity` and `Cursor` are present.

- [ ] **Entertainment Category**
    - [ ] Add Social Media platforms (`reddit`, `pinterest`, `discord`*). *Manage priority vs Communication*.
    - [ ] Add Gaming Platforms (`steam`, `epic games`, `battle.net`).

## Phase 2: Logic Implementation ("Browser Guard")
Modify `ClassificationService.cs` or `UsageTrackerService.cs` to handle intelligent classification.

- [ ] **Create `BrowserHelpers`**
    - [ ] Implement `IsBrowserProcess(string processName)` returning `true` for `chrome`, `msedge`, `firefox`, `brave`, `opera`, `vivaldi`.
    
- [ ] **Update `DetermineCategory` Logic**
    - [ ] **Before** checking database rules, check for "Doom Scrolling" keywords (`shorts`, `reels`, `tiktok`).
    - [ ] **Condition**: `IF (Title contains "shorts") AND (IsBrowserProcess(process) == FALSE) -> DO NOT classify as Doom Scrolling.`
    - [ ] **Fallback**: Let it fall through to standard rules (where "code" or "studio" will pick it up as Productive).

## Phase 3: Testing & Verification
- [ ] **Test Case 1: The "Programmer" Test**
    - [ ] Open VS Code with a file named `shorts_generator.py`.
    - [ ] **Expectation**: Categorized as **Productive**.
    - [ ] **Failure**: Categorized as Doom Scrolling.

- [ ] **Test Case 2: The "Student" Test**
    - [ ] Open a PDF named `Gonit_Homework.pdf` or a browser tab "HSC Biggan Class".
    - [ ] **Expectation**: Categorized as **Study**.

- [ ] **Test Case 3: The "Gamer" Test**
    - [ ] Launch `Valorant` or `Steam`.
    - [ ] **Expectation**: Categorized as **Entertainment**.

- [ ] **Test Case 4: The "Browser" Test**
    - [ ] Open Chrome to `youtube.com/shorts/...`.
    - [ ] **Expectation**: Categorized as **Doom Scrolling**.

## Future Improvements
- [ ] Add API integration for IGDB to fetch game executable names dynamically.
- [ ] Implement User Override (Right-click -> "Always classify X as Y").
