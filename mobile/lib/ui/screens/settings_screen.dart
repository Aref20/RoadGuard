import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import '../../core/config/app_config.dart';
import 'package:hive/hive.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({Key? key}) : super(key: key);

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _audioAlerts = true;
  bool _hapticAlerts = true;
  bool _autoDetect = true;
  int _tolerance = 5;

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    try {
      final res = await ApiClient().dio.get('/users/me/settings');
      setState(() {
        _audioAlerts = res.data['soundEnabled'] ?? true;
        _hapticAlerts = res.data['vibrationEnabled'] ?? true;
        _autoDetect = res.data['autoDetectDrivingEnabled'] ?? true;
        _tolerance = res.data['overspeedTolerance'] ?? 5;
      });
    } catch (e) {
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
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          SwitchListTile(
            title: const Text('Audio Alerts'),
            value: _audioAlerts,
            onChanged: (v) => setState(() => _audioAlerts = v),
          ),
          SwitchListTile(
            title: const Text('Vibration Alerts'),
            value: _hapticAlerts,
            onChanged: (v) => setState(() => _hapticAlerts = v),
          ),
          SwitchListTile(
            title: const Text('Auto-Detect Driving (Hands-free)'),
            subtitle: const Text('Uses Activity Recognition to start tracking automatically.'),
            value: _autoDetect,
            onChanged: (v) => setState(() => _autoDetect = v),
          ),
          ListTile(
            title: const Text('Overspeed Tolerance (km/h)'),
            trailing: Text('+$_tolerance', style: const TextStyle(fontSize: 18)),
            onTap: () {
              setState(() => _tolerance = _tolerance == 5 ? 10 : 5);
            },
          ),
          const Divider(),
          ListTile(
            title: const Text('Connected Server'),
            subtitle: Text(AppConfig.apiBaseUrl),
            leading: const Icon(Icons.cloud_done_outlined),
          ),
          ListTile(
            title: const Text('Environment'),
            subtitle: Text(AppConfig.environment),
            leading: const Icon(Icons.settings_ethernet),
          ),
          const Divider(),
          ListTile(
            title: const Text('Logout', style: TextStyle(color: Colors.red)),
            leading: const Icon(Icons.logout, color: Colors.red),
            onTap: _logout,
          )
        ],
      ),
    );
  }
}
