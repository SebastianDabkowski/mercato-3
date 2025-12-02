// Push Notifications Manager

class PushNotificationManager {
    constructor() {
        this.vapidPublicKey = null;
        this.registration = null;
        this.isInitialized = false;
    }

    /**
     * Initialize the push notification manager
     */
    async init() {
        if (this.isInitialized) {
            return;
        }

        // Check if browser supports service workers and push notifications
        if (!('serviceWorker' in navigator)) {
            console.log('Service workers are not supported in this browser');
            return;
        }

        if (!('PushManager' in window)) {
            console.log('Push notifications are not supported in this browser');
            return;
        }

        try {
            // Register service worker
            this.registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });

            console.log('Service Worker registered:', this.registration);

            // Wait for service worker to be ready
            await navigator.serviceWorker.ready;

            // Get VAPID public key from server
            await this.fetchVapidPublicKey();

            this.isInitialized = true;

            // Check current permission status
            const permission = Notification.permission;
            console.log('Current notification permission:', permission);

            // Auto-subscribe if permission was already granted
            if (permission === 'granted') {
                await this.subscribe();
            }
        } catch (error) {
            console.error('Failed to initialize push notifications:', error);
        }
    }

    /**
     * Fetch VAPID public key from server
     */
    async fetchVapidPublicKey() {
        try {
            const response = await fetch('/Api/Push/Status');
            if (!response.ok) {
                throw new Error('Failed to fetch VAPID public key');
            }
            const data = await response.json();
            this.vapidPublicKey = data.vapidPublicKey;
            console.log('VAPID public key received');
        } catch (error) {
            console.error('Error fetching VAPID public key:', error);
            throw error;
        }
    }

    /**
     * Request permission and subscribe to push notifications
     */
    async requestPermissionAndSubscribe() {
        if (!this.isInitialized) {
            await this.init();
        }

        if (!this.isInitialized) {
            console.error('Push notification manager is not initialized');
            return false;
        }

        try {
            // Request permission
            const permission = await Notification.requestPermission();
            console.log('Notification permission:', permission);

            if (permission === 'granted') {
                // Subscribe to push notifications
                return await this.subscribe();
            } else {
                console.log('Notification permission denied');
                return false;
            }
        } catch (error) {
            console.error('Error requesting notification permission:', error);
            return false;
        }
    }

    /**
     * Subscribe to push notifications
     */
    async subscribe() {
        if (!this.isInitialized || !this.registration) {
            console.error('Push notification manager is not initialized');
            return false;
        }

        try {
            // Check if already subscribed
            const existingSubscription = await this.registration.pushManager.getSubscription();
            
            if (existingSubscription) {
                console.log('Already subscribed to push notifications');
                // Still send to server to ensure it's registered
                await this.sendSubscriptionToServer(existingSubscription);
                return true;
            }

            // Create new subscription
            const subscription = await this.registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: this.urlBase64ToUint8Array(this.vapidPublicKey)
            });

            console.log('Subscribed to push notifications:', subscription);

            // Send subscription to server
            await this.sendSubscriptionToServer(subscription);

            return true;
        } catch (error) {
            console.error('Error subscribing to push notifications:', error);
            return false;
        }
    }

    /**
     * Unsubscribe from push notifications
     */
    async unsubscribe() {
        if (!this.registration) {
            console.error('Service worker not registered');
            return false;
        }

        try {
            const subscription = await this.registration.pushManager.getSubscription();
            
            if (!subscription) {
                console.log('Not currently subscribed to push notifications');
                return true;
            }

            // Unsubscribe from push service
            await subscription.unsubscribe();
            console.log('Unsubscribed from push notifications');

            // Notify server
            await this.sendUnsubscribeToServer(subscription.endpoint);

            return true;
        } catch (error) {
            console.error('Error unsubscribing from push notifications:', error);
            return false;
        }
    }

    /**
     * Send subscription to server
     */
    async sendSubscriptionToServer(subscription) {
        try {
            const response = await fetch('/Api/Push/Subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    endpoint: subscription.endpoint,
                    keys: {
                        p256dh: this.arrayBufferToBase64(subscription.getKey('p256dh')),
                        auth: this.arrayBufferToBase64(subscription.getKey('auth'))
                    }
                })
            });

            if (!response.ok) {
                throw new Error('Failed to send subscription to server');
            }

            const data = await response.json();
            console.log('Subscription sent to server:', data);
        } catch (error) {
            console.error('Error sending subscription to server:', error);
            throw error;
        }
    }

    /**
     * Send unsubscribe request to server
     */
    async sendUnsubscribeToServer(endpoint) {
        try {
            const response = await fetch('/Api/Push/Unsubscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ endpoint })
            });

            if (!response.ok) {
                throw new Error('Failed to send unsubscribe request to server');
            }

            console.log('Unsubscribe request sent to server');
        } catch (error) {
            console.error('Error sending unsubscribe request to server:', error);
            throw error;
        }
    }

    /**
     * Get current subscription status
     */
    async getSubscriptionStatus() {
        if (!this.registration) {
            return { subscribed: false, permission: Notification.permission };
        }

        try {
            const subscription = await this.registration.pushManager.getSubscription();
            return {
                subscribed: subscription !== null,
                permission: Notification.permission,
                subscription: subscription
            };
        } catch (error) {
            console.error('Error getting subscription status:', error);
            return { subscribed: false, permission: Notification.permission };
        }
    }

    /**
     * Convert URL-safe base64 to Uint8Array
     */
    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    /**
     * Convert ArrayBuffer to base64
     */
    arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }
}

// Create global instance
window.pushNotificationManager = new PushNotificationManager();

// Initialize on page load if user is authenticated
document.addEventListener('DOMContentLoaded', async () => {
    // Check if user is authenticated (by checking for authentication-specific elements)
    const isAuthenticated = document.querySelector('[data-authenticated]') !== null;
    
    if (isAuthenticated) {
        try {
            await window.pushNotificationManager.init();
        } catch (error) {
            console.error('Failed to initialize push notifications on page load:', error);
        }
    }
});
