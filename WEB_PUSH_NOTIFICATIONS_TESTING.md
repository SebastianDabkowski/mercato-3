# Web Push Notifications - Testing Guide

## Prerequisites
- Browser with push notification support (Chrome, Firefox, Edge, or Safari 16+)
- HTTPS connection (or localhost for development)
- User account in the application

## Test Scenario 1: Permission Request Flow

### Steps
1. Start the application: `dotnet run`
2. Open browser and navigate to http://localhost:5000
3. Login with a test user account
4. Wait 3 seconds for the permission banner to appear

### Expected Results
- ✅ Permission banner displays at bottom of page with:
  - Bell icon
  - Message: "Stay Updated! Enable push notifications..."
  - "Enable" button
  - "Later" button

### Test Cases

#### TC1.1: Grant Permission
1. Click "Enable" button on the banner
2. Accept the browser's permission prompt

**Expected:**
- ✅ Browser shows native permission prompt
- ✅ After accepting, banner disappears
- ✅ Success message appears: "Push notifications enabled! You'll now receive updates."
- ✅ localStorage key 'pushNotificationBannerDismissed' is set to 'true'
- ✅ Console shows: "Subscribed to push notifications"
- ✅ Console shows subscription details

#### TC1.2: Deny Permission
1. Click "Enable" button on the banner
2. Deny the browser's permission prompt

**Expected:**
- ✅ Banner remains visible
- ✅ Console shows: "Notification permission denied"
- ✅ No error messages

#### TC1.3: Dismiss Banner
1. Click "Later" button on the banner

**Expected:**
- ✅ Banner disappears
- ✅ localStorage key 'pushNotificationBannerDismissed' is set to 'true'
- ✅ Banner doesn't reappear on page refresh

## Test Scenario 2: Service Worker Registration

### Steps
1. Login to the application
2. Open DevTools > Application > Service Workers

### Expected Results
- ✅ Service worker "service-worker.js" is registered
- ✅ Status shows "activated and is running"
- ✅ Scope is "/"
- ✅ No errors in console

### Test Cases

#### TC2.1: Service Worker Update
1. Make a change to service-worker.js
2. Refresh the page
3. Check Service Workers panel

**Expected:**
- ✅ New service worker appears as "waiting to activate"
- ✅ After closing all tabs, new version activates
- ✅ skipWaiting() ensures immediate activation

## Test Scenario 3: Push Subscription API

### Steps
1. Login to the application
2. Open browser console
3. Check subscription status

### Test Cases

#### TC3.1: Check Subscription Status
**Execute in console:**
```javascript
const status = await window.pushNotificationManager.getSubscriptionStatus();
console.log(status);
```

**Expected:**
- ✅ Returns object with:
  - `subscribed: true/false`
  - `permission: "granted"/"denied"/"default"`
  - `subscription: <subscription object>` (if subscribed)

#### TC3.2: Manual Subscribe
**Execute in console:**
```javascript
const success = await window.pushNotificationManager.requestPermissionAndSubscribe();
console.log('Subscribe success:', success);
```

**Expected:**
- ✅ Permission prompt appears (if not granted)
- ✅ Returns true if successful
- ✅ Subscription sent to server
- ✅ Console shows: "Subscribed to push notifications"

#### TC3.3: Manual Unsubscribe
**Execute in console:**
```javascript
const success = await window.pushNotificationManager.unsubscribe();
console.log('Unsubscribe success:', success);
```

**Expected:**
- ✅ Returns true
- ✅ Console shows: "Unsubscribed from push notifications"
- ✅ Server notified of unsubscription

#### TC3.4: Get VAPID Public Key
**Execute in console:**
```javascript
fetch('/Api/Push/Status')
  .then(r => r.json())
  .then(data => console.log('VAPID Public Key:', data.vapidPublicKey));
```

**Expected:**
- ✅ Returns JSON with vapidPublicKey
- ✅ Returns subscriptionCount
- ✅ Returns array of subscriptions

## Test Scenario 4: Push Notification Delivery

### Steps
1. Login and enable push notifications
2. Trigger an event that creates a notification

### Test Cases

