# Social Sentry PC: Codebase Analysis & Roadmap

## 1. Executive Summary
The "Social Sentry PC" application is designed as a hybrid monitoring system that attempts to bridge the gap between native desktop tracking and granular web activity monitoring. 

**Current Status:** The systems are built but **disconnected**.
- The **Native Windows App** tracks generic usage (which app is open, window titles).
- The **Browser Extension** tracks detailed behavior (scrolling, video watching, specific URLs) identical to the Android Accessibility Service.
- **Critical Gap:** The data from the Browser Extension is being sent to the native app's local server (`LocalApiServer`), but the native app is **ignoring it**. The "brain" (Extension) is talking, but the "body" (App) is not listening.

---

## 2. Architecture Comparison: Android vs. Windows

The User's previous experience with Android Accessibility Services allowed for a "God Mode" view of the specific device. Windows operates differently.

| Feature | Android (Accessibility Service) | Windows (Current Native Implementation) | Windows (With Extension Integrated) |
| :--- | :--- | :--- | :--- |
| **Scope** | Global (All Apps) | Global (All Active Windows) | Global Windows + Deep Browser |
| **Granularity** | High (Read text, buttons, scroll position) | Low (Window Title & Process Name only) | High (DOM access inside Browser) |
| **Context** | "User is watching distinct video X" | "User is in Google Chrome" | "User is watching YouTube Video X" |
| **Scrolling** | Detects scroll events in any app | Hard to detect natively | **Perfectly detected in Browser** |
| **Text Scraping** | Can read any text on screen | Very difficult (OCR / Heavy UIAutomation) | **Easy inside Browser** |

### Key Takeaway
On Windows, because users spend 90% of their "distracted" time in a Browser (YouTube, Facebook, Instagram are websites on PC, not apps), **the Browser Extension is the equivalent of the Android Accessibility Service.**

---

## 3. Current Codebase Status

### A. The Good (What is working)
1.  **Native Tracking (`UsageTrackerService.cs`)**:
    -   Correctly uses `SetWinEventHook` to detect when the user switches windows.
    -   Captures `ProcessName` (e.g., `chrome.exe`) and `WindowTitle` (e.g., "YouTube - Google Chrome").
    -   Has an `IdleDetector` to pause tracking when the user walks away.
2.  **The "Ghost" Brain (`content.js` + `LocalApiServer.cs`)**:
    -   You *already have* the code to detect:
        -   **Doom Scrolling**: Checks scroll depth & velocity.
        -   **Video Watching**: Detects HTML5 `<video>` elements playing.
        -   **Reels/Shorts**: Scans URLs for `/shorts/` or `/reels/`.
        -   **Studying**: Counts word density on a page to guess if it's an article/study material.

### B. The Bad (The Missing Link)
-   **No Connection:** `App.xaml.cs` starts the `LocalApiServer` using a *new, empty* `ActivityTracker`. Meanwhile, `MainWindow.xaml.cs` starts the real `UsageTrackerService`.
-   **Result:** The extension sends data to port 5123. The `LocalApiServer` receives it. And then... nothing happens. It is never saved to the database or shown on the dashboard.

---

## 4. Feasibility of User Requirements

The user asked: *"Can we do that all the things text scrap from the user screen and it according to all the things it can category make it in details?"*

**Answer:** 
-   **Inside Browser (YouTube/Facebook/Web):** **YES.** We can do exactly what you did on Android. We can read the text, see the video title, measure scroll speed, etc.
-   **Inside Native Apps (Word/Telegram/Games):** **PARTIALLY.** We can see the Window Title ("Telegram - Chat with Mom"). We *cannot* easily read the message text inside the chat window without very invasive and heavy performance costs (using Windows UIAutomation or OCR), which usually slows down the computer. 

**Recommendation:** Focus on "Perfect Browser Monitoring" + "Good Native Monitoring". This covers 99% of use cases.

---

## 5. Implementation Plan

### Phase 1: The "Wiring" Fix (Immediate)
*We need to connect the Extension brain to the App body.*
1.  **Refactor `App.xaml.cs`**: Stop creating a dummy `ActivityTracker`.
2.  **Singleton Services**: Make `LocalApiServer` accessible to `UsageTrackerService`.
3.  **Consume Data**: In `UsageTrackerService`, subscribe to `LocalApiServer.OnActivityReceived`.
    -   When `OnActivityReceived` fires "Doom Scrolling", update the current session in the database with a tag/category "Doom Scrolling".
    -   When "Video Watching" is detected, log it as "Entertainment" dynamically.

### Phase 2: Enhanced Context Features (Short Term)
1.  **YouTube Specifics**: Update `content.js` to grab the specific H1 title of the video and the Channel Name.
2.  **Facebook/Instagram**: Detect "Feed" vs "Profile" vs "Reels" URLs explicitly.
3.  **Real-time Classification**: Use the scraped text (already implemented in `content.js` as `wordCount`) to classify "Studying" vs "Browsing".

### Phase 3: Advanced Native Features (Future)
1.  **OCR / Vision**: (Optional) Use Windows 10/11 built-in OCR APIs to take screenshots of *native* apps (like Telegram) and read text. This is resource-intensive but possible if the user *really* needs monitoring outside the browser.

---

## 6. Detailed Roadmap Checklist

- [ ] **Fix Service wiring**: Pass the `LocalApiServer` instance into `UsageTrackerService`.
- [ ] **Handle Browser Data**: In `UsageTrackerService`, when an extension event ("Doom Scrolling") arrives, override the generic "Chrome" activity log with the specific detail.
- [ ] **Persist to DB**: Ensure `DatabaseService` can store "Activity Type" (e.g. "Browsing" vs "Watching Video") alongside the duration.
- [ ] **Dashboard Update**: Show "Doom Scrolling" as a distinct category in the Donut Chart.

This plan restores the "Android-like" feeling by leveraging the Browser Extension as your new "Accessibility Service".
