'use client';

import { Globe, LayoutDashboard, LogOut, Settings, Users } from 'lucide-react';
import { AdminView } from './use-admin-dashboard';

type SidebarProps = {
  currentView: AdminView;
  onSelectView: (view: AdminView) => void;
  onToggleLanguage: () => void;
  onLogout: () => void;
  isRtl: boolean;
  language: 'ar' | 'en';
  t: (key: string) => string;
};

function NavItem({
  active,
  icon,
  label,
  onClick,
  isRtl,
}: {
  active: boolean;
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
  isRtl: boolean;
}) {
  return (
    <button
      onClick={onClick}
      className={`flex w-full items-center rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
        active ? 'bg-red-500/10 text-red-500' : 'text-slate-400 hover:bg-slate-800/50 hover:text-slate-200'
      }`}
    >
      <span className={isRtl ? 'ml-3' : 'mr-3'}>{icon}</span>
      {label}
    </button>
  );
}

export function Sidebar({
  currentView,
  onSelectView,
  onToggleLanguage,
  onLogout,
  isRtl,
  language,
  t,
}: SidebarProps) {
  return (
    <aside className={`flex w-64 flex-col ${isRtl ? 'border-l' : 'border-r'} border-slate-800 bg-slate-900`}>
      <div className="flex h-16 items-center border-b border-slate-800 px-6">
        <div className={`h-3 w-3 rounded-full bg-red-500 ${isRtl ? 'ml-3' : 'mr-3'}`} />
        <span className="text-lg font-bold tracking-tight text-slate-100">{t('Speed Alert')}</span>
      </div>

      <nav className="flex-1 space-y-2 overflow-y-auto px-4 py-6">
        <NavItem
          active={currentView === 'dashboard'}
          icon={<LayoutDashboard size={20} />}
          label={t('Dashboard')}
          onClick={() => onSelectView('dashboard')}
          isRtl={isRtl}
        />
        <NavItem
          active={currentView === 'users'}
          icon={<Users size={20} />}
          label={t('Users Management')}
          onClick={() => onSelectView('users')}
          isRtl={isRtl}
        />
        <NavItem
          active={currentView === 'settings'}
          icon={<Settings size={20} />}
          label={t('Provider Settings')}
          onClick={() => onSelectView('settings')}
          isRtl={isRtl}
        />
      </nav>

      <div className="space-y-2 border-t border-slate-800 p-4">
        <button
          onClick={onToggleLanguage}
          className="flex w-full items-center px-2 py-2 text-sm text-slate-300 transition-colors hover:text-white"
        >
          <Globe size={18} className={isRtl ? 'ml-3' : 'mr-3'} />
          {t('Language')} ({language === 'ar' ? 'العربية' : 'English'})
        </button>
        <button
          onClick={onLogout}
          className="flex w-full items-center px-2 py-2 text-sm text-red-400 transition-colors hover:text-red-300"
        >
          <LogOut size={18} className={isRtl ? 'ml-3' : 'mr-3'} />
          {t('Sign Out')}
        </button>
      </div>
    </aside>
  );
}
