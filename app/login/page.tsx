'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Activity, Globe, LogIn, ShieldAlert } from 'lucide-react';
import { API_ORIGIN, ApiError, api, setAuthToken } from '@/lib/api';
import { useLanguage } from '@/lib/i18n';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const { t, language, setLanguage, isRtl } = useLanguage();

  const handleLogin = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await api.login(email.trim(), password);
      if (response.role !== 'Admin') {
        throw new Error(t('AUTH_FORBIDDEN'));
      }

      setAuthToken(response.token);
      router.push('/');
    } catch (error) {
      if (error instanceof ApiError) {
        setError(t(error.code || error.message));
      } else {
        setError(error instanceof Error ? error.message : t('Connection to .NET API failed.'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-slate-950 p-4" dir={isRtl ? 'rtl' : 'ltr'}>
      <div className={`absolute top-4 ${isRtl ? 'left-4' : 'right-4'} flex items-center`}>
        <button
          onClick={() => setLanguage(language === 'ar' ? 'en' : 'ar')}
          className="flex items-center text-sm text-slate-400 transition-colors hover:text-slate-100"
        >
          <Globe size={16} className={isRtl ? 'ml-2' : 'mr-2'} />
          {language === 'ar' ? 'English' : 'العربية'}
        </button>
      </div>

      <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-2xl">
        <div className="mb-8 flex flex-col items-center">
          <ShieldAlert className="mb-4 text-red-500" size={48} />
          <h1 className="text-2xl font-bold text-slate-100">{t('Speed Alert Admin')}</h1>
          <p className="mt-2 text-sm text-slate-400">{t('Sign in to access telemetry dashboard')}</p>
        </div>

        {error ? (
          <div className="mb-6 rounded-lg border border-red-500/20 bg-red-500/10 p-3 text-center text-sm font-medium text-red-300">
            {error}
          </div>
        ) : null}

        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Admin Email')}</label>
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none transition-colors focus:border-red-500/50"
              placeholder="admin@roadguard.com"
              required
              dir="ltr"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-400">{t('Password')}</label>
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-800 bg-slate-950 px-4 py-2.5 text-slate-200 outline-none transition-colors focus:border-red-500/50"
              placeholder="••••••••"
              required
              dir="ltr"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="mt-6 flex w-full items-center justify-center rounded-lg bg-red-600 py-2.5 font-medium text-white transition-colors hover:bg-red-700 disabled:opacity-50"
          >
            {loading ? (
              <Activity className="animate-spin" size={20} />
            ) : (
              <>
                <LogIn size={18} className={isRtl ? 'ml-2' : 'mr-2'} />
                {t('Sign In')}
              </>
            )}
          </button>
        </form>

        <div className="mt-6 text-center text-xs text-slate-500">
          {t('Backend API expects')}{' '}
          <code className="rounded bg-slate-800 px-1 py-0.5 text-slate-400" dir="ltr">
            {API_ORIGIN}
          </code>
        </div>
      </div>
    </div>
  );
}
