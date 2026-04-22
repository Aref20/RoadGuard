import 'dart:async';
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_background_service_android/flutter_background_service_android.dart';
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
    
    StreamSubscription<Position>? positionStream;
    Timer? heartbeatTimer;

    // Listen for stops
    service.on('stopService').listen((event) async {
      positionStream?.cancel();
      heartbeatTimer?.cancel();
      service.stopSelf();
    });

    await Hive.initFlutter();
    await Hive.openBox('settings');

    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) return;

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) return;

    final overspeedEngine = OverspeedEngine(toleranceKph: 5, requiredDurationSeconds: 3);

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10,
    );

    double currentSpeedLimit = -1.0;
    String status = "Waiting for data...";
    DateTime lastCacheTime = DateTime.fromMillisecondsSinceEpoch(0);
    
    // Session State
    bool isDriving = false;
    String? currentSessionId;
    DateTime lastMotionTime = DateTime.now();
    bool wasAlerting = false; // For edge detection
    List<Map<String, dynamic>> _offlinePointQueue = [];

    positionStream = Geolocator.getPositionStream(locationSettings: locationSettings).listen(
      (Position position) async {
        final apiClient = ApiClient();
        double speedKph = position.speed * 3.6;

        // Auto-detect driving (Speed > 15kph starts session)
        if (speedKph > 15) {
          lastMotionTime = DateTime.now();
          if (!isDriving) {
             isDriving = true;
             try {
                final response = await apiClient.dio.post('/sessions/start', data: {
                   'reason': 'auto_detect',
                   'isAuto': true
                });
                if (response.statusCode == 200) {
                   currentSessionId = response.data['sessionId'];
                   _offlinePointQueue.clear();
                }
             } catch (e) {
                // Cannot start session right now, will try next tick if speed sustains
             }
          }
        } else {
          // If stationary for > 2 mins, end session
          if (isDriving && DateTime.now().difference(lastMotionTime).inMinutes >= 2) {
             isDriving = false;
             if (currentSessionId != null) {
                try {
                  await apiClient.dio.put('/sessions/$currentSessionId/end', data: {
                    'reason': 'inactivity',
                    'distanceMeters': 0 // We'd keep a cumulative sum if needed
                  });
                } catch(e) {}
                currentSessionId = null;
                _offlinePointQueue.clear();
             }
          }
        }
        
        // Rate-limit API calls (fetch limit approx every 1 minute)
        if (DateTime.now().difference(lastCacheTime).inSeconds > 60) {
            try {
               final result = await apiClient.dio.get('/speed-limit/lookup?lat=${position.latitude}&lng=${position.longitude}');
               if (result.statusCode == 200) {
                  currentSpeedLimit = ((result.data['speedLimitKph'] ?? -1) as num).toDouble();
                  status = result.data['source'] ?? "Unknown";
                  lastCacheTime = DateTime.now();
               }
            } catch (e) {
               currentSpeedLimit = -1.0;
               status = "Offline Fallback";
            }
        }

        if (currentSpeedLimit > 0) {
           overspeedEngine.processNewLocation(position, currentSpeedLimit);
        } else {
           overspeedEngine.reset();
        }

        // Upload Points periodically or continuously if driving
        if (isDriving && currentSessionId != null) {
           var pointData = {
              'timestamp': DateTime.now().toIso8601String(),
              'lat': position.latitude,
              'lng': position.longitude,
              'speed': speedKph,
              'accuracy': position.accuracy
           };
           
           _offlinePointQueue.add(pointData);
           
           // Attempt flush
           try {
             // Only upload max 50 points to prevent huge payload
             var payload = _offlinePointQueue.take(50).toList();
             await apiClient.dio.post('/sessions/$currentSessionId/points', data: payload);
             _offlinePointQueue.removeWhere((p) => payload.contains(p));
           } catch (e) {
             // Leave in queue
             if (_offlinePointQueue.length > 500) {
               _offlinePointQueue.removeRange(0, _offlinePointQueue.length - 500); // cap size at 500
             }
           }
        }

        // Handle Alerts
        if (overspeedEngine.isAlerting) {
           if (!wasAlerting && currentSessionId != null) {
              // Edge Trigger: just started alerting, post to API
              try {
                await apiClient.dio.post('/sessions/$currentSessionId/alerts', data: [
                  {
                    'timestamp': DateTime.now().toIso8601String(),
                    'alertType': 'Overspeed',
                    'actualSpeed': speedKph,
                    'speedLimit': currentSpeedLimit
                  }
                ]);
              } catch (e) {}
           }
           wasAlerting = true;
        } else {
           wasAlerting = false;
        }

        // Update Foreground Notification
        if (service is AndroidServiceInstance) {
          if (overspeedEngine.isAlerting) {
            service.setForegroundNotificationInfo(
              title: "SPEED WARNING",
              content: "Reduce speed immediately! Limit: ${currentSpeedLimit.toInt()} km/h",
            );
          } else {
            service.setForegroundNotificationInfo(
              title: isDriving ? "Active Monitoring" : "Passive Readiness Active",
              content: isDriving ? "Current Speed: ${speedKph.toInt()} km/h" : "Waiting for vehicle motion...",
            );
          }
        }

        // Send data to foreground UI
        service.invoke('update', {
          "currentSpeed": speedKph,
          "speedLimit": currentSpeedLimit,
          "isAlerting": overspeedEngine.isAlerting,
          "providerStatus": status,
          "isDriving": isDriving
        });

      }
    );

    // Periodic heartbeat
    heartbeatTimer = Timer.periodic(const Duration(minutes: 1), (timer) async {
      if (service is AndroidServiceInstance) {
        if (!(await service.isForegroundService())) {
          service.stopSelf();
        }
      }
    });
  }
}
