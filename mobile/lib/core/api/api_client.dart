import 'package:dio/dio.dart';
import 'package:hive_flutter/hive_flutter.dart';

class ApiClient {
  ApiClient._internal() {
    _dio = Dio(BaseOptions(
      baseUrl: _defaultBaseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      sendTimeout: const Duration(seconds: 15),
      headers: {'Accept': 'application/json'},
    ));

    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final box = Hive.box('settings');
        final configuredBaseUrl = (box.get('api_base_url', defaultValue: _defaultBaseUrl) as String?)?.trim();
        options.baseUrl = configuredBaseUrl == null || configuredBaseUrl.isEmpty ? _defaultBaseUrl : configuredBaseUrl;

        final token = box.get('jwt_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }

        handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode == 401 || error.response?.statusCode == 403) {
          await Hive.box('settings').delete('jwt_token');
        }

        handler.next(error);
      },
    ));
  }

  static const String _defaultBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://api-production-ecc5.up.railway.app/api',
  );

  static final ApiClient _instance = ApiClient._internal();

  late final Dio _dio;

  factory ApiClient() {
    return _instance;
  }

  Dio get dio => _dio;
}
