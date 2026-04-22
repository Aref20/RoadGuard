import 'package:geolocator/geolocator.dart';

class OverspeedEngine {
  OverspeedEngine({
    this.toleranceKph = 5,
    this.requiredDurationSeconds = 3,
  });

  int toleranceKph;
  int requiredDurationSeconds;

  final List<Position> _recentPoints = [];
  bool _isCurrentlyAlerting = false;
  DateTime? _overspeedStartTime;

  bool get isAlerting => _isCurrentlyAlerting;

  void updateSettings({
    required int toleranceKph,
    required int requiredDurationSeconds,
  }) {
    this.toleranceKph = toleranceKph;
    this.requiredDurationSeconds = requiredDurationSeconds;
  }

  void processNewLocation(Position point, double currentSpeedLimit) {
    _recentPoints.add(point);
    _recentPoints.removeWhere(
      (item) => DateTime.now().difference(item.timestamp).inSeconds > 10,
    );

    if (_recentPoints.length < 2) {
      return;
    }

    final averageSpeed = _recentPoints
            .map((item) => item.speed * 3.6)
            .reduce((left, right) => left + right) /
        _recentPoints.length;

    if (averageSpeed > currentSpeedLimit + toleranceKph) {
      _overspeedStartTime ??= DateTime.now();

      final secondsOver = DateTime.now().difference(_overspeedStartTime!).inSeconds;
      if (secondsOver >= requiredDurationSeconds && !_isCurrentlyAlerting) {
        _isCurrentlyAlerting = true;
      }
    } else {
      _overspeedStartTime = null;
      _isCurrentlyAlerting = false;
    }
  }

  void reset() {
    _recentPoints.clear();
    _overspeedStartTime = null;
    _isCurrentlyAlerting = false;
  }
}
