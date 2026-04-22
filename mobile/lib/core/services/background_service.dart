import 'dart:async';
import 'dart:math' as math;
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../../l10n/app_localizations.dart';
import '../api/api_client.dart';
import '../engine/overspeed_engine.dart';
import 'sync_service.dart';

const _foregroundChannelId = 'speed_alert_foreground';
const _warningChannelId = 'speed_alert_warning';
const _foregroundNotificationId = 888;
const _warningNotificationId = 889;
const _startDrivingThresholdKph = 15.0;
const _stopDrivingThresholdKph = 8.0;

class BackgroundMonitoringService {
  static Future<void> initialize() async {
    final service = FlutterBackgroundService();
    final notifications = FlutterLocalNotificationsPlugin();

    const foregroundChannel = AndroidNotificationChannel(
      _foregroundChannelId,
      'RoadGuard Monitoring',
      description: 'Shows the current RoadGuard monitoring status.',
      importance: Importance.low,
    );

    const warningChannel = AndroidNotificationChannel(
      _warningChannelId,
      'RoadGuard Warnings',
      description: 'Plays RoadGuard overspeed warnings.',
      importance: Importance.max,
    );

    final androidPlugin = notifications.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>();
    await androidPlugin?.createNotificationChannel(foregroundChannel);
    await androidPlugin?.createNotificationChannel(warningChannel);
    await notifications.initialize(
      const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      ),
    );

    await service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: _foregroundChannelId,
        initialNotificationTitle: 'RoadGuard',
        initialNotificationContent: 'Preparing monitoring...',
        foregroundServiceNotificationId: _foregroundNotificationId,
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

