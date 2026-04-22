'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { ShieldAlert, LogIn, Activity } from 'lucide-react';
import { API_BASE_URL, setAuthToken } from '@/lib/api';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const res = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      if (!res.ok) {
        throw new Error('Invalid Admin Credentials or Backend Offline');
      }

      const data = await res.json();
      if (data.token) {
        setAuthToken(data.token);
        router.push('/');
      } else {
        throw new Error('No token received');
      }
    } catch (err: any) {
      setError(err.message || 'Connection to .NET API failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-950 p-4">
      <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-2xl p-8 shadow-2xl">
        <div className="flex flex-col items-center mb-8">
          <ShieldAlert className="text-red-500 mb-4" size={48} />
          <h1 className="text-2xl font-bold text-slate-100">Speed Alert Admin</h1>
          <p className="text-slate-400 text-sm mt-2">Sign in to access telemetry dashboard</p>
        </div>

        {error && (
          <div className="mb-6 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm font-medium text-center">
            {error}
          </div>
        )}

        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">Admin Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50 transition-colors"
              placeholder="admin@speedalert.com"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50 transition-colors"
              placeholder="••••••••"
              required
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-red-600 hover:bg-red-700 text-white font-medium py-2.5 rounded-lg transition-colors flex items-center justify-center mt-6 disabled:opacity-50"
          >
            {loading ? <Activity className="animate-spin" size={20} /> : <><LogIn size={18} className="mr-2" /> Sign In</>}
          </button>
        </form>
        
        <div className="mt-6 text-center text-xs text-slate-500">
          Connected API endpoint: <code className="bg-slate-800 px-1 py-0.5 rounded text-slate-400">{API_BASE_URL}</code>
        </div>
      </div>
    </div>
  );
}
