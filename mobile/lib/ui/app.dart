import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'screens/home_screen.dart';
import 'screens/monitor_screen.dart';
import 'screens/login_screen.dart';
import 'screens/settings_screen.dart';
import '../core/engine/overspeed_engine.dart';

class SpeedAlertApp extends StatelessWidget {
  const SpeedAlertApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    final box = Hive.box('settings');
    final hasToken = box.get('jwt_token') != null;

    return MaterialApp(
      title: 'Speed Alert',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blueAccent),
        useMaterial3: true,
      ),
      darkTheme: ThemeData.dark(useMaterial3: true).copyWith(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blueAccent, brightness: Brightness.dark),
      ),
      themeMode: ThemeMode.system,
      initialRoute: hasToken ? '/' : '/login',
      routes: {
        '/': (context) => const HomeScreen(),
        '/login': (context) => const LoginScreen(),
        '/monitor': (context) => const MonitorScreen(),
        '/settings': (context) => const SettingsScreen(),
      },
    );
  }
}
