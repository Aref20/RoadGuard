'use client';

import { ReactNode } from 'react';

type UserModalProps = {
  title: string;
  submitLabel: string;
  cancelLabel: string;
  isRtl: boolean;
  onClose: () => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  children: ReactNode;
};

export function UserModal({
  title,
  submitLabel,
  cancelLabel,
  isRtl,
  onClose,
  onSubmit,
  children,
}: UserModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
      <div
        className={`w-full max-w-md rounded-xl border border-slate-800 bg-slate-900 p-6 shadow-2xl ${isRtl ? 'text-right' : 'text-left'}`}
        dir={isRtl ? 'rtl' : 'ltr'}
      >
        <h2 className="mb-4 text-xl font-bold text-slate-100">{title}</h2>
        <form onSubmit={onSubmit} className="space-y-4">
          {children}
          <div className="mt-6 flex justify-end space-x-3 border-t border-slate-800 pt-4">
            <button
              type="button"
              onClick={onClose}
              className={`px-4 py-2 text-sm font-medium text-slate-400 hover:text-slate-200 ${isRtl ? 'ml-3' : ''}`}
            >
              {cancelLabel}
            </button>
            <button
              type="submit"
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-500"
            >
              {submitLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
