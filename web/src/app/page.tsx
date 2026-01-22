'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuthStore } from '@/stores/auth-store';
import { Button } from '@/components/ui/button';

export default function Home() {
  const router = useRouter();
  const { isAuthenticated, isLoading, setLoading } = useAuthStore();

  useEffect(() => {
    // Mark loading as complete after hydration
    setLoading(false);
  }, [setLoading]);

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      router.push('/dashboard');
    }
  }, [isAuthenticated, isLoading, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col bg-gradient-to-br from-background to-muted">
      <header className="container mx-auto flex items-center justify-between p-4">
        <h1 className="text-2xl font-bold text-primary">ExtraTime</h1>
        <div className="flex gap-2">
          <Button variant="ghost" asChild>
            <Link href="/login">Sign in</Link>
          </Button>
          <Button asChild>
            <Link href="/register">Get started</Link>
          </Button>
        </div>
      </header>

      <main className="container mx-auto flex flex-1 flex-col items-center justify-center px-4 text-center">
        <h2 className="mb-4 text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl">
          Bet on Football
          <br />
          <span className="text-primary">With Friends</span>
        </h2>
        <p className="mb-8 max-w-2xl text-lg text-muted-foreground">
          Create leagues, predict match scores, and compete for bragging rights.
          No real money involved - just pure football fun!
        </p>
        <div className="flex flex-col gap-4 sm:flex-row">
          <Button size="lg" asChild>
            <Link href="/register">Create your account</Link>
          </Button>
          <Button size="lg" variant="outline" asChild>
            <Link href="/login">Sign in</Link>
          </Button>
        </div>
      </main>

      <footer className="container mx-auto p-4 text-center text-sm text-muted-foreground">
        <p>Built for football fans who love friendly competition</p>
      </footer>
    </div>
  );
}
