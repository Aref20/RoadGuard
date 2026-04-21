import 'package:flutter/material.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Speed Alert Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () { 
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Settings available in full release.')),
              );
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
            const Expanded(
              child: Center(child: Text('No recent sessions found.')),
            )
          ],
        ),
      ),
    );
  }

  Widget _buildStatusCard(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            const Icon(Icons.check_circle, color: Colors.green, size: 48),
            const SizedBox(height: 8),
            const Text('Passive Readiness Active', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const Text('Waiting for vehicle motion...'),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildDiagnosticIcon(Icons.location_on, Colors.green),
                _buildDiagnosticIcon(Icons.directions_run, Colors.green),
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
