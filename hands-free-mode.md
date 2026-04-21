# Hands-Free Mode Design

## Core Philosophy
The driver safety assistant must require *no routine user action* once initially configured. It must run securely within the operating system's background execution limits, detecting driving passively and spinning up active GPS monitoring only when required.

## Android Implementation
* **Trigger Mechanism**: `Activity Recognition API` (Transition API). Detects `IN_VEHICLE` activities at minimal battery cost.
* **Tracking State**: When `IN_VEHICLE` is detected, the app promotes itself to a Foreground Service (type: `location`). This guarantees location updates and prevents Doze mode from killing the tracker.
* **Notification**: A persistent notification is displayed while active tracking happens, complying with Android requirements.
* **Reboot Recovery**: Leveraging `RECEIVE_BOOT_COMPLETED`, the background transition listener is quickly re-registered on device startup.
* **Degradation**: If Battery Optimization is strict (e.g. Huawei, Samsung), the foreground service might be killed if screen is off for long. The app includes a Settings diagnostic to warn the user using `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`.

## iOS Implementation
* **Trigger Mechanism**: `Significant-Change Location Service` combined with `CMMotionActivityManager`.
* **Tracking State**: iOS doesn't have \"persistent foreground services\" in the same way. We request `Always` location authorization. When motion is detected, we elevate to High Accuracy location tracking. 
* **Limitations**: iOS may suspend the app if it determines it's consuming too much power. We rely on standard background location updates with the `showsBackgroundLocationIndicator` enabled, which keeps the app alive as a background location session.
* **Reboot Recovery**: `Significant-Change` automatically resurrects the app in the background.

## The State Machine (DrivingDetectionEngine)
1. `idle`: Passively listening to low-power OS events.
2. `possible_vehicle_motion`: Motion triggered, booting up GPS to verify speed > 15 km/h.
3. `actively_driving`: Speed confirmed. Requesting speed limits, evaluating overspeed logic.
4. `stopped_temporarily`: Speed < 5 km/h for less than 3 minutes.
5. `driving_ended`: Speed < 5 km/h for > 5 minutes. Downshifting back to `idle`.
