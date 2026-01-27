# TODO: Dashboard & Session Logic Revamp

## Phase 1: Backend Foundation & Validity
- [x] **Database Schema Update**
    - [x] Add `HourlyStats` table definition to `DatabaseService.InitializeDatabase()`.
    - [x] Create migration logic for existing users.
    - [x] Update `mastersqlalinhere.txt` (canonical schema file).
- [x] **Database Queries**
    - [x] Implement `GetHourlyUsage(DateTime date)` in `DatabaseService`.
    - [x] Implement `GetDailyUsageRange(DateTime start, DateTime end)` in `DatabaseService`.
    - [x] Implement `GetTopAppsForRange(...)`.
- [x] **Session Logic hardening (UsageTrackerService.cs)**
    - [x] Create `SessionBuffer` logic: ignore sessions < 2s.
    - [x] Implement `SessionCoalescing`: merge sessions if returning to same app within 5s.
    - [x] **CRITICAL FIX**: Update `LoadTodayUsage()` to populate `_hourlyUsage` from DB on startup.
    - [x] Implement `VerifyStatsIntegrity()` method to run on startup.

## Phase 2: Reactivity & ViewModels
- [x] **DashboardViewModel Overhaul**
    - [x] Add `SelectedScope` property (Day/Week/Month).
    - [x] Add `SelectedDate` property.
    - [x] Add loading state (fetching historical data can be async).
    - [x] Implement `RefreshData()` command that calls appropriate DB method based on Scope.
    - [x] Logic to transform `HourlyStats` -> `ChartDataPoint` (24 bars).
    - [x] Logic to transform `DailyUsageRange` -> `ChartDataPoint` (7/30 bars).

## Phase 3: UI Implementation
- [x] **DashboardView.xaml**
    - [x] Add Header Controls: Date Picker + Scope Selector.
    - [x] Update "Total Screen Time" text binding.
    - [x] Optimize Chart Control for variable number of bars.
    - [x] Add Tooltips to chart bars for precise duration.

## Phase 4: Verification
- [x] **Unit Tests** for Session Logic (mocking `ActivityEvent`).
- [x] **Manual Verification**:
    - [x] Switch date -> Verify graph updates.
    - [x] Restart App -> Verify graph persists.
    - [x] Rapid Alt-Tab -> Verify session count stable.
