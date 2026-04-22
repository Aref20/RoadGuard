'use client';

import { Activity, Bell, Database, Server, ShieldAlert, Users } from 'lucide-react';
import { AdminSession, SystemHealth } from '@/lib/api';
import { TelemetryMode } from './use-admin-dashboard';

function MetricCard({
  title,
  value,
  subtitle,
  icon,
}: {
  title: string;
  value: string;
  subtitle: string;
  icon: React.ReactNode;
}) {
  return (
    <div className="group relative overflow-hidden rounded-xl border border-slate-800 bg-slate-900 p-6">
      <div className="absolute right-0 top-0 translate-x-2 -translate-y-2 p-6 opacity-20 transition-transform duration-300 group-hover:scale-110">
        {icon}
      </div>
      <h3 className="mb-2 text-sm font-medium text-slate-400">{title}</h3>
      <div className="mb-2 text-3xl font-bold tracking-tight text-slate-100" dir="ltr">
        {value}
      </div>
      <span className="text-xs font-medium text-slate-500">{subtitle}</span>
    </div>
  );
}

type OverviewPanelProps = {
  health: SystemHealth | null;
  sessions: AdminSession[];
  isBackendConnected: boolean | null;
  telemetryMode: TelemetryMode;
  isRtl: boolean;
  language: 'ar' | 'en';
  t: (key: string) => string;
};

export function OverviewPanel({
  health,
  sessions,
  isBackendConnected,
  telemetryMode,
  isRtl,
  language,
  t,
}: OverviewPanelProps) {
  const telemetryLabel = telemetryMode === 'realtime'
    ? t('Live Socket')
    : telemetryMode === 'polling'
      ? t('Polling Mode')
      : t('Offline');

  return (
    <>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="mb-1 text-3xl font-bold tracking-tight text-white">{t('Overview')}</h1>
          <p className="text-sm text-slate-400">{t('Real-time telemetry and violation tracking.')}</p>
        </div>
        <div className={isRtl ? 'text-left' : 'text-right'}>
          <div className={`mb-1 flex items-center justify-end text-xs text-slate-500 ${isRtl ? 'flex-row-reverse' : ''}`}>
            <Database size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
            {t('Database:')}
            <span className={`mx-1 ${health?.databaseStatus === 'Healthy' ? 'text-emerald-400' : 'text-red-400'}`}>
              {health?.databaseStatus ? t(health.databaseStatus) : t('Unknown')}
            </span>
          </div>
          <div className={`mb-1 flex items-center justify-end text-xs text-slate-500 ${isRtl ? 'flex-row-reverse' : ''}`}>
            <Server size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
            {t('Server Time:')} {health?.serverTime ? new Date(health.serverTime).toLocaleTimeString(language === 'ar' ? 'ar-EG' : 'en-US') : t('Offline')}
          </div>
          <div className={`flex items-center justify-end text-xs text-slate-500 ${isRtl ? 'flex-row-reverse' : ''}`}>
            <ShieldAlert size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
            {t('Selected Provider')}:
            <span className="mx-1 text-slate-300">{health?.selectedProvider || t('Unknown')}</span>
            <span className="text-slate-500">({health?.providerHealth || t('Unknown')})</span>
          </div>
        </div>
      </div>

      <div className="mb-8 grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title={t('Registered Drivers')}
          value={(health?.totalUsers ?? 0).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}
          subtitle={isBackendConnected ? telemetryLabel : t('Offline')}
          icon={<Users size={22} className="text-blue-500" />}
        />
        <MetricCard
          title={t('Active Sessions')}
          value={(health?.activeSessions ?? 0).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}
          subtitle={isBackendConnected ? `${health?.totalSessions ?? 0} ${t('Total Sessions')}` : t('Offline')}
          icon={<Activity size={22} className="text-emerald-500" />}
        />
        <MetricCard
          title={t('Total Violations')}
          value={(health?.totalViolations ?? 0).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}
          subtitle={t('Recorded Events')}
          icon={<ShieldAlert size={22} className="text-orange-500" />}
        />
        <MetricCard
          title={t('Total Alerts Triggered')}
          value={(health?.totalAlerts ?? 0).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}
          subtitle={t('Audio/Haptic Warns')}
          icon={<Bell size={22} className="text-red-500" />}
        />
      </div>

      <div className="rounded-xl border border-slate-800 bg-slate-900">
        <div className="border-b border-slate-800 px-6 py-5">
          <h3 className="font-semibold text-slate-100">{t('Live Driving Sessions')}</h3>
        </div>
        <div className="overflow-x-auto">
          <table className={`w-full whitespace-nowrap text-sm ${isRtl ? 'text-right' : 'text-left'}`}>
            <thead>
              <tr className="bg-slate-950/30 text-slate-500">
                <th className="px-6 py-3 font-medium">{t('Session ID')}</th>
                <th className="px-6 py-3 font-medium">{t('Started At')}</th>
                <th className="px-6 py-3 font-medium">{t('Trigger')}</th>
                <th className="px-6 py-3 font-medium">{t('Status')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/50">
              {sessions.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-slate-500">
                    {isBackendConnected ? t('No active driving sessions.') : t('Connect to backend to view sessions.')}
                  </td>
                </tr>
              ) : (
                sessions.slice(0, 10).map((session) => (
                  <tr key={session.id} className="transition-colors hover:bg-slate-800/50">
                    <td className="px-6 py-4 font-medium text-slate-300">{session.id.slice(0, 8)}</td>
                    <td className="px-6 py-4 text-slate-400" dir="ltr">
                      {new Date(session.startedAt).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}
                    </td>
                    <td className="px-6 py-4 text-slate-400">{session.wasAutoStarted ? t('Auto-Detected') : t('Manual')}</td>
                    <td className="px-6 py-4 text-slate-300">{session.status}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
