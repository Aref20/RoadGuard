import 'dart:async';
import 'package:geolocator/geolocator.dart';

class OverspeedEngine {
  final int toleranceKph;
  final int requiredDurationSeconds;
  
  List<Position> _recentPoints = [];
  bool _isCurrentlyAlerting = false;
  DateTime? _overspeedStartTime;

  OverspeedEngine({
    this.toleranceKph = 5,
    this.requiredDurationSeconds = 3,
  });

  bool get isAlerting => _isCurrentlyAlerting;

  void processNewLocation(Position point, double currentSpeedLimit) {
    _recentPoints.add(point);
    // Keep only last 10 seconds of points for smoothing
    _recentPoints.removeWhere((p) => DateTime.now().difference(p.timestamp).inSeconds > 10);
    
    if (_recentPoints.length < 2) return;

    double avgSpeed = _recentPoints.map((p) => p.speed * 3.6).reduce((a, b) => a + b) / _recentPoints.length;

    if (avgSpeed > (currentSpeedLimit + toleranceKph)) {
      _overspeedStartTime ??= DateTime.now();
      
      final secondsOver = DateTime.now().difference(_overspeedStartTime!).inSeconds;
      if (secondsOver >= requiredDurationSeconds && !_isCurrentlyAlerting) {
        _triggerAlert();
      }
    } else {
      _overspeedStartTime = null;
      if (_isCurrentlyAlerting) {
        _stopAlert();
      }
    }
  }

  void _triggerAlert() {
    _isCurrentlyAlerting = true;
    // Notify streams / fire sound & vibration
  }

  void _stopAlert() {
    _isCurrentlyAlerting = false;
  }
}