#### TC4.1: Order Placed Notification
1. Place an order as a buyer
2. Check for push notification

**Expected:**
- ✅ Push notification appears in system notification center
- ✅ Title: "Order Placed" (or similar)
- ✅ Body: Contains order details
- ✅ Icon: Application icon
- ✅ Click opens order details page

#### TC4.2: Order Status Update
1. As a seller, update order status
2. Check buyer receives notification

**Expected:**
- ✅ Push notification appears
- ✅ Title: "Order Status Update" (or similar)
- ✅ Body: Contains new status
- ✅ Click opens order details

#### TC4.3: New Message Notification
1. Send a message in order thread
2. Check recipient receives notification

**Expected:**
- ✅ Push notification appears
- ✅ Title: "New Message" (or similar)
- ✅ Body: Message preview
- ✅ Click opens message thread

#### TC4.4: Return Request Notification
1. Create a return request
2. Check seller receives notification

**Expected:**
- ✅ Push notification appears
- ✅ Title: "Return Request" (or similar)
- ✅ Body: Contains return details
- ✅ Click opens return request page

## Test Scenario 5: Notification Click Handling

### Steps
1. Ensure push notifications are enabled
2. Receive a push notification
3. Click the notification

### Expected Results
- ✅ Notification closes
- ✅ Browser/tab opens or focuses
- ✅ Navigates to correct URL from notification data
- ✅ If tab already open with same URL, focuses that tab
- ✅ If no tab open, opens new tab

## Test Scenario 6: Multi-Device Support

### Steps
1. Login on Device 1 (e.g., Desktop Chrome)
2. Enable push notifications
3. Login on Device 2 (e.g., Mobile Chrome)
4. Enable push notifications
5. Trigger a notification event

### Expected Results
- ✅ Both devices receive the push notification
- ✅ Both subscriptions stored in database
- ✅ Each device has unique endpoint

### Verification
**Check database:**
```sql
SELECT * FROM PushSubscriptions WHERE UserId = <userId>;
```

**Expected:**
- ✅ Multiple rows for same user
- ✅ Different endpoints
- ✅ Different UserAgent strings
- ✅ All IsActive = true

## Test Scenario 7: Error Handling

### Test Cases

#### TC7.1: Invalid Subscription
1. Subscribe to push notifications
2. Manually invalidate subscription in push service
3. Trigger a notification

**Expected:**
- ✅ Service receives 410 Gone error
- ✅ Subscription marked as inactive in database
- ✅ No errors thrown to user
- ✅ Log shows: "Deactivated invalid push subscription"

#### TC7.2: Network Failure
1. Subscribe to push notifications
2. Disconnect network
3. Try to subscribe again

**Expected:**
- ✅ Retry mechanism activates
- ✅ Error logged to console
- ✅ Retries with exponential backoff
- ✅ After 3 attempts, shows error

#### TC7.3: Server Error
1. Stop the application
2. Try to subscribe to push notifications

**Expected:**
- ✅ Fetch request times out after 10 seconds
- ✅ Error logged: "Error fetching VAPID public key"
- ✅ Retry mechanism attempts 3 times
- ✅ Graceful failure, no crash

## Test Scenario 8: Browser Compatibility

### Browsers to Test
- ✅ Chrome 90+ (Windows, Mac, Linux)
- ✅ Firefox 88+ (Windows, Mac, Linux)
- ✅ Edge 90+ (Windows, Mac)
- ✅ Safari 16+ (Mac, iOS)
- ✅ Opera 76+

### Test Cases
For each browser:
1. Verify service worker registers
2. Verify permission prompt appears
3. Verify subscription succeeds
4. Verify notifications display
5. Verify notification clicks work

## Test Scenario 9: Security Validation

### Test Cases

#### TC9.1: Authentication Required
**Test unauthenticated access:**
```bash
curl -X POST http://localhost:5000/Api/Push/Subscribe \
  -H "Content-Type: application/json" \
  -d '{"endpoint":"test","keys":{"p256dh":"test","auth":"test"}}'
```

**Expected:**
- ✅ Returns 401 Unauthorized
- ✅ No subscription created

