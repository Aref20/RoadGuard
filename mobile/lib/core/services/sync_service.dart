import 'package:dio/dio.dart';
import 'package:hive/hive.dart';
import '../api/api_client.dart';

class SyncService {
  static const int _maxQueueItems = 300;
  static const String _boxName = 'offline_queue';

  static final SyncService _instance = SyncService._internal();

  SyncService._internal();

  factory SyncService() => _instance;

  Future<void> queueRequest({
    required String endpoint,
    required Object payload,
    String method = 'POST',
  }) async {
    final box = Hive.box(_boxName);
    await box.add({
      'endpoint': endpoint,
      'payload': payload,
      'method': method,
      'attempts': 0,
      'createdAt': DateTime.now().toIso8601String(),
    });

    await _trimQueue(box);
  }

  Future<void> queueSessionPoints(String sessionId, List<Map<String, dynamic>> points) {
    return queueRequest(endpoint: '/sessions/$sessionId/points', payload: points);
  }

  Future<void> queueSessionAlerts(String sessionId, List<Map<String, dynamic>> alerts) {
    return queueRequest(endpoint: '/sessions/$sessionId/alerts', payload: alerts);
  }

  Future<void> queueSessionEnd(String sessionId, Map<String, dynamic> payload) {
    return queueRequest(endpoint: '/sessions/$sessionId/end', payload: payload, method: 'PUT');
  }

  Future<void> flushQueuedRequests({ApiClient? client}) async {
    final apiClient = client ?? ApiClient();
    final box = Hive.box(_boxName);
    final keys = box.keys.toList()..sort((left, right) => left.toString().compareTo(right.toString()));

    for (final key in keys) {
      final rawItem = box.get(key);
      if (rawItem is! Map) {
        await box.delete(key);
        continue;
      }

      final item = Map<String, dynamic>.from(rawItem.cast<dynamic, dynamic>());

      try {
        await apiClient.dio.request(
          item['endpoint'] as String,
          data: item['payload'],
          options: Options(method: item['method'] as String? ?? 'POST'),
        );

        await box.delete(key);
      } on DioException catch (error) {
        final statusCode = error.response?.statusCode ?? 0;
        final attempts = (item['attempts'] as int? ?? 0) + 1;

        if (statusCode >= 400 && statusCode < 500 && statusCode != 408 && statusCode != 429) {
          await box.delete(key);
          continue;
        }

        await box.put(key, {
          ...item,
          'attempts': attempts,
          'lastAttemptAt': DateTime.now().toIso8601String(),
        });
        break;
      } catch (_) {
        break;
      }
    }
  }

  Future<void> _trimQueue(Box box) async {
    final overflow = box.length - _maxQueueItems;
    if (overflow <= 0) {
      return;
    }

    final keys = box.keys.toList()..sort((left, right) => left.toString().compareTo(right.toString()));
    for (var index = 0; index < overflow; index++) {
      await box.delete(keys[index]);
    }
  }
}
