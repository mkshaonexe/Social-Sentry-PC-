/**
 * Facebook Parser
 * Handles data extraction for Facebook feeds and reels.
 */
class FacebookParser {
    constructor() {
        this.name = 'facebook';
    }

    parse() {
        const url = window.location.href;
        let contentType = 'Feed';
        let metadata = {};

        // 1. Detect Reels
        if (url.includes('/reel/')) {
            contentType = 'Reels';
            // Reels detection logic
            // Attempt to grab any visible text or description
            const activeVideo = document.querySelector('video');
            metadata = {
                duration: activeVideo ? activeVideo.duration : 0,
                currentTime: activeVideo ? activeVideo.currentTime : 0
            };
        }
        else {
            // General Feed parsing
            // This is harder on FB, we might just track scroll and time
            contentType = 'Feed';
            metadata = {
                title: document.title
            };
        }

        return {
            platform: 'Facebook',
            contentType: contentType,
            url: url,
            metadata: metadata
        };
    }

    isApplicable() {
        return window.location.hostname.includes('facebook.com');
    }
}
