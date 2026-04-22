'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { motion } from 'motion/react';
import { 
  Car, Activity, ShieldAlert, Users, Database, 
  Server, MapPin, AlertTriangle, Settings, Bell, 
  Search, LayoutDashboard, History, LogOut, CheckCircle2,
  Save, Globe, UserPlus, Key, Power, PowerOff
} from 'lucide-react';
import { api, getAuthToken, removeAuthToken } from '@/lib/api';
import * as signalR from '@microsoft/signalr';
import { useLanguage } from '@/lib/i18n';

type SystemHealth = {
  totalUsers: number;
  totalSessions: number;
  activeSessions: number;
  autoStartedSessions: number;
  totalViolations: number;
  totalAlerts: number;
  databaseStatus: string;
  serverTime: string;
};

type User = {
  id: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  role?: string;
};

type Session = {
  id: string;
  userId: string;
  startedAt: string;
  wasAutoStarted: boolean;
};

type ProviderConfig = {
  providerKey: string;
  displayName: string;
  isEnabled: boolean;
  isSelected: boolean;
  priorityOrder: number;
  updatedAt: string;
};

export default function AdminDashboard() {
  const router = useRouter();
  const { t, isRtl, language, setLanguage } = useLanguage();
  const [currentView, setCurrentView] = useState<'dashboard' | 'settings' | 'users'>('dashboard');
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [providers, setProviders] = useState<ProviderConfig[]>([]);
  const [isBackendConnected, setIsBackendConnected] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingSettings, setSavingSettings] = useState(false);
  
  // User Management State
  const [isAddUserOpen, setIsAddUserOpen] = useState(false);
  const [isResetPasswordOpen, setIsResetPasswordOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [userEmail, setUserEmail] = useState('');
  const [userPassword, setUserPassword] = useState('');
  const [userConfirmPassword, setUserConfirmPassword] = useState('');
  
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  const performLogout = () => {
    removeAuthToken();
    router.push('/login');
  };

  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      router.push('/login');
      return;
    }

    const fetchInitialData = async () => {
      try {
        const [healthData, usersData, sessionsData, providersData] = await Promise.all([
          api.getHealth(),
          api.getUsers(),
          api.getSessions(),
          api.getProviderSettings().catch(() => []) // Fallback if API hasn't updated yet
        ]);
        
        setHealth({
           totalUsers: healthData.totalUsers || healthData.TotalUsers || 0,
           totalSessions: healthData.totalSessions || healthData.TotalSessions || 0,
           activeSessions: healthData.activeSessions || healthData.ActiveSessions || 0,
           autoStartedSessions: healthData.autoStartedSessions || healthData.AutoStartedSessions || 0,
           totalViolations: healthData.totalViolations || healthData.TotalViolations || 0,
           totalAlerts: healthData.totalAlerts || healthData.TotalAlerts || 0,
           databaseStatus: healthData.databaseStatus || healthData.DatabaseStatus,
           serverTime: healthData.serverTime || healthData.ServerTime
        });
        setUsers(usersData);
        setSessions(sessionsData);
        setProviders(providersData);
        setIsBackendConnected(true);
      } catch (err) {
        setIsBackendConnected(false);
      } finally {
        setLoading(false);
      }
    };

    fetchInitialData();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080'}/hub/telemetry`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connectionRef.current = connection;

    connection.on('ReceiveHealth', (data) => {
      setHealth({
           totalUsers: data.totalUsers || data.TotalUsers || 0,
           totalSessions: data.totalSessions || data.TotalSessions || 0,
           activeSessions: data.activeSessions || data.ActiveSessions || 0,
           autoStartedSessions: data.autoStartedSessions || data.AutoStartedSessions || 0,
           totalViolations: data.totalViolations || data.TotalViolations || 0,
           totalAlerts: data.totalAlerts || data.TotalAlerts || 0,
           databaseStatus: data.databaseStatus || data.DatabaseStatus,
           serverTime: data.serverTime || data.ServerTime
      });
      setIsBackendConnected(true);
    });

    connection.on('ReceiveUsers', (data) => setUsers(data));
    connection.on('ReceiveSessions', (data) => setSessions(data));
    connection.onclose(() => setIsBackendConnected(false));
    connection.onreconnecting(() => setIsBackendConnected(false));
    connection.onreconnected(() => setIsBackendConnected(true));

    const startConnection = async () => {
        try {
            await connection.start();
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
            setIsBackendConnected(false);
        }
    };

    startConnection();

    return () => {
      connection.stop();
    };
  }, [router]);

  const saveProviderSettings = async () => {
    setSavingSettings(true);
    try {
      await api.updateProviderSettings(providers);
      alert(t('Settings saved successfully!'));
    } catch (err) {
      alert(t('Failed to save settings'));
    } finally {
      setSavingSettings(false);
    }
  };

  const handleProviderToggle = (key: string) => {
    setProviders(prev => prev.map(p => {
      if (p.providerKey === key) return { ...p, isEnabled: !p.isEnabled };
      return p;
    }));
  };

  const handleProviderSelect = (key: string) => {
    setProviders(prev => prev.map(p => ({
      ...p,
      isSelected: p.providerKey === key
    })));
  };

  const moveProvider = (key: string, direction: 'up' | 'down') => {
    setProviders(prev => {
      const idx = prev.findIndex(p => p.providerKey === key);
      if (idx === -1) return prev;
      if (direction === 'up' && idx === 0) return prev;
      if (direction === 'down' && idx === prev.length - 1) return prev;

      const newArr = [...prev];
      const targetIdx = direction === 'up' ? idx - 1 : idx + 1;
      
      const temp = newArr[idx];
      newArr[idx] = newArr[targetIdx];
      newArr[targetIdx] = temp;
      
      return newArr.map((p, i) => ({ ...p, priorityOrder: i }));
    });
  };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (userPassword !== userConfirmPassword) {
      alert(t('Passwords do not match.'));
      return;
    }
    if (userPassword.length < 8) {
      alert(t('Password must be at least 8 characters.'));
      return;
    }

    try {
      const newUser = await api.createUser({ email: userEmail, password: userPassword });
      setUsers(prev => [...prev, newUser]);
      setIsAddUserOpen(false);
      setUserEmail('');
      setUserPassword('');
      setUserConfirmPassword('');
      alert(t('User successfully created!'));
    } catch (err: any) {
      alert(t(err.message));
    }
  };

  const handleToggleUserStatus = async (id: string, currentStatus: boolean) => {
    try {
      await api.updateUserStatus(id, !currentStatus);
      setUsers(prev => prev.map(u => u.id === id ? { ...u, isActive: !currentStatus } : u));
    } catch (err: any) {
      alert(t(err.message));
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (userPassword !== userConfirmPassword) {
      alert(t('Passwords do not match.'));
      return;
    }
    if (userPassword.length < 8) {
      alert(t('Password must be at least 8 characters.'));
      return;
    }
    if (!selectedUserId) return;

    try {
      await api.resetUserPassword(selectedUserId, userPassword);
      setIsResetPasswordOpen(false);
      setSelectedUserId(null);
      setUserPassword('');
      setUserConfirmPassword('');
      alert(t('Password reset successfully!'));
    } catch (err: any) {
      alert(t(err.message));
    }
  };

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-950">
        <div className="animate-spin text-red-500"><Activity size={48} /></div>
      </div>
    );
  }

  return (
    <div className={`flex h-screen overflow-hidden bg-slate-950`} dir={isRtl ? 'rtl' : 'ltr'}>
      
      {/* Sidebar Navigation */}
      <aside className={`w-64 border-${isRtl ? 'l' : 'r'} border-slate-800 bg-slate-900 flex flex-col`}>
        <div className="h-16 flex items-center px-6 border-b border-slate-800">
          <ShieldAlert className={`text-red-500 ${isRtl ? 'ml-3' : 'mr-3'}`} size={28} />
          <span className="text-lg font-bold tracking-tight text-slate-100">{t('Speed Alert')}</span>
        </div>
        
        <nav className="flex-1 px-4 py-6 space-y-2 overflow-y-auto">
          <NavItem 
            icon={<LayoutDashboard size={20}/>} 
            label={t('Dashboard')} 
            active={currentView === 'dashboard'} 
            onClick={() => setCurrentView('dashboard')} 
            isRtl={isRtl}
          />
          <NavItem 
            icon={<Users size={20}/>} 
            label={t('Users Management')} 
            active={currentView === 'users'} 
            onClick={() => setCurrentView('users')}
            isRtl={isRtl}
          />
          <NavItem 
            icon={<Settings size={20}/>} 
            label={t('Provider Settings')} 
            active={currentView === 'settings'} 
            onClick={() => setCurrentView('settings')}
            isRtl={isRtl}
          />
        </nav>
        
        <div className="p-4 border-t border-slate-800 space-y-2">
          <button 
            onClick={() => setLanguage(language === 'ar' ? 'en' : 'ar')}
            className={`flex items-center text-sm w-full px-2 py-2 transition-colors ${
              language === 'ar' ? 'text-slate-100 hover:text-white' : 'text-slate-400 hover:text-slate-300'
            }`}
          >
            <Globe size={18} className={isRtl ? 'ml-3' : 'mr-3'} />
            {t('Language')} ({language === 'ar' ? 'عربي' : 'English'})
          </button>
          <button onClick={performLogout} className="flex items-center text-sm text-red-400 hover:text-red-300 transition-colors w-full px-2 py-2">
            <LogOut size={18} className={isRtl ? 'ml-3' : 'mr-3'} />
            {t('Sign Out')}
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col h-full overflow-hidden relative">
        <header className="h-16 border-b border-slate-800 bg-slate-950/50 backdrop-blur-md flex items-center justify-between px-8 z-10">
          <div className="flex items-center bg-slate-900 border border-slate-800 rounded-full px-4 py-1.5 w-96">
            <Search size={16} className={`text-slate-500 ${isRtl ? 'ml-2' : 'mr-2'}`} />
            <input 
              dir={isRtl ? 'rtl' : 'ltr'}
              type="text" 
              placeholder={t('Search driver by ID or License...')} 
              className="bg-transparent border-none outline-none text-sm w-full text-slate-200 placeholder:text-slate-600"
            />
          </div>
          <div className="flex items-center space-x-4">
            <div className="relative">
              <Bell size={20} className="text-slate-400 hover:text-slate-100 cursor-pointer" />
              {isBackendConnected && <span className={`absolute -top-1 ${isRtl ? '-left-1' : '-right-1'} bg-red-500 w-2.5 h-2.5 rounded-full`}></span>}
            </div>
            <div className={`w-8 h-8 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center ${isRtl ? 'mr-4' : ''}`}>
              <span className="text-xs font-medium text-slate-300">{t('AD')}</span>
            </div>
          </div>
        </header>

        <div className="flex-1 overflow-y-auto p-8">
          
          {/* C# Backend Connection Warning */}
          {isBackendConnected === false && (
            <motion.div 
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className="mb-8 bg-red-500/10 border border-red-500/20 rounded-lg p-4 flex items-start"
            >
              <AlertTriangle className={`text-red-500 ${isRtl ? 'ml-3' : 'mr-3'} mt-0.5 whitespace-nowrap`} size={20} />
              <div>
                <h4 className="text-red-500 font-medium text-sm">{t('WebSocket Disconnected / Backend API Offline')}</h4>
                <p className="text-red-500/70 text-xs mt-1">
                  {t('The Web UI is fully functional but unable to establish a WebSocket stream to')} <code className="bg-red-500/20 px-1 py-0.5 rounded">{process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080'}</code>. 
                  {t('Please ensure the Service is running and accepting connections.')}
                </p>
              </div>
            </motion.div>
          )}

          {currentView === 'dashboard' ? (
            <>
              <div className="flex items-center justify-between mb-8">
                <div>
                  <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t('Overview')}</h1>
                  <p className="text-slate-400 text-sm">{t('Real-time telemetry and violation tracking.')}</p>
                </div>
                <div className={`text-${isRtl ? 'left' : 'right'}`}>
                  <div className={`flex items-center justify-end text-xs text-slate-500 mb-1 ${isRtl && 'flex-row-reverse'}`}>
                    <Database size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
                    {t('Database:')} <span className={health?.databaseStatus === 'Healthy' ? 'text-emerald-400 mx-1' : 'text-red-400 mx-1'}>
                      {health?.databaseStatus ? t(health.databaseStatus) : t('Unknown')}
                    </span>
                  </div>
                  <div className={`flex items-center justify-end text-xs text-slate-500 ${isRtl && 'flex-row-reverse'}`}>
                    <Server size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
                    {t('Server Time:')} {health?.serverTime ? new Date(health.serverTime).toLocaleTimeString(language === 'ar' ? 'ar-EG' : 'en-US') : t('Offline')}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
                <MetricCard 
                  title={t('Registered Drivers')} 
                  value={health?.totalUsers?.toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US') || '0'} 
                  icon={<Users size={22} className="text-blue-500" />} 
                  trend={isBackendConnected ? t("Live Socket") : t("Offline")}
                />
                <MetricCard 
                  title={t('Active Sessions')} 
                  value={health?.activeSessions?.toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US') || '0'} 
                  icon={<Activity size={22} className="text-emerald-500" />} 
                  trend={isBackendConnected ? `${health?.totalSessions || 0} ${t('Total Sessions')}` : t("Offline")}
                />
                <MetricCard 
                  title={t('Total Violations')} 
                  value={health?.totalViolations?.toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US') || '0'} 
                  icon={<ShieldAlert size={22} className="text-orange-500" />} 
                  trend={isBackendConnected ? t("Recorded Events") : t("Offline")}
                />
                <MetricCard 
                  title={t('Total Alerts Triggered')} 
                  value={health?.totalAlerts?.toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US') || '0'} 
                  icon={<Bell size={22} className="text-red-500" />} 
                  trend={isBackendConnected ? t("Audio/Haptic Warns") : t("Offline")}
                />
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                
                {/* Live Sessions Table */}
                <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden flex flex-col">
                  <div className="px-6 py-5 border-b border-slate-800 flex justify-between items-center bg-slate-900/50">
                    <h3 className="font-semibold text-slate-100 flex items-center">
                      <div className={`w-2.5 h-2.5 rounded-full ${isRtl ? 'ml-3' : 'mr-3'} ${isBackendConnected ? 'bg-emerald-500 animate-pulse' : 'bg-slate-600'}`}></div>
                      {t('Live Driving Sessions')}
                    </h3>
                  </div>
                  <div className="overflow-x-auto min-h-[200px]">
                    <table className={`w-full text-${isRtl ? 'right' : 'left'} text-sm whitespace-nowrap`}>
                      <thead>
                        <tr className="text-slate-500 bg-slate-950/30">
                          <th className="px-6 py-3 font-medium">{t('Session ID')}</th>
                          <th className="px-6 py-3 font-medium">{t('Started At')}</th>
                          <th className="px-6 py-3 font-medium text-center">{t('Trigger')}</th>
                        </tr>
                      </thead>
                      <tbody className={`divide-y divide-slate-800/50`}>
                        {sessions.length === 0 && (
                          <tr>
                            <td colSpan={3} className="px-6 py-8 text-center text-slate-500">
                              {isBackendConnected ? t("No active driving sessions.") : t("Connect to backend to view sessions.")}
                            </td>
                          </tr>
                        )}
                        {sessions.map((s, idx) => (
                          <motion.tr 
                            initial={{ opacity: 0, y: 10 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ delay: idx * 0.1 }}
                            key={s.id} 
                            className="hover:bg-slate-800/50 transition-colors"
                          >
                            <td className="px-6 py-4 font-medium text-slate-300 font-mono text-xs" dir="ltr">{s.id.split('-')[0]}</td>
                            <td className="px-6 py-4 text-slate-400" dir="ltr">{new Date(s.startedAt).toLocaleString(language === 'ar' ? 'ar-EG' : 'en-US')}</td>
                            <td className="px-6 py-4 text-center">
                              {s.wasAutoStarted ? (
                                <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-500/10 text-blue-400">{t('Auto-Detected')}</span>
                              ) : (
                                <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-500/10 text-slate-400">{t('Manual')}</span>
                              )}
                            </td>
                          </motion.tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>

                {/* Users Roster */}
                <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden flex flex-col">
                  <div className="px-6 py-5 border-b border-slate-800 flex justify-between items-center bg-slate-900/50">
                    <h3 className="font-semibold text-slate-100 flex items-center">
                      <Users size={18} className={`text-blue-500 ${isRtl ? 'ml-2' : 'mr-2'}`} />
                      {t('Driver Roster')}
                    </h3>
                  </div>
                  <div className="overflow-x-auto min-h-[200px]">
                    <table className={`w-full text-${isRtl ? 'right' : 'left'} text-sm whitespace-nowrap`}>
                      <thead>
                        <tr className="text-slate-500 bg-slate-950/30">
                          <th className="px-6 py-3 font-medium">{t('Email')}</th>
                          <th className="px-6 py-3 font-medium">{t('Registered')}</th>
                          <th className="px-6 py-3 font-medium text-center">{t('Status')}</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-800/50">
                        {users.length === 0 && (
                          <tr>
                            <td colSpan={3} className="px-6 py-8 text-center text-slate-500">
                              {isBackendConnected ? t("No users registered.") : t("Connect to backend to view users.")}
                            </td>
                          </tr>
                        )}
                        {users.map((u, idx) => (
                          <motion.tr 
                            initial={{ opacity: 0, y: 10 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ delay: idx * 0.1 }}
                            key={u.id} 
                            className="hover:bg-slate-800/50 transition-colors"
                          >
                            <td className="px-6 py-4 text-slate-300">{u.email}</td>
                            <td className="px-6 py-4 text-slate-400" dir="ltr">{new Date(u.createdAt).toLocaleDateString(language === 'ar' ? 'ar-EG' : 'en-US')}</td>
                            <td className="px-6 py-4 text-center">
                              {u.isActive ? (
                                <span className="inline-flex w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                              ) : (
                                <span className="inline-flex w-2.5 h-2.5 rounded-full bg-red-500"></span>
                              )}
                            </td>
                          </motion.tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>

              </div>
            </>
          ) : currentView === 'settings' ? (
            <div className="max-w-4xl mx-auto mt-4">
              <div className="flex items-center justify-between mb-8">
                <div>
                  <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t('Speed Providers')}</h1>
                  <p className="text-slate-400 text-sm">{t('Configure primary and fallback providers for speed limit checks.')}</p>
                </div>
                <button
                  onClick={saveProviderSettings}
                  disabled={savingSettings}
                  className="flex items-center px-4 py-2 bg-red-600 hover:bg-red-500 text-white rounded-lg text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Save size={16} className={`${isRtl ? 'ml-2' : 'mr-2'}`} />
                  {savingSettings ? t('Saving...') : t('Save Settings')}
                </button>
              </div>

              <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden mb-8">
                <div className="px-6 py-5 border-b border-slate-800 bg-slate-900/50 text-sm font-medium text-slate-300">
                  {t('Select Primary Provider')}
                </div>
                <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-4">
                  {providers.map(p => (
                    <div 
                      key={p.providerKey}
                      onClick={() => handleProviderSelect(p.providerKey)}
                      className={`cursor-pointer rounded-xl border p-4 flex flex-col items-center justify-center text-center transition-all ${
                        p.isSelected 
                          ? 'border-red-500 bg-red-500/10' 
                          : 'border-slate-700 bg-slate-800 hover:border-slate-500'
                      }`}
                    >
                      {p.isSelected && <CheckCircle2 className="text-red-500 mb-2" size={24} />}
                      {!p.isSelected && <div className="w-6 h-6 mb-2 rounded-full border-2 border-slate-600"></div>}
                      <span className="font-medium text-slate-100">{p.displayName}</span>
                      <span className="text-xs text-slate-400 mt-1" dir="ltr">{p.providerKey} {t('API')}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden">
                <div className="px-6 py-5 border-b border-slate-800 bg-slate-900/50 flex justify-between items-center">
                  <span className="text-sm font-medium text-slate-300">{t('Fallback Strategy (Priority Order)')}</span>
                  <span className="text-xs text-slate-500">{t('Drag not implemented, use arrows')}</span>
                </div>
                <div className="p-6">
                  {providers.length > 0 ? (
                    <div className="space-y-3">
                      {providers.sort((a,b) => a.priorityOrder - b.priorityOrder).map((p, idx) => (
                        <div key={p.providerKey} className="flex items-center justify-between p-4 rounded-lg border border-slate-800 bg-slate-950/50">
                          <div className="flex items-center space-x-4">
                            <span className={`text-slate-500 font-mono text-xs w-4 ${isRtl ? 'ml-4' : ''}`}>{idx + 1}.</span>
                            <span className={`font-medium text-slate-300 ${isRtl ? 'ml-4' : ''}`}>{p.displayName}</span>
                            {p.isSelected && <span className="px-2 py-0.5 rounded text-[10px] uppercase font-bold bg-slate-800 text-slate-400">{t('Primary')}</span>}
                          </div>
                          
                          <div className="flex items-center space-x-4">
                            <label className="flex items-center cursor-pointer">
                              <span className={`text-xs text-slate-500 ${isRtl ? 'ml-2' : 'mr-2'}`}>{p.isEnabled ? t('Enabled') : t('Disabled')}</span>
                              <div className="relative">
                                <input 
                                  type="checkbox" 
                                  className="sr-only" 
                                  checked={p.isEnabled}
                                  onChange={() => handleProviderToggle(p.providerKey)}
                                />
                                <div className={`block w-10 h-6 rounded-full transition-colors ${p.isEnabled ? 'bg-red-500' : 'bg-slate-700'}`}></div>
                                <div className={`dot absolute ${isRtl ? 'right-1' : 'left-1'} top-1 bg-white w-4 h-4 rounded-full transition-transform ${p.isEnabled ? (isRtl ? 'transform -translate-x-4' : 'transform translate-x-4') : ''}`}></div>
                              </div>
                            </label>
                            
                            <div className={`flex flex-col space-y-1 ${isRtl ? 'mr-4 border-r pr-4' : 'ml-4 border-l pl-4'} border-slate-800`}>
                              <button 
                                onClick={() => moveProvider(p.providerKey, 'up')}
                                disabled={idx === 0}
                                className="text-slate-500 hover:text-slate-300 disabled:opacity-30"
                              >
                                ▲
                              </button>
                              <button 
                                onClick={() => moveProvider(p.providerKey, 'down')}
                                disabled={idx === providers.length - 1}
                                className="text-slate-500 hover:text-slate-300 disabled:opacity-30"
                              >
                                ▼
                              </button>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-8 text-slate-500 text-sm">{t('No providers found.')}</div>
                  )}
                </div>
              </div>

            </div>
          ) : currentView === 'users' ? (
            <div className="max-w-6xl mx-auto mt-4">
              <div className="flex items-center justify-between mb-8">
                <div>
                  <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t('Users Management')}</h1>
                  <p className="text-slate-400 text-sm">Create, update, and manage mobile and admin users.</p>
                </div>
                <button
                  onClick={() => setIsAddUserOpen(true)}
                  className="flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-sm font-medium transition-colors"
                >
                  <UserPlus size={16} className={`${isRtl ? 'ml-2' : 'mr-2'}`} />
                  {t('Create User')}
                </button>
              </div>

              <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden flex flex-col">
                <div className="overflow-x-auto min-h-[400px]">
                  <table className={`w-full text-${isRtl ? 'right' : 'left'} text-sm whitespace-nowrap`}>
                    <thead>
                      <tr className="text-slate-500 bg-slate-950/30">
                        <th className="px-6 py-3 font-medium">{t('Email')}</th>
                        <th className="px-6 py-3 font-medium text-center">{t('Role')}</th>
                        <th className="px-6 py-3 font-medium text-center">{t('Active')}</th>
                        <th className="px-6 py-3 font-medium">{t('Registered')}</th>
                        <th className="px-6 py-3 font-medium text-center">{t('Actions')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800/50">
                      {users.length === 0 && (
                        <tr>
                          <td colSpan={5} className="px-6 py-8 text-center text-slate-500">
                            {isBackendConnected ? t("No users registered.") : t("Connect to backend to view users.")}
                          </td>
                        </tr>
                      )}
                      {users.map((u, idx) => (
                        <motion.tr 
                          initial={{ opacity: 0, y: 10 }}
                          animate={{ opacity: 1, y: 0 }}
                          transition={{ delay: idx * 0.05 }}
                          key={u.id} 
                          className="hover:bg-slate-800/50 transition-colors"
                        >
                          <td className="px-6 py-4 text-slate-300 font-medium">{u.email}</td>
                          <td className="px-6 py-4 text-center">
                            <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-bold ${
                              (u as any).role === 'Admin' ? 'bg-purple-500/10 text-purple-400' : 'bg-blue-500/10 text-blue-400'
                            }`}>
                              {(u as any).role || 'User'}
                            </span>
                          </td>
                          <td className="px-6 py-4 text-center">
                            <button 
                              onClick={() => handleToggleUserStatus(u.id, u.isActive)}
                              // Prevent locking out all admins randomly, though real app checks role in backend
                              className={`inline-flex items-center justify-center p-1.5 rounded-full transition-colors ${
                                u.isActive ? 'bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20' : 'bg-slate-800 text-slate-500 hover:bg-slate-700 hover:text-slate-300'
                              }`}
                              title={u.isActive ? 'Deactivate' : 'Activate'}
                            >
                              <Power size={14} />
                            </button>
                          </td>
                          <td className="px-6 py-4 text-slate-400" dir="ltr">
                            {new Date(u.createdAt).toLocaleDateString(language === 'ar' ? 'ar-EG' : 'en-US')}
                          </td>
                          <td className="px-6 py-4 text-center flex items-center justify-center space-x-2">
                             <button
                               onClick={() => {
                                 setSelectedUserId(u.id);
                                 setIsResetPasswordOpen(true);
                               }}
                               className={`px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded text-xs transition-colors flex items-center ${isRtl ? 'ml-2' : ''}`}
                             >
                                <Key size={12} className={isRtl ? 'ml-1.5' : 'mr-1.5'} />
                                {t('Reset Password')}
                             </button>
                          </td>
                        </motion.tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          ) : null}
        </div>
      </main>

      {/* Create User Modal */}
      {isAddUserOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <motion.div 
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className={`bg-slate-900 border border-slate-800 rounded-xl p-6 w-full max-w-md shadow-2xl text-${isRtl ? 'right' : 'left'}`}
            dir={isRtl ? 'rtl' : 'ltr'}
          >
            <h2 className="text-xl font-bold text-slate-100 mb-4">{t('Create User')}</h2>
            <form onSubmit={handleCreateUser} className="space-y-4">
               <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">{t('Email')}</label>
                  <input
                    type="email"
                    value={userEmail}
                    onChange={(e) => setUserEmail(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
                    required
                    dir="ltr"
                  />
               </div>
               <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">{t('Password')}</label>
                  <input
                    type="password"
                    value={userPassword}
                    onChange={(e) => setUserPassword(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
                    required
                    dir="ltr"
                  />
               </div>
               <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">{t('Confirm Password')}</label>
                  <input
                    type="password"
                    value={userConfirmPassword}
                    onChange={(e) => setUserConfirmPassword(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
                    required
                    dir="ltr"
                  />
               </div>
               
               <div className="flex justify-end space-x-3 mt-6 pt-4 border-t border-slate-800">
                  <button 
                    type="button"
                    onClick={() => {
                        setIsAddUserOpen(false);
                        setUserEmail('');
                        setUserPassword('');
                        setUserConfirmPassword('');
                    }}
                    className={`px-4 py-2 text-sm font-medium text-slate-400 hover:text-slate-200 ${isRtl ? 'ml-3' : ''}`}
                  >
                    {t('Cancel')}
                  </button>
                  <button 
                    type="submit"
                    className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-sm font-medium"
                  >
                    {t('Save')}
                  </button>
               </div>
            </form>
          </motion.div>
        </div>
      )}

      {/* Reset Password Modal */}
      {isResetPasswordOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <motion.div 
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className={`bg-slate-900 border border-slate-800 rounded-xl p-6 w-full max-w-md shadow-2xl text-${isRtl ? 'right' : 'left'}`}
            dir={isRtl ? 'rtl' : 'ltr'}
          >
            <h2 className="text-xl font-bold text-slate-100 mb-4">{t('Reset Password')}</h2>
            <form onSubmit={handleResetPassword} className="space-y-4">
               <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">{t('Password')}</label>
                  <input
                    type="password"
                    value={userPassword}
                    onChange={(e) => setUserPassword(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
                    required
                    dir="ltr"
                  />
               </div>
               <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">{t('Confirm Password')}</label>
                  <input
                    type="password"
                    value={userConfirmPassword}
                    onChange={(e) => setUserConfirmPassword(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50"
                    required
                    dir="ltr"
                  />
               </div>
               
               <div className="flex justify-end space-x-3 mt-6 pt-4 border-t border-slate-800">
                  <button 
                    type="button"
                    onClick={() => {
                        setIsResetPasswordOpen(false);
                        setSelectedUserId(null);
                        setUserPassword('');
                        setUserConfirmPassword('');
                    }}
                    className={`px-4 py-2 text-sm font-medium text-slate-400 hover:text-slate-200 ${isRtl ? 'ml-3' : ''}`}
                  >
                    {t('Cancel')}
                  </button>
                  <button 
                    type="submit"
                    className="px-4 py-2 bg-red-600 hover:bg-red-500 text-white rounded-lg text-sm font-medium"
                  >
                    {t('Save')}
                  </button>
               </div>
            </form>
          </motion.div>
        </div>
      )}
    </div>
  );
}

function NavItem({ icon, label, active = false, onClick, isRtl }: { icon: React.ReactNode, label: string, active?: boolean, onClick?: () => void, isRtl?: boolean }) {
  return (
    <button 
      onClick={onClick}
      className={`flex items-center w-full px-3 py-2.5 rounded-lg transition-colors text-sm font-medium ${
        active 
          ? 'bg-red-500/10 text-red-500' 
          : 'text-slate-400 hover:bg-slate-800/50 hover:text-slate-200'
      }`}
    >
      <span className={isRtl ? 'ml-3' : 'mr-3'}>{icon}</span>
      {label}
    </button>
  );
}

function MetricCard({ title, value, icon, trend }: { title: string, value: string, icon: React.ReactNode, trend: string }) {
  // ... original MetricCard logic
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 relative overflow-hidden group">
      <div className="absolute top-0 right-0 p-6 opacity-20 transform translate-x-2 -translate-y-2 group-hover:scale-110 transition-transform duration-300">
        {icon}
      </div>
      <h3 className="text-slate-400 text-sm font-medium mb-2">{title}</h3>
      <div className="text-3xl font-bold text-slate-100 tracking-tight mb-2" dir="ltr">{value}</div>
      <span className="text-slate-500 text-xs font-medium">{trend}</span>
    </div>
  );
}
