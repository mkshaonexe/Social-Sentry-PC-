/**
 * Social Sentry Content Script - Controller
 * Orchestrates activity tracking using modular parsers.
 */

// Import parsers (In a real build system we'd check/require, but for simple chrome ext we concat or load all)
// The manifest will ensure these files are loaded before content.js, or we can include them here if we had a bundler.
// Since we are doing "raw" js for now, we assume the classes are available in global scope 
// OR we will use a simpler monolithic file approach for now if loading is complex without a bundler.
// STRATEGY: To avoid complex build steps for the user, we will actually inline the logic or rely on manifest order.
// Let's rely on Manifest V3 order. We will list parsers first in manifest.

(function () {
    'use strict';

    class ParserController {
        constructor() {
            this.parsers = [
                // We assume these classes are globally available because we'll load them first in manifest
                new YouTubeParser(),
                new FacebookParser(),
                new GenericParser() // Last resort
            ];
            this.activeParser = null;
            this.currentActivity = null;
            this.reportInterval = 5000;
        }

        init() {
            this.detectParser();
            this.startTracking();
            this.setupObservers();
        }

        detectParser() {
            for (const parser of this.parsers) {
                if (parser.isApplicable()) {
                    this.activeParser = parser;
                    console.log(`[Social Sentry] Activated parser: ${parser.name}`);
                    break;
                }
            }
        }

        async tick() {
            if (!this.activeParser) return;

            // check for idle? (To be implemented fully later, for now we trust the parser or add global idle check)
            // let's add a simple idle check here or inside parsers?
            // simpler to keep it global.

            const activityData = this.activeParser.parse();

            // Add global context
            const payload = {
                ...activityData,
                timestamp: new Date().toISOString(),
                session: {
                    tabId: null, // Filled by background
                    windowId: null, // Filled by background
                    isFocused: document.hasFocus()
                }
            };

            chrome.runtime.sendMessage({ type: 'ACTIVITY_UPDATE_V2', data: payload });
        }

        startTracking() {
            setInterval(() => this.tick(), this.reportInterval);
        }

        setupObservers() {
            // Watch for URL changes (SPA support)
            let lastUrl = location.href;
            new MutationObserver(() => {
                const url = location.href;
                if (url !== lastUrl) {
                    lastUrl = url;
                    // Re-evaluate parser on URL change? Usually platform stays same, but good to check.
                    // For now, just logging navigation.
                    this.detectParser();
                    this.tick(); // Send immediate update on nav
                }
            }).observe(document, { subtree: true, childList: true });
        }
    }

    // Start everything
    const controller = new ParserController();
    // Wait for load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => controller.init());
    } else {
        controller.init();
    }

})();
