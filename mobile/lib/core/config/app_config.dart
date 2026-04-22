import 'package:flutter/foundation.dart';

class AppConfig {
  static const String environment = String.fromEnvironment(
    'ENVIRONMENT',
    defaultValue: 'development',
  );

  static const String _apiBaseUrlOverride = String.fromEnvironment(
    'API_BASE_URL',
  );

  static String get apiBaseUrl {
    final configuredBaseUrl = _normalizeApiBaseUrl(_apiBaseUrlOverride);
    if (configuredBaseUrl != null) {
      return configuredBaseUrl;
    }

    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:8080/api';
    }

    return 'http://localhost:8080/api';
  }

  static String? _normalizeApiBaseUrl(String rawValue) {
    final trimmedValue = rawValue.trim();
    if (trimmedValue.isEmpty) {
      return null;
    }

    final normalizedValue = trimmedValue.endsWith('/')
        ? trimmedValue.substring(0, trimmedValue.length - 1)
        : trimmedValue;

    if (normalizedValue.endsWith('/api')) {
      return normalizedValue;
    }

    return '$normalizedValue/api';
  }
}
