# PRD: Dashboard Revamp & Session Logic Overhaul

## 1. Executive Summary
**Goal:** Transform the "Dashboard" into a comprehensive, historical, and interactive usage visualization tool.
**Key Objective:** Allow users to view screen time trends (hourly, daily, weekly, monthly) and ensure data validity is rigorously checked to prevent false or duplicate session counts.
**Target Outcome:** A premium, "Digital Wellbeing" style dashboard where users can drill down into their habits with confidence in the data accuracy.

## 2. User Stories & Requirements

### 2.1 Historical Data Visualization
*   **As a User**, I want to toggle between "Today", "Week", "Month", and "Custom Date" views so I can understand my long-term habits.
*   **As a User**, I want to see a Bar Graph that adapts to the selected view:
    *   **Day View**: 24 bars (00:00 - 23:00) showing activity per hour.
    *   **Week View**: 7 bars (Mon - Sun) showing total activity per day.
    *   **Month View**: 28-31 bars (Day 1 - 31) showing total activity per day.

### 2.2 Session Validity & Accuracy
*   **As a User**, I want the "Session Count" to be meaningful. Rapidly switching apps (Alt-Tab) should not artificially inflate the session count.
*   **As a User**, I want to ensure that "idle time" or "fake activity" is filtered out, so the screen time reflects actual usage.
*   **As a Developer**, I need the system to self-verify: if the app crashes or restarts, the graph must rebuild correctly from the database, not start from zero.

### 2.3 UI/UX Enhancements
*   **Date Navigation**: A "Date Picker" or "Calendar" button to select specific historical dates.
*   **Hero Section**: Display "Total Screen Time" for the selected period prominently.
*   **Graph Interaction**: Hovering over a bar should show the specific time and duration (e.g., "5:00 PM - 45 mins").

---

## 3. Technical Implementation Plan

### 3.1 Backend: Session Logic Refinement
**Current State:**
*   Sessions < 0.1s are ignored.
*   `SessionCount` increments on every switch > 0.1s.
*   Data is stored in `ActivityLog` (granular) and `DailyStats` (aggregated daily).
*   **Issue:** `UsageTrackerService` only loads `DailyStats` on start. It does NOT load hourly distribution, leading to empty graphs on restart.

**Proposed Changes:**
1.  **Session Debouncing:**
    *   Increase minimum session threshold to **2 seconds** (configurable).
    *   Implement **Session Coalescing**: If a user returns to App A within 5 seconds of leaving it, merge the sessions effectively (or just don't increment the session count).
2.  **Data Persistence (Fixing the "Empty Graph" Bug):**
    *   **Critical:** On startup (`LoadTodayUsage`), the service must query `ActivityLog` or the new `HourlyStats` table to populate the in-memory `_hourlyUsage` dictionary.
3.  **New Database Table: `HourlyStats`**:
    *   To optimize the graph performance, we will aggregate data hourly.
    *   Schema: `Date` (TEXT), `Hour` (INT), `ProcessName` (TEXT), `Category` (TEXT), `DurationSeconds` (REAL).

### 3.2 Backend: Data Retrieval
*   **New Methods in `DatabaseService`:**
    *   `GetHourlyUsage(DateTime date)`: Returns `Dictionary<int, double>` (Hour -> TotalSeconds).
    *   `GetDailyUsageRange(DateTime start, DateTime end)`: Returns `Dictionary<DateTime, double>` for Week/Month views.
    *   `GetTopAppsForRange(DateTime start, DateTime end)`: Aggregates usage by App Name for the list view.

### 3.3 Frontend: Dashboard View
*   **Controls**:
    *   Add a `ComboBox` or `SegmentedControl` for Scope: [Today] [Week] [Month] [Custom].
    *   Add a `DatePicker` (visible when "Custom" or specific day is needed).
*   **Graph Component**:
    *   Refactor `DashboardViewModel` to handle `ObservableCollection<ChartDataPoint>` dynamic resizing (24 items vs 7 items vs 30 items).
    *   Ensure the Y-Axis scales dynamically based on the maximum value in the range.

---

## 4. Verification & Validation (Self-Check)

### 4.1 "Valid Yourself Verify Itself" Strategy
*   **Automated Integrity Check**: On startup, run a background task that sums up all `ActivityLog` entries for today and compares it with `DailyStats`. If they mismatch > 1%, trigger a "Recalculate Stats" routine.
*   **Session Validity Test**: Manual test script:
    1.  Open Notepad.
    2.  Alt-Tab away and back instantly (0.5s). -> Should **NOT** increment session count.
    3.  Stay in Notepad for 10s. -> Should increment session count and duration.
    4.  Restart App. -> Graph should still show the 10s usage at the correct hour.

---

## 5. Schema Updates (`mastersqlalinhere.txt`)

```sql
-- ADDED: Hourly aggregation for graph performance
CREATE TABLE IF NOT EXISTS HourlyStats (
    Date TEXT,
    Hour INTEGER,
    ProcessName TEXT,
    Category TEXT,
    TotalSeconds REAL,
    PRIMARY KEY (Date, Hour, ProcessName)
);

-- UPDATE: Ensure ActivityLog has metadata for validation
-- (Already present in code, ensuring schema consistency)
```
