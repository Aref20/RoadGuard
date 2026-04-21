import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';

const inter = Inter({ subsets: ['latin'], variable: '--font-sans' });

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
    <html lang="en" className={`dark ${inter.variable}`}>
      <body className="bg-slate-950 text-slate-50 min-h-screen font-sans antialiased selection:bg-red-500/30 suppressHydrationWarning">
        {children}
      </body>
    </html>
  );
}
