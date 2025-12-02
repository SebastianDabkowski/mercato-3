// Service Worker for handling push notifications

self.addEventListener('install', (event) => {
    console.log('Service Worker: Installed');
    // Activate immediately
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('Service Worker: Activated');
    // Claim clients immediately
    event.waitUntil(clients.claim());
});

self.addEventListener('push', (event) => {
    console.log('Service Worker: Push notification received', event);

    if (!event.data) {
        console.log('Service Worker: Push notification has no data');
        return;
    }

    let notificationData;
    try {
        notificationData = event.data.json();
    } catch (e) {
        console.error('Service Worker: Failed to parse push notification data', e);
        return;
    }

    const title = notificationData.title || 'Mercato Notification';
    const options = {
        body: notificationData.body || '',
        icon: notificationData.icon || '/favicon.ico',
        badge: '/favicon.ico',
        tag: 'mercato-notification',
        data: {
            url: notificationData.url || '/'
        },
        requireInteraction: false,
        silent: false
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

self.addEventListener('notificationclick', (event) => {
    console.log('Service Worker: Notification clicked', event);

    event.notification.close();

    const urlToOpen = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then((clientList) => {
                // Parse URLs for proper comparison
                const targetUrl = new URL(urlToOpen, self.location.origin);
                
                // Check if there's already a window open with the app
                for (const client of clientList) {
                    const clientUrl = new URL(client.url);
                    // Compare pathname and search, ignoring hash and protocol differences
                    if (clientUrl.pathname === targetUrl.pathname && 
                        clientUrl.search === targetUrl.search && 
                        'focus' in client) {
                        return client.focus();
                    }
                }
                // If no window is open, open a new one
                if (clients.openWindow) {
                    return clients.openWindow(targetUrl.href);
                }
            })
    );
});

self.addEventListener('notificationclose', (event) => {
    console.log('Service Worker: Notification closed', event);
});
