# Product Requirements Document: Category Intelligence & Modern UI Overhaul

## 1. Overview
The goal is to revamp the "Categories" section of Social Sentry to provide granular insights into user behavior ("Doom Scrolling" vs "Entertainment", "Study" context detection) and to replace the current UI with a modern, animated, and premium aesthetic.

## 2. Intelligence & Logic Requirements

### 2.1. Doom Scrolling Detection
**Objective:** Distinguish between productive/general entertainment and addictive short-form content.
- **Logic:**
  - Intercept URL activity from browsers (Chrome, Edge).
  - Check for specific URL patterns:
    - **YouTube Shorts:** `youtube.com/shorts`
    - **Facebook Reels:** `facebook.com/reel`
    - **Instagram Reels:** `instagram.com/reels`
    - **TikTok:** `tiktok.com`
  - **Action:** If detected, these activities must be classified distinctively as "Doom Scrolling" rather than generic "Entertainment" or "Social Media".
- **Implementation Strategy:**
  - Modify `UsageTrackerService` to inspect URLs.
  - If a specific pattern is found, override the reported `ProcessName` to a specific identifier (e.g., "YouTube Shorts") which maps to a "Doom Scrolling" category rule.

### 2.2. Study Mode Detection
**Objective:** Automatically credit time as "Study" if the user is consuming educational content, regardless of the platform (e.g., YouTube, PDF Reader).
- **Logic:**
  - Maintain a list of **Study Keywords** (e.g., "lecture", "tutorial", "math", "physics", "course", "study", "exam").
  - Inspect **Window Titles** and **URLs** for these keywords.
  - **Action:** If a keyword matches, override the classification to "Study".
  - **Example:** A window titled "Calculus 101 - YouTube" should count as "Study", not "Entertainment".

### 2.3. Productivity & Communication Lists
**Objective:** Ensure comprehensive coverage of common apps.
- **Productivity:** Add rules for: VS Code, Visual Studio, Word, Excel, PowerPoint, Notion, Obsidian, Trello, Jira, Slack (if work), etc.
- **Communication:** Add rules for: Discord, Slack, WhatsApp, Telegram, Messenger, Skype, Zoom, Teams, Outlook.

## 3. UI/UX Requirements

### 3.1. Design Aesthetic
**Goal:** "Modern, Amazing, Premium".
- **Style:** 
  - Glassmorphism/Neumorphism elements or clean Flat Design with depth (shadows).
  - Dark mode optimized.
  - **Animations:** Smooth entry animations (SlideIn + FadeIn), hover effects on cards, progress bar animations.
- **Layout:**
  - Remove the "boring" list.
  - Use a **Grid of Cards** or a **Interactive Bubble Chart** (if feasible) or a **Sleek List** with extensive visual flair.
  - **Hero Section:** Show the dominant category with a large visual.

### 3.2. Detailed Category Cards
Each category card should display:
- Category Name & Icon.
- Total Time & Percentage.
- A mini-list of top apps within that category (e.g., "YouTube, Netflix" under Entertainment).
- Color-coded visual indicators.

## 4. Technical Implementation Plan

### 4.1. Backend (Services)
- **`ClassificationService.cs`:**
  - Update `SeedDefaultRules` with comprehensive lists.
  - Add logic to handle "Context-based" rules (Study keywords).
- **`UsageTrackerService.cs`:**
  - Update `GetSmartProcessName` to support context overrides.
  - **Logic:** 
    1. Check for Doom Scrolling URL patterns -> Return "YouTube Shorts", etc.
    2. Check for Study Keywords in Title -> Return "[App] (Study)".
    3. Fallback to standard app name.

### 4.2. Frontend (Views/ViewModels)
- **`CategoryViewModel.cs`:**
  - Ensure it correctly groups the new context-aware names.
  - Add properties for UI animations if needed.
- **`CategoryView.xaml`:**
  - **Rewrite completely.**
  - Use `ItemsControl` or `ListBox` with a custom `DataTemplate`.
  - Implement `DoubleAnimation` for loading effects.
  - Use `Border` with `CornerRadius` and `DropShadowEffect` for cards.

## 5. Verification
- **Test Doom Scrolling:** Visit `youtube.com/shorts` and verify it appears as "Doom Scrolling" in the dashboard.
- **Test Study:** Open a notepad named "Math Notes" or watch a video titled "Physics Lecture" and verify it counts as "Study".
- **Test UI:** Verify animations play on load and the design looks premium.
