'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import * as signalR from '@microsoft/signalr';
import {
  API_BASE_URL,
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
export type TelemetryMode = 'connecting' | 'realtime' | 'polling' | 'offline';
export type Notice = {
  kind: 'success' | 'error' | 'info';
  message: string;
} | null;

const TELEMETRY_POLL_INTERVAL_MS = 15000;
const TELEMETRY_RETRY_INTERVAL_MS = 45000;

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

function getHubUrls() {
  return [...new Set([
    `${API_ORIGIN}/hub/telemetry`,
    `${API_BASE_URL}/hub/telemetry`,
  ])];
}

export function useAdminDashboard(t: (key: string) => string) {
  const router = useRouter();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const pollingTimerRef = useRef<number | null>(null);
  const retryTimerRef = useRef<number | null>(null);
  const backendConnectedRef = useRef<boolean | null>(null);
  const telemetryModeRef = useRef<TelemetryMode>('connecting');
  const isStartingConnectionRef = useRef(false);
  const [currentView, setCurrentViewState] = useState<AdminView>('dashboard');
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [sessions, setSessions] = useState<AdminSession[]>([]);
  const [providers, setProviders] = useState<ProviderConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingProviders, setLoadingProviders] = useState(false);
  const [savingProviders, setSavingProviders] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState<boolean | null>(null);
  const [telemetryMode, setTelemetryMode] = useState<TelemetryMode>('connecting');
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

    const updateBackendConnection = (value: boolean | null) => {
      backendConnectedRef.current = value;
      setIsBackendConnected(value);
    };

    const updateTelemetryMode = (value: TelemetryMode) => {
      telemetryModeRef.current = value;
      setTelemetryMode(value);
    };

    const stopPolling = () => {
      if (pollingTimerRef.current !== null) {
        window.clearInterval(pollingTimerRef.current);
        pollingTimerRef.current = null;
      }
    };

    const stopRetryLoop = () => {
      if (retryTimerRef.current !== null) {
        window.clearInterval(retryTimerRef.current);
        retryTimerRef.current = null;
      }
    };

    const loadSnapshot = async (includeProviders = false, silent = false) => {
      try {
        const [healthData, usersData, sessionsData, providersData] = await Promise.all([
          api.getHealth(),
          api.getUsers(),
          api.getSessions(),
          includeProviders ? api.getProviderSettings() : Promise.resolve(null),
        ]);

        if (!isActive) {
          return;
        }

        setHealth(normalizeHealth(healthData));
        setUsers(usersData);
        setSessions(sessionsData);
        if (providersData) {
          setProviders(providersData.sort((left, right) => left.priorityOrder - right.priorityOrder));
        }

        updateBackendConnection(true);

        if (pollingTimerRef.current !== null && telemetryModeRef.current !== 'realtime') {
          updateTelemetryMode('polling');
        }
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (isAuthError(error)) {
          removeAuthToken();
          router.push('/login');
          return;
        }

        updateBackendConnection(false);

        if (telemetryModeRef.current !== 'realtime') {
          updateTelemetryMode('offline');
        }

        if (!silent) {
          setNotice({ kind: 'error', message: error instanceof Error ? error.message : t('Failed to fetch dashboard data.') });
        }
      }
    };

    const scheduleRetryLoop = () => {
      if (retryTimerRef.current !== null) {
        return;
      }

      retryTimerRef.current = window.setInterval(() => {
        void startRealtimeConnection();
      }, TELEMETRY_RETRY_INTERVAL_MS);
    };

    const startPolling = () => {
      if (pollingTimerRef.current !== null) {
        return;
      }

      void loadSnapshot(false, true);
      pollingTimerRef.current = window.setInterval(() => {
        void loadSnapshot(false, true);
      }, TELEMETRY_POLL_INTERVAL_MS);
    };

    const enterFallbackMode = (error?: unknown) => {
      if (!isActive) {
        return;
      }

      if (isAuthError(error)) {
        removeAuthToken();
        router.push('/login');
        return;
      }

      updateTelemetryMode(backendConnectedRef.current ? 'polling' : 'offline');
      startPolling();
      scheduleRetryLoop();
    };

    const buildConnection = (hubUrl: string) => {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token,
          withCredentials: false,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .build();

      connection.on('ReceiveHealth', (payload) => {
        setHealth(normalizeHealth(payload));
        updateBackendConnection(true);
      });

      connection.on('ReceiveUsers', (payload) => {
        setUsers(payload);
      });

      connection.on('ReceiveSessions', (payload) => {
        setSessions(payload);
      });

      connection.onreconnecting(() => {
        if (!isActive) {
          return;
        }

        updateTelemetryMode(backendConnectedRef.current ? 'polling' : 'offline');
        startPolling();
      });

      connection.onreconnected(() => {
        if (!isActive) {
          return;
        }

        updateBackendConnection(true);
        updateTelemetryMode('realtime');
        stopPolling();
        stopRetryLoop();
      });

      connection.onclose((error) => {
        if (!isActive || connectionRef.current !== connection) {
          return;
        }

        enterFallbackMode(error);
      });

      return connection;
    };

    const startRealtimeConnection = async () => {
      if (!isActive || isStartingConnectionRef.current) {
        return;
      }

      const currentState = connectionRef.current?.state;
      if (currentState === signalR.HubConnectionState.Connected ||
        currentState === signalR.HubConnectionState.Connecting ||
        currentState === signalR.HubConnectionState.Reconnecting) {
        return;
      }

      isStartingConnectionRef.current = true;

      try {
        let lastError: unknown = null;

        for (const hubUrl of getHubUrls()) {
          if (!isActive) {
            return;
          }

          const connection = buildConnection(hubUrl);
          connectionRef.current = connection;

          try {
            await connection.start();

            if (!isActive) {
              await connection.stop().catch(() => undefined);
              return;
            }

            updateBackendConnection(true);
            updateTelemetryMode('realtime');
            stopPolling();
            stopRetryLoop();
            return;
          } catch (error) {
            lastError = error;
            connectionRef.current = null;
            await connection.stop().catch(() => undefined);
          }
        }

        enterFallbackMode(lastError);
      } finally {
        isStartingConnectionRef.current = false;
      }
    };

    void loadSnapshot(true).finally(() => {
      if (isActive) {
        setLoading(false);
      }
    });
    void startRealtimeConnection();

    return () => {
      isActive = false;
      stopPolling();
      stopRetryLoop();
      connectionRef.current?.stop().catch(() => undefined);
      connectionRef.current = null;
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
    telemetryMode,
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
