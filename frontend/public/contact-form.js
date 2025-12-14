/**
 * Contact Form Handler with SMTP Integration
 * Includes honeypot spam protection and form validation
 */

class ContactFormHandler {
  constructor(formSelector, apiEndpoint) {
    this.form = document.querySelector(formSelector);
    this.apiEndpoint = apiEndpoint;
    this.submitButton = null;
    this.statusMessage = null;

    if (this.form) {
      this.init();
    }
  }

  init() {
    this.submitButton = this.form.querySelector('button[type="submit"]');
    this.createStatusMessage();
    this.attachEventListeners();
  }

  createStatusMessage() {
    // Create status message element if it doesn't exist
    let statusEl = this.form.querySelector('.form-status');
    if (!statusEl) {
      statusEl = document.createElement('div');
      statusEl.className = 'form-status';
      statusEl.setAttribute('role', 'alert');
      statusEl.setAttribute('aria-live', 'polite');

      // Insert before form actions
      const formActions = this.form.querySelector('.form-actions');
      if (formActions) {
        formActions.parentNode.insertBefore(statusEl, formActions);
      } else {
        this.form.appendChild(statusEl);
      }
    }
    this.statusMessage = statusEl;
  }

  attachEventListeners() {
    this.form.addEventListener('submit', (e) => this.handleSubmit(e));
  }

  async handleSubmit(event) {
    event.preventDefault();

    // Clear previous status
    this.clearStatus();

    // Get form data
    const formData = new FormData(this.form);
    const data = {
      name: formData.get('name')?.trim() || '',
      email: formData.get('email')?.trim() || '',
      message: formData.get('message')?.trim() || '',
      honeyPot: formData.get('website')?.trim() || '', // Honeypot field
      sendConfirmation: true
    };

    // Validate form data
    const validation = this.validateForm(data);
    if (!validation.valid) {
      this.showStatus(validation.message, 'error');
      return;
    }

    // Check honeypot (if filled, it's likely a bot)
    if (data.honeyPot) {
      console.warn('Honeypot triggered - potential bot detected');
      // Show success to bot but don't actually send
      this.showStatus('Vielen Dank für Ihre Nachricht!', 'success');
      this.form.reset();
      return;
    }

    // Disable submit button
    this.setLoading(true);

    try {
      const response = await fetch(this.apiEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data)
      });

      const result = await response.json();

      if (response.ok && result.success) {
        this.showStatus(
          result.message || 'Vielen Dank für Ihre Nachricht! Ich werde mich so schnell wie möglich bei Ihnen melden.',
          'success'
        );
        this.form.reset();

        // Optional: Track form submission with analytics
        if (window.analytics && typeof window.analytics.trackEvent === 'function') {
          window.analytics.trackEvent('contact_form_submitted');
        }
      } else {
        this.showStatus(
          result.message || 'Es gab ein Problem beim Senden Ihrer Nachricht. Bitte versuchen Sie es später erneut.',
          'error'
        );
      }
    } catch (error) {
      console.error('Contact form error:', error);
      this.showStatus(
        'Es gab ein Problem beim Senden Ihrer Nachricht. Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.',
        'error'
      );
    } finally {
      this.setLoading(false);
    }
  }

  validateForm(data) {
    // Name validation
    if (!data.name || data.name.length < 2) {
      return {
        valid: false,
        message: 'Bitte geben Sie einen gültigen Namen ein (mindestens 2 Zeichen).'
      };
    }

    if (data.name.length > 100) {
      return {
        valid: false,
        message: 'Der Name darf nicht länger als 100 Zeichen sein.'
      };
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!data.email || !emailRegex.test(data.email)) {
      return {
        valid: false,
        message: 'Bitte geben Sie eine gültige E-Mail-Adresse ein.'
      };
    }

    if (data.email.length > 255) {
      return {
        valid: false,
        message: 'Die E-Mail-Adresse darf nicht länger als 255 Zeichen sein.'
      };
    }

    // Message validation
    if (!data.message || data.message.length < 10) {
      return {
        valid: false,
        message: 'Bitte geben Sie eine Nachricht ein (mindestens 10 Zeichen).'
      };
    }

    if (data.message.length > 5000) {
      return {
        valid: false,
        message: 'Die Nachricht darf nicht länger als 5000 Zeichen sein.'
      };
    }

    return { valid: true };
  }

  showStatus(message, type) {
    if (!this.statusMessage) return;

    this.statusMessage.textContent = message;
    this.statusMessage.className = `form-status form-status--${type}`;
    this.statusMessage.style.display = 'block';

    // Auto-hide success messages after 8 seconds
    if (type === 'success') {
      setTimeout(() => {
        this.clearStatus();
      }, 8000);
    }
  }

  clearStatus() {
    if (!this.statusMessage) return;
    this.statusMessage.textContent = '';
    this.statusMessage.className = 'form-status';
    this.statusMessage.style.display = 'none';
  }

  setLoading(loading) {
    if (!this.submitButton) return;

    if (loading) {
      this.submitButton.disabled = true;
      this.submitButton.dataset.originalText = this.submitButton.textContent;
      this.submitButton.textContent = 'Wird gesendet...';
      this.submitButton.classList.add('loading');
    } else {
      this.submitButton.disabled = false;
      this.submitButton.textContent = this.submitButton.dataset.originalText || 'Nachricht senden';
      this.submitButton.classList.remove('loading');
    }
  }
}

