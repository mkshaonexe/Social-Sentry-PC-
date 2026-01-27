/**
 * Generic Parser
 * Fallback parser for general browsing activity.
 */
class GenericParser {
    constructor() {
        this.name = 'generic';
    }

    /**
     * Parse the current page for activity data.
     * @returns {Object} Extracted metadata
     */
    parse() {
        return {
            platform: 'Generic',
            contentType: 'WebPage',
            url: window.location.href,
            metadata: {
                title: document.title,
                hostname: window.location.hostname,
                path: window.location.pathname,
                timestamp: new Date().toISOString()
            }
        };
    }

    /**
     * Check if this parser applies to the current page.
     * @returns {boolean} True if this is the fallback parser
     */
    isApplicable() {
        return true;
    }
}

// Export for use in Controller
if (typeof module !== 'undefined' && module.exports) {
    module.exports = GenericParser;
}
