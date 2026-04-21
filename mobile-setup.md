# Mobile Setup Guide

## Requirements
- Flutter SDK `^3.2.0`
- Android Studio / Xcode
- Android SDK 34 for Android builds
- iOS 12.0+ deployment target

## Environment Variables
Create a `.env` file in the `/mobile` root directory for local dev:
```env
API_BASE_URL=http://localhost:8080/api
ENVIRONMENT=development
```
*(In production, use `--dart-define` to inject these values).*

## Permissions Configuration
### Android (`android/app/src/main/AndroidManifest.xml`)
The system requires these permission tags to effectively run Activity Recognition and Location Services:
- `android.permission.INTERNET`
- `android.permission.ACCESS_FINE_LOCATION`
- `android.permission.ACCESS_COARSE_LOCATION`
- `android.permission.ACCESS_BACKGROUND_LOCATION`
- `android.permission.ACTIVITY_RECOGNITION`
- `android.permission.FOREGROUND_SERVICE`
- `android.permission.FOREGROUND_SERVICE_LOCATION`
- `android.permission.POST_NOTIFICATIONS`
- `android.permission.VIBRATE`
- `android.permission.RECEIVE_BOOT_COMPLETED`

### iOS (`ios/Runner/Info.plist`)
Provide accurate descriptions for Apple Review:
- `NSLocationWhenInUseUsageDescription`: To monitor speed while driving.
- `NSLocationAlwaysAndWhenInUseUsageDescription`: To automatically detect driving in the background without launching the app.
- `NSMotionUsageDescription`: To smartly determine when you are in a vehicle versus walking, saving battery.
- `UIBackgroundModes`: Location + Processing

## Running Locally
```bash
cd mobile
flutter pub get

# Generate Hive typed adapters if necessary
flutter packages pub run build_runner build

# Run
flutter run
```
