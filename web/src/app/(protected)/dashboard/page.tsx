'use client';

import Link from 'next/link';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { useAuthStore } from '@/stores/auth-store';
import { useLogout } from '@/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

function DashboardContent() {
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();

  return (
    <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
      <div className="mx-auto max-w-4xl space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-primary">ExtraTime</h1>
            <p className="text-muted-foreground">Welcome back, {user?.username}!</p>
          </div>
          <Button variant="outline" onClick={() => logout.mutate()} disabled={logout.isPending}>
            {logout.isPending ? 'Signing out...' : 'Sign out'}
          </Button>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <Card>
            <CardHeader>
              <CardTitle>Your Profile</CardTitle>
              <CardDescription>Account information</CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              <div>
                <p className="text-sm text-muted-foreground">Username</p>
                <p className="font-medium">{user?.username}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Email</p>
                <p className="font-medium">{user?.email}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Role</p>
                <p className="font-medium">{user?.role}</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Upcoming Matches</CardTitle>
              <CardDescription>Place your bets</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">Coming soon...</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Your Leagues</CardTitle>
              <CardDescription>Compete with friends</CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild className="w-full">
                <Link href="/leagues">View Leagues</Link>
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  return (
    <ProtectedRoute>
      <DashboardContent />
    </ProtectedRoute>
  );
}
