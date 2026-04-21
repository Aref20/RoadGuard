# Known Limitations

This application provides hands-free driving capabilities, but cannot circumvent hard OS limitations.

## Android
1. **Battery Optimization**: Devices from manufacturers like Huawei, Xiaomi, and Samsung aggressively kill background apps, even foreground services, if the screen is off. The user **MUST** manually exclude the app from Battery Savings if they want true hands-free reliability on these devices.
2. **Activity Recognition Delays**: The `IN_VEHICLE` transition relies on Google Play Services and may take 1-3 minutes of driving to trigger reliably.

## iOS
1. **No True "Foreground Services"**: iOS apps cannot permanently run in the background doing nothing. We must rely on `Significant-Change Location Service` to wake the app. When awoken, we have a short time to establish a High-Accuracy background location session.
2. **Silent Terminations**: If the app uses too much CPU or memory while backgrounded, iOS will silently terminate it. The app relies on the next location event to reboot, meaning partial trips might be lost in edge cases.

## System / GPS
1. **Tunnels / Urban Canyons**: GPS signal loss will freeze actual speed. The system degrades gracefully and stops alerting when location accuracy drops > 50 meters.
2. **Data Staleness**: Speed limit APIs may not reflect temporary road works. The app makes no legal claim to accuracy.
