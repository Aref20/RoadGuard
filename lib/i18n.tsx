'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

type Language = 'ar' | 'en';

interface TranslationDictionary {
  [key: string]: string;
}

const dictionaries: Record<Language, TranslationDictionary> = {
  ar: {
    'Speed Alert': 'تنبيه السرعة',
    'Dashboard': 'لوحة القيادة',
    'Provider Settings': 'إعدادات المزود',
    'Sign Out': 'تسجيل الخروج',
    'Search driver by ID or License...': 'البحث عن سائق بالهوية أو الرخصة...',
    'AD': 'مدير',
    'WebSocket Disconnected / Backend API Offline': 'انقطع اتصال WebSocket / واجهة برمجة التطبيقات غير متصلة',
    'The Web UI is fully functional but unable to establish a WebSocket stream to': 'واجهة الويب تعمل بكامل طاقتها ولكن لا يمكنها إنشاء دفق WebSocket إلى',
    'Please ensure the Service is running and accepting connections.': 'يرجى التأكد من أن الخدمة قيد التشغيل وتقبل الاتصالات.',
    'Overview': 'نظرة عامة',
    'Real-time telemetry and violation tracking.': 'القياس عن بعد وتتبع المخالفات في الوقت الفعلي.',
    'Database:': 'قاعدة البيانات:',
    'Healthy': 'متصل',
    'Disconnected': 'منفصل',
    'Unknown': 'غير معروف',
    'Server Time:': 'وقت الخادم:',
    'Offline': 'غير متصل',
    'Registered Drivers': 'السائقون المسجلون',
    'Live Socket': 'مقبس مباشر',
    'Active Sessions': 'الجلسات النشطة',
    'Total Sessions': 'إجمالي الجلسات',
    'Total Violations': 'إجمالي المخالفات',
    'Recorded Events': 'الأحداث المسجلة',
    'Total Alerts Triggered': 'إجمالي التنبيهات الصادرة',
    'Audio/Haptic Warns': 'تحذيرات صوتية / لمسية',
    'Live Driving Sessions': 'جلسات القيادة المباشرة',
    'Session ID': 'معرف الجلسة',
    'Started At': 'بدأت في',
    'Trigger': 'طريقة البدء',
    'No active driving sessions.': 'لا توجد جلسات قيادة نشطة.',
    'Connect to backend to view sessions.': 'اتصل بالخادم لعرض الجلسات.',
    'Auto-Detected': 'تلقائي',
    'Manual': 'يدوي',
    'Driver Roster': 'قائمة السائقين',
    'Email': 'البريد الإلكتروني',
    'Registered': 'تاريخ التسجيل',
    'Status': 'الحالة',
    'No users registered.': 'لا يوجد مستخدمون مسجلون.',
    'Connect to backend to view users.': 'اتصل بالخادم لعرض المستخدمين.',
    'Speed Providers': 'موفرو السرعة',
    'Configure primary and fallback providers for speed limit checks.': 'تكوين المزودين الأساسيين والاحتياطيين لفحوصات حدود السرعة.',
    'Saving...': 'جاري الحفظ...',
    'Save Settings': 'حفظ الإعدادات',
    'Select Primary Provider': 'تحديد المزود الأساسي',
    'API': 'API',
    'Fallback Strategy (Priority Order)': 'استراتيجية الاحتياطي (حسب الأولوية)',
    'Drag not implemented, use arrows': 'السحب غير مدعوم، استخدم الأسهم',
    'Primary': 'أساسي',
    'Enabled': 'مفعل',
    'Disabled': 'معطل',
    'No providers found.': 'لم يتم العثور على مزودين.',
    'Settings saved successfully!': 'تم حفظ الإعدادات بنجاح!',
    'Failed to save settings': 'فشل حفظ الإعدادات',
    'Speed Alert Admin': 'مسؤول تنبيه السرعة',
    'Sign in to access telemetry dashboard': 'تسجيل الدخول للوصول إلى لوحة معلومات القياس عن بعد',
    'Admin Email': 'البريد الإلكتروني للمسؤول',
    'Password': 'كلمة المرور',
    'Sign In': 'تسجيل الدخول',
    'Backend API expects': 'يتوقع الخادم عنوان',
    'Invalid Admin Credentials or Backend Offline': 'بيانات الاعتماد غير صالحة أو الخادم غير متصل',
    'No token received': 'لم يتم استلام رمز التحقق',
    'Connection to .NET API failed.': 'فشل الاتصال بواجهة برمجة تطبيقات .NET.',
    'AUTH_INVALID_CREDENTIALS': 'بيانات الاعتماد غير صالحة',
    'AUTH_EMAIL_IN_USE': 'البريد الإلكتروني قيد الاستخدام بالفعل',
    'Language': 'اللغة'
  },
  en: {
    'Speed Alert': 'Speed Alert',
    'Dashboard': 'Dashboard',
    'Provider Settings': 'Provider Settings',
    'Sign Out': 'Sign Out',
    'Search driver by ID or License...': 'Search driver by ID or License...',
    'AD': 'AD',
    'WebSocket Disconnected / Backend API Offline': 'WebSocket Disconnected / Backend API Offline',
    'The Web UI is fully functional but unable to establish a WebSocket stream to': 'The Web UI is fully functional but unable to establish a WebSocket stream to',
    'Please ensure the Service is running and accepting connections.': 'Please ensure the Service is running and accepting connections.',
    'Overview': 'Overview',
    'Real-time telemetry and violation tracking.': 'Real-time telemetry and violation tracking.',
    'Database:': 'Database:',
    'Healthy': 'Healthy',
    'Disconnected': 'Disconnected',
    'Unknown': 'Unknown',
    'Server Time:': 'Server Time:',
    'Offline': 'Offline',
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
    'No active driving sessions.': 'No active driving sessions.',
    'Connect to backend to view sessions.': 'Connect to backend to view sessions.',
    'Auto-Detected': 'Auto-Detected',
    'Manual': 'Manual',
    'Driver Roster': 'Driver Roster',
    'Email': 'Email',
    'Registered': 'Registered',
    'Status': 'Status',
    'No users registered.': 'No users registered.',
    'Connect to backend to view users.': 'Connect to backend to view users.',
    'Speed Providers': 'Speed Providers',
    'Configure primary and fallback providers for speed limit checks.': 'Configure primary and fallback providers for speed limit checks.',
    'Saving...': 'Saving...',
    'Save Settings': 'Save Settings',
    'Select Primary Provider': 'Select Primary Provider',
    'API': 'API',
    'Fallback Strategy (Priority Order)': 'Fallback Strategy (Priority Order)',
    'Drag not implemented, use arrows': 'Drag not implemented, use arrows',
    'Primary': 'Primary',
    'Enabled': 'Enabled',
    'Disabled': 'Disabled',
    'No providers found.': 'No providers found.',
    'Settings saved successfully!': 'Settings saved successfully!',
    'Failed to save settings': 'Failed to save settings',
    'Speed Alert Admin': 'Speed Alert Admin',
    'Sign in to access telemetry dashboard': 'Sign in to access telemetry dashboard',
    'Admin Email': 'Admin Email',
    'Password': 'Password',
    'Sign In': 'Sign In',
    'Backend API expects': 'Backend API expects',
    'Invalid Admin Credentials or Backend Offline': 'Invalid Admin Credentials or Backend Offline',
    'No token received': 'No token received',
    'Connection to .NET API failed.': 'Connection to .NET API failed.',
    'AUTH_INVALID_CREDENTIALS': 'Invalid credentials',
    'AUTH_EMAIL_IN_USE': 'Email already in use',
    'Language': 'Language'
  }
};

