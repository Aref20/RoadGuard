import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';

class MonitorScreen extends StatefulWidget {
  const MonitorScreen({Key? key}) : super(key: key);

  @override
  State<MonitorScreen> createState() => _MonitorScreenState();
}

class _MonitorScreenState extends State<MonitorScreen> {

  @override
  Widget build(BuildContext context) {
    return StreamBuilder<Map<String, dynamic>?>(
      stream: FlutterBackgroundService().on('update'),
      builder: (context, snapshot) {
        if (!snapshot.hasData) {
          return const Scaffold(
            backgroundColor: Colors.black,
            body: Center(child: Text("Waiting for GPS telemetry...", style: TextStyle(color: Colors.white54))),
          );
        }

        final data = snapshot.data!;
        double currentSpeed = (data['currentSpeed'] as num?)?.toDouble() ?? 0.0;
        double speedLimit = (data['speedLimit'] as num?)?.toDouble() ?? -1.0;
        bool isAlerting = data['isAlerting'] == true;
        String providerStatus = data['providerStatus'] as String? ?? "Unknown";
        
        final isDanger = speedLimit > 0 && currentSpeed > speedLimit;
        final hasLimit = speedLimit > 0;

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
                    border: Border.all(color: hasLimit ? Colors.redAccent : Colors.grey, width: 8),
                    color: Colors.white,
                  ),
                  child: Text(
                    hasLimit ? '${speedLimit.toInt()}' : '--',
                    style: TextStyle(fontSize: 48, fontWeight: FontWeight.bold, color: hasLimit ? Colors.black : Colors.grey),
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
            onPressed: () {
              FlutterBackgroundService().invoke('stopService');
              Navigator.pop(context);
            },
            backgroundColor: Colors.red,
            child: const Icon(Icons.stop),
          ),
          floatingActionButtonLocation: FloatingActionButtonLocation.centerFloat,
        );
      }
    );
  }
}
