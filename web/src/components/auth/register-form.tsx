'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRegister } from '@/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import type { ApiError } from '@/types';

export function RegisterForm() {
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const register = useRegister();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    register.mutate({ email, username, password });
  };

  const error = register.error as ApiError | null;
  const fieldErrors = error?.errors;

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-2xl font-bold tracking-tight">Create an account</CardTitle>
        <CardDescription>Enter your details to get started</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && !fieldErrors && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {error.message || 'Registration failed'}
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              disabled={register.isPending}
            />
            {fieldErrors?.Email && (
              <p className="text-sm text-destructive">{fieldErrors.Email[0]}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="username">Username</Label>
            <Input
              id="username"
              type="text"
              placeholder="johndoe"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              autoComplete="username"
              disabled={register.isPending}
            />
            {fieldErrors?.Username && (
              <p className="text-sm text-destructive">{fieldErrors.Username[0]}</p>
            )}
            <p className="text-xs text-muted-foreground">3-50 characters, letters, numbers, and underscores only</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="new-password"
              disabled={register.isPending}
            />
            {fieldErrors?.Password && (
              <p className="text-sm text-destructive">{fieldErrors.Password[0]}</p>
            )}
            <p className="text-xs text-muted-foreground">
              8+ characters with uppercase, lowercase, and number
            </p>
          </div>

          <Button type="submit" className="w-full" disabled={register.isPending}>
            {register.isPending ? 'Creating account...' : 'Create account'}
          </Button>

          <p className="text-center text-sm text-muted-foreground">
            Already have an account?{' '}
            <Link href="/login" className="font-medium text-primary hover:underline">
              Sign in
            </Link>
          </p>
        </form>
      </CardContent>
    </Card>
  );
}
