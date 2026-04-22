'use client';

import { KeyRound, Plus, Power, PowerOff } from 'lucide-react';
import { AdminUser } from '@/lib/api';

type UsersPanelProps = {
  users: AdminUser[];
  language: 'ar' | 'en';
  isBackendConnected: boolean | null;
  isRtl: boolean;
  onOpenCreate: () => void;
  onOpenReset: (id: string) => void;
  onToggleUser: (id: string, nextStatus: boolean) => void;
  t: (key: string) => string;
};

export function UsersPanel({
  users,
  language,
  isBackendConnected,
  isRtl,
  onOpenCreate,
  onOpenReset,
  onToggleUser,
  t,
}: UsersPanelProps) {
  return (
    <div className="mx-auto mt-4 max-w-6xl">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="mb-1 text-3xl font-bold tracking-tight text-white">{t('Users Management')}</h1>
          <p className="text-sm text-slate-400">{t('Manage mobile user accounts, access, and password resets.')}</p>
        </div>
        <button
          onClick={onOpenCreate}
          className="flex items-center rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-500"
        >
          <Plus size={16} className={isRtl ? 'ml-2' : 'mr-2'} />
          {t('Create User')}
        </button>
      </div>

      <div className="rounded-xl border border-slate-800 bg-slate-900">
        <div className="overflow-x-auto">
          <table className={`w-full whitespace-nowrap text-sm text-${isRtl ? 'right' : 'left'}`}>
            <thead>
              <tr className="bg-slate-950/30 text-slate-500">
                <th className="px-6 py-3 font-medium">{t('Email')}</th>
                <th className="px-6 py-3 font-medium text-center">{t('Role')}</th>
                <th className="px-6 py-3 font-medium text-center">{t('Active')}</th>
                <th className="px-6 py-3 font-medium">{t('Registered')}</th>
                <th className="px-6 py-3 font-medium text-center">{t('Actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/50">
              {users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-8 text-center text-slate-500">
                    {isBackendConnected ? t('No users registered.') : t('Connect to backend to view users.')}
                  </td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id} className="transition-colors hover:bg-slate-800/50">
                    <td className="px-6 py-4 font-medium text-slate-300">{user.email}</td>
                    <td className="px-6 py-4 text-center">
                      <span className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-bold ${
                        user.role === 'Admin' ? 'bg-purple-500/10 text-purple-400' : 'bg-blue-500/10 text-blue-400'
                      }`}>
                        {user.role}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-center">
                      <button
                        onClick={() => onToggleUser(user.id, !user.isActive)}
                        disabled={user.role === 'Admin'}
                        className={`inline-flex items-center justify-center rounded-full p-1.5 transition-colors ${
                          user.isActive
                            ? 'bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20'
                            : 'bg-slate-800 text-slate-500 hover:bg-slate-700 hover:text-slate-300'
                        } disabled:cursor-not-allowed disabled:opacity-50`}
                        title={user.isActive ? t('Deactivate') : t('Activate')}
                      >
                        {user.isActive ? <Power size={14} /> : <PowerOff size={14} />}
                      </button>
                    </td>
                    <td className="px-6 py-4 text-slate-400" dir="ltr">
                      {new Date(user.createdAt).toLocaleDateString(language === 'ar' ? 'ar-EG' : 'en-US')}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <button
                        onClick={() => onOpenReset(user.id)}
                        disabled={user.role === 'Admin'}
                        className="inline-flex items-center rounded bg-slate-800 px-3 py-1.5 text-xs text-slate-300 transition-colors hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <KeyRound size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
                        {t('Reset Password')}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
