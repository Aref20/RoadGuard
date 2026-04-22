import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import 'package:hive/hive.dart';
import '../../l10n/app_localizations.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final CancelToken _cancelToken = CancelToken();
  bool _audioAlerts = true;
  bool _hapticAlerts = true;
  bool _autoDetect = true;
  int _tolerance = 5;

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  @override
  void dispose() {
    _cancelToken.cancel('Settings screen disposed');
    super.dispose();
  }

  Future<void> _loadSettings() async {
    try {
      final res = await ApiClient().dio.get(
            '/users/me/settings',
            cancelToken: _cancelToken,
          );
      if (!mounted) {
        return;
      }

      setState(() {
        _audioAlerts = res.data['soundEnabled'] ?? true;
        _hapticAlerts = res.data['vibrationEnabled'] ?? true;
        _autoDetect = res.data['autoDetectDrivingEnabled'] ?? true;
        _tolerance = res.data['overspeedTolerance'] ?? 5;
      });
    } on DioException catch (e) {
      if (CancelToken.isCancel(e)) {
        return;
      }

      // Fallback to local
    }
  }

  Future<void> _logout() async {
    await Hive.box('settings').delete('jwt_token');
    if (mounted) Navigator.pushReplacementNamed(context, '/login');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(context.tr('settings'))),
      body: ListView(
        children: [
          SwitchListTile(
            title: Text(context.tr('audioAlerts')),
            value: _audioAlerts,
            onChanged: (v) => setState(() => _audioAlerts = v),
          ),
          SwitchListTile(
            title: Text(context.tr('vibrationAlerts')),
            value: _hapticAlerts,
            onChanged: (v) => setState(() => _hapticAlerts = v),
          ),
          SwitchListTile(
            title: Text(context.tr('autoDetect')),
            subtitle: Text(context.tr('autoDetectDesc')),
            value: _autoDetect,
            onChanged: (v) => setState(() => _autoDetect = v),
          ),
          ListTile(
            title: Text(context.tr('tolerance')),
            trailing:
                Text('+$_tolerance', style: const TextStyle(fontSize: 18)),
            onTap: () {
              setState(() => _tolerance = _tolerance == 5 ? 10 : 5);
            },
          ),
          const Divider(),
          ListTile(
            title: Text(context.tr('language')),
            trailing: const Icon(Icons.language),
            onTap: () {
              final box = Hive.box('settings');
              final curr = box.get('language', defaultValue: 'ar');
              box.put('language', curr == 'ar' ? 'en' : 'ar');
            },
          ),
          const Divider(),
          ListTile(
            title: Text(context.tr('logout'),
                style: const TextStyle(color: Colors.red)),
            leading: const Icon(Icons.logout, color: Colors.red),
            onTap: _logout,
          )
        ],
      ),
    );
  }
}
