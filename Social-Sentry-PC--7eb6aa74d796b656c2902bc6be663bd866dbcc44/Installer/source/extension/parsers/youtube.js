/**
 * YouTube Parser
 * Handles data extraction for YouTube videos and Shorts.
 */
class YouTubeParser {
    constructor() {
        this.name = 'youtube';
    }

    parse() {
        const url = window.location.href;
        let contentType = 'Browse';
        let metadata = {};

        // 1. Detect Shorts
        if (url.includes('/shorts/')) {
            contentType = 'Shorts';
            // Try to find the active Short
            // Note: YouTube Shorts DOM is tricky, might need specific selectors
            const activeShort = document.querySelector('ytd-reel-video-renderer[is-active]');
            if (activeShort) {
                const videoElement = activeShort.querySelector('video');
                const titleElement = activeShort.querySelector('.title, #title'); // Adjust selectors

                metadata = {
                    title: titleElement ? titleElement.innerText : 'Unknown Short',
                    duration: videoElement ? videoElement.duration : 0,
                    currentTime: videoElement ? videoElement.currentTime : 0,
                    isShort: true
                };
            }
        }
        // 2. Detect Main Video
        else if (url.includes('/watch')) {
            contentType = 'Video';
            const videoElement = document.querySelector('video.html5-main-video');
            const titleElement = document.querySelector('h1.ytd-video-primary-info-renderer'); // Adjust
            const channelElement = document.querySelector('ytd-video-owner-renderer #channel-name a');

            metadata = {
                title: titleElement ? titleElement.innerText : document.title,
                channel: channelElement ? channelElement.innerText : '',
                duration: videoElement ? videoElement.duration : 0,
                currentTime: videoElement ? videoElement.currentTime : 0,
                isShort: false
            };
        }

        return {
            platform: 'YouTube',
            contentType: contentType,
            url: url,
            metadata: metadata
        };
    }

    isApplicable() {
        return window.location.hostname.includes('youtube.com');
    }
}
