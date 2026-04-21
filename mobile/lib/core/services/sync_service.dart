import 'dart:async';
import 'package:hive/hive.dart';
import '../api/api_client.dart';

class SyncService {
  static final SyncService _instance = SyncService._internal();
  factory SyncService() => _instance;
  
  Timer? _syncTimer;

  SyncService._internal() {
    _startPeriodicSync();
  }

  void _startPeriodicSync() {
    _syncTimer = Timer.periodic(const Duration(minutes: 5), (timer) async {
      await syncOfflineQueue();
    });
  }

  Future<void> queueData(String endpoint, Map<String, dynamic> payload) async {
    final box = Hive.box('offline_queue');
    await box.add({'endpoint': endpoint, 'payload': payload});
  }

  Future<void> syncOfflineQueue() async {
    final box = Hive.box('offline_queue');
    if (box.isEmpty) return;

    final items = box.toMap();
    for (final key in items.keys) {
      final item = items[key];
      try {
        await ApiClient().dio.post(item['endpoint'], data: item['payload']);
        await box.delete(key);
      } catch (e) {
        // Leave in queue if network fails
        break; 
      }
    }
  }

  void dispose() {
    _syncTimer?.cancel();
  }
}
