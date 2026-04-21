import 'dart:async';
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

class BackgroundMonitoringService {
  static Future<void> initialize() async {
    final service = FlutterBackgroundService();

    const AndroidNotificationChannel channel = AndroidNotificationChannel(
      'speed_alert_foreground', // id
      'Active Monitoring', // title
      description: 'Constantly monitoring speed and safety.', // description
      importance: Importance.low,
    );

    final FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin = FlutterLocalNotificationsPlugin();
    await flutterLocalNotificationsPlugin
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);

    await service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: 'speed_alert_foreground',
        initialNotificationTitle: 'Speed Alert',
        initialNotificationContent: 'Monitoring environment...',
        foregroundServiceNotificationId: 888,
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: onStart,
        onBackground: onIosBackground,
      ),
    );
  }

  @pragma('vm:entry-point')
  static Future<bool> onIosBackground(ServiceInstance service) async {
    return true;
  }

  @pragma('vm:entry-point')
  static void onStart(ServiceInstance service) async {
    DartPluginRegistrant.ensureInitialized();
    
    // Listen for stops
    service.on('stopService').listen((event) {
      service.stopSelf();
    });

    // Initialize Geolocator and bind to OverspeedEngine
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) return;

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) return;

    final overspeedEngine = OverspeedEngine(toleranceKph: 5, requiredDurationSeconds: 3);

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10, // meters
    );

    StreamSubscription<Position> positionStream = Geolocator.getPositionStream(locationSettings: locationSettings).listen(
      (Position position) async {
        // Here we would normally query the local cache or backend API for the current speed limit.
        // For the hands-free offline pipeline, we assume 60kph if unknown, or retrieve from Hive.
        double currentSpeedLimit = 60.0; // Mock lookup

        overspeedEngine.processNewLocation(position, currentSpeedLimit);

        if (overspeedEngine.isAlerting) {
          if (service is AndroidServiceInstance) {
            service.setForegroundNotificationInfo(
              title: "SPEED WARNING",
              content: "Reduce speed immediately! Limit: $currentSpeedLimit",
            );
          }
          // Fire haptic/audio alert here using Vibration/AudioPlayers plugin
        } else {
          if (service is AndroidServiceInstance) {
            service.setForegroundNotificationInfo(
              title: "Active Monitoring",
              content: "Current Speed: ${(position.speed * 3.6).toInt()} km/h",
            );
          }
        }
      }
    );

    // Periodic heartbeat
    Timer.periodic(const Duration(minutes: 1), (timer) async {
      if (service is AndroidServiceInstance) {
        if (!(await service.isForegroundService())) {
          service.stopSelf();
        }
      }
    });
  }
}
