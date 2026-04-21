'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { motion } from 'motion/react';
import { 
  Car, Activity, ShieldAlert, Users, Database, 
  Server, MapPin, AlertTriangle, Settings, Bell, 
  Search, LayoutDashboard, History, LogOut
} from 'lucide-react';
import { api, getAuthToken, removeAuthToken } from '@/lib/api';

type SystemHealth = {
  totalUsers: number;
  totalSessions: number;
  databaseStatus: string;
  serverTime: string;
};

type User = {
  id: string;
  email: string;
  isActive: boolean;
  createdAt: string;
};

type Session = {
  id: string;
  userId: string;
  startedAt: string;
  wasAutoStarted: boolean;
};

export default function AdminDashboard() {
  const router = useRouter();
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [isBackendConnected, setIsBackendConnected] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);

  const performLogout = () => {
    removeAuthToken();
    router.push('/login');
  };

  useEffect(() => {
    // Auth Check
    if (!getAuthToken()) {
      router.push('/login');
      return;
    }

    const fetchData = async () => {
      try {
        const [healthData, usersData, sessionsData] = await Promise.all([
          api.getHealth(),
          api.getUsers(),
          api.getSessions()
        ]);
        
        setHealth({
           totalUsers: healthData.totalUsers || healthData.TotalUsers,
           totalSessions: healthData.totalSessions || healthData.TotalSessions,
           databaseStatus: healthData.databaseStatus || healthData.DatabaseStatus,
           serverTime: healthData.serverTime || healthData.ServerTime
        });
        setUsers(usersData);
        setSessions(sessionsData);
        setIsBackendConnected(true);
      } catch (err) {
        setIsBackendConnected(false);
      } finally {
        setLoading(false);
      }
    };
    
    fetchData();
    const interval = setInterval(fetchData, 10000); // Poll every 10s
    return () => clearInterval(interval);
  }, [router]);

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-950">
        <div className="animate-spin text-red-500"><Activity size={48} /></div>
      </div>
    );
  }

  return (
    <div className="flex h-screen overflow-hidden bg-slate-950">
      
      {/* Sidebar Navigation */}
      <aside className="w-64 border-r border-slate-800 bg-slate-900 flex flex-col">
        <div className="h-16 flex items-center px-6 border-b border-slate-800">
          <ShieldAlert className="text-red-500 mr-3" size={28} />
          <span className="text-lg font-bold tracking-tight text-slate-100">Speed Alert</span>
        </div>
        
        <nav className="flex-1 px-4 py-6 space-y-2 overflow-y-auto">
          <NavItem icon={<LayoutDashboard size={20}/>} label="Dashboard" active />
          <NavItem icon={<Users size={20}/>} label="Users & Drivers" />
          <NavItem icon={<Car size={20}/>} label="Live Map" />
          <NavItem icon={<History size={20}/>} label="Violation Log" />
          <NavItem icon={<Settings size={20}/>} label="System Settings" />
        </nav>
        
        <div className="p-4 border-t border-slate-800">
          <button onClick={performLogout} className="flex items-center text-sm text-red-400 hover:text-red-300 transition-colors w-full px-2 py-2">
            <LogOut size={18} className="mr-3" />
            Sign Out
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col h-full overflow-hidden relative">
        <header className="h-16 border-b border-slate-800 bg-slate-950/50 backdrop-blur-md flex items-center justify-between px-8 z-10">
          <div className="flex items-center bg-slate-900 border border-slate-800 rounded-full px-4 py-1.5 w-96">
            <Search size={16} className="text-slate-500 mr-2" />
            <input 
              type="text" 
              placeholder="Search driver by ID or License..." 
              className="bg-transparent border-none outline-none text-sm w-full text-slate-200 placeholder:text-slate-600"
            />
          </div>
          <div className="flex items-center space-x-4">
            <div className="relative">
              <Bell size={20} className="text-slate-400 hover:text-slate-100 cursor-pointer" />
              {isBackendConnected && <span className="absolute -top-1 -right-1 bg-red-500 w-2.5 h-2.5 rounded-full"></span>}
            </div>
            <div className="w-8 h-8 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center">
              <span className="text-xs font-medium text-slate-300">AD</span>
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
              <AlertTriangle className="text-red-500 mr-3 mt-0.5 whitespace-nowrap" size={20} />
              <div>
                <h4 className="text-red-500 font-medium text-sm">Backend API Offline</h4>
                <p className="text-red-500/70 text-xs mt-1">
                  The Web UI is fully functional and attempting to fetch from <code className="bg-red-500/20 px-1 py-0.5 rounded">http://localhost:8080/api</code>. 
                  However, the C# .NET Backend and PostgreSQL databases are unavailable. Please export the project and run <code>docker-compose up</code> to see live data.
                </p>
              </div>
            </motion.div>
          )}

          <div className="flex items-center justify-between mb-8">
            <div>
              <h1 className="text-3xl font-bold tracking-tight text-white mb-1">Overview</h1>
              <p className="text-slate-400 text-sm">Real-time telemetry and violation tracking.</p>
            </div>
            <div className="text-right">
              <div className="flex items-center justify-end text-xs text-slate-500 mb-1">
                <Database size={12} className="mr-1.5" />
                Database: <span className={health?.databaseStatus === 'Healthy' ? 'text-emerald-400 ml-1' : 'text-red-400 ml-1'}>
                  {health?.databaseStatus || 'Unknown'}
                </span>
              </div>
              <div className="flex items-center justify-end text-xs text-slate-500">
                <Server size={12} className="mr-1.5" />
                Server Time: {health?.serverTime ? new Date(health.serverTime).toLocaleTimeString() : 'Offline'}
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            <MetricCard 
              title="Registered Drivers" 
              value={health?.totalUsers?.toLocaleString() || '0'} 
              icon={<Users size={22} className="text-blue-500" />} 
              trend={isBackendConnected ? "Live Query" : "Offline"}
            />
            <MetricCard 
              title="Active Sessions" 
              value={health?.totalSessions?.toString() || '0'} 
              icon={<Activity size={22} className="text-emerald-500" />} 
              trend={isBackendConnected ? "Tracking..." : "Offline"}
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            
            {/* Live Sessions Table */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden flex flex-col">
              <div className="px-6 py-5 border-b border-slate-800 flex justify-between items-center bg-slate-900/50">
                <h3 className="font-semibold text-slate-100 flex items-center">
                  <div className={`w-2.5 h-2.5 rounded-full mr-3 ${isBackendConnected ? 'bg-emerald-500 animate-pulse' : 'bg-slate-600'}`}></div>
                  Live Driving Sessions
                </h3>
              </div>
              <div className="overflow-x-auto min-h-[200px]">
                <table className="w-full text-left text-sm whitespace-nowrap">
                  <thead>
                    <tr className="text-slate-500 bg-slate-950/30">
                      <th className="px-6 py-3 font-medium">Session ID</th>
                      <th className="px-6 py-3 font-medium">Started At</th>
                      <th className="px-6 py-3 font-medium text-center">Trigger</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/50">
                    {sessions.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-6 py-8 text-center text-slate-500">
                          {isBackendConnected ? "No active driving sessions." : "Connect to backend to view sessions."}
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
                        <td className="px-6 py-4 font-medium text-slate-300 font-mono text-xs">{s.id.split('-')[0]}</td>
                        <td className="px-6 py-4 text-slate-400">{new Date(s.startedAt).toLocaleString()}</td>
                        <td className="px-6 py-4 text-center">
                          {s.wasAutoStarted ? (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-500/10 text-blue-400">Auto-Detected</span>
                          ) : (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-500/10 text-slate-400">Manual</span>
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
                  <Users size={18} className="text-blue-500 mr-2" />
                  Driver Roster
                </h3>
              </div>
              <div className="overflow-x-auto min-h-[200px]">
                <table className="w-full text-left text-sm whitespace-nowrap">
                  <thead>
                    <tr className="text-slate-500 bg-slate-950/30">
                      <th className="px-6 py-3 font-medium">Email</th>
                      <th className="px-6 py-3 font-medium">Registered</th>
                      <th className="px-6 py-3 font-medium text-center">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/50">
                    {users.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-6 py-8 text-center text-slate-500">
                          {isBackendConnected ? "No users registered." : "Connect to backend to view users."}
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
                        <td className="px-6 py-4 text-slate-400">{new Date(u.createdAt).toLocaleDateString()}</td>
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
        </div>
      </main>
    </div>
  );
}

function NavItem({ icon, label, active = false }: { icon: React.ReactNode, label: string, active?: boolean }) {
  return (
    <button 
      className={`flex items-center w-full px-3 py-2.5 rounded-lg transition-colors text-sm font-medium ${
        active 
          ? 'bg-red-500/10 text-red-500' 
          : 'text-slate-400 hover:bg-slate-800/50 hover:text-slate-200'
      }`}
    >
      <span className="mr-3">{icon}</span>
      {label}
    </button>
  );
}

function MetricCard({ title, value, icon, trend }: { title: string, value: string, icon: React.ReactNode, trend: string }) {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 relative overflow-hidden group">
      <div className="absolute top-0 right-0 p-6 opacity-20 transform translate-x-2 -translate-y-2 group-hover:scale-110 transition-transform duration-300">
        {icon}
      </div>
      <h3 className="text-slate-400 text-sm font-medium mb-2">{title}</h3>
      <div className="text-3xl font-bold text-slate-100 tracking-tight mb-2">{value}</div>
      <span className="text-slate-500 text-xs font-medium">{trend}</span>
    </div>
  );
}
