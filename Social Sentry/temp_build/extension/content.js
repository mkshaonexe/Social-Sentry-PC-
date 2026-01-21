// Social Sentry Content Script
// Tracks user activity on web pages

(function () {
    'use strict';

    // Activity state
    let currentActivity = {
        type: 'browsing',
        startTime: Date.now(),
        scrollDepth: 0,
        videoWatchTime: 0,
        isIdle: false,
        lastInteraction: Date.now()
    };

    // Configuration
    const IDLE_THRESHOLD = 30000; // 30 seconds
    const REPORT_INTERVAL = 5000; // 5 seconds
    const SCROLL_SAMPLE_RATE = 100; // ms

    // Detect activity type based on page content
    function detectActivityType() {
        const url = window.location.href;
        const hostname = window.location.hostname;

        // Reels/Shorts detection
        if (url.includes('/shorts') || url.includes('/reels') || url.includes('/reel/')) {
            return 'reels';
        }

        // Video detection
        const videos = document.querySelectorAll('video');
        for (const video of videos) {
            if (!video.paused && video.readyState >= 2) {
                // Check if it's a main video player (not a small preview)
                const rect = video.getBoundingClientRect();
                if (rect.width > 200 && rect.height > 150) {
                    return 'video_watching';
                }
            }
        }

        // Study/Reading detection (text-heavy, slow scrolling)
        const textContent = document.body?.innerText || '';
        const wordCount = textContent.split(/\s+/).length;
        const isTextHeavy = wordCount > 500;

        if (isTextHeavy && currentActivity.scrollDepth < 30) {
            // Low scroll + text heavy = likely reading/studying
            const timeSinceStart = Date.now() - currentActivity.startTime;
            if (timeSinceStart > 10000) { // More than 10 seconds on page
                return 'studying';
            }
        }

        // Social media feed detection
        if (hostname.includes('facebook.com') ||
            hostname.includes('twitter.com') ||
            hostname.includes('x.com') ||
            hostname.includes('instagram.com') ||
            hostname.includes('tiktok.com') ||
            hostname.includes('reddit.com')) {
            if (currentActivity.scrollDepth > 50) {
                return 'doom_scrolling';
            }
            return 'social_feed';
        }

        // Default
        return 'browsing';
    }

    // Track scroll depth
    let lastScrollY = 0;
    let scrollVelocity = 0;

    function updateScrollDepth() {
        const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
        if (scrollHeight > 0) {
            currentActivity.scrollDepth = Math.round((window.scrollY / scrollHeight) * 100);
        }

        // Calculate scroll velocity
        scrollVelocity = Math.abs(window.scrollY - lastScrollY);
        lastScrollY = window.scrollY;
    }

    // Track video watch time
    function trackVideoTime() {
        const videos = document.querySelectorAll('video');
        for (const video of videos) {
            if (!video.paused) {
                currentActivity.videoWatchTime += REPORT_INTERVAL / 1000;
            }
        }
    }

    // Check for idle state
    function checkIdle() {
        const timeSinceInteraction = Date.now() - currentActivity.lastInteraction;
        currentActivity.isIdle = timeSinceInteraction > IDLE_THRESHOLD;

        if (currentActivity.isIdle) {
            currentActivity.type = 'idle';
        }
    }

    // Update interaction timestamp
    function updateInteraction() {
        currentActivity.lastInteraction = Date.now();
        currentActivity.isIdle = false;
    }

    // Send activity report to background script
    function sendActivityReport() {
        checkIdle();
        trackVideoTime();

        if (!currentActivity.isIdle) {
            currentActivity.type = detectActivityType();
        }

        const report = {
            activityType: currentActivity.type,
            scrollDepth: currentActivity.scrollDepth,
            videoWatchTime: currentActivity.videoWatchTime,
            duration: Math.round((Date.now() - currentActivity.startTime) / 1000),
            metadata: {
                hostname: window.location.hostname,
                pathname: window.location.pathname,
                scrollVelocity: scrollVelocity
            }
        };

        chrome.runtime.sendMessage({ type: 'ACTIVITY_UPDATE', data: report });
    }

    // Event listeners
    window.addEventListener('scroll', () => {
        updateScrollDepth();
        updateInteraction();
    }, { passive: true });

    window.addEventListener('mousemove', updateInteraction, { passive: true });
    window.addEventListener('keydown', updateInteraction, { passive: true });
    window.addEventListener('click', updateInteraction, { passive: true });
    window.addEventListener('touchstart', updateInteraction, { passive: true });

    // Initial scroll depth
    updateScrollDepth();

    // Periodic reporting
    setInterval(sendActivityReport, REPORT_INTERVAL);

    // Report on page unload
    window.addEventListener('beforeunload', sendActivityReport);

    console.log('Social Sentry content script loaded');
})();
