'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { ShieldAlert, LogIn, Activity, Globe } from 'lucide-react';
import { API_BASE_URL, setAuthToken } from '@/lib/api';
import { useLanguage } from '@/lib/i18n';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const { t, language, setLanguage, isRtl } = useLanguage();

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
        let errCode = 'Invalid Admin Credentials or Backend Offline';
        try {
          const errData = await res.json();
          if (errData.code) {
             errCode = errData.code;
          }
        } catch(e) {}
        throw new Error(t(errCode));
      }

      const data = await res.json();
      if (data.token) {
        setAuthToken(data.token);
        router.push('/');
      } else {
        throw new Error(t('No token received'));
      }
    } catch (err: any) {
      setError(err.message || t('Connection to .NET API failed.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={`min-h-screen flex items-center justify-center bg-slate-950 p-4 relative ${isRtl ? 'rtl' : 'ltr'}`}>
      <div className="absolute top-4 right-4 flex items-center">
        <button 
          onClick={() => setLanguage(language === 'ar' ? 'en' : 'ar')}
          className="flex items-center text-sm text-slate-400 hover:text-slate-100 transition-colors"
        >
          <Globe size={16} className={isRtl ? 'ml-2' : 'mr-2'} />
          {language === 'ar' ? 'English' : 'عربي'}
        </button>
      </div>
      <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-2xl p-8 shadow-2xl">
        <div className="flex flex-col items-center mb-8">
          <ShieldAlert className="text-red-500 mb-4" size={48} />
          <h1 className="text-2xl font-bold text-slate-100">{t('Speed Alert Admin')}</h1>
          <p className="text-slate-400 text-sm mt-2">{t('Sign in to access telemetry dashboard')}</p>
        </div>

        {error && (
          <div className="mb-6 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm font-medium text-center">
            {error}
          </div>
        )}

        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">{t('Admin Email')}</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50 transition-colors"
              placeholder="admin@speedalert.com"
              required
              dir="ltr"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">{t('Password')}</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-lg px-4 py-2.5 text-slate-200 outline-none focus:border-red-500/50 transition-colors"
              placeholder="••••••••"
              required
              dir="ltr"
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-red-600 hover:bg-red-700 text-white font-medium py-2.5 rounded-lg transition-colors flex items-center justify-center mt-6 disabled:opacity-50"
          >
            {loading ? <Activity className="animate-spin" size={20} /> : <><LogIn size={18} className={isRtl ? 'ml-2' : 'mr-2'} /> {t('Sign In')}</>}
          </button>
        </form>
        
        <div className="mt-6 text-center text-xs text-slate-500">
          {t('Backend API expects')} <code className="bg-slate-800 px-1 py-0.5 rounded text-slate-400" dir="ltr">http://localhost:8080</code>
        </div>
      </div>
    </div>
  );
}
