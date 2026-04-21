import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import '../../core/api/api_client.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  bool _isLocationGranted = false;
  bool _isServiceEnabled = false;

  @override
  void initState() {
    super.initState();
    _checkStatus();
  }

  Future<void> _checkStatus() async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    LocationPermission permission = await Geolocator.checkPermission();
    
    setState(() {
      _isServiceEnabled = serviceEnabled;
      _isLocationGranted = permission == LocationPermission.always || permission == LocationPermission.whileInUse;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Speed Alert Dashboard'),
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
              onPressed: () => Navigator.pushNamed(context, '/monitor'),
              icon: const Icon(Icons.speed),
              label: const Text('Manual Override: Start Monitor'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
            const SizedBox(height: 24),
            const Text('Recent Sessions', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Expanded(
              child: FutureBuilder(
                future: ApiClient().dio.get('/sessions'),
                builder: (context, AsyncSnapshot snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (snapshot.hasError || !snapshot.hasData || snapshot.data.data.isEmpty) {
                    return const Center(child: Text('No recent sessions found.'));
                  }
                  
                  final sessions = snapshot.data.data as List;
                  return ListView.builder(
                    itemCount: sessions.length,
                    itemBuilder: (context, index) {
                      final s = sessions[index];
                      return ListTile(
                        leading: Icon(s['wasAutoStarted'] == true ? Icons.settings_remote : Icons.car_rental),
                        title: Text('Session ${s['id'].toString().substring(0,8)}'),
                        subtitle: Text(s['startedAt'] ?? 'Unknown'),
                        trailing: Text('${s['alertEventCount'] ?? 0} Alerts', style: TextStyle(color: (s['alertEventCount'] ?? 0) > 0 ? Colors.red : Colors.green)),
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

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            Icon(isReady ? Icons.check_circle : Icons.error, color: isReady ? Colors.green : Colors.red, size: 48),
            const SizedBox(height: 8),
            Text(isReady ? 'Passive Readiness Active' : 'Setup Required', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(isReady ? 'Waiting for vehicle motion...' : 'Please enable location services and grant permissions.'),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildDiagnosticIcon(Icons.location_on, _isLocationGranted ? Colors.green : Colors.red),
                _buildDiagnosticIcon(Icons.gps_fixed, _isServiceEnabled ? Colors.green : Colors.red),
                _buildDiagnosticIcon(Icons.battery_alert, Colors.orange), // E.g. battery optimization not disabled
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

