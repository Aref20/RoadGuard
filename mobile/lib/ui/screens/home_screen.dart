import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import '../../core/api/api_client.dart';
import '../../l10n/app_localizations.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  bool _isLocationGranted = false;
  bool _isServiceEnabled = false;
  bool _isMonitorRunning = false;
  bool _isDriving = false;

  @override
  void initState() {
    super.initState();
    _checkStatus();
    
    // Listen to background service updates
    FlutterBackgroundService().on('update').listen((event) {
      if (!mounted) return;
      setState(() {
        _isMonitorRunning = true;
        _isDriving = event?['isDriving'] == true;
      });
    });
  }

  Future<void> _checkStatus() async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    LocationPermission permission = await Geolocator.checkPermission();
    
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    
    bool running = await FlutterBackgroundService().isRunning();
    
    if (mounted) {
      setState(() {
        _isServiceEnabled = serviceEnabled;
        _isLocationGranted = permission == LocationPermission.always || permission == LocationPermission.whileInUse;
        _isMonitorRunning = running;
      });
    }
  }

  Future<void> _toggleService() async {
    final service = FlutterBackgroundService();
    bool running = await service.isRunning();
    if (running) {
      service.invoke('stopService');
      setState(() { _isMonitorRunning = false; _isDriving = false; });
    } else {
      await service.startService();
      setState(() { _isMonitorRunning = true; });
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
            onPressed: () { 
              Navigator.pushNamed(context, '/history');
            },
          ),
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () { 
               Navigator.pushNamed(context, '/settings');
            },
          )
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _buildStatusCard(context),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () {
                if (_isMonitorRunning) {
                   Navigator.pushNamed(context, '/monitor');
                } else {
                   _toggleService();
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
                child: Text(context.tr('stopBackgroundMonitor'), style: const TextStyle(color: Colors.red)),
              ),
            const SizedBox(height: 12),
            Text(context.tr('recentSessions'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Expanded(
              child: FutureBuilder(
                future: ApiClient().dio.get('/sessions'),
                builder: (context, AsyncSnapshot snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (snapshot.hasError || !snapshot.hasData || snapshot.data.data.isEmpty) {
                    return Center(child: Text(context.tr('noRecentSessionsFound')));
                  }
                  
                  final sessions = snapshot.data.data as List;
                  return ListView.builder(
                    itemCount: sessions.length,
                    itemBuilder: (context, index) {
                      final s = sessions[index];
                      return ListTile(
                        leading: Icon(s['wasAutoStarted'] == true ? Icons.settings_remote : Icons.car_rental),
                        title: Text('${context.tr('session')} ${s['id'].toString().substring(0,8)}'),
                        subtitle: Text(s['startedAt'] ?? context.tr('unknown')),
                        trailing: Text('${s['alertEventCount'] ?? 0} ${context.tr('alerts')}', style: TextStyle(color: (s['alertEventCount'] ?? 0) > 0 ? Colors.red : Colors.green)),
                      );
                    }
                  );
                }
              ),
            )
          ],
        ),
      ),
    );
  }

  Widget _buildStatusCard(BuildContext context) {
    bool isReady = _isLocationGranted && _isServiceEnabled;
    
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
       subtitle = context.tr('waitingForVehicleMotion');
    } else {
       icon = Icons.pause_circle;
       color = Colors.orange;
       title = context.tr('monitoringPaused');
       subtitle = context.tr('monitoringPausedDesc');
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            Icon(icon, color: color, size: 48),
            const SizedBox(height: 8),
            Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
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

