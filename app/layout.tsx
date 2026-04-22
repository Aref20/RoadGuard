import type { Metadata } from 'next';
import { Inter, Cairo } from 'next/font/google';
import './globals.css';
import { LanguageProvider } from '@/lib/i18n';

const inter = Inter({ subsets: ['latin'], variable: '--font-sans' });
const cairo = Cairo({ subsets: ['arabic'], variable: '--font-cairo' });

export const metadata: Metadata = {
  title: 'Speed Alert | Admin Dashboard',
  description: 'Web administration panel for the Speed Alert driving assistant system.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="ar" dir="rtl" className={`dark ${inter.variable} ${cairo.variable}`}>
      <body className="bg-slate-950 text-slate-50 min-h-screen font-sans antialiased selection:bg-red-500/30 suppressHydrationWarning" style={{ fontFamily: 'var(--font-cairo), var(--font-sans), sans-serif' }}>
        <LanguageProvider>
          {children}
        </LanguageProvider>
      </body>
    </html>
  );
}
