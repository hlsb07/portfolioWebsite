/**
 * GDPR-Compliant Consent Banner
 *
 * Manages user consent for analytics tracking
 * - First-party cookies only
 * - Blocks tracking until explicit consent
 * - Equal prominence for accept/reject (no dark patterns)
 */

(function() {
    'use strict';

    const ESSENTIAL_NOTICE_ID = 'analytics-essential-notice';
    let basicPingSent = false;

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
        console.log(`Cookie set: ${name}=${value} (expires in ${days} days, SameSite=${sameSite}, protocol=${window.location.protocol})`);

        // Verify cookie was set (important for iOS debugging)
        const verification = getCookie(name);
        if (verification !== value) {
            console.error(`Cookie verification failed! Expected: ${value}, Got: ${verification}`);
        }
    }

    function deleteCookie(name) {
        let cookieString = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;SameSite=Lax`;
        if (window.location.protocol === 'https:') {
            cookieString += ';Secure';
        }
        document.cookie = cookieString;
    }

    /**
     * Essential-only UI helpers
     */
    function showEssentialOnlyNotice() {
        const notice = document.getElementById(ESSENTIAL_NOTICE_ID);
        if (notice) {
            notice.style.display = 'block';
        }
    }

    function hideEssentialOnlyNotice() {
        const notice = document.getElementById(ESSENTIAL_NOTICE_ID);
        if (notice) {
            notice.style.display = 'none';
        }
    }

    /**
     * Minimal device detection without user-agent parsing
     */
    function getDeviceCategoryFromViewport() {
        const width = window.innerWidth || document.documentElement.clientWidth || 0;
        return width < 768 ? 'mobile' : 'desktop';
    }

    /**
     * Share the API base URL logic without triggering analytics
     */
    function getApiBaseUrl() {
        if (window.ANALYTICS_API_BASE) {
            return window.ANALYTICS_API_BASE;
        }

        const hostname = window.location.hostname;
        const protocol = window.location.protocol;
        const isLocal = hostname === 'localhost' ||
            hostname === '127.0.0.1' ||
            hostname.endsWith('.local') ||
            /^192\.168\.\d{1,3}\.\d{1,3}$/.test(hostname) ||
            /^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(hostname) ||
            /^172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}$/.test(hostname) ||
            hostname.includes('-');

        if (isLocal) {
            return `${protocol}//${hostname}:5000/api/analytics`;
        }

        return 'https://api.jan-huelsbrink.de/api/analytics';
    }

    /**
     * Optional, cookieless page view ping for essential-only mode
     */
    async function sendBasicPageViewPing() {
        if (basicPingSent) return;
        basicPingSent = true;

        const apiBase = getApiBaseUrl();
        if (!apiBase) return;

        try {
            const response = await fetch(`${apiBase}/basic`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'omit', // absolutely no cookies or identifiers
                keepalive: true,
                body: JSON.stringify({
                    path: window.location.pathname,
                    device: getDeviceCategoryFromViewport()
                })
            });
            if (!response.ok) {
                console.warn('Consent: Basic page view ping rejected', response.status, response.statusText);
                return;
            }
            console.log('Consent: Basic page view ping stored (cookieless)');
        } catch (error) {
            console.warn('Consent: Basic page view ping failed', error);
        }
    }

    /**
     * Enforce essential-only mode after rejection
     */
    function enterEssentialOnlyMode() {
        // Remove any analytics session identifiers
        deleteCookie('analytics_session');
        try {
            localStorage.removeItem('analytics_session');
            sessionStorage.removeItem('analytics_session');
        } catch (storageError) {
            console.warn('Consent: Unable to clear storage', storageError);
        }

        showEssentialOnlyNotice();
        sendBasicPageViewPing();
    }

    /**
     * Consent state management
     */
    function hasConsent() {
        const consent = getCookie('analytics_consent');
        return consent; // Returns: 'accepted' | 'rejected' | null
    }

    function setConsent(accepted) {
        const value = accepted ? 'accepted' : 'rejected';
        setCookie('analytics_consent', value, 182); // 6 months = 182 days

        if (!accepted) {
            // Delete session cookie if user rejects
            deleteCookie('analytics_session');
            enterEssentialOnlyMode();
        } else {
            hideEssentialOnlyNotice();
        }
    }

    /**
     * Banner UI management
     */
    function showConsentBanner() {
        const banner = document.getElementById('consent-banner');
        if (banner) {
            banner.style.display = 'block';
        }
    }

    function hideConsentBanner() {
        const banner = document.getElementById('consent-banner');
        if (banner) {
            banner.style.display = 'none';
        }
    }

    /**
     * Event handlers
     */
    function onAcceptConsent() {
        console.log('Consent: User accepted analytics');
        setConsent(true);
        hideConsentBanner();

        // Initialize analytics if available, with retry for iOS
        function tryInitialize(attempts = 0) {
            if (window.initializeAnalytics) {
                console.log('Consent: Initializing analytics...');
                window.initializeAnalytics();
            } else if (attempts < 10) {
                // Retry up to 10 times (1 second total) for iOS
                console.log(`Consent: Analytics not ready, retrying... (${attempts + 1}/10)`);
                setTimeout(() => tryInitialize(attempts + 1), 100);
            } else {
                console.error('Consent: Analytics initialization failed - window.initializeAnalytics not found');
            }
        }
        tryInitialize();
    }

    function onRejectConsent() {
        setConsent(false);
        hideConsentBanner();
        console.log('Consent: User rejected analytics - switching to essential-only mode');
    }

    /**
     * Initialize consent banner
     */
    function initConsentBanner() {
        console.log('Consent: Initializing consent banner...');
        console.log('Consent: User-Agent:', navigator.userAgent);
        console.log('Consent: Protocol:', window.location.protocol);
        console.log('Consent: Host:', window.location.host);

        const consent = hasConsent();
        console.log('Consent: Current consent status:', consent);

        if (consent === null) {
            // No consent decision yet - show banner
            console.log('Consent: No consent decision - showing banner');
            showConsentBanner();
        } else if (consent === 'accepted') {
            console.log('Consent: Already accepted - auto-initializing analytics');
            // Auto-initialize analytics if consent was previously given
            if (window.initializeAnalytics) {
                window.initializeAnalytics();
            }
        } else if (consent === 'rejected') {
            console.log('Consent: Already rejected - enforcing essential-only mode');
            enterEssentialOnlyMode();
        }

        // Attach event listeners
        const acceptBtn = document.getElementById('consent-accept');
        const rejectBtn = document.getElementById('consent-reject');

        console.log('Consent: Accept button found:', !!acceptBtn);
        console.log('Consent: Reject button found:', !!rejectBtn);

        if (acceptBtn) acceptBtn.addEventListener('click', onAcceptConsent);
        if (rejectBtn) rejectBtn.addEventListener('click', onRejectConsent);
    }

    /**
     * Expose public API
     */
    window.ConsentManager = {
        hasConsent,
        setConsent,
        showConsentBanner,
        hideConsentBanner,
        enterEssentialOnlyMode
    };

    /**
     * Auto-initialize when DOM is ready
     */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initConsentBanner);
    } else {
        initConsentBanner();
    }

})();
