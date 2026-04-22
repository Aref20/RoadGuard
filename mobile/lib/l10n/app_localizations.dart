import 'package:flutter/material.dart';

class AppLocalizations {
  AppLocalizations(this.locale);

  final Locale locale;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate = _AppLocalizationsDelegate();

  static const Map<String, Map<String, String>> _localizedValues = {
    'en': {
      'title': 'RoadGuard',
      'login': 'Login',
      'email': 'Email',
      'password': 'Password',
      'signIn': 'Sign In',
      'loginFailed': 'Login failed. Please check your credentials.',
      'accountDisabled': 'This account is disabled. Contact an administrator.',
      'invalidCredentials': 'Invalid email or password.',
      'adminNotAllowedOnMobile': 'Admin accounts must use the web dashboard.',
      'setupRequired': 'Setup required',
      'setupDesc': 'Enable location services and grant the required permissions.',
      'activeMonitoring': 'Active monitoring',
      'activeMonitoringDesc': 'A verified driving session is running in the background.',
      'passiveReadinessActive': 'Passive readiness active',
      'waitingForVehicleMotion': 'Waiting for vehicle movement...',
      'monitoringPaused': 'Monitoring paused',
      'monitoringPausedDesc': 'Background monitoring is stopped.',
      'speedWarning': 'Slow down now',
      'stopBackgroundMonitor': 'Stop background monitor',
      'recentSessions': 'Recent sessions',
      'noRecentSessionsFound': 'No recent sessions found.',
      'limitStatus': 'Speed limit source',
      'settings': 'Settings',
      'history': 'History',
      'language': 'Language',
      'arabic': 'Arabic',
      'english': 'English',
      'dashboard': 'RoadGuard Dashboard',
      'viewLiveMonitor': 'View live monitor',
      'manualOverrideStart': 'Start monitoring now',
      'session': 'Session',
      'alerts': 'Alerts',
      'unknown': 'Unknown',
      'audioAlerts': 'Sound warnings',
      'vibrationAlerts': 'Vibration warnings',
      'voiceAlerts': 'Voice warnings',
      'autoDetect': 'Auto-detect driving',
      'autoDetectDesc': 'Start tracking automatically when vehicle motion is detected.',
      'tolerance': 'Overspeed tolerance',
      'alertDelay': 'Alert delay (seconds)',
      'alertCooldown': 'Alert cooldown (seconds)',
      'logout': 'Logout',
      'drivingHistory': 'Driving history',
      'noHistoryFound': 'No driving history found.',
      'startedAt': 'Started',
      'permissionsRequired': 'Permissions required before monitoring can start.',
      'locationPermissionDeniedForever': 'Location permission is permanently denied. Open settings to continue.',
      'locationServicesDisabled': 'Location services are disabled. Turn them on to continue.',
      'openSettings': 'Open settings',
      'saveChanges': 'Save changes',
      'settingsSaved': 'Settings saved successfully.',
      'settingsSaveFailed': 'Failed to save settings.',
      'loadingSettings': 'Loading settings...',
      'monitorStartFailed': 'RoadGuard could not start a verified driving session yet. It will retry safely.',
      'monitorStartPending': 'Trying to start a verified driving session...',
      'speedLimitUnavailable': 'Speed limit unavailable',
      'waitingForData': 'Waiting for data...',
      'warningNotificationTitle': 'Speed warning',
      'warningNotificationBody': 'Reduce speed now. Limit: {limit} km/h',
      'monitoringNotificationTitle': 'RoadGuard monitoring',
      'monitoringNotificationBody': 'Current speed: {speed} km/h',
      'passiveNotificationTitle': 'RoadGuard ready',
      'passiveNotificationBody': 'Waiting for vehicle movement...',
      'inactiveNotificationBody': 'Monitoring paused',
    },
    'ar': {
      'title': 'رودجارد',
      'login': 'تسجيل الدخول',
      'email': 'البريد الإلكتروني',
      'password': 'كلمة المرور',
      'signIn': 'دخول',
      'loginFailed': 'فشل تسجيل الدخول. يرجى التحقق من البيانات.',
      'accountDisabled': 'هذا الحساب معطل. يرجى التواصل مع المسؤول.',
      'invalidCredentials': 'البريد الإلكتروني أو كلمة المرور غير صحيحة.',
      'adminNotAllowedOnMobile': 'حسابات الإدارة تستخدم لوحة الويب فقط.',
      'setupRequired': 'يلزم الإعداد',
      'setupDesc': 'فعّل خدمات الموقع وامنح الأذونات المطلوبة.',
      'activeMonitoring': 'المراقبة النشطة',
      'activeMonitoringDesc': 'توجد جلسة قيادة موثقة تعمل في الخلفية.',
      'passiveReadinessActive': 'وضع الاستعداد يعمل',
      'waitingForVehicleMotion': 'بانتظار حركة المركبة...',
      'monitoringPaused': 'المراقبة متوقفة',
      'monitoringPausedDesc': 'تم إيقاف المراقبة في الخلفية.',
      'speedWarning': 'خفف السرعة الآن',
      'stopBackgroundMonitor': 'إيقاف المراقبة الخلفية',
      'recentSessions': 'الجلسات الأخيرة',
      'noRecentSessionsFound': 'لا توجد جلسات حديثة.',
      'limitStatus': 'مصدر حد السرعة',
      'settings': 'الإعدادات',
      'history': 'السجل',
      'language': 'اللغة',
      'arabic': 'العربية',
      'english': 'English',
      'dashboard': 'لوحة رودجارد',
      'viewLiveMonitor': 'عرض المراقبة المباشرة',
      'manualOverrideStart': 'بدء المراقبة الآن',
      'session': 'جلسة',
      'alerts': 'تنبيهات',
      'unknown': 'غير معروف',
      'audioAlerts': 'تنبيهات صوتية',
      'vibrationAlerts': 'تنبيهات اهتزاز',
      'voiceAlerts': 'تنبيهات صوتية ناطقة',
      'autoDetect': 'اكتشاف القيادة تلقائياً',
      'autoDetectDesc': 'ابدأ التتبع تلقائياً عند اكتشاف حركة المركبة.',
      'tolerance': 'هامش السرعة الزائدة',
      'alertDelay': 'تأخير التنبيه (ثوانٍ)',
      'alertCooldown': 'فاصل التنبيه (ثوانٍ)',
      'logout': 'تسجيل الخروج',
      'drivingHistory': 'سجل القيادة',
      'noHistoryFound': 'لا يوجد سجل قيادة.',
      'startedAt': 'بدأت',
      'permissionsRequired': 'يلزم منح الأذونات قبل بدء المراقبة.',
      'locationPermissionDeniedForever': 'تم رفض إذن الموقع نهائياً. افتح الإعدادات للمتابعة.',
      'locationServicesDisabled': 'خدمات الموقع غير مفعلة. فعّلها للمتابعة.',
      'openSettings': 'فتح الإعدادات',
      'saveChanges': 'حفظ التغييرات',
      'settingsSaved': 'تم حفظ الإعدادات بنجاح.',
      'settingsSaveFailed': 'تعذر حفظ الإعدادات.',
      'loadingSettings': 'جارٍ تحميل الإعدادات...',
      'monitorStartFailed': 'تعذر بدء جلسة قيادة موثقة حالياً. سيعيد التطبيق المحاولة بأمان.',
      'monitorStartPending': 'جارٍ محاولة بدء جلسة قيادة موثقة...',
      'speedLimitUnavailable': 'حد السرعة غير متاح',
      'waitingForData': 'بانتظار البيانات...',
      'warningNotificationTitle': 'تحذير سرعة',
      'warningNotificationBody': 'خفف السرعة الآن. الحد: {limit} كم/س',
      'monitoringNotificationTitle': 'مراقبة رودجارد',
      'monitoringNotificationBody': 'السرعة الحالية: {speed} كم/س',
      'passiveNotificationTitle': 'رودجارد جاهز',
      'passiveNotificationBody': 'بانتظار حركة المركبة...',
      'inactiveNotificationBody': 'المراقبة متوقفة',
    },
  };

  String translate(String key, {Map<String, String>? params}) {
    return translateFor(locale.languageCode, key, params: params);
  }

  static String translateFor(String languageCode, String key, {Map<String, String>? params}) {
    final language = _localizedValues.containsKey(languageCode) ? languageCode : 'ar';
    var value = _localizedValues[language]?[key] ?? _localizedValues['ar']?[key] ?? key;

    if (params != null) {
      for (final entry in params.entries) {
        value = value.replaceAll('{${entry.key}}', entry.value);
      }
    }

    return value;
  }
}

class _AppLocalizationsDelegate extends LocalizationsDelegate<AppLocalizations> {
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
  String tr(String key, {Map<String, String>? params}) => AppLocalizations.of(this).translate(key, params: params);
}
