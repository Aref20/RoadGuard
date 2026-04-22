'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import * as signalR from '@microsoft/signalr';
import {
  API_ORIGIN,
  AdminSession,
  AdminUser,
  ApiError,
  ProviderConfig,
  SystemHealth,
  api,
  getAuthToken,
  removeAuthToken,
} from '@/lib/api';

export type AdminView = 'dashboard' | 'settings' | 'users';
export type Notice = {
  kind: 'success' | 'error' | 'info';
  message: string;
} | null;

function normalizeHealth(payload: any): SystemHealth {
  return {
    totalUsers: payload.totalUsers ?? payload.TotalUsers ?? 0,
    totalSessions: payload.totalSessions ?? payload.TotalSessions ?? 0,
    activeSessions: payload.activeSessions ?? payload.ActiveSessions ?? 0,
    autoStartedSessions: payload.autoStartedSessions ?? payload.AutoStartedSessions ?? 0,
    totalViolations: payload.totalViolations ?? payload.TotalViolations ?? 0,
    totalAlerts: payload.totalAlerts ?? payload.TotalAlerts ?? 0,
    databaseStatus: payload.databaseStatus ?? payload.DatabaseStatus ?? 'Unknown',
    selectedProvider: payload.selectedProvider ?? payload.SelectedProvider ?? null,
    providerHealth: payload.providerHealth ?? payload.ProviderHealth ?? null,
    serverTime: payload.serverTime ?? payload.ServerTime ?? new Date().toISOString(),
  };
}

function isAuthError(error: unknown) {
  return error instanceof ApiError &&
    (error.status === 401 || (error.status === 403 && ['AUTH_FORBIDDEN', 'AUTH_ACCOUNT_DISABLED'].includes(error.code || '')));
}

export function useAdminDashboard(t: (key: string) => string) {
  const router = useRouter();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [currentView, setCurrentViewState] = useState<AdminView>('dashboard');
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [sessions, setSessions] = useState<AdminSession[]>([]);
  const [providers, setProviders] = useState<ProviderConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingProviders, setLoadingProviders] = useState(false);
  const [savingProviders, setSavingProviders] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState<boolean | null>(null);
  const [notice, setNotice] = useState<Notice>(null);

  const refreshProviders = async () => {
    setLoadingProviders(true);
    try {
      const providerData = await api.getProviderSettings();
      setProviders(providerData.sort((left, right) => left.priorityOrder - right.priorityOrder));
    } catch (error) {
      setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to fetch provider settings.') });
    } finally {
      setLoadingProviders(false);
    }
  };

  const setCurrentView = (view: AdminView) => {
    setCurrentViewState(view);
    if (view === 'settings' && !loading) {
      void refreshProviders();
    }
  };

  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      router.push('/login');
      return;
    }

    let isActive = true;

    const bootstrap = async () => {
      try {
        const [healthData, usersData, sessionsData, providersData] = await Promise.all([
          api.getHealth(),
          api.getUsers(),
          api.getSessions(),
          api.getProviderSettings(),
        ]);

        if (!isActive) {
          return;
        }

        setHealth(normalizeHealth(healthData));
        setUsers(usersData);
        setSessions(sessionsData);
        setProviders(providersData.sort((left, right) => left.priorityOrder - right.priorityOrder));
        setIsBackendConnected(true);
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (isAuthError(error)) {
          removeAuthToken();
          router.push('/login');
          return;
        }

        setIsBackendConnected(false);
        setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to fetch dashboard data.') });
      } finally {
        if (isActive) {
          setLoading(false);
        }
      }
    };

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_ORIGIN}/hub/telemetry`, {
        accessTokenFactory: () => token,
        withCredentials: false,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connectionRef.current = connection;

    connection.on('ReceiveHealth', (payload) => {
      setHealth(normalizeHealth(payload));
      setIsBackendConnected(true);
    });

    connection.on('ReceiveUsers', (payload) => {
      setUsers(payload);
    });

    connection.on('ReceiveSessions', (payload) => {
      setSessions(payload);
    });

    connection.onclose(() => setIsBackendConnected(false));
    connection.onreconnecting(() => setIsBackendConnected(false));
    connection.onreconnected(() => setIsBackendConnected(true));

    bootstrap();

    connection.start().catch((error) => {
      setIsBackendConnected(false);
      setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('SignalR connection failed.') });
    });

    return () => {
      isActive = false;
      connection.stop().catch(() => undefined);
    };
  }, [router, t]);

  const saveProviderSettings = async () => {
    setSavingProviders(true);
    try {
      await api.updateProviderSettings(providers);
      await refreshProviders();
      setNotice({ kind: 'success', message: t('Settings saved successfully!') });
    } catch (error) {
      setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to save settings') });
    } finally {
      setSavingProviders(false);
    }
  };

  const toggleProvider = (providerKey: string) => {
    setProviders((current) =>
      current.map((provider) =>
        provider.providerKey === providerKey
          ? {
              ...provider,
              isEnabled: !provider.isEnabled,
              isSelected: provider.isSelected && provider.isEnabled ? false : provider.isSelected,
            }
          : provider,
      ),
    );
  };

  const selectProvider = (providerKey: string) => {
    setProviders((current) =>
      current.map((provider) => ({
        ...provider,
        isSelected: provider.providerKey === providerKey,
        isEnabled: provider.providerKey === providerKey ? true : provider.isEnabled,
      })),
    );
  };

  const moveProvider = (providerKey: string, direction: 'up' | 'down') => {
    setProviders((current) => {
      const sorted = [...current].sort((left, right) => left.priorityOrder - right.priorityOrder);
      const index = sorted.findIndex((provider) => provider.providerKey === providerKey);
      if (index < 0) {
        return current;
      }

      const targetIndex = direction === 'up' ? index - 1 : index + 1;
      if (targetIndex < 0 || targetIndex >= sorted.length) {
        return current;
      }

      [sorted[index], sorted[targetIndex]] = [sorted[targetIndex], sorted[index]];
      return sorted.map((provider, providerIndex) => ({ ...provider, priorityOrder: providerIndex }));
    });
  };

  const createUser = async (payload: { email: string; password: string }) => {
    const createdUser = await api.createUser(payload);
    setUsers((current) => [createdUser, ...current]);
    setNotice({ kind: 'success', message: t('User successfully created!') });
  };

  const updateUserStatus = async (id: string, isActive: boolean) => {
    await api.updateUserStatus(id, isActive);
    setUsers((current) => current.map((user) => (user.id === id ? { ...user, isActive } : user)));
    setNotice({ kind: 'success', message: t('User status updated!') });
  };

  const resetUserPassword = async (id: string, password: string) => {
    await api.resetUserPassword(id, password);
    setNotice({ kind: 'success', message: t('Password reset successfully!') });
  };

  const logout = () => {
    removeAuthToken();
    router.push('/login');
  };

  return {
    currentView,
    setCurrentView,
    health,
    users,
    sessions,
    providers,
    loading,
    loadingProviders,
    savingProviders,
    isBackendConnected,
    notice,
    setNotice,
    toggleProvider,
    selectProvider,
    moveProvider,
    saveProviderSettings,
    refreshProviders,
    createUser,
    updateUserStatus,
    resetUserPassword,
    logout,
  };
}