#### TC9.2: User Isolation
1. Login as User A
2. Subscribe to push notifications
3. Note the subscription endpoint
4. Login as User B
5. Try to unsubscribe User A's endpoint

**Expected:**
- ✅ Returns 404 Not Found
- ✅ User A's subscription remains active
- ✅ Users can only manage their own subscriptions

#### TC9.3: VAPID Key Security
1. Check server logs for VAPID keys
2. Inspect network traffic
3. Check client-side JavaScript

**Expected:**
- ✅ Private key never sent to client
- ✅ Public key available in API response
- ✅ Keys logged only in development mode
- ✅ Production keys should be configured (not logged)

## Test Scenario 10: Performance Testing

### Test Cases

#### TC10.1: Subscription Response Time
1. Subscribe to push notifications
2. Measure API response time

**Expected:**
- ✅ Subscribe API responds within 500ms
- ✅ Status API responds within 200ms
- ✅ Unsubscribe API responds within 300ms

#### TC10.2: Notification Delivery Time
1. Trigger notification event
2. Measure time until push notification appears

**Expected:**
- ✅ Notification appears within 2 seconds
- ✅ No significant delay even with multiple subscriptions

#### TC10.3: Multiple Subscriptions
1. Create 10 subscriptions for same user (different browsers/devices)
2. Trigger a notification
3. Verify all devices receive notification

**Expected:**
- ✅ All subscriptions receive notification
- ✅ Database shows LastUsedAt updated for all
- ✅ No timeout errors

## Automated Testing Considerations

### Unit Tests
- ✅ Test PushNotificationService.SubscribeAsync()
- ✅ Test PushNotificationService.UnsubscribeAsync()
- ✅ Test PushNotificationService.SendPushNotificationAsync()
- ✅ Test invalid subscription cleanup
- ✅ Test VAPID key generation

### Integration Tests
- ✅ Test API endpoints with authentication
- ✅ Test API endpoints without authentication
- ✅ Test invalid request payloads
- ✅ Test concurrent subscriptions

### E2E Tests (with Playwright/Selenium)
- ✅ Test permission request flow
- ✅ Test service worker registration
- ✅ Test notification display
- ✅ Test notification click
- ✅ Test multi-tab behavior

## Troubleshooting Guide

### Issue: Service Worker Not Registering
**Symptoms:** Console shows "Service workers are not supported"
**Solutions:**
- Ensure browser supports service workers
- Ensure running on HTTPS or localhost
- Check for service worker registration errors in console

### Issue: Permission Denied
**Symptoms:** Permission prompt doesn't appear or always denied
**Solutions:**
- Check browser notification settings
- Clear site permissions and retry
- Ensure site is running on HTTPS (production)

### Issue: Notifications Not Appearing
**Symptoms:** Subscription succeeds but no notifications
**Solutions:**
- Check browser notification settings (not blocked)
- Verify service worker is active
- Check browser DevTools > Application > Push for errors
- Verify server logs for push sending errors

### Issue: VAPID Error
**Symptoms:** Error: "Unauthorized VAPID keys"
**Solutions:**
- Verify VAPID keys are correctly configured
- Ensure keys match between client and server
- Regenerate VAPID keys if necessary

### Issue: Subscription Fails
**Symptoms:** Subscribe button doesn't work
**Solutions:**
- Check browser console for errors
- Verify API endpoints are accessible
- Check authentication status
- Verify VAPID public key is available

## Cleanup After Testing

1. **Clear Subscriptions:**
   ```javascript
   await window.pushNotificationManager.unsubscribe();
   ```

2. **Clear Service Worker:**
   - DevTools > Application > Service Workers
   - Click "Unregister"

3. **Clear Permissions:**
   - Browser Settings > Permissions
   - Remove notification permission for site

4. **Clear localStorage:**
   ```javascript
   localStorage.removeItem('pushNotificationBannerDismissed');
   ```

## Success Criteria

All test scenarios should pass with:
- ✅ No errors in browser console
- ✅ No errors in server logs
- ✅ Notifications delivered within 2 seconds
- ✅ All browsers supported
- ✅ Security validation passes
- ✅ Performance meets expectations
