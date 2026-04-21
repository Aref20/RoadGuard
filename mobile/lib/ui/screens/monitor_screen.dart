import 'package:flutter/material.dart';

class MonitorScreen extends StatefulWidget {
  const MonitorScreen({Key? key}) : super(key: key);

  @override
  State<MonitorScreen> createState() => _MonitorScreenState();
}

class _MonitorScreenState extends State<MonitorScreen> {
  // Mock data for UI representation
  double currentSpeed = 65.0;
  double speedLimit = 60.0;
  bool isAlerting = true;
  String providerStatus = "Verified recently";

  @override
  Widget build(BuildContext context) {
    final isDanger = currentSpeed > speedLimit;

    return Scaffold(
      backgroundColor: isAlerting ? Colors.red.shade900 : Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: const Text('Live Monitor'),
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              '${currentSpeed.toInt()}',
              style: const TextStyle(fontSize: 120, fontWeight: FontWeight.bold, color: Colors.white),
            ),
            const Text('km/h', style: TextStyle(fontSize: 24, color: Colors.white70)),
            const SizedBox(height: 48),
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: Colors.redAccent, width: 8),
                color: Colors.white,
              ),
              child: Text(
                '${speedLimit.toInt()}',
                style: const TextStyle(fontSize: 48, fontWeight: FontWeight.bold, color: Colors.black),
              ),
            ),
            const SizedBox(height: 24),
            Text('Limit Status: $providerStatus', style: const TextStyle(color: Colors.white54)),
            const SizedBox(height: 48),
            if (isAlerting)
              const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.warning, color: Colors.orangeAccent),
                  SizedBox(width: 8),
                  Text('SLOW DOWN', style: TextStyle(fontSize: 24, color: Colors.orangeAccent, fontWeight: FontWeight.bold)),
                ],
              )
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => Navigator.pop(context),
        child: const Icon(Icons.stop),
        backgroundColor: Colors.red,
      ),
      floatingActionButtonLocation: FloatingActionButtonLocation.centerFloat,
    );
  }
}
