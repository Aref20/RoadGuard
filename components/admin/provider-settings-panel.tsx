'use client';

import { ArrowDown, ArrowUp, CheckCircle2, LoaderCircle, Save } from 'lucide-react';
import { ProviderConfig } from '@/lib/api';

type ProviderSettingsPanelProps = {
  providers: ProviderConfig[];
  isRtl: boolean;
  loadingProviders: boolean;
  savingProviders: boolean;
  onToggleProvider: (providerKey: string) => void;
  onSelectProvider: (providerKey: string) => void;
  onMoveProvider: (providerKey: string, direction: 'up' | 'down') => void;
  onSave: () => void;
  t: (key: string) => string;
};

export function ProviderSettingsPanel({
  providers,
  isRtl,
  loadingProviders,
  savingProviders,
  onToggleProvider,
  onSelectProvider,
  onMoveProvider,
  onSave,
  t,
}: ProviderSettingsPanelProps) {
  const sortedProviders = [...providers].sort((left, right) => left.priorityOrder - right.priorityOrder);

  return (
    <div className="mx-auto mt-4 max-w-5xl">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="mb-1 text-3xl font-bold tracking-tight text-white">{t('Speed Providers')}</h1>
          <p className="text-sm text-slate-400">{t('Configure primary and fallback providers for speed limit checks.')}</p>
        </div>
        <button
          onClick={onSave}
          disabled={savingProviders || loadingProviders}
          className="flex items-center rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Save size={16} className={isRtl ? 'ml-2' : 'mr-2'} />
          {savingProviders ? t('Saving...') : t('Save Settings')}
        </button>
      </div>

      {loadingProviders ? (
        <div className="flex items-center justify-center rounded-xl border border-slate-800 bg-slate-900 p-10 text-slate-400">
          <LoaderCircle className="animate-spin" size={20} />
        </div>
      ) : (
        <>
          <div className="mb-8 rounded-xl border border-slate-800 bg-slate-900">
            <div className="border-b border-slate-800 px-6 py-5 text-sm font-medium text-slate-300">
              {t('Select Primary Provider')}
            </div>
            <div className="grid grid-cols-1 gap-4 p-6 md:grid-cols-3">
              {sortedProviders.map((provider) => (
                <button
                  type="button"
                  key={provider.providerKey}
                  onClick={() => onSelectProvider(provider.providerKey)}
                  className={`rounded-xl border p-4 text-center transition-all ${
                    provider.isSelected ? 'border-red-500 bg-red-500/10' : 'border-slate-700 bg-slate-800 hover:border-slate-500'
                  }`}
                >
                  <div className="mb-2 flex justify-center">
                    {provider.isSelected ? (
                      <CheckCircle2 className="text-red-500" size={24} />
                    ) : (
                      <div className="h-6 w-6 rounded-full border-2 border-slate-600" />
                    )}
                  </div>
                  <span className="block font-medium text-slate-100">{provider.displayName}</span>
                  <span className="mt-1 block text-xs text-slate-400" dir="ltr">
                    {provider.providerKey}
                  </span>
                  <span
                    className={`mt-2 inline-flex rounded px-2 py-0.5 text-[10px] font-bold ${
                      provider.isConfigured ? 'bg-emerald-500/10 text-emerald-400' : 'bg-amber-500/10 text-amber-300'
                    }`}
                  >
                    {provider.isConfigured ? t('Configured') : t('Not Configured')}
                  </span>
                </button>
              ))}
            </div>
          </div>

          <div className="rounded-xl border border-slate-800 bg-slate-900">
            <div className="flex items-center justify-between border-b border-slate-800 px-6 py-5">
              <span className="text-sm font-medium text-slate-300">{t('Fallback Strategy (Priority Order)')}</span>
              <span className="text-xs text-slate-500">{t('Use the arrows to change fallback priority.')}</span>
            </div>
            <div className="space-y-3 p-6">
              {sortedProviders.map((provider, index) => (
                <div key={provider.providerKey} className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
                  <div className="flex items-center justify-between gap-6">
                    <div className="flex items-center gap-4">
                      <span className="w-4 font-mono text-xs text-slate-500">{index + 1}.</span>
                      <div>
                        <div className="font-medium text-slate-300">{provider.displayName}</div>
                        <div className="text-xs text-slate-500" dir="ltr">
                          {provider.providerKey} • {provider.healthStatus}
                        </div>
                        {provider.lastFailureReason ? (
                          <div className="mt-1 text-xs text-amber-300">{provider.lastFailureReason}</div>
                        ) : null}
                      </div>
                    </div>

                    <div className="flex items-center gap-4">
                      <label className="flex cursor-pointer items-center">
                        <span className={`text-xs text-slate-500 ${isRtl ? 'ml-2' : 'mr-2'}`}>
                          {provider.isEnabled ? t('Enabled') : t('Disabled')}
                        </span>
                        <div className="relative">
                          <input
                            type="checkbox"
                            className="sr-only"
                            checked={provider.isEnabled}
                            onChange={() => onToggleProvider(provider.providerKey)}
                          />
                          <div className={`block h-6 w-10 rounded-full transition-colors ${provider.isEnabled ? 'bg-red-500' : 'bg-slate-700'}`} />
                          <div
                            className={`absolute top-1 h-4 w-4 rounded-full bg-white transition-transform ${
                              isRtl ? 'right-1' : 'left-1'
                            } ${provider.isEnabled ? (isRtl ? '-translate-x-4 transform' : 'translate-x-4 transform') : ''}`}
                          />
                        </div>
                      </label>

                      <div className={`flex flex-col ${isRtl ? 'border-r pr-4' : 'border-l pl-4'} border-slate-800`}>
                        <button
                          type="button"
                          onClick={() => onMoveProvider(provider.providerKey, 'up')}
                          disabled={index === 0}
                          className="text-slate-500 transition-colors hover:text-slate-300 disabled:opacity-30"
                        >
                          <ArrowUp size={14} />
                        </button>
                        <button
                          type="button"
                          onClick={() => onMoveProvider(provider.providerKey, 'down')}
                          disabled={index === sortedProviders.length - 1}
                          className="text-slate-500 transition-colors hover:text-slate-300 disabled:opacity-30"
                        >
                          <ArrowDown size={14} />
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