    final notifications = FlutterLocalNotificationsPlugin();
    await notifications.initialize(
      const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      ),
    );

    await Hive.initFlutter();
    final settingsBox = Hive.isBoxOpen('settings') ? Hive.box('settings') : await Hive.openBox('settings');
    if (!Hive.isBoxOpen('offline_queue')) {
      await Hive.openBox('offline_queue');
    }

    final syncService = SyncService();
    final overspeedEngine = OverspeedEngine();

    StreamSubscription<Position>? positionStream;
    Timer? heartbeatTimer;
    final pointBuffer = <Map<String, dynamic>>[];

    String? currentSessionId = settingsBox.get('active_session_id') as String?;
    double currentSpeedLimit = -1.0;
    String providerStatus = _translate(settingsBox, 'waitingForData');
    DateTime lastMotionTime = DateTime.now();
    DateTime lastWarningAt = DateTime.fromMillisecondsSinceEpoch(0);
    DateTime nextLookupAllowedAt = DateTime.fromMillisecondsSinceEpoch(0);
    DateTime nextSessionStartAttemptAt = DateTime.fromMillisecondsSinceEpoch(0);
    DateTime lastPointFlushAt = DateTime.fromMillisecondsSinceEpoch(0);
    int lookupFailureCount = 0;
    int sessionStartFailureCount = 0;
    bool sessionStartInFlight = false;
    bool wasAlerting = false;

    Future<void> updateForegroundNotification(double speedKph) async {
      if (service is! AndroidServiceInstance) {
        return;
      }

      final isDrivingSessionActive = currentSessionId != null;
      if (overspeedEngine.isAlerting && currentSpeedLimit > 0) {
        service.setForegroundNotificationInfo(
          title: _translate(settingsBox, 'warningNotificationTitle'),
          content: _translate(
            settingsBox,
            'warningNotificationBody',
            params: {'limit': currentSpeedLimit.toInt().toString()},
          ),
        );
        return;
      }

      if (isDrivingSessionActive) {
        service.setForegroundNotificationInfo(
          title: _translate(settingsBox, 'monitoringNotificationTitle'),
          content: _translate(
            settingsBox,
            'monitoringNotificationBody',
            params: {'speed': speedKph.toInt().toString()},
          ),
        );
        return;
      }

      service.setForegroundNotificationInfo(
        title: _translate(settingsBox, 'passiveNotificationTitle'),
        content: _translate(settingsBox, 'passiveNotificationBody'),
      );
    }

    Future<void> flushPointBuffer(ApiClient apiClient) async {
      if (currentSessionId == null || pointBuffer.isEmpty) {
        return;
      }

      final payload = List<Map<String, dynamic>>.from(pointBuffer);
      pointBuffer.clear();
      lastPointFlushAt = DateTime.now();

      await syncService.queueSessionPoints(currentSessionId!, payload);
      await syncService.flushQueuedRequests(client: apiClient);
    }

    Future<void> persistPreSessionPoint(Map<String, dynamic> point) async {
      final pendingPoints = _readPreSessionPoints(settingsBox);
      pendingPoints.add(point);
      while (pendingPoints.length > 120) {
        pendingPoints.removeAt(0);
      }

      await settingsBox.put('pre_session_points', pendingPoints);
    }

    Future<void> flushPreSessionPoints(ApiClient apiClient) async {
      if (currentSessionId == null) {
        return;
      }

      final pendingPoints = _readPreSessionPoints(settingsBox);
      if (pendingPoints.isEmpty) {
        return;
      }

      await settingsBox.delete('pre_session_points');
      await syncService.queueSessionPoints(currentSessionId!, pendingPoints);
      await syncService.flushQueuedRequests(client: apiClient);
    }

    Future<bool> ensureSessionStarted(ApiClient apiClient) async {
      if (currentSessionId != null) {
        return true;
      }

      final now = DateTime.now();
      if (sessionStartInFlight || now.isBefore(nextSessionStartAttemptAt)) {
        return false;
      }

      sessionStartInFlight = true;
      try {
        final response = await apiClient.dio.post('/sessions/start', data: {
          'reason': 'auto_detect',
          'isAuto': true,
        });

        final sessionId = response.data['sessionId']?.toString();
        if (sessionId == null || sessionId.isEmpty) {
          throw StateError('Missing session id');
        }

        currentSessionId = sessionId;
        await settingsBox.put('active_session_id', currentSessionId);
        sessionStartFailureCount = 0;
        providerStatus = providerStatus == _translate(settingsBox, 'monitorStartPending')
            ? _translate(settingsBox, 'waitingForData')
            : providerStatus;

        await flushPreSessionPoints(apiClient);
        await syncService.flushQueuedRequests(client: apiClient);
        return true;
      } catch (_) {
        sessionStartFailureCount += 1;
        final backoffSeconds = math.min(180, 5 * math.pow(2, sessionStartFailureCount - 1).toInt());
        nextSessionStartAttemptAt = now.add(Duration(seconds: backoffSeconds));
        providerStatus = _translate(settingsBox, 'monitorStartPending');
        return false;
      } finally {
        sessionStartInFlight = false;
      }
    }

    Future<void> endSession(ApiClient apiClient, String reason) async {
      final sessionId = currentSessionId;
      currentSessionId = null;
      await settingsBox.delete('active_session_id');

      await flushPointBuffer(apiClient);
      await syncService.flushQueuedRequests(client: apiClient);

      if (sessionId == null) {
        return;
      }

      try {
        await apiClient.dio.put('/sessions/$sessionId/end', data: {'reason': reason});
      } catch (_) {
        await syncService.queueSessionEnd(sessionId, {'reason': reason});
      }

      await notifications.cancel(_warningNotificationId);
    }

    Future<void> emitWarning() async {
      final soundEnabled = _boolSetting(settingsBox, 'soundEnabled', true);
      final vibrationEnabled = _boolSetting(settingsBox, 'vibrationEnabled', true);
      final cooldownSeconds = _intSetting(settingsBox, 'alertCooldownSeconds', 10);

      if (!soundEnabled && !vibrationEnabled) {
        return;
      }

      final now = DateTime.now();
      if (now.difference(lastWarningAt).inSeconds < cooldownSeconds) {
        return;
      }

      lastWarningAt = now;
      await notifications.show(
        _warningNotificationId,
        _translate(settingsBox, 'warningNotificationTitle'),
        _translate(
          settingsBox,
          'warningNotificationBody',
          params: {'limit': currentSpeedLimit.toInt().toString()},
        ),
        NotificationDetails(
          android: AndroidNotificationDetails(
            _warningChannelId,
            'RoadGuard Warnings',
            channelDescription: 'RoadGuard overspeed warnings',
            importance: Importance.max,
            priority: Priority.high,
            playSound: soundEnabled,
            enableVibration: vibrationEnabled,
          ),
        ),
      );
    }

    Future<void> publishState(double speedKph) async {
      service.invoke('update', {
        'currentSpeed': speedKph,
        'speedLimit': currentSpeedLimit,
        'isAlerting': overspeedEngine.isAlerting,
        'providerStatus': providerStatus,
        'isDriving': currentSessionId != null,
      });
    }

    service.on('stopService').listen((event) async {
      final apiClient = ApiClient();
      await endSession(apiClient, 'manual_stop');
      positionStream?.cancel();
      heartbeatTimer?.cancel();
      service.stopSelf();
    });

    final serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      providerStatus = _translate(settingsBox, 'locationServicesDisabled');
      await updateForegroundNotification(0);
      await publishState(0);
      return;
    }

    final permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) {
      providerStatus = _translate(settingsBox, 'permissionsRequired');
      await updateForegroundNotification(0);
      await publishState(0);
      return;
    }

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10,
    );

    positionStream = Geolocator.getPositionStream(locationSettings: locationSettings).listen((position) async {
      final apiClient = ApiClient();
      final now = DateTime.now();
      final speedKph = math.max(0.0, position.speed * 3.6).toDouble();
      final autoDetectDrivingEnabled = _boolSetting(settingsBox, 'autoDetectDrivingEnabled', true);
      final toleranceKph = _intSetting(settingsBox, 'overspeedTolerance', 5);
      final alertDelaySeconds = _intSetting(settingsBox, 'alertDelaySeconds', 3);

      overspeedEngine.updateSettings(
        toleranceKph: toleranceKph,
        requiredDurationSeconds: alertDelaySeconds,
      );

      final pointData = {
        'timestamp': now.toIso8601String(),
        'lat': position.latitude,
        'lng': position.longitude,
        'speed': speedKph,
        'accuracy': position.accuracy,
      };

      if (speedKph >= _startDrivingThresholdKph) {
        lastMotionTime = now;

        if (autoDetectDrivingEnabled) {
          final started = await ensureSessionStarted(apiClient);
          if (!started) {
            await persistPreSessionPoint(pointData);
          }
        }
      } else if (currentSessionId != null &&
          speedKph <= _stopDrivingThresholdKph &&
          now.difference(lastMotionTime).inMinutes >= 2) {
        await endSession(apiClient, 'inactivity');
      }

      if (currentSessionId != null) {
        pointBuffer.add(pointData);
        if (pointBuffer.length >= 5 || now.difference(lastPointFlushAt).inSeconds >= 15) {
          await flushPointBuffer(apiClient);
        }
      } else if (speedKph >= _startDrivingThresholdKph) {
        await persistPreSessionPoint(pointData);
      }

      if (now.isAfter(nextLookupAllowedAt)) {
        try {
          final lookupResponse = await apiClient.dio.get(
            '/speed-limit/lookup',
            queryParameters: {'lat': position.latitude, 'lng': position.longitude},
          );

          final speedLimitValue = (lookupResponse.data['speedLimitKph'] as num?)?.toDouble();
          final lookupStatus = lookupResponse.data['status']?.toString();

          if (speedLimitValue != null &&
              speedLimitValue > 0 &&
              lookupStatus != 'Unavailable' &&
              lookupStatus != 'ProviderFailure' &&
              lookupStatus != 'NotFound') {
            currentSpeedLimit = speedLimitValue;
            providerStatus = (lookupResponse.data['providerUsed'] ??
                    lookupResponse.data['source'] ??
                    _translate(settingsBox, 'unknown'))
                .toString();
            lookupFailureCount = 0;
            nextLookupAllowedAt = now.add(const Duration(seconds: 60));
          } else {
            currentSpeedLimit = -1.0;
            providerStatus = _translate(settingsBox, 'speedLimitUnavailable');
            lookupFailureCount += 1;
            nextLookupAllowedAt = now.add(Duration(seconds: math.min(300, 30 * lookupFailureCount)));
          }
        } catch (_) {
          currentSpeedLimit = -1.0;
          providerStatus = _translate(settingsBox, 'speedLimitUnavailable');
          lookupFailureCount += 1;
          nextLookupAllowedAt = now.add(Duration(seconds: math.min(300, 30 * lookupFailureCount)));
        }
      }

      if (currentSpeedLimit > 0) {
        overspeedEngine.processNewLocation(position, currentSpeedLimit);
      } else {
        overspeedEngine.reset();
      }

      if (overspeedEngine.isAlerting) {
        if (!wasAlerting) {
          if (currentSessionId != null) {
            await syncService.queueSessionAlerts(currentSessionId!, [
              {
                'timestamp': now.toIso8601String(),
                'alertType': 'Overspeed',
                'actualSpeed': speedKph,
                'speedLimit': currentSpeedLimit > 0 ? currentSpeedLimit : 0,
              }
            ]);
            await syncService.flushQueuedRequests(client: apiClient);
          }

          await emitWarning();
        }

        wasAlerting = true;
      } else {
        if (wasAlerting) {
          await notifications.cancel(_warningNotificationId);
        }

        wasAlerting = false;
      }

      await updateForegroundNotification(speedKph);
      await publishState(speedKph);
    });

    heartbeatTimer = Timer.periodic(const Duration(minutes: 1), (timer) async {
      final apiClient = ApiClient();
      await flushPointBuffer(apiClient);
      await syncService.flushQueuedRequests(client: apiClient);

      if (service is AndroidServiceInstance && !(await service.isForegroundService())) {
        service.stopSelf();
      }
    });
  }
}

bool _boolSetting(Box settingsBox, String key, bool defaultValue) {
  return settingsBox.get(key, defaultValue: defaultValue) == true;
}

int _intSetting(Box settingsBox, String key, int defaultValue) {
  final value = settingsBox.get(key, defaultValue: defaultValue);
  if (value is int) {
    return value;
  }

  if (value is num) {
    return value.toInt();
  }

  return defaultValue;
}

String _translate(Box settingsBox, String key, {Map<String, String>? params}) {
  final languageCode = settingsBox.get('language', defaultValue: 'ar') as String? ?? 'ar';
  return AppLocalizations.translateFor(languageCode, key, params: params);
}

List<Map<String, dynamic>> _readPreSessionPoints(Box settingsBox) {
  final rawPoints = settingsBox.get('pre_session_points', defaultValue: const []);
  if (rawPoints is! List) {
    return [];
  }

  return rawPoints
      .whereType<Map>()
      .map((item) => item.map((key, value) => MapEntry(key.toString(), value)))
      .cast<Map<String, dynamic>>()
      .toList();
}
