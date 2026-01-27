// Social Sentry Background Service Worker
// Handles API communication with Social Sentry desktop app

const API_BASE = 'http://localhost:5123/api';
let isConnected = false;

// Activity queue for batching
let activityQueue = [];

// Check connection to Social Sentry
async function checkConnection() {
    try {
        const response = await fetch(`${API_BASE}/heartbeat`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });
        isConnected = response.ok;
        await chrome.storage.local.set({ isConnected });
        return isConnected;
    } catch (error) {
        isConnected = false;
        await chrome.storage.local.set({ isConnected });
        return false;
    }
}

// Send activity data to Social Sentry
async function sendActivity(activity) {
    if (!isConnected) {
        await checkConnection();
    }

    try {
        const response = await fetch(`${API_BASE}/activity`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(activity)
        });
        return response.ok;
    } catch (error) {
        console.error('Failed to send activity:', error);
        return false;
    }
}

// Process queued activities
async function flushActivityQueue() {
    if (activityQueue.length === 0) return;

    const activities = [...activityQueue];
    activityQueue = [];

    for (const activity of activities) {
        await sendActivity(activity);
    }
}

// Listen for messages from content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === 'ACTIVITY_UPDATE') {
        const activity = {
            ...message.data,
            tabId: sender.tab?.id,
            url: sender.tab?.url,
            title: sender.tab?.title,
            timestamp: new Date().toISOString()
        };

        activityQueue.push(activity);
        sendResponse({ success: true });
    }

    if (message.type === 'CHECK_CONNECTION') {
        checkConnection().then(connected => {
            sendResponse({ connected });
        });
        return true; // Async response
    }

    return true;
});

// Set up periodic tasks
chrome.alarms.create('flushActivities', { periodInMinutes: 0.1 }); // Every 6 seconds
chrome.alarms.create('checkConnection', { periodInMinutes: 1 });

chrome.alarms.onAlarm.addListener((alarm) => {
    if (alarm.name === 'flushActivities') {
        flushActivityQueue();
    }
    if (alarm.name === 'checkConnection') {
        checkConnection();
    }
});

// Initial connection check
checkConnection();

console.log('Social Sentry Extension loaded');
