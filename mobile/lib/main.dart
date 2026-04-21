import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'ui/app.dart';
import 'core/services/background_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Init local storage
  await Hive.initFlutter();
  await Hive.openBox('settings');
  await Hive.openBox('offline_queue');

  // Init hands-free background tracker
  await BackgroundMonitoringService.initialize();

  runApp(
    const ProviderScope(
      child: SpeedAlertApp(),
    ),
  );
}
