import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';
import '../../core/api/api_client.dart';
import '../../l10n/app_localizations.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  bool _isLocationGranted = false;
  bool _isServiceEnabled = false;
  bool _isMonitorRunning = false;
  bool _isDriving = false;
  String? _providerStatus;
  StreamSubscription<Map<String, dynamic>?>? _serviceSubscription;
  Future<List<dynamic>>? _sessionsFuture;

  @override
  void initState() {
    super.initState();
    _sessionsFuture = _loadRecentSessions();
    _checkStatus();

    _serviceSubscription = FlutterBackgroundService().on('update').listen((event) {
      if (!mounted || event == null) {
        return;
      }

      setState(() {
        _isMonitorRunning = true;
        _isDriving = event['isDriving'] == true;
        _providerStatus = event['providerStatus']?.toString();
      });
    });
  }

  @override
  void dispose() {
    _serviceSubscription?.cancel();
    super.dispose();
  }

  Future<List<dynamic>> _loadRecentSessions() async {
    try {
      final response = await ApiClient().dio.get('/sessions');
      if (response.data is List) {
        return List<dynamic>.from(response.data as List);
      }
    } catch (_) {}

    return [];
  }

  Future<void> _checkStatus() async {
    final serviceEnabled = await Geolocator.isLocationServiceEnabled();
    var permission = await Geolocator.checkPermission();
    final running = await FlutterBackgroundService().isRunning();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (!mounted) {
      return;
    }

    setState(() {
      _isServiceEnabled = serviceEnabled;
      _isLocationGranted = permission == LocationPermission.always || permission == LocationPermission.whileInUse;
      _isMonitorRunning = running;
    });
  }

  Future<bool> _ensureMonitoringPermissions() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      if (!mounted) {
        return false;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(context.tr('locationServicesDisabled')),
          action: SnackBarAction(
            label: context.tr('openSettings'),
            onPressed: Geolocator.openLocationSettings,
          ),
        ),
      );
      return false;
    }

    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      if (!mounted) {
        return false;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(context.tr('locationPermissionDeniedForever')),
          action: SnackBarAction(
            label: context.tr('openSettings'),
            onPressed: openAppSettings,
          ),
        ),
      );
      return false;
    }

    await Permission.notification.request();
    await Permission.activityRecognition.request();

    final granted = permission == LocationPermission.always || permission == LocationPermission.whileInUse;
    if (!granted && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.tr('permissionsRequired'))),
      );
    }

    return granted;
  }

  Future<void> _toggleService() async {
    final service = FlutterBackgroundService();
    final running = await service.isRunning();

    if (running) {
      service.invoke('stopService');
      if (mounted) {
        setState(() {
          _isMonitorRunning = false;
          _isDriving = false;
        });
      }
      return;
    }

    if (!await _ensureMonitoringPermissions()) {
      return;
    }

    await service.startService();
    final isRunning = await service.isRunning();
    if (!mounted) {
      return;
    }

    setState(() => _isMonitorRunning = isRunning);
    if (!isRunning) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.tr('monitorStartFailed'))),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.tr('monitorStartPending'))),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(context.tr('dashboard')),
        actions: [
          IconButton(
            icon: const Icon(Icons.history),
            onPressed: () => Navigator.pushNamed(context, '/history'),
          ),
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () => Navigator.pushNamed(context, '/settings'),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _buildStatusCard(context),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () async {
                if (_isMonitorRunning) {
                  Navigator.pushNamed(context, '/monitor');
                } else {
                  await _toggleService();
                }
              },
              icon: Icon(_isMonitorRunning ? Icons.speed : Icons.play_arrow),
              label: Text(_isMonitorRunning ? context.tr('viewLiveMonitor') : context.tr('manualOverrideStart')),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                backgroundColor: _isMonitorRunning ? Colors.blue : null,
              ),
            ),
            if (_isMonitorRunning)
              TextButton(
                onPressed: _toggleService,
                child: Text(
                  context.tr('stopBackgroundMonitor'),
                  style: const TextStyle(color: Colors.red),
                ),
              ),
            const SizedBox(height: 12),
            Text(
              context.tr('recentSessions'),
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            Expanded(
              child: FutureBuilder<List<dynamic>>(
                future: _sessionsFuture,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: CircularProgressIndicator());
                  }

                  final sessions = snapshot.data ?? const [];
                  if (snapshot.hasError || sessions.isEmpty) {
                    return Center(child: Text(context.tr('noRecentSessionsFound')));
                  }

                  return ListView.builder(
                    itemCount: sessions.length,
                    itemBuilder: (context, index) {
                      final session = Map<String, dynamic>.from(sessions[index] as Map);
                      final alertCount = (session['alertEventCount'] as num?)?.toInt() ?? 0;
                      return ListTile(
                        leading: Icon(session['wasAutoStarted'] == true ? Icons.settings_remote : Icons.car_rental),
                        title: Text('${context.tr('session')} ${session['id'].toString().substring(0, 8)}'),
                        subtitle: Text(session['startedAt']?.toString() ?? context.tr('unknown')),
                        trailing: Text(
                          '$alertCount ${context.tr('alerts')}',
                          style: TextStyle(color: alertCount > 0 ? Colors.red : Colors.green),
                        ),
                      );
                    },
                  );
                },
              ),
            )
          ],
        ),
      ),
    );
  }

  Widget _buildStatusCard(BuildContext context) {
    final isReady = _isLocationGranted && _isServiceEnabled;

    IconData icon;
    Color color;
    String title;
    String subtitle;

    if (!isReady) {
      icon = Icons.error;
      color = Colors.red;
      title = context.tr('setupRequired');
      subtitle = context.tr('setupDesc');
    } else if (_isDriving) {
      icon = Icons.directions_car;
      color = Colors.blue;
      title = context.tr('activeMonitoring');
      subtitle = context.tr('activeMonitoringDesc');
    } else if (_isMonitorRunning) {
      icon = Icons.check_circle;
      color = Colors.green;
      title = context.tr('passiveReadinessActive');
      subtitle = _providerStatus ?? context.tr('waitingForVehicleMotion');
    } else {
      icon = Icons.pause_circle;
      color = Colors.orange;
      title = context.tr('monitoringPaused');
      subtitle = context.tr('monitoringPausedDesc');
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(icon, color: color, size: 48),
            const SizedBox(height: 8),
            Text(
              title,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            Text(subtitle, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildDiagnosticIcon(Icons.location_on, _isLocationGranted ? Colors.green : Colors.red),
                _buildDiagnosticIcon(Icons.gps_fixed, _isServiceEnabled ? Colors.green : Colors.red),
                _buildDiagnosticIcon(Icons.monitor_heart, _isMonitorRunning ? Colors.green : Colors.grey),
              ],
            )
          ],
        ),
      ),
    );
  }

  Widget _buildDiagnosticIcon(IconData icon, Color color) {
    return Icon(icon, color: color);
  }
}
