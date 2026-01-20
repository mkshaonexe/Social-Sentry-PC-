# Product Requirements Document (PRD): Social Sentry - Intelligence & Security Upgrade

**Version:** 1.0  
**Date:** 2026-01-21  
**Status:** Draft / Pending Approval  

## 1. Executive Summary
The goal of this sprint is to transform "Social Sentry" from a basic activity logger into a robust, tamper-resistant digital wellbeing tool. We will address three critical deficits:
1.  **Lack of Intelligence**: Rigid, hardcoded categorization logic.
2.  **Lack of Context**: Inability to distinguish between "working" vs "playing" in the same app.
3.  **Security Vulnerability**: Vulnerability to simple process termination (e.g., Task Manager).

## 2. Scope & Objectives

| Feature | Priority | Description |
| :--- | :--- | :--- |
| **Dynamic Classification** | High | Replace hardcoded `CategoryViewModel` logic with a Database-backed Rule Engine. |
| **Media Context** | High | Detect active media playback (Video/Audio) to identify "Entertainment" regardless of the app. |
| **Self-Protection** | Critical | Implement Process ACLs to prevent unauthorized termination. |
| **Global SQL Master** | Required | Maintain `mastersqlalinhere.txt` with all schema changes for disaster recovery. |

## 3. Detailed Requirements

### 3.1. Dynamic Classification Engine ("The Brain")
**Problem:** Currently, categories are hardcoded (e.g., `if (name.Contains("youtube"))`). This is brittle and requires recompilation to change.
**Solution:** Move logic to a `ClassificationService` powered by a database table.

*   **Database Schema (`ClassificationRules`)**:
    *   `Id` (PK)
    *   `Pattern` (Text): e.g., "youtube", "visual studio", ".pdf"
    *   `MatchType` (Text): "Contains", "Exact", "Regex"
    *   `Category` (Text): "Entertainment", "Productive", "Communication"
    *   `Priority` (Int): To handle conflicts (e.g., "VS Code" is Productive, but "VS Code - Rick Roll" is Entertainment).

*   **Service Layer (`ClassificationService`)**:
    *   Method: `string Categorize(string processName, string windowTitle)`
    *   Logic: Fetch rules -> Match Input -> Return Category (default to "Uncategorized").

### 3.2. Media & Context Awareness
**Problem:** The app sees `chrome.exe` but doesn't know if the user is reading documentation or watching Netflix.
**Solution:** Hook into Windows Media Controls.

*   **Implementation**:
    *   Use `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`.
    *   Detect generic `IsPlaying` status.
    *   **Logic**: IF `Process == Chrome` AND `Media == Playing` THEN `Category = Entertainment`.

### 3.3. Self-Protection ("The Muscle")
**Problem:** User can kill the app via Task Manager.
**Solution:** Apply a Deny Access Control List (ACL) to the OS Process object.

*   **Implementation**:
    *   Target `SelfProtectionService.cs`.
    *   Un-comment/Implement `SetProcessSecurityDescriptor`.
    *   **Rule**: Deny `PROCESS_TERMINATE` rights to "Everyone" or the current user, except "SYSTEM".
    *   **Safeguard**: Ensure Debug builds do *not* apply this, or provide a "Secret Key" uninstaller, otherwise development becomes impossible.

### 3.4. Database & SQL Maintenance
*   **Requirement**: Any change to SQLite tables (e.g., adding `ClassificationRules`) MUST be reflected in `mastersqlalinhere/mastersqlalinhere.txt`.

## 4. Implementation Logic & Roadmap

### Step 1: Database Refactor (The Foundation)
1.  Create `ClassificationRules` table in SQLite.
2.  Seed it with the current hardcoded values from `CategoryViewModel.cs`.
3.  Update `mastersqlalinhere.txt`.

### Step 2: Service Layer (The Brain)
1.  Create `ClassificationService.cs`.
2.  Implement `Categorize()` logic.
3.  Inject into `CategoryViewModel` and remove old `if/else` logic.

### Step 3: Context Awareness (The Eyes)
1.  Update `ActivityTracker` to poll for Media State.
2.  Pass Media State to `ClassificationService`.

### Step 4: Security (The Shield)
1.  Update `SelfProtectionService.cs`.
2.  Test ACLs in a controlled manner (Release build only or specific flag).

## 5. Success Metrics
*   **Flexibility**: A new category rule can be added via SQL/UI without recompiling.
*   **Accuracy**: Watching a video in a browser is logged as "Entertainment".
*   **Security**: Task Manager "End Task" fails with "Access Denied".
