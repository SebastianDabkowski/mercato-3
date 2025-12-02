# Web Push Notifications - Implementation Summary

## Overview
This implementation adds web push notification support to MercatoApp, allowing users to receive real-time notifications even when the application is not open in their browser.

## Features Implemented

### 1. Backend Infrastructure
- **PushSubscription Model**: Stores user device subscriptions with encryption keys
- **IPushNotificationService**: Service interface for managing push notifications
- **PushNotificationService**: Implementation that handles:
  - Subscription management (subscribe/unsubscribe)
  - VAPID key generation and management
  - Sending push notifications to subscribed devices
  - Automatic cleanup of invalid subscriptions

### 2. API Endpoints
- `POST /Api/Push/Subscribe`: Subscribe to push notifications
- `POST /Api/Push/Unsubscribe`: Unsubscribe from push notifications
- `GET /Api/Push/Status`: Get subscription status and VAPID public key

### 3. Service Worker
- **service-worker.js**: Handles push events in the background
  - Listens for push notifications
  - Displays notifications to users
  - Handles notification clicks to open relevant pages
  - Manages notification lifecycle

### 4. Client-Side JavaScript
- **push-notifications.js**: Manages push notification subscriptions
  - Registers service worker
  - Requests user permission
  - Subscribes to push notifications
  - Manages subscription state
  - Communicates with backend API

### 5. User Interface
- **Permission Banner**: Prompts users to enable push notifications
  - Shows after 3 seconds delay for authenticated users
  - Can be dismissed (saved in localStorage)
  - Shows success message after enabling
- **Automatic Integration**: Push notifications sent automatically when creating in-app notifications

## Configuration

### VAPID Keys
VAPID (Voluntary Application Server Identification) keys are required for web push. The system auto-generates keys in development, but for production, configure them in `appsettings.json`:

```json
{
  "Push": {
    "VapidPublicKey": "your-public-key-here",
    "VapidPrivateKey": "your-private-key-here",
    "VapidSubject": "mailto:admin@yourdomain.com"
  }
}
```

To generate VAPID keys, run the application once and check the logs for the generated keys.

## Security Features

1. **VAPID Authentication**: Ensures notifications come from authorized server
2. **End-to-End Encryption**: Uses p256dh and auth keys for encryption
3. **User Authorization**: API endpoints require authentication
4. **Permission-Based**: Requires explicit user permission
5. **Subscription Validation**: Automatically removes invalid subscriptions

## Integration with Existing Notifications

The system automatically sends push notifications when creating in-app notifications through `NotificationService.CreateNotificationAsync()`. This means:

- Order status updates trigger push notifications
- New messages trigger push notifications
- Return request updates trigger push notifications
- All existing notification types work with push

## Browser Compatibility

Requires browsers that support:
- Service Workers
- Push API
- Notification API

Supported browsers:
- Chrome/Edge 50+
- Firefox 44+
- Safari 16+
- Opera 37+

## Testing Push Notifications

### Manual Testing Steps

1. **Start the Application**
   ```bash
   dotnet run
   ```

2. **Login as a Test User**
   - Navigate to http://localhost:5000
   - Login with test credentials

3. **Enable Push Notifications**
   - Wait for the permission banner to appear (or click notification icon)
   - Click "Enable" button
   - Grant permission in browser prompt

4. **Verify Subscription**
   - Open browser DevTools > Console
   - Check for "Subscribed to push notifications" message
   - Navigate to `/Api/Push/Status` to see subscription details

5. **Test Notification Delivery**
   - Trigger an event that creates a notification (e.g., place an order, send a message)
   - Push notification should appear in the system tray/notification center
   - Click the notification to navigate to the relevant page

### Testing with Browser DevTools

1. **Service Worker Status**
   - Open DevTools > Application > Service Workers
   - Verify service-worker.js is registered and activated

2. **Push Subscription**
   - Open DevTools > Application > Service Workers
   - Check "Push" subscription details

3. **Manual Push Test** (Chrome/Edge)
   - Open DevTools > Application > Service Workers
   - Click "Push" next to the service worker
   - Should display a test notification

## Notification Flow

1. **User Action** → Creates notification in database
2. **NotificationService** → Calls PushNotificationService
3. **PushNotificationService** → Sends to all user's subscribed devices
4. **Push Service** (browser vendor) → Delivers to device
5. **Service Worker** → Receives push event, displays notification
6. **User Clicks** → Opens relevant page in browser

## Database Schema

### PushSubscription Table
- `Id`: Primary key
- `UserId`: Foreign key to User
- `Endpoint`: Push service URL
- `P256dh`: Encryption key (base64)
- `Auth`: Auth secret (base64)
- `CreatedAt`: Subscription creation time
- `LastUsedAt`: Last successful notification delivery
- `IsActive`: Subscription active status
- `UserAgent`: Browser user agent string

## Performance Considerations

- Push notifications are sent asynchronously
- Failed subscriptions are automatically deactivated
- Database stores minimal subscription data
- Service worker caches for offline functionality

## Known Limitations

1. **WebPush Package Vulnerability**: The WebPush package has a dependency on an old version of Newtonsoft.Json with a known vulnerability. This should be addressed in a future update by either:
   - Updating to a newer version of WebPush if available
   - Switching to an alternative push notification library
   - Upgrading Newtonsoft.Json directly if compatible

2. **HTTPS Requirement**: Push notifications require HTTPS in production (localhost works for development)

3. **Browser-Specific Behavior**: Notification appearance and behavior varies by browser and OS

4. **Service Worker Scope**: Service worker is registered at root scope ("/")

## Future Enhancements

1. **Notification Preferences**: Allow users to customize which events trigger push notifications
2. **Notification Actions**: Add action buttons to notifications (e.g., "Mark as Read", "View")
3. **Rich Notifications**: Support images, badges, and custom layouts
4. **Notification Analytics**: Track delivery rates and user engagement
5. **Multi-Device Management**: Allow users to view and manage subscriptions across devices
6. **Notification Grouping**: Group related notifications to reduce notification spam
