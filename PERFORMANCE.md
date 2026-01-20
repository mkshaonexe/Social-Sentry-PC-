# Performance Impact Analysis & Optimization Plan

## 1. Addressing Your Concerns

### Q1: Does checking every 1s consume huge RAM/CPU?
*   **Current State**: The `NativeMethods.GetForegroundWindow` call uses **less than 0.01% CPU**. It is extremely lightweight.
*   **The Bottleneck**: The **Browser Monitoring** (UI Automation) is heavier. If we scan the chrome address bar every second, it can cause **CPU spikes (1-3%)**.
*   **Verdict**: It is safe for a Prototype, but for a "Pro" background app, we should optimize it.

### Q2: Is there a "Native API" like Android's UsageStats?
*   **Yes!** It is called **WinEventHook**.
*   Instead of a Timer asking "What's open?" every second, we can tell Windows: *"Please call my function only when the user switches windows."*
*   **Benefit**: 0% CPU usage when the user is not switching apps.

### Q3: Writing to Database every second?
*   **Valid Concern**: Writing to the disk every second is bad for SSD health and creates a bloated database (86,400 rows per day).
*   **Solution**: **Batching & Coalescing**.
    *   **Logic**: If the user is on "Chrome" for 60 seconds, we should **not** write 60 rows. We should write **1 row** with `Duration = 60s`.
    *   We only write to the DB when the *Activity Changes* or every 5 minutes.

---

## 2. Optimization Roadmap (Phase 3)

We will switch from "Polling" (Timer) to "Event-Driven" (Hooks) to match your requirement for "Android-like efficiency".

### Step 1: Switch to `SetWinEventHook` (The "Android" Way)
We will replace the 1-second Timer with the Windows Event System.

```csharp
// Concept Code
SetWinEventHook(EVENT_SYSTEM_FOREGROUND, ..., MyCallbackFunction);

void MyCallbackFunction(...) {
    // This only runs when the user actually ALT+TABs or clicks a new window
    CheckActivity();
}
```

### Step 2: Smart Database Logging (The "Coalescing" Logic)
We will change `DatabaseService` to handle "Sessions" instead of "Ticks".

#### Current (Bad):
| Time | App | Duration |
| :--- | :--- | :--- |
| 10:00:01 | Chrome | 1s |
| 10:00:02 | Chrome | 1s |
| 10:00:03 | Chrome | 1s |

#### Optimized (Good):
| Time | App | Duration |
| :--- | :--- | :--- |
| 10:00:01 | Chrome | **3s** |

### Step 3: Browser throttling
*   We will only check the URL when the *Title Changes* or when the user presses `Enter` key (using a Hook), rather than every second.

## 3. Summary
*   **Current Prototype**: Low impact, but creates too much data.
*   **Optimized Version**: Near-zero CPU impact, efficient database usage.

**Recommendation**: I will implement **Step 1 (Event Hooks)** and **Step 2 (Database Batching)** in the next phase to resolve your concerns.
