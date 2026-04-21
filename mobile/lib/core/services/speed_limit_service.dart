import 'package:dio/dio.dart';
import '../api/api_client.dart';

class SpeedLimitService {
  final ApiClient _apiClient = ApiClient();

  Future<Map<String, dynamic>?> getSpeedLimit(double lat, double lng) async {
    try {
      final response = await _apiClient.dio.get(
        '/speed-limit/lookup',
        queryParameters: {'lat': lat, 'lng': lng},
      );
      return response.data;
    } catch (e) {
      if (e is DioException && e.response?.statusCode == 401) {
        // Auth error, avoid spamming
      }
      return null;
    }
  }
}
