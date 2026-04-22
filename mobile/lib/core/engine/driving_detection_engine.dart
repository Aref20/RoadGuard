import 'dart:async';
import 'package:flutter_activity_recognition/flutter_activity_recognition.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:hive_flutter/hive_flutter.dart';

enum DrivingState { idle, possibleMotion, driving, stoppedTemporarily }

class DrivingDetectionEngine {
  DrivingState _currentState = DrivingState.idle;
  StreamSubscription<Activity>? _activitySubscription;

  Future<void> startListening() async {
    final activityRecognition = FlutterActivityRecognition.instance;

    var permissionResult = await activityRecognition.checkPermission();
    if (permissionResult == PermissionRequestResult.DENIED) {
      permissionResult = await activityRecognition.requestPermission();
    }

    if (permissionResult != PermissionRequestResult.GRANTED) {
      return;
    }

    _activitySubscription = activityRecognition.activityStream.listen((activity) {
      _evaluateActivity(activity);
    });
  }

  void _evaluateActivity(Activity activity) async {
    if (activity.confidence == ActivityConfidence.LOW) {
      return;
    }

    final settingsBox = Hive.box('settings');
    if (settingsBox.get('autoDetectDrivingEnabled', defaultValue: true) != true) {
      return;
    }

    if (activity.type == ActivityType.IN_VEHICLE) {
      if (_currentState != DrivingState.driving) {
        _currentState = DrivingState.driving;
        final service = FlutterBackgroundService();
        if (!(await service.isRunning())) {
          await service.startService();
        }
      }
    } else if (activity.type == ActivityType.STILL || activity.type == ActivityType.WALKING) {
      if (_currentState == DrivingState.driving) {
        _currentState = DrivingState.stoppedTemporarily;
        Future.delayed(const Duration(minutes: 5), () async {
          if (_currentState == DrivingState.stoppedTemporarily) {
            _currentState = DrivingState.idle;
            FlutterBackgroundService().invoke('stopService');
          }
        });
      }
    }
  }

  void stopListening() {
    _activitySubscription?.cancel();
  }
}