interface LanguageContextProps {
  language: Language;
  setLanguage: (lang: Language) => void;
  t: (key: string) => string;
  isRtl: boolean;
}

const LanguageContext = createContext<LanguageContextProps | undefined>(undefined);

export const LanguageProvider = ({ children }: { children: ReactNode }) => {
  const [language, setLanguageState] = useState<Language>('ar');
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    const storedLang = localStorage.getItem('appLang') as Language;
    if (storedLang && (storedLang === 'ar' || storedLang === 'en')) {
      setLanguageState(storedLang);
      document.documentElement.dir = storedLang === 'ar' ? 'rtl' : 'ltr';
      document.documentElement.lang = storedLang;
    } else {
      document.documentElement.dir = 'rtl';
      document.documentElement.lang = 'ar';
    }
    setMounted(true);
  }, []);

  const setLanguage = (lang: Language) => {
    setLanguageState(lang);
    localStorage.setItem('appLang', lang);
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  };

  const t = (key: string): string => {
    return dictionaries[language][key] || dictionaries['ar'][key] || key;
  };

  if (!mounted) {
    return null; // Avoid hydration mismatch
  }

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, isRtl: language === 'ar' }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => {
  const context = useContext(LanguageContext);
  if (context === undefined) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }
  return context;
};
