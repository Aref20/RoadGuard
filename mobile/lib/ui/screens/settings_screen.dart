import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:hive/hive.dart';
import '../../core/api/api_client.dart';
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
  bool _voiceAlerts = false;
  bool _autoDetect = true;
  int _tolerance = 5;
  int _alertDelaySeconds = 3;
  int _alertCooldownSeconds = 10;
  bool _isLoading = true;
  bool _isSaving = false;

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
    final box = Hive.box('settings');

    try {
      final response = await ApiClient().dio.get(
            '/users/me/settings',
            cancelToken: _cancelToken,
          );

      await _persistLocalSettings(response.data as Map<String, dynamic>);

      if (!mounted) {
        return;
      }

      setState(() {
        _audioAlerts = response.data['soundEnabled'] ?? true;
        _hapticAlerts = response.data['vibrationEnabled'] ?? true;
        _voiceAlerts = response.data['voiceEnabled'] ?? false;
        _autoDetect = response.data['autoDetectDrivingEnabled'] ?? true;
        _tolerance = response.data['overspeedTolerance'] ?? 5;
        _alertDelaySeconds = response.data['alertDelaySeconds'] ?? 3;
        _alertCooldownSeconds = response.data['alertCooldownSeconds'] ?? 10;
        _isLoading = false;
      });
    } on DioException catch (error) {
      if (CancelToken.isCancel(error)) {
        return;
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _audioAlerts = box.get('soundEnabled', defaultValue: true) == true;
        _hapticAlerts = box.get('vibrationEnabled', defaultValue: true) == true;
        _voiceAlerts = box.get('voiceEnabled', defaultValue: false) == true;
        _autoDetect = box.get('autoDetectDrivingEnabled', defaultValue: true) == true;
        _tolerance = box.get('overspeedTolerance', defaultValue: 5) as int;
        _alertDelaySeconds = box.get('alertDelaySeconds', defaultValue: 3) as int;
        _alertCooldownSeconds = box.get('alertCooldownSeconds', defaultValue: 10) as int;
        _isLoading = false;
      });
    }
  }

  Future<void> _persistLocalSettings(Map<String, dynamic> payload) async {
    final box = Hive.box('settings');
    await box.putAll({
      'soundEnabled': payload['soundEnabled'] ?? _audioAlerts,
      'vibrationEnabled': payload['vibrationEnabled'] ?? _hapticAlerts,
      'voiceEnabled': payload['voiceEnabled'] ?? _voiceAlerts,
      'autoDetectDrivingEnabled': payload['autoDetectDrivingEnabled'] ?? _autoDetect,
      'overspeedTolerance': payload['overspeedTolerance'] ?? _tolerance,
      'alertDelaySeconds': payload['alertDelaySeconds'] ?? _alertDelaySeconds,
      'alertCooldownSeconds': payload['alertCooldownSeconds'] ?? _alertCooldownSeconds,
    });
  }

  Future<void> _saveSettings() async {
    setState(() => _isSaving = true);

    final payload = {
      'speedUnit': 'km/h',
      'overspeedTolerance': _tolerance,
      'alertDelaySeconds': _alertDelaySeconds,
      'alertCooldownSeconds': _alertCooldownSeconds,
      'soundEnabled': _audioAlerts,
      'vibrationEnabled': _hapticAlerts,
      'voiceEnabled': _voiceAlerts,
      'autoDetectDrivingEnabled': _autoDetect,
      'autoStartMonitoringEnabled': true,
    };

    try {
      final response = await ApiClient().dio.put('/users/me/settings', data: payload);
      await _persistLocalSettings(Map<String, dynamic>.from(response.data as Map));

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.tr('settingsSaved'))),
      );
    } on DioException {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.tr('settingsSaveFailed'))),
      );
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  Future<void> _logout() async {
    await Hive.box('settings').delete('jwt_token');
    if (mounted) {
      Navigator.pushReplacementNamed(context, '/login');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(context.tr('settings')),
        actions: [
          TextButton(
            onPressed: _isLoading || _isSaving ? null : _saveSettings,
            child: _isSaving
                ? const SizedBox(
                    height: 16,
                    width: 16,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(context.tr('saveChanges')),
          )
        ],
      ),
      body: _isLoading
          ? Center(child: Text(context.tr('loadingSettings')))
          : ListView(
              children: [
                SwitchListTile(
                  title: Text(context.tr('audioAlerts')),
                  value: _audioAlerts,
                  onChanged: (value) => setState(() => _audioAlerts = value),
                ),
                SwitchListTile(
                  title: Text(context.tr('vibrationAlerts')),
                  value: _hapticAlerts,
                  onChanged: (value) => setState(() => _hapticAlerts = value),
                ),
                SwitchListTile(
                  title: Text(context.tr('voiceAlerts')),
                  value: _voiceAlerts,
                  onChanged: (value) => setState(() => _voiceAlerts = value),
                ),
                SwitchListTile(
                  title: Text(context.tr('autoDetect')),
                  subtitle: Text(context.tr('autoDetectDesc')),
                  value: _autoDetect,
                  onChanged: (value) => setState(() => _autoDetect = value),
                ),
                _SliderTile(
                  title: context.tr('tolerance'),
                  value: _tolerance.toDouble(),
                  min: 0,
                  max: 20,
                  divisions: 20,
                  label: '$_tolerance km/h',
                  onChanged: (value) => setState(() => _tolerance = value.round()),
                ),
                _SliderTile(
                  title: context.tr('alertDelay'),
                  value: _alertDelaySeconds.toDouble(),
                  min: 1,
                  max: 10,
                  divisions: 9,
                  label: '$_alertDelaySeconds s',
                  onChanged: (value) => setState(() => _alertDelaySeconds = value.round()),
                ),
                _SliderTile(
                  title: context.tr('alertCooldown'),
                  value: _alertCooldownSeconds.toDouble(),
                  min: 5,
                  max: 60,
                  divisions: 11,
                  label: '$_alertCooldownSeconds s',
                  onChanged: (value) => setState(() => _alertCooldownSeconds = value.round()),
                ),
                const Divider(),
                ListTile(
                  title: Text(context.tr('language')),
                  subtitle: Text(Hive.box('settings').get('language', defaultValue: 'ar') == 'ar'
                      ? context.tr('arabic')
                      : context.tr('english')),
                  trailing: const Icon(Icons.language),
                  onTap: () async {
                    final box = Hive.box('settings');
                    final currentLanguage = box.get('language', defaultValue: 'ar');
                    await box.put('language', currentLanguage == 'ar' ? 'en' : 'ar');
                    if (mounted) {
                      setState(() {});
                    }
                  },
                ),
                const Divider(),
                ListTile(
                  title: Text(
                    context.tr('logout'),
                    style: const TextStyle(color: Colors.red),
                  ),
                  leading: const Icon(Icons.logout, color: Colors.red),
                  onTap: _logout,
                )
              ],
            ),
    );
  }
}

class _SliderTile extends StatelessWidget {
  const _SliderTile({
    required this.title,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    required this.label,
    required this.onChanged,
  });

  final String title;
  final double value;
  final double min;
  final double max;
  final int divisions;
  final String label;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(title),
      subtitle: Slider(
        value: value,
        min: min,
        max: max,
        divisions: divisions,
        label: label,
        onChanged: onChanged,
      ),
      trailing: Text(label),
    );
  }
}
