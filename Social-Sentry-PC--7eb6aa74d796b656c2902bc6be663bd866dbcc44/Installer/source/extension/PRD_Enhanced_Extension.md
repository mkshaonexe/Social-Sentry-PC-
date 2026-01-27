# Enhanced Browser Extension - Product Requirements Document (PRD)

## 1. Executive Summary
The goal is to transform the "Social Sentry" browser extension into the **"Main Brain"** of user activity tracking on the desktop. It must capture granular, high-fidelity data from major social media platforms (YouTube, Facebook, Instagram, TikTok, etc.) to enable detailed digital well-being analysis. usage data should be captured with the same or better fidelity than Android Accessibility Services.

## 2. Core Philosophy & Requirements
- **"The Brain"**: The extension is not just a passive logger; it actively inspects the DOM to understand *context*.
- **Platform Agnostic**: Must work seamlessly on Chrome, Edge, Brave, and other Chromium-based browsers.
- **Zero Privacy Constraints**: As per user instruction, we collect *everything* allowed by the browser APIs to provide maximum value to the user.
- **Robust Communication**: Reliable data transmission to the Desktop App via a secure local API (Native Messaging or Localhost with retry logic).

## 3. detailed Feature Requirements

### 3.1. Advanced Content Detection (The "Parsers")
The extension will have modular "Parsers" for specific domains:

#### A. YouTube / YouTube Shorts
- **Detection**: Differentiate between Watch Page, Home Feed, and Shorts Feed.
- **Data Points**:
    - Video Title
    - Channel Name
    - Video Duration
    - **Current Time** (How much watched)
    - **Is Short?** (Boolean)
    - **Category** (Music, Education, Gaming - if visible)

#### B. Facebook & Instagram
- **Detection**: Differentiate between Main Feed, Reels/Stories, and Profile views.
- **Data Points**:
    - **Post Type**: Video, Image, Text, Reel, Story.
    - **Orgin**: "Sponsored" (Ad detection), "Suggested for you", or "Friend/Following".
    - **Content Context**: Capture visible text summaries (e.g., first 50 chars of post) to categorize "Doom Scrolling" vs "Meaningful Socializing".
    - **Reel/Video Duration**: Watch time per item.

#### C. General Browsing (Fallback)
- **Active Tab Time**: Precise accounting of time spent on the *active, focused* tab.
- **Scroll Depth**: How far down the page the user has gone (Indicator of "Doom Scrolling").
- **Url & Title**: Standard capture.

### 3.2. Data Transmission Protocol
- **Endpoint**: `POST http://localhost:5123/api/v2/activity` (New V2 endpoint).
- **Payload Structure**:
  ```json
  {
    "timestamp": "ISO-8601",
    "platform": "YouTube",  // Enum: YouTube, Facebook, Reddit, Generic...
    "contentType": "Shorts", // Enum: Video, Reel, Post, Feed...
    "url": "https://www.youtube.com/shorts/...",
    "metadata": {
      "title": "Funny Cat Video",
      "channel": "CatLover",
      "duration": 60,
      "watched": 55,
      "isAd": false
    },
    "session": {
      "tabId": 123,
      "windowId": 456,
      "isFocused": true,
      "userIdle": false
    }
  }
  ```
- **Resilience**:
    - **Offline Queue**: If Desktop App is closed, buffer events in `chrome.storage.local`.
    - **Batching**: Send data every 2-5 seconds or on critical events (Tab Closed, Page Changed) to reduce overhead.

### 3.3. Desktop Integration
- **Extension Manager**: A UI in the Desktop App to "Install/Repair" the extension.
- **Status Indicator**: "Extension Connected" status on the Dashboard.
- **Zero Trust**: Data is encrypted immediately upon receipt by the Desktop App Service before storage.

## 4. Technical Architecture

### 4.1. Extension Side
- **Manifest V3**: Future-proof.
- **Service Worker (`background.js`)**: Orchestrates data transmission and state management.
- **Content Scripts (`content.js` + `parsers/*.js`)**:
    - `Director`: Identifies current site and loads appropriate Parser.
    - `Parsers`: Domain-specific logic to scrape DOM safely.
    - `Observers`: `MutationObserver` to detect SPA navigation (Single Page App changes without full reload).

### 4.2. Desktop Side
- **`LocalApiServer.cs`**: Upgrade to handle `V2` payload.
- **`ActivityTracker.cs`**: Enhanced to parse and categorize rich metadata.
- **`DatabaseService`**: Schema update to store `Metadata` JSON blob (or new columns).

## 5. Success Metrics
- **Shorts vs Video**: 100% accuracy in distinguishing a YouTube Short from a regular Video.
- **Title Capture**: >95% success rate in capturing video/post titles.
- **Connection**: Auto-reconnects within 5 seconds if Desktop App restarts.
