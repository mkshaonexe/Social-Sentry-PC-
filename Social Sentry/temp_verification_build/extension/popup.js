// Popup script for Social Sentry extension

document.addEventListener('DOMContentLoaded', async () => {
    const statusIndicator = document.getElementById('statusIndicator');
    const statusLabel = document.getElementById('statusLabel');
    const statusDetail = document.getElementById('statusDetail');
    const activityType = document.getElementById('activityType');
    const pageTime = document.getElementById('pageTime');
    const scrollDepth = document.getElementById('scrollDepth');

    // Check connection status
    const { isConnected } = await chrome.storage.local.get('isConnected');

    if (isConnected) {
        statusIndicator.classList.add('connected');
        statusLabel.textContent = 'Connected';
        statusDetail.textContent = 'Tracking activity';
    } else {
        statusIndicator.classList.add('disconnected');
        statusLabel.textContent = 'Disconnected';
        statusDetail.textContent = 'Open Social Sentry app';
    }

    // Get current tab info
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    if (tab) {
        // Request activity info from content script
        try {
            const response = await chrome.tabs.sendMessage(tab.id, { type: 'GET_ACTIVITY' });
            if (response) {
                activityType.textContent = formatActivityType(response.activityType);
                pageTime.textContent = formatTime(response.duration);
                scrollDepth.textContent = `${response.scrollDepth}%`;
            }
        } catch (e) {
            activityType.textContent = 'N/A';
        }
    }

    // Open app link
    document.getElementById('openApp').addEventListener('click', (e) => {
        e.preventDefault();
        // Could use a custom protocol handler here
        window.close();
    });
});

function formatActivityType(type) {
    const typeMap = {
        'video_watching': '🎬 Watching Video',
        'reels': '📱 Viewing Reels',
        'doom_scrolling': '📜 Scrolling Feed',
        'social_feed': '📱 Social Media',
        'studying': '📚 Reading/Studying',
        'browsing': '🌐 Browsing',
        'idle': '💤 Idle'
    };
    return typeMap[type] || type;
}

function formatTime(seconds) {
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}
