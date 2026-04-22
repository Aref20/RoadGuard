'use client';

import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';

type Language = 'ar' | 'en';
type TranslationDictionary = Record<string, string>;

const dictionaries: Record<Language, TranslationDictionary> = {
  ar: {
    'Speed Alert': 'رودجارد',
    'Dashboard': 'لوحة التحكم',
    'Provider Settings': 'إعدادات المزودين',
    'Sign Out': 'تسجيل الخروج',
    'Language': 'اللغة',
    'Overview': 'نظرة عامة',
    'Real-time telemetry and violation tracking.': 'متابعة مباشرة للجلسات والمخالفات والتنبيهات.',
    'Database:': 'قاعدة البيانات:',
    'Healthy': 'سليم',
    'Disconnected': 'غير متصل',
    'Unknown': 'غير معروف',
    'Server Time:': 'وقت الخادم:',
    'Offline': 'غير متصل',
    'Selected Provider': 'المزود المحدد',
    'Registered Drivers': 'السائقون المسجلون',
    'Live Socket': 'اتصال مباشر',
    'Active Sessions': 'الجلسات النشطة',
    'Total Sessions': 'إجمالي الجلسات',
    'Total Violations': 'إجمالي المخالفات',
    'Recorded Events': 'الأحداث المسجلة',
    'Total Alerts Triggered': 'إجمالي التنبيهات',
    'Audio/Haptic Warns': 'تنبيهات صوتية واهتزازية',
    'Live Driving Sessions': 'جلسات القيادة المباشرة',
    'Session ID': 'معرف الجلسة',
    'Started At': 'بدأت في',
    'Trigger': 'طريقة البدء',
    'Status': 'الحالة',
    'No active driving sessions.': 'لا توجد جلسات قيادة نشطة.',
    'Connect to backend to view sessions.': 'اتصل بالخادم لعرض الجلسات.',
    'Auto-Detected': 'تلقائي',
    'Manual': 'يدوي',
    'Speed Providers': 'مزودو حدود السرعة',
    'Configure primary and fallback providers for speed limit checks.': 'حدد المزود الأساسي وترتيب المزودين الاحتياطيين لفحص حدود السرعة.',
    'Saving...': 'جارٍ الحفظ...',
    'Save Settings': 'حفظ الإعدادات',
    'Select Primary Provider': 'اختيار المزود الأساسي',
    'Configured': 'مهيأ',
    'Not Configured': 'غير مهيأ',
    'Fallback Strategy (Priority Order)': 'ترتيب المزودين الاحتياطيين',
    'Use the arrows to change fallback priority.': 'استخدم الأسهم لتغيير ترتيب المزودين.',
    'Primary': 'أساسي',
    'Enabled': 'مفعل',
    'Disabled': 'معطل',
    'No providers found.': 'لا يوجد مزودون.',
    'Users Management': 'إدارة المستخدمين',
    'Manage mobile user accounts, access, and password resets.': 'إدارة حسابات تطبيق الهاتف، التفعيل، وإعادة تعيين كلمات المرور.',
    'Create User': 'إنشاء مستخدم',
    'Edit User': 'تعديل المستخدم',
    'Email': 'البريد الإلكتروني',
    'Role': 'الدور',
    'Active': 'نشط',
    'Registered': 'تاريخ الإنشاء',
    'Actions': 'الإجراءات',
    'No users registered.': 'لا يوجد مستخدمون.',
    'Connect to backend to view users.': 'اتصل بالخادم لعرض المستخدمين.',
    'Deactivate': 'إيقاف',
    'Activate': 'تفعيل',
    'Reset Password': 'إعادة تعيين كلمة المرور',
    'Cancel': 'إلغاء',
    'Save': 'حفظ',
    'Password': 'كلمة المرور',
    'Confirm Password': 'تأكيد كلمة المرور',
    'Passwords do not match.': 'كلمتا المرور غير متطابقتين.',
    'Password must be at least 8 characters.': 'يجب أن تكون كلمة المرور 8 أحرف على الأقل.',
    'User successfully created!': 'تم إنشاء المستخدم بنجاح.',
    'User status updated!': 'تم تحديث حالة المستخدم.',
    'Password reset successfully!': 'تمت إعادة تعيين كلمة المرور بنجاح.',
    'Failed to create user.': 'تعذر إنشاء المستخدم.',
    'Failed to reset password.': 'تعذرت إعادة تعيين كلمة المرور.',
    'Failed to update user status.': 'تعذر تحديث حالة المستخدم.',
    'Settings saved successfully!': 'تم حفظ الإعدادات بنجاح.',
    'Failed to save settings': 'تعذر حفظ الإعدادات.',
    'Admin Control Center': 'مركز تحكم الإدارة',
    'Secure telemetry, provider controls, and mobile user management.': 'لوحة آمنة للقياس الحي، التحكم بالمزودين، وإدارة مستخدمي الهاتف.',
    'Connected': 'متصل',
    'Live': 'مباشر',
    'Polling': 'تحديث دوري',
    'Checking': 'جارٍ الفحص',
    'Polling Mode': 'وضع التحديث الدوري',
    'Dismiss': 'إخفاء',
    'Backend API Offline': 'خادم الواجهة غير متاح',
    'The dashboard will keep retrying the API and telemetry stream in the background.': 'ستواصل لوحة التحكم محاولة إعادة الاتصال بالخادم وبث القياس في الخلفية.',
    'Live Telemetry Unavailable': 'القياس المباشر غير متاح',
    'The dashboard switched to polling mode while the SignalR endpoint is unavailable.': 'تم تحويل لوحة التحكم إلى وضع التحديث الدوري ريثما يتعذر الوصول إلى نقطة SignalR.',
    'WebSocket Disconnected / Backend API Offline': 'انقطع اتصال البث المباشر أو أن الخادم غير متاح',
    'The admin UI is running, but the backend telemetry stream is unavailable right now.': 'واجهة الإدارة تعمل، لكن بث القياس المباشر من الخادم غير متاح حالياً.',
    'Speed Alert Admin': 'إدارة رودجارد',
    'Sign in to access telemetry dashboard': 'سجل الدخول للوصول إلى لوحة الإدارة والقياس المباشر.',
    'Admin Email': 'بريد المدير',
    'Sign In': 'تسجيل الدخول',
    'Backend API expects': 'عنوان الخادم الحالي',
    'Connection to .NET API failed.': 'تعذر الاتصال بخادم .NET.',
    'AUTH_INVALID_CREDENTIALS': 'بيانات الدخول غير صحيحة.',
    'AUTH_EMAIL_IN_USE': 'البريد الإلكتروني مستخدم بالفعل.',
    'AUTH_ACCOUNT_DISABLED': 'الحساب معطل. يرجى التواصل مع المدير.',
    'AUTH_FORBIDDEN': 'هذا الحساب لا يملك صلاحية الوصول إلى لوحة الإدارة.',
    'AUTH_UNAUTHORIZED': 'يلزم تسجيل الدخول أولاً.',
    'AUTH_SELF_REGISTRATION_DISABLED': 'إنشاء الحسابات العامة متوقف. يجب أن ينشئ المدير الحساب.',
    'No token received': 'لم يتم استلام رمز الدخول.',
    'Failed to fetch dashboard data.': 'تعذر تحميل بيانات لوحة الإدارة.',
    'SignalR connection failed.': 'تعذر بدء الاتصال المباشر.',
    'Failed to fetch provider settings.': 'تعذر تحميل إعدادات المزودين.',
  },
  en: {
    'Speed Alert': 'RoadGuard',
    'Dashboard': 'Dashboard',
    'Provider Settings': 'Provider Settings',
    'Sign Out': 'Sign Out',
    'Language': 'Language',
    'Overview': 'Overview',
    'Real-time telemetry and violation tracking.': 'Real-time session, violation, and alert telemetry.',
    'Database:': 'Database:',
    'Healthy': 'Healthy',
    'Disconnected': 'Disconnected',
    'Unknown': 'Unknown',
    'Server Time:': 'Server Time:',
    'Offline': 'Offline',
    'Selected Provider': 'Selected Provider',
    'Registered Drivers': 'Registered Drivers',
    'Live Socket': 'Live Socket',
    'Active Sessions': 'Active Sessions',
    'Total Sessions': 'Total Sessions',
    'Total Violations': 'Total Violations',
    'Recorded Events': 'Recorded Events',
    'Total Alerts Triggered': 'Total Alerts Triggered',
    'Audio/Haptic Warns': 'Audio/Haptic Warns',
    'Live Driving Sessions': 'Live Driving Sessions',
    'Session ID': 'Session ID',
    'Started At': 'Started At',
    'Trigger': 'Trigger',
    'Status': 'Status',
    'No active driving sessions.': 'No active driving sessions.',
    'Connect to backend to view sessions.': 'Connect to the backend to view sessions.',
    'Auto-Detected': 'Auto-Detected',
    'Manual': 'Manual',
    'Speed Providers': 'Speed Providers',
    'Configure primary and fallback providers for speed limit checks.': 'Configure the primary provider and fallback order for speed-limit lookups.',
    'Saving...': 'Saving...',
    'Save Settings': 'Save Settings',
    'Select Primary Provider': 'Select Primary Provider',
    'Configured': 'Configured',
    'Not Configured': 'Not Configured',
    'Fallback Strategy (Priority Order)': 'Fallback Strategy (Priority Order)',
    'Use the arrows to change fallback priority.': 'Use the arrows to change fallback priority.',
    'Primary': 'Primary',
    'Enabled': 'Enabled',
    'Disabled': 'Disabled',
    'No providers found.': 'No providers found.',
    'Users Management': 'Users Management',
    'Manage mobile user accounts, access, and password resets.': 'Manage mobile users, account access, and password resets.',
    'Create User': 'Create User',
    'Edit User': 'Edit User',
    'Email': 'Email',
    'Role': 'Role',
    'Active': 'Active',
    'Registered': 'Registered',
    'Actions': 'Actions',
    'No users registered.': 'No users registered.',
    'Connect to backend to view users.': 'Connect to the backend to view users.',
    'Deactivate': 'Deactivate',
    'Activate': 'Activate',
    'Reset Password': 'Reset Password',
    'Cancel': 'Cancel',
    'Save': 'Save',
    'Password': 'Password',
    'Confirm Password': 'Confirm Password',
    'Passwords do not match.': 'Passwords do not match.',
    'Password must be at least 8 characters.': 'Password must be at least 8 characters.',
    'User successfully created!': 'User successfully created.',
    'User status updated!': 'User status updated.',
    'Password reset successfully!': 'Password reset successfully.',
    'Failed to create user.': 'Failed to create user.',
    'Failed to reset password.': 'Failed to reset password.',
    'Failed to update user status.': 'Failed to update user status.',
    'Settings saved successfully!': 'Settings saved successfully.',
    'Failed to save settings': 'Failed to save settings.',
    'Admin Control Center': 'Admin Control Center',
    'Secure telemetry, provider controls, and mobile user management.': 'Secure telemetry, provider controls, and mobile user management.',
    'Connected': 'Connected',
    'Live': 'Live',
    'Polling': 'Polling',
    'Checking': 'Checking',
    'Polling Mode': 'Polling mode',
    'Dismiss': 'Dismiss',
    'Backend API Offline': 'Backend API offline',
    'The dashboard will keep retrying the API and telemetry stream in the background.': 'The dashboard will keep retrying the API and telemetry stream in the background.',
    'Live Telemetry Unavailable': 'Live telemetry unavailable',
    'The dashboard switched to polling mode while the SignalR endpoint is unavailable.': 'The dashboard switched to polling mode while the SignalR endpoint is unavailable.',
    'WebSocket Disconnected / Backend API Offline': 'WebSocket disconnected / backend API offline',
    'The admin UI is running, but the backend telemetry stream is unavailable right now.': 'The admin UI is running, but the backend telemetry stream is unavailable right now.',
    'Speed Alert Admin': 'RoadGuard Admin',
    'Sign in to access telemetry dashboard': 'Sign in to access the telemetry dashboard.',
    'Admin Email': 'Admin Email',
    'Sign In': 'Sign In',
    'Backend API expects': 'Current backend origin',
    'Connection to .NET API failed.': 'Connection to the .NET API failed.',
    'AUTH_INVALID_CREDENTIALS': 'Invalid email or password.',
    'AUTH_EMAIL_IN_USE': 'Email is already in use.',
    'AUTH_ACCOUNT_DISABLED': 'This account is disabled. Please contact an administrator.',
    'AUTH_FORBIDDEN': 'This account does not have permission to access the admin dashboard.',
    'AUTH_UNAUTHORIZED': 'Authentication is required.',
    'AUTH_SELF_REGISTRATION_DISABLED': 'Public self-registration is disabled. An administrator must create the account.',
    'No token received': 'No token received.',
    'Failed to fetch dashboard data.': 'Failed to fetch dashboard data.',
    'SignalR connection failed.': 'SignalR connection failed.',
    'Failed to fetch provider settings.': 'Failed to fetch provider settings.',
  },
};

type LanguageContextProps = {
  language: Language;
  setLanguage: (lang: Language) => void;
  t: (key: string) => string;
  isRtl: boolean;
};

const LanguageContext = createContext<LanguageContextProps | undefined>(undefined);

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<Language>(() => {
    if (typeof window === 'undefined') {
      return 'ar';
    }

    const storedLanguage = localStorage.getItem('appLang');
    return storedLanguage === 'ar' || storedLanguage === 'en' ? storedLanguage : 'ar';
  });

  useEffect(() => {
    document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = language;
  }, [language]);

  const setLanguage = (lang: Language) => {
    setLanguageState(lang);
    localStorage.setItem('appLang', lang);
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  };

  const value = useMemo<LanguageContextProps>(() => ({
    language,
    setLanguage,
    t: (key: string) => dictionaries[language][key] || dictionaries.ar[key] || key,
    isRtl: language === 'ar',
  }), [language]);

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }

  return context;
}
