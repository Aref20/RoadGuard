import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../l10n/app_localizations.dart';
import 'screens/home_screen.dart';
import 'screens/monitor_screen.dart';
import 'screens/login_screen.dart';
import 'screens/settings_screen.dart';
import 'screens/history_screen.dart';

class SpeedAlertApp extends StatelessWidget {
  const SpeedAlertApp({super.key});

  ThemeData _buildTheme(String langCode, Brightness brightness) {
    return ThemeData(
      colorScheme: ColorScheme.fromSeed(
        seedColor: Colors.blueAccent,
        brightness: brightness,
      ),
      useMaterial3: true,
      fontFamily: langCode == 'ar' ? 'Cairo' : null,
    );
  }

  @override
  Widget build(BuildContext context) {
    final box = Hive.box('settings');
    final hasToken = box.get('jwt_token') != null;

    return ValueListenableBuilder<Box>(
      valueListenable: box.listenable(keys: ['language']),
      builder: (context, box, _) {
        final langCode = box.get('language', defaultValue: 'ar');

        return MaterialApp(
          title: 'Speed Alert',
          locale: Locale(langCode),
          supportedLocales: const [
            Locale('ar'),
            Locale('en'),
          ],
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          theme: _buildTheme(langCode, Brightness.light),
          darkTheme: _buildTheme(langCode, Brightness.dark),
          themeMode: ThemeMode.system,
          initialRoute: hasToken ? '/' : '/login',
          routes: {
            '/': (context) => const HomeScreen(),
            '/login': (context) => const LoginScreen(),
            '/monitor': (context) => const MonitorScreen(),
            '/settings': (context) => const SettingsScreen(),
            '/history': (context) => const HistoryScreen(),
          },
        );
      },
    );
  }
}