/**
 * Quick Feedback Button Handler
 * Sends a simple notification email when user clicks "Bewerbung angesehen"
 */
class QuickFeedbackHandler {
  constructor(buttonSelector, apiEndpoint) {
    this.button = document.querySelector(buttonSelector);
    this.statusElement = document.querySelector('#quick-feedback-status');
    this.apiEndpoint = apiEndpoint;

    if (this.button) {
      this.init();
    }
  }

  init() {
    this.button.addEventListener('click', () => this.handleClick());
  }

  async handleClick() {
    // Disable button
    this.button.disabled = true;
    const originalText = this.button.textContent;
    this.button.textContent = 'Wird gesendet...';

    try {
      // Get visitor info from browser
      const visitorInfo = this.getVisitorInfo();

      // Send notification email
      const response = await fetch(this.apiEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: 'Anonymer Besucher',
          email: 'noreply@portfolio.com', // Placeholder email
          message: `Ein Besucher hat die Bewerbung angesehen und auf "Bewerbung angesehen 👍" geklickt.\n\nBesucher-Informationen:\n${visitorInfo}`,
          honeyPot: '',
          sendConfirmation: false // Don't send confirmation to placeholder email
        })
      });

      const result = await response.json();

      if (response.ok && result.success) {
        this.showStatus('Vielen Dank für Ihr Feedback! 🎉', 'success');
        this.button.textContent = 'Feedback gesendet ✓';

        // Track with analytics if available
        if (window.analytics && typeof window.analytics.trackEvent === 'function') {
          window.analytics.trackEvent('quick_feedback_submitted');
        }

        // Keep button disabled
        setTimeout(() => {
          this.hideStatus();
        }, 5000);
      } else {
        throw new Error(result.message || 'Failed to send feedback');
      }
    } catch (error) {
      console.error('Quick feedback error:', error);
      this.showStatus('Feedback konnte nicht gesendet werden. Bitte nutzen Sie das Kontaktformular unten.', 'error');
      this.button.disabled = false;
      this.button.textContent = originalText;

      setTimeout(() => {
        this.hideStatus();
      }, 8000);
    }
  }

  getVisitorInfo() {
    const now = new Date();
    const info = [];

    // Timestamp
    info.push(`Zeitpunkt: ${now.toLocaleString('de-DE')}`);

    // Browser info
    if (navigator.userAgent) {
      info.push(`Browser: ${navigator.userAgent}`);
    }

    // Screen resolution
    if (window.screen) {
      info.push(`Bildschirmauflösung: ${window.screen.width}x${window.screen.height}`);
    }

    // Language
    if (navigator.language) {
      info.push(`Sprache: ${navigator.language}`);
    }

    // Referrer
    if (document.referrer) {
      info.push(`Referrer: ${document.referrer}`);
    }

    return info.join('\n');
  }

  showStatus(message, type) {
    if (!this.statusElement) return;

    this.statusElement.textContent = message;
    this.statusElement.className = `quick-feedback-status quick-feedback-status--${type}`;
    this.statusElement.style.display = 'block';
  }

  hideStatus() {
    if (!this.statusElement) return;
    this.statusElement.style.display = 'none';
  }
}

// Initialize contact form and quick feedback when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  // Determine API endpoint based on environment
  const isDevelopment = window.location.hostname === 'localhost' ||
                        window.location.hostname === '127.0.0.1' ||
                        window.location.hostname.includes('192.168');

  const apiEndpoint = isDevelopment
    ? 'http://localhost:5000/api/contact/submit'
    : 'https://app.jan-huelsbrink.de/api/contact/submit'; // Change to your production URL

  // Initialize the contact form handler
  new ContactFormHandler('.contact-form', apiEndpoint);

  // Initialize the quick feedback button
  new QuickFeedbackHandler('#quick-feedback-btn', apiEndpoint);
});
