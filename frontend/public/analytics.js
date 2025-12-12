/**
 * GDPR-Compliant Analytics Tracker for Portfolio Website
 *
 * Features:
 * - No cookies
 * - No IP address storage
 * - Anonymous visitor identification using SHA-256 hash
 * - Tracks: page visits, scroll depth, section viewing, session duration
 */

(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        apiBaseUrl: window.location.hostname === 'localhost'
            ? 'http://localhost:5000/api/analytics'
            : 'https://api.jan-huelsbrink.de/api/analytics',

        // Sections to track (based on your portfolio structure)
        sections: ['hero', 'ueber-mich', 'statements', 'projekte', 'lebenslauf', 'abschluss', 'kontakt'],

        // Scroll depth milestones to track
        scrollMilestones: [25, 50, 75, 100],

        // Debounce timing for scroll events (ms)
        scrollDebounce: 500,

        // Section view minimum duration to record (ms)
        minSectionDuration: 1000
    };

    // State management
    let state = {
        anonymousId: null,
        visitStart: null,
        currentSection: null,
        sectionEnterTime: null,
        recordedScrollDepths: new Set(),
        isTracking: false
    };

    /**
     * Generates an anonymous SHA-256 hash from the User-Agent
     * This creates a non-reversible, GDPR-compliant visitor identifier
     */
    async function generateAnonymousId() {
        const userAgent = navigator.userAgent;
        const encoder = new TextEncoder();
        const data = encoder.encode(userAgent);

        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');

        return hashHex;
    }

    /**
     * Sends analytics data to the backend
     */
    async function sendAnalytics(endpoint, data) {
        try {
            const response = await fetch(`${CONFIG.apiBaseUrl}/${endpoint}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    ...data,
                    anonymousIdHash: state.anonymousId
                })
            });

            if (!response.ok) {
                console.warn(`Analytics: Failed to send ${endpoint}`, response.statusText);
            }
        } catch (error) {
            console.warn(`Analytics: Error sending ${endpoint}`, error);
        }
    }

    /**
     * Tracks the initial page visit
     */
    async function trackVisit() {
        await sendAnalytics('visit', {
            page: window.location.pathname,
            referrer: document.referrer || null,
            userAgent: navigator.userAgent
        });
    }

    /**
     * Tracks scroll depth at specific milestones (25%, 50%, 75%, 100%)
     */
    function trackScrollDepth() {
        const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
        const scrolled = window.scrollY;
        const scrollPercent = Math.round((scrolled / scrollHeight) * 100);

        // Find the highest milestone reached
        for (const milestone of CONFIG.scrollMilestones) {
            if (scrollPercent >= milestone && !state.recordedScrollDepths.has(milestone)) {
                state.recordedScrollDepths.add(milestone);
                sendAnalytics('scroll', {
                    scrollDepthPercent: milestone
                });
            }
        }
    }

    /**
     * Debounced scroll handler
     */
    let scrollTimeout;
    function handleScroll() {
        clearTimeout(scrollTimeout);
        scrollTimeout = setTimeout(trackScrollDepth, CONFIG.scrollDebounce);
    }

    /**
     * Tracks which section of the portfolio is currently in view
     */
    function trackSectionView() {
        const sections = CONFIG.sections
            .map(id => document.getElementById(id))
            .filter(el => el !== null);

        if (sections.length === 0) return;

        // Find the section most visible in viewport
        const viewportMiddle = window.scrollY + (window.innerHeight / 2);

        let activeSection = null;
        let minDistance = Infinity;

        for (const section of sections) {
            const rect = section.getBoundingClientRect();
            const sectionMiddle = window.scrollY + rect.top + (rect.height / 2);
            const distance = Math.abs(viewportMiddle - sectionMiddle);

            if (distance < minDistance && rect.top < window.innerHeight && rect.bottom > 0) {
                minDistance = distance;
                activeSection = section.id;
            }
        }

        // Section changed
        if (activeSection && activeSection !== state.currentSection) {
            // Record time spent in previous section
            if (state.currentSection && state.sectionEnterTime) {
                const duration = Date.now() - state.sectionEnterTime;

                if (duration >= CONFIG.minSectionDuration) {
                    sendAnalytics('section', {
                        sectionName: state.currentSection,
                        durationMs: duration
                    });
                }
            }

            // Update current section
            state.currentSection = activeSection;
            state.sectionEnterTime = Date.now();
        }
    }

    /**
     * Combined scroll and section tracking handler
     */
    let trackingTimeout;
    function handleScrollAndSection() {
        clearTimeout(trackingTimeout);
        trackingTimeout = setTimeout(() => {
            trackScrollDepth();
            trackSectionView();
        }, CONFIG.scrollDebounce);
    }

    /**
     * Tracks the end of a visit (when user leaves the page)
     */
    function trackVisitEnd() {
        if (!state.isTracking || !state.visitStart) return;

        const duration = Date.now() - state.visitStart;

        // Record final section duration
        if (state.currentSection && state.sectionEnterTime) {
            const sectionDuration = Date.now() - state.sectionEnterTime;
            if (sectionDuration >= CONFIG.minSectionDuration) {
                sendAnalytics('section', {
                    sectionName: state.currentSection,
                    durationMs: sectionDuration
                });
            }
        }

        // Use sendBeacon for reliable delivery on page unload
        const data = JSON.stringify({
            anonymousIdHash: state.anonymousId,
            durationMs: duration
        });

        navigator.sendBeacon(`${CONFIG.apiBaseUrl}/end`, data);
    }

    /**
     * Initializes the analytics tracker
     */
    async function initializeAnalytics() {
        try {
            // Generate anonymous ID
            state.anonymousId = await generateAnonymousId();
            state.visitStart = Date.now();
            state.isTracking = true;

            // Track initial visit
            await trackVisit();

            // Set up scroll tracking
            window.addEventListener('scroll', handleScrollAndSection, { passive: true });

            // Track initial section (in case user doesn't scroll)
            trackSectionView();

            // Track visit end
            window.addEventListener('beforeunload', trackVisitEnd);
            window.addEventListener('pagehide', trackVisitEnd);

            console.log('Analytics: Tracking initialized (GDPR-compliant)');
        } catch (error) {
            console.error('Analytics: Initialization failed', error);
        }
    }

    // Start tracking when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAnalytics);
    } else {
        initializeAnalytics();
    }

})();
