import 'dart:async';
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../api/api_client.dart';
import '../engine/overspeed_engine.dart';

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

    // Need Hive for auth token if connecting to backend inside background isolate
    await Hive.initFlutter();
    await Hive.openBox('settings');

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

    double currentSpeedLimit = -1.0;
    String status = "Waiting for data...";
    DateTime lastCacheTime = DateTime.fromMillisecondsSinceEpoch(0);

    StreamSubscription<Position> positionStream = Geolocator.getPositionStream(locationSettings: locationSettings).listen(
      (Position position) async {
        
        // Rate-limit API calls (fetch limit approx every 1 minute or manually cache locally)
        if (DateTime.now().difference(lastCacheTime).inSeconds > 60) {
            try {
               final apiClient = ApiClient();
               final result = await apiClient.dio.get('/speed-limit/lookup?lat=${position.latitude}&lng=${position.longitude}');
               if (result.statusCode == 200) {
                  currentSpeedLimit = result.data['speedLimitKph']?.toDouble() ?? -1.0;
                  status = result.data['source'] ?? "Unknown";
                  lastCacheTime = DateTime.now();
               }
            } catch (e) {
               currentSpeedLimit = -1.0;
               status = "Offline Fallback";
            }
        }

        double speedKph = position.speed * 3.6;

        if (currentSpeedLimit > 0) {
           overspeedEngine.processNewLocation(position, currentSpeedLimit);
        }

        if (service is AndroidServiceInstance) {
          if (overspeedEngine.isAlerting) {
            service.setForegroundNotificationInfo(
              title: "SPEED WARNING",
              content: "Reduce speed immediately! Limit: $currentSpeedLimit",
            );
            // Fire haptic/audio alert here using Vibration/AudioPlayers plugin
          } else {
            service.setForegroundNotificationInfo(
              title: "Active Monitoring",
              content: "Current Speed: ${speedKph.toInt()} km/h",
            );
          }
        }

        // Send data to foreground UI
        service.invoke('update', {
          "currentSpeed": speedKph,
          "speedLimit": currentSpeedLimit,
          "isAlerting": overspeedEngine.isAlerting,
          "providerStatus": status,
        });

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
