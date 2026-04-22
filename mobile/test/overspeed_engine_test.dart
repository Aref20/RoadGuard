import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:speed_alert/core/engine/overspeed_engine.dart';

void main() {
  Position position({
    required DateTime timestamp,
    required double speedMetersPerSecond,
  }) {
    return Position(
      longitude: 35.91,
      latitude: 31.95,
      timestamp: timestamp,
      accuracy: 5,
      altitude: 0,
      altitudeAccuracy: 0,
      heading: 0,
      headingAccuracy: 0,
      speed: speedMetersPerSecond,
      speedAccuracy: 0.5,
    );
  }

  test('alerts when average speed stays above tolerance', () {
    final engine = OverspeedEngine(
      toleranceKph: 5,
      requiredDurationSeconds: 0,
    );

    final now = DateTime.now();
    engine.processNewLocation(
      position(timestamp: now.subtract(const Duration(seconds: 1)), speedMetersPerSecond: 20),
      60,
    );
    engine.processNewLocation(
      position(timestamp: now, speedMetersPerSecond: 21),
      60,
    );

    expect(engine.isAlerting, isTrue);
  });

  test('reset clears the active alert state', () {
    final engine = OverspeedEngine(
      toleranceKph: 5,
      requiredDurationSeconds: 0,
    );

    final now = DateTime.now();
    engine.processNewLocation(
      position(timestamp: now.subtract(const Duration(seconds: 1)), speedMetersPerSecond: 20),
      60,
    );
    engine.processNewLocation(
      position(timestamp: now, speedMetersPerSecond: 21),
      60,
    );
    expect(engine.isAlerting, isTrue);

    engine.reset();

    expect(engine.isAlerting, isFalse);
  });
}
