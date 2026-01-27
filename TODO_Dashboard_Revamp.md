# TODO: Dashboard & Session Logic Revamp

## Phase 1: Backend Foundation & Validity
- [ ] **Database Schema Update**
    - [ ] Add `HourlyStats` table definition to `DatabaseService.InitializeDatabase()`.
    - [ ] Create migration logic for existing users.
    - [ ] Update `mastersqlalinhere.txt` (canonical schema file).
- [ ] **Database Queries**
    - [ ] Implement `GetHourlyUsage(DateTime date)` in `DatabaseService`.
    - [ ] Implement `GetDailyUsageRange(DateTime start, DateTime end)` in `DatabaseService`.
    - [ ] Implement `GetTopAppsForRange(...)`.
- [ ] **Session Logic hardening (UsageTrackerService.cs)**
    - [ ] Create `SessionBuffer` logic: ignore sessions < 2s.
    - [ ] Implement `SessionCoalescing`: merge sessions if returning to same app within 5s.
    - [ ] **CRITICAL FIX**: Update `LoadTodayUsage()` to populate `_hourlyUsage` from DB on startup.
    - [ ] Implement `VerifyStatsIntegrity()` method to run on startup.

## Phase 2: Reactivity & ViewModels
- [ ] **DashboardViewModel Overhaul**
    - [ ] Add `SelectedScope` property (Day/Week/Month).
    - [ ] Add `SelectedDate` property.
    - [ ] Add loading state (fetching historical data can be async).
    - [ ] Implement `RefreshData()` command that calls appropriate DB method based on Scope.
    - [ ] Logic to transform `HourlyStats` -> `ChartDataPoint` (24 bars).
    - [ ] Logic to transform `DailyUsageRange` -> `ChartDataPoint` (7/30 bars).

## Phase 3: UI Implementation
- [ ] **DashboardView.xaml**
    - [ ] Add Header Controls: Date Picker + Scope Selector.
    - [ ] Update "Total Screen Time" text binding.
    - [ ] Optimize Chart Control for variable number of bars.
    - [ ] Add Tooltips to chart bars for precise duration.

## Phase 4: Verification
- [ ] **Unit Tests** for Session Logic (mocking `ActivityEvent`).
- [ ] **Manual Verification**:
    - [ ] Switch date -> Verify graph updates.
    - [ ] Restart App -> Verify graph persists.
    - [ ] Rapid Alt-Tab -> Verify session count stable.
