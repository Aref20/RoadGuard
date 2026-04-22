import 'package:flutter/material.dart';

class AppLocalizations {
  final Locale locale;

  AppLocalizations(this.locale);

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  static const Map<String, Map<String, String>> _localizedValues = {
    'en': {
      'title': 'Speed Alert',
      'login': 'Login',
      'email': 'Email',
      'password': 'Password',
      'signIn': 'Sign In',
      'loginFailed': 'Login failed. Check credentials.',
      'setupRequired': 'Setup Required',
      'setupDesc': 'Please enable location services and grant permissions.',
      'activeMonitoring': 'Active Monitoring',
      'activeMonitoringDesc': 'Vehicle motion detected. Safe travels!',
      'passiveReadinessActive': 'Passive readiness active',
      'waitingForVehicleMotion': 'Waiting for vehicle motion...',
      'monitoringPaused': 'Monitoring Paused',
      'monitoringPausedDesc': 'Background service is stopped.',
      'speedWarning': 'Slow Down!',
      'stopBackgroundMonitor': 'Stop Background Monitor',
      'recentSessions': 'Recent Sessions',
      'noRecentSessionsFound': 'No recent sessions found.',
      'limitStatus': 'Speed Limit Status',
      'settings': 'Settings',
      'history': 'History',
      'language': 'Language',
      'arabic': 'عربي',
      'english': 'English',
      'dashboard': 'Speed Alert Dashboard',
      'viewLiveMonitor': 'View Live Monitor',
      'manualOverrideStart': 'Manual Override: Start Monitor',
      'session': 'Session',
      'alerts': 'Alerts',
      'unknown': 'Unknown',
      'audioAlerts': 'Audio Alerts',
      'vibrationAlerts': 'Vibration Alerts',
      'autoDetect': 'Auto-Detect Driving (Hands-free)',
      'autoDetectDesc': 'Uses Activity Recognition to start tracking automatically.',
      'tolerance': 'Overspeed Tolerance (km/h)',
      'logout': 'Logout',
      'drivingHistory': 'Driving History',
      'noHistoryFound': 'No history found.',
      'startedAt': 'Started',
    },
    'ar': {
      'title': 'تنبيه السرعة',
      'login': 'تسجيل الدخول',
      'email': 'البريد الإلكتروني',
      'password': 'كلمة المرور',
      'signIn': 'دخول',
      'loginFailed': 'فشل تسجيل الدخول. تحقق من بيانات الاعتماد.',
      'setupRequired': 'يتطلب الإعداد',
      'setupDesc': 'يرجى تمكين خدمات الموقع ومنح الأذونات.',
      'activeMonitoring': 'المراقبة النشطة',
      'activeMonitoringDesc': 'تم اكتشاف حركة المركبة. رحلة آمنة!',
      'passiveReadinessActive': 'الاستعداد السلبي نشط',
      'waitingForVehicleMotion': 'في انتظار حركة المركبة...',
      'monitoringPaused': 'تم إيقاف المراقبة مؤقتاً',
      'monitoringPausedDesc': 'خدمة الخلفية متوقفة.',
      'speedWarning': 'أبطئ سرعتك!',
      'stopBackgroundMonitor': 'إيقاف مراقبة الخلفية',
      'recentSessions': 'الجلسات الأخيرة',
      'noRecentSessionsFound': 'لم يتم العثور على جلسات أخيرة.',
      'limitStatus': 'حالة حد السرعة',
      'settings': 'الإعدادات',
      'history': 'السجل',
      'language': 'اللغة',
      'arabic': 'عربي',
      'english': 'English',
      'dashboard': 'لوحة تحكم تنبيه السرعة',
      'viewLiveMonitor': 'عرض المراقبة المباشرة',
      'manualOverrideStart': 'تجاوز يدوي: بدء المراقبة',
      'session': 'جلسة',
      'alerts': 'تنبيهات',
      'unknown': 'غير معروف',
      'audioAlerts': 'تنبيهات صوتية',
      'vibrationAlerts': 'تنبيهات الاهتزاز',
      'autoDetect': 'اكتشاف القيادة التلقائي',
      'autoDetectDesc': 'يستخدم التعرف على النشاط لبدء التتبع تلقائيًا.',
      'tolerance': 'تسامح السرعة الزائدة (كم/س)',
      'logout': 'تسجيل الخروج',
      'drivingHistory': 'سجل القيادة',
      'noHistoryFound': 'لم يتم العثور على سجل مسبق.',
      'startedAt': 'بدأت',
    },
  };

  String translate(String key) {
    return _localizedValues[locale.languageCode]?[key] ?? key;
  }
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  bool isSupported(Locale locale) => ['en', 'ar'].contains(locale.languageCode);

  @override
  Future<AppLocalizations> load(Locale locale) async {
    return AppLocalizations(locale);
  }

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

extension AppLocalizationsExtension on BuildContext {
  String tr(String key) => AppLocalizations.of(this).translate(key);
}
