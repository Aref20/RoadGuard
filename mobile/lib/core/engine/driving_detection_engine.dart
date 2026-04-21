import 'dart:async';
import 'package:flutter_activity_recognition/flutter_activity_recognition.dart';
import 'package:flutter_background_service/flutter_background_service.dart';

enum DrivingState { idle, possibleMotion, driving, stoppedTemporarily }

class DrivingDetectionEngine {
  DrivingState _currentState = DrivingState.idle;
  StreamSubscription<Activity>? _activitySubscription;

  Future<void> startListening() async {
    final activityRecognition = FlutterActivityRecognition.instance;
    
    // Check permissions
    PermissionRequestResult reqResult = await activityRecognition.checkPermission();
    if (reqResult == PermissionRequestResult.PERMANENTLY_DENIED) return;

    _activitySubscription = activityRecognition.activityStream.listen((Activity activity) {
      _evaluateActivity(activity);
    });
  }

  void _evaluateActivity(Activity activity) async {
    if (activity.confidence == ActivityConfidence.LOW) return;

    if (activity.type == ActivityType.IN_VEHICLE) {
      if (_currentState != DrivingState.driving) {
        _currentState = DrivingState.driving;
        // Promote to active tracking
        final service = FlutterBackgroundService();
        if (!(await service.isRunning())) {
          service.startService();
        }
      }
    } else if (activity.type == ActivityType.STILL || activity.type == ActivityType.WALKING) {
      if (_currentState == DrivingState.driving) {
        _currentState = DrivingState.stoppedTemporarily;
        // Stop service after timeout (downshift logic)
        Future.delayed(const Duration(minutes: 5), () async {
          if (_currentState == DrivingState.stoppedTemporarily) {
            _currentState = DrivingState.idle;
            final service = FlutterBackgroundService();
            service.invoke('stopService');
          }
        });
      }
    }
  }

  void stopListening() {
    _activitySubscription?.cancel();
  }
}
