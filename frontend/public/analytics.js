/**
 * GDPR-Compliant Analytics Tracker for Portfolio Website
 *
 * Features:
 * - First-party cookies for consent and session management
 * - No IP address storage
 * - Session-based tracking (30-minute timeout)
 * - No cross-day visitor identification
 * - Tracks: page visits, scroll depth, section viewing, session duration
 */

(function() {
    'use strict';

    /**
     * Generate UUID v4 with fallback for older browsers (iOS < 15.4)
     */
    function generateUUID() {
        // Try modern crypto.randomUUID() first
        if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
            return crypto.randomUUID();
        }

        // Fallback for older browsers
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    // Configuration
    const CONFIG = {
        apiBaseUrl: (function() {
            const hostname = window.location.hostname;
            const protocol = window.location.protocol;

            // Use HTTPS as primary indicator for production
            // Only use local API if it's HTTP (not HTTPS) AND a local/private IP
            const isLocal = protocol === 'http:' && (
                           hostname === 'localhost' ||
                           hostname === '127.0.0.1' ||
                           hostname.endsWith('.local') ||
                           /^192\.168\.\d{1,3}\.\d{1,3}$/.test(hostname) || // 192.168.x.x
                           /^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(hostname) || // 10.x.x.x
                           /^172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}$/.test(hostname) || // 172.16-31.x.x
                           hostname.includes('-')); // Surface-Jan, etc.

            if (isLocal) {
                // Use same hostname as frontend (so iPhone can reach Windows PC backend)
                const apiUrl = `${protocol}//${hostname}:5000/api/analytics`;
                console.log(`Analytics: Local network detected - hostname: ${hostname}, API: ${apiUrl}`);
                return apiUrl;
            } else {
                // Production: Use relative URL with base path detection
                const basePath = window.location.pathname.split('/')[1];
                const prodUrl = basePath ? `/${basePath}/api/analytics` : '/api/analytics';
                console.log(`Analytics: Production detected - protocol: ${protocol}, hostname: ${hostname}, basePath: ${basePath}, API: ${prodUrl}`);
                return prodUrl;
            }
        })(),

        // Sections to track (based on your portfolio structure)
        sections: ['hero', 'ueber-mich', 'statements', 'projekte', 'lebenslauf', 'abschluss', 'kontakt'],

        // Scroll depth milestones to track
        scrollMilestones: [25, 50, 75, 100],

        // Debounce timing for scroll events (ms)
        scrollDebounce: 500,

        // Section view minimum duration to record (ms)
        minSectionDuration: 1000
    };

    // Share API base for consent script to send cookieless pings without duplicating logic
    window.ANALYTICS_API_BASE = CONFIG.apiBaseUrl;

    // State management
    let state = {
        sessionId: null,
        visitStart: null,
        currentSection: null,
        sectionEnterTime: null,
        recordedScrollDepths: new Set(),
        isTracking: false,
        isPaused: false,
        pausedAt: null,
        totalPausedTime: 0,
        visitEndSent: false,
        lastUpdateSent: 0,
        periodicSaveInterval: null
    };

    /**
     * Cookie utility functions
     */
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    function setCookie(name, value, days, sameSite = 'Lax', secure = true) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));

        let cookieString = `${name}=${value};expires=${expires.toUTCString()};path=/;SameSite=${sameSite}`;

        // Only set Secure flag on HTTPS (not on localhost for development)
        if (secure && window.location.protocol === 'https:') {
            cookieString += ';Secure';
        }

        document.cookie = cookieString;
    }

    /**
     * Gets or creates a session ID from cookie
     * Sessions timeout after 30 minutes of inactivity (sliding window)
     */
    function getOrCreateSessionId() {
        const existingSession = getCookie('analytics_session');

        if (existingSession) {
            // Extend session timeout (sliding window) - 30 minutes
            const isSecure = window.location.protocol === 'https:';
            setCookie('analytics_session', existingSession, 30 / (24 * 60), 'Lax', isSecure);
            return existingSession;
        }

        // Create new session ID (UUID v4) - with fallback for older iOS
        const sessionId = generateUUID();

        // Set session cookie (expires in 30 minutes)
        const isSecure = window.location.protocol === 'https:';
        setCookie('analytics_session', sessionId, 30 / (24 * 60), 'Lax', isSecure);

        return sessionId;
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
                credentials: 'include', // CRITICAL FOR COOKIES
                body: JSON.stringify({
                    ...data,
                    sessionId: state.sessionId
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
     * Detects if the browser is a mobile browser
     * Mobile browsers often don't fire beforeunload/pagehide reliably
     */
    function isMobileBrowser() {
        const ua = navigator.userAgent;

        // Check for iOS devices (includes Chrome, Firefox, Safari on iOS)
        const isIOS = /iPad|iPhone|iPod/.test(ua);

        // Check for Android devices
        const isAndroid = /Android/.test(ua);

        // Check if it's a mobile device in general
        const isMobile = /Mobile|Android|iPhone|iPad|iPod/.test(ua);

        // Additional check: small screen size (mobile)
        const isSmallScreen = window.innerWidth <= 768;

        // Touch support check
        const hasTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

        const result = isIOS || isAndroid || isMobile || (isSmallScreen && hasTouch);

        // Debug logging
        console.log('Analytics: Mobile detection -', {
            userAgent: ua,
            isIOS,
            isAndroid,
            isMobile,
            isSmallScreen,
            hasTouch,
            isMobileBrowser: result
        });

        return result;
    }

    /**
     * Handles page visibility changes (tab switching)
     * Also used for mobile browsers which don't fire beforeunload reliably
     */
    function handleVisibilityChange() {
        if (!state.isTracking) return;

        if (document.hidden) {
            // Tab became hidden - pause tracking
            state.isPaused = true;
            state.pausedAt = Date.now();

            // On mobile browsers (iOS Safari, Chrome Mobile, etc.),
            // visibilitychange:hidden is often the only reliable exit event
            // Send the visit end immediately
            if (isMobileBrowser()) {
                trackVisitEnd();
            }
        } else {
            // Tab became visible again - resume tracking
            if (state.isPaused && state.pausedAt) {
                const pauseDuration = Date.now() - state.pausedAt;
                state.totalPausedTime += pauseDuration;
                state.isPaused = false;
                state.pausedAt = null;
            }
        }
    }

    /**
     * Tracks the end of a visit (when user leaves the page)
     */
    function trackVisitEnd() {
        if (!state.isTracking || !state.visitStart || state.visitEndSent) return;

        // Mark as sent to prevent duplicate sends (important for mobile)
        state.visitEndSent = true;

        // Clear periodic update interval if running
        if (state.periodicSaveInterval) {
            clearInterval(state.periodicSaveInterval);
            state.periodicSaveInterval = null;
        }

        // If currently paused, add the current pause time
        let totalPaused = state.totalPausedTime;
        if (state.isPaused && state.pausedAt) {
            totalPaused += Date.now() - state.pausedAt;
        }

        // Calculate actual active duration (total time - paused time)
        const totalDuration = Date.now() - state.visitStart;
        const activeDuration = totalDuration - totalPaused;

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
            sessionId: state.sessionId,
            durationMs: Math.max(0, activeDuration) // Ensure non-negative
        });

        // Create a Blob with proper content-type for JSON
        const blob = new Blob([data], { type: 'application/json' });
        navigator.sendBeacon(`${CONFIG.apiBaseUrl}/end`, blob);

        console.log(`Analytics: Visit ended - ${Math.round(activeDuration / 1000)}s active time`);
    }

    /**
     * Periodic update for mobile browsers (updates visit duration every 10 seconds)
     * This is a fallback for mobile browsers that might not fire visibility events
     */
    function sendPeriodicUpdate() {
        if (!state.isTracking || !state.visitStart || state.visitEndSent) return;

        const now = Date.now();

        // Only send updates every 10 seconds minimum
        if (now - state.lastUpdateSent < 10000) return;

        // Calculate active duration
        let totalPaused = state.totalPausedTime;
        if (state.isPaused && state.pausedAt) {
            totalPaused += now - state.pausedAt;
        }

        const totalDuration = now - state.visitStart;
        const activeDuration = totalDuration - totalPaused;

        // Send update using sendBeacon (non-blocking)
        const data = JSON.stringify({
            sessionId: state.sessionId,
            durationMs: Math.max(0, activeDuration)
        });

        const blob = new Blob([data], { type: 'application/json' });
        navigator.sendBeacon(`${CONFIG.apiBaseUrl}/end`, blob);

        state.lastUpdateSent = now;
        console.log(`Analytics: Periodic update sent - ${Math.round(activeDuration / 1000)}s`);
    }

    /**
     * Initializes the analytics tracker
     * ONLY if user has given consent
     */
    async function initializeAnalytics() {
        // CHECK CONSENT FIRST - CRITICAL GDPR REQUIREMENT
        const consentStatus = window.ConsentManager?.hasConsent();

        if (consentStatus !== 'accepted') {
            console.log('Analytics: Tracking blocked - no consent');
            return; // EXIT - DO NOT TRACK
        }

        try {
            // Generate or retrieve session ID from cookie
            state.sessionId = getOrCreateSessionId();
            state.visitStart = Date.now();
            state.isTracking = true;

            // Track initial visit
            await trackVisit();

            // Set up scroll tracking
            window.addEventListener('scroll', handleScrollAndSection, { passive: true });

            // Track initial section (in case user doesn't scroll)
            trackSectionView();

            // Track page visibility changes (tab switching)
            document.addEventListener('visibilitychange', handleVisibilityChange);

            // Track visit end - multiple events for cross-browser compatibility
            window.addEventListener('beforeunload', trackVisitEnd);
            window.addEventListener('pagehide', trackVisitEnd);

            // Page Lifecycle API for mobile (especially Chrome on Android)
            // 'freeze' is fired when the page is about to be frozen by the browser
            document.addEventListener('freeze', trackVisitEnd, { capture: true });

            // For mobile browsers: send periodic updates as a fallback
            // This ensures we capture data even if visibility events don't fire
            if (isMobileBrowser()) {
                state.periodicSaveInterval = setInterval(sendPeriodicUpdate, 10000); // Every 10 seconds
                console.log('Analytics: Mobile browser detected - periodic updates enabled');
            }

            console.log('Analytics: Session tracking initialized (GDPR-compliant)');
        } catch (error) {
            console.error('Analytics: Initialization failed', error);
        }
    }

    // Expose for consent banner to call after acceptance
    window.initializeAnalytics = initializeAnalytics;

    // Start tracking when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAnalytics);
    } else {
        initializeAnalytics();
    }

})();
