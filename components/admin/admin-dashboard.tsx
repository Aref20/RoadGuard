'use client';

import { useState } from 'react';
import { AlertTriangle, LoaderCircle } from 'lucide-react';
import { Sidebar } from './sidebar';
import { OverviewPanel } from './overview-panel';
import { ProviderSettingsPanel } from './provider-settings-panel';
import { UsersPanel } from './users-panel';
import { UserModal } from './user-modal';
import { useAdminDashboard } from './use-admin-dashboard';
import { useLanguage } from '@/lib/i18n';

export function AdminDashboard() {
  const { t, isRtl, language, setLanguage } = useLanguage();
  const dashboard = useAdminDashboard(t);
  const [isCreateUserOpen, setIsCreateUserOpen] = useState(false);
  const [isResetPasswordOpen, setIsResetPasswordOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const resetForm = () => {
    setEmail('');
    setPassword('');
    setConfirmPassword('');
    setSelectedUserId(null);
  };

  const closeCreateModal = () => {
    setIsCreateUserOpen(false);
    resetForm();
  };

  const closeResetModal = () => {
    setIsResetPasswordOpen(false);
    resetForm();
  };

  const handleCreateUser = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (password !== confirmPassword) {
      dashboard.setNotice({ kind: 'error', message: t('Passwords do not match.') });
      return;
    }

    if (password.length < 8) {
      dashboard.setNotice({ kind: 'error', message: t('Password must be at least 8 characters.') });
      return;
    }

    try {
      await dashboard.createUser({ email, password });
      closeCreateModal();
    } catch (error) {
      dashboard.setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to create user.') });
    }
  };

  const handleResetPassword = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedUserId) {
      return;
    }

    if (password !== confirmPassword) {
      dashboard.setNotice({ kind: 'error', message: t('Passwords do not match.') });
      return;
    }

    if (password.length < 8) {
      dashboard.setNotice({ kind: 'error', message: t('Password must be at least 8 characters.') });
      return;
    }

    try {
      await dashboard.resetUserPassword(selectedUserId, password);
      closeResetModal();
    } catch (error) {
      dashboard.setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to reset password.') });
    }
  };

  const statusBadgeClassName = dashboard.telemetryMode === 'realtime'
    ? 'bg-emerald-500/10 text-emerald-400'
    : dashboard.telemetryMode === 'polling'
      ? 'bg-amber-500/10 text-amber-300'
      : dashboard.telemetryMode === 'offline'
        ? 'bg-red-500/10 text-red-300'
        : 'bg-slate-800 text-slate-300';

  const statusBadgeLabel = dashboard.telemetryMode === 'realtime'
    ? t('Live')
    : dashboard.telemetryMode === 'polling'
      ? t('Polling')
      : dashboard.telemetryMode === 'offline'
        ? t('Offline')
        : t('Checking');

  return (
    <div className="flex h-screen overflow-hidden bg-slate-950" dir={isRtl ? 'rtl' : 'ltr'}>
      <Sidebar
        currentView={dashboard.currentView}
        onSelectView={dashboard.setCurrentView}
        onToggleLanguage={() => setLanguage(language === 'ar' ? 'en' : 'ar')}
        onLogout={dashboard.logout}
        isRtl={isRtl}
        language={language}
        t={t}
      />

      <main className="flex flex-1 flex-col overflow-hidden">
        <header className="flex h-16 items-center justify-between border-b border-slate-800 bg-slate-950/50 px-8 backdrop-blur-md">
          <div>
            <h1 className="text-lg font-semibold text-slate-100">{t('Admin Control Center')}</h1>
            <p className="text-xs text-slate-500">{t('Secure telemetry, provider controls, and mobile user management.')}</p>
          </div>
          <div className={`inline-flex items-center rounded-full px-3 py-1 text-xs ${statusBadgeClassName}`}>
            {statusBadgeLabel}
          </div>
        </header>

        <div className="flex-1 overflow-y-auto p-8">
          {dashboard.notice ? (
            <div
              className={`mb-6 flex items-start rounded-lg border p-4 ${
                dashboard.notice.kind === 'success'
                  ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300'
                  : dashboard.notice.kind === 'info'
                    ? 'border-blue-500/30 bg-blue-500/10 text-blue-200'
                    : 'border-red-500/30 bg-red-500/10 text-red-200'
              }`}
            >
              <AlertTriangle className={isRtl ? 'ml-3' : 'mr-3'} size={18} />
              <div className="flex-1 text-sm">{dashboard.notice.message}</div>
              <button
                onClick={() => dashboard.setNotice(null)}
                className="text-xs uppercase tracking-wide text-slate-400 hover:text-slate-200"
              >
                {t('Dismiss')}
              </button>
            </div>
          ) : null}

          {dashboard.isBackendConnected === false ? (
            <div className="mb-6 flex items-start rounded-lg border border-red-500/20 bg-red-500/10 p-4 text-red-200">
              <AlertTriangle className={isRtl ? 'ml-3' : 'mr-3'} size={18} />
              <div className="text-sm">
                <div className="font-medium">{t('Backend API Offline')}</div>
                <div className="mt-1 text-red-200/80">
                  {t('The dashboard will keep retrying the API and telemetry stream in the background.')}
                </div>
              </div>
            </div>
          ) : null}

          {dashboard.isBackendConnected && dashboard.telemetryMode === 'polling' ? (
            <div className="mb-6 flex items-start rounded-lg border border-amber-500/20 bg-amber-500/10 p-4 text-amber-100">
              <AlertTriangle className={isRtl ? 'ml-3' : 'mr-3'} size={18} />
              <div className="text-sm">
                <div className="font-medium">{t('Live Telemetry Unavailable')}</div>
                <div className="mt-1 text-amber-100/80">
                  {t('The dashboard switched to polling mode while the SignalR endpoint is unavailable.')}
                </div>
              </div>
            </div>
          ) : null}

          {dashboard.loading ? (
            <div className="flex h-full items-center justify-center text-slate-400">
              <LoaderCircle className="animate-spin" size={28} />
            </div>
          ) : null}

          {!dashboard.loading && dashboard.currentView === 'dashboard' ? (
            <OverviewPanel
              health={dashboard.health}
              sessions={dashboard.sessions}
              isBackendConnected={dashboard.isBackendConnected}
              telemetryMode={dashboard.telemetryMode}
              isRtl={isRtl}
              language={language}
              t={t}
            />
          ) : null}

          {!dashboard.loading && dashboard.currentView === 'settings' ? (
            <ProviderSettingsPanel
              providers={dashboard.providers}
              isRtl={isRtl}
              loadingProviders={dashboard.loadingProviders}
              savingProviders={dashboard.savingProviders}
              onToggleProvider={dashboard.toggleProvider}
              onSelectProvider={dashboard.selectProvider}
              onMoveProvider={dashboard.moveProvider}
              onSave={dashboard.saveProviderSettings}
              t={t}
            />
          ) : null}

          {!dashboard.loading && dashboard.currentView === 'users' ? (
            <UsersPanel
              users={dashboard.users}
              language={language}
              isBackendConnected={dashboard.isBackendConnected}
              isRtl={isRtl}
              onOpenCreate={() => setIsCreateUserOpen(true)}
              onOpenReset={(id) => {
                setSelectedUserId(id);
                setIsResetPasswordOpen(true);
              }}
              onToggleUser={(id, nextStatus) => {
                dashboard.updateUserStatus(id, nextStatus).catch((error) => {
                  dashboard.setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to update user status.') });
                });
              }}
              t={t}
            />
          ) : null}
        </div>
      </main>

      {isCreateUserOpen ? (
        <UserModal
          title={t('Create User')}
          submitLabel={t('Save')}
          cancelLabel={t('Cancel')}
          isRtl={isRtl}
          onClose={closeCreateModal}
          onSubmit={handleCreateUser}
        >
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Email')}</label>
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
              required
              dir="ltr"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Password')}</label>
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
              required
              dir="ltr"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Confirm Password')}</label>
            <input
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
              required
              dir="ltr"
            />
          </div>
        </UserModal>
      ) : null}

      {isResetPasswordOpen ? (
        <UserModal
          title={t('Reset Password')}
          submitLabel={t('Save')}
          cancelLabel={t('Cancel')}
          isRtl={isRtl}
          onClose={closeResetModal}
          onSubmit={handleResetPassword}
        >
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Password')}</label>
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
              required
              dir="ltr"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Confirm Password')}</label>
            <input
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
              required
              dir="ltr"
            />
          </div>
        </UserModal>
      ) : null}
    </div>
  );
}
