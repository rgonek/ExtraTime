'use client';

import { type LucideIcon, ChevronLeft } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface PageHeaderAction {
  label: string;
  onClick?: () => void;
  href?: string;
  icon?: LucideIcon;
  variant?: 'default' | 'secondary' | 'accent' | 'outline' | 'ghost';
}

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  icon?: LucideIcon;
  gradient?: boolean;
  backHref?: string;
  onBack?: () => void;
  actions?: PageHeaderAction[];
  children?: React.ReactNode;
  className?: string;
}

export function PageHeader({
  title,
  subtitle,
  icon: Icon,
  gradient = false,
  backHref,
  onBack,
  actions,
  children,
  className,
}: PageHeaderProps) {
  const router = useRouter();

  const handleBack = () => {
    if (onBack) {
      onBack();
    } else if (backHref) {
      router.push(backHref);
    } else {
      router.back();
    }
  };

  const showBackButton = backHref || onBack;

  return (
    <div className={cn('space-y-1 mb-6', className)}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-4 min-w-0">
          {/* Back button */}
          {showBackButton && (
            <Button
              variant="ghost"
              size="icon"
              onClick={handleBack}
              className="shrink-0 -ml-2 mt-0.5 text-muted-foreground hover:text-foreground"
            >
              <ChevronLeft className="h-5 w-5" />
            </Button>
          )}

          {/* Icon */}
          {Icon && (
            <div className="shrink-0 flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br from-primary to-secondary shadow-md shadow-primary/20">
              <Icon className="h-6 w-6 text-white" />
            </div>
          )}

          {/* Title & Subtitle */}
          <div className="min-w-0">
            <h1
              className={cn(
                'text-2xl sm:text-3xl font-bold tracking-tight',
                gradient ? 'text-gradient-primary' : 'text-foreground'
              )}
            >
              {title}
            </h1>
            {subtitle && (
              <p className="text-muted-foreground mt-1">{subtitle}</p>
            )}
          </div>
        </div>

        {/* Actions */}
        {actions && actions.length > 0 && (
          <div className="flex items-center gap-2 shrink-0">
            {actions.map((action, index) => {
              const ActionIcon = action.icon;
              return (
                <Button
                  key={index}
                  variant={action.variant ?? (index === actions.length - 1 ? 'default' : 'outline')}
                  onClick={action.onClick}
                  asChild={!!action.href}
                >
                  {action.href ? (
                    <a href={action.href}>
                      {ActionIcon && <ActionIcon className="h-4 w-4 mr-2" />}
                      {action.label}
                    </a>
                  ) : (
                    <>
                      {ActionIcon && <ActionIcon className="h-4 w-4 mr-2" />}
                      {action.label}
                    </>
                  )}
                </Button>
              );
            })}
          </div>
        )}
      </div>

      {/* Additional content */}
      {children}
    </div>
  );
}

interface PageHeaderSkeletonProps {
  showIcon?: boolean;
  showSubtitle?: boolean;
  className?: string;
}

export function PageHeaderSkeleton({
  showIcon = false,
  showSubtitle = true,
  className,
}: PageHeaderSkeletonProps) {
  return (
    <div className={cn('space-y-1 mb-6', className)}>
      <div className="flex items-start gap-4">
        {showIcon && (
          <div className="shrink-0 w-12 h-12 rounded-xl bg-muted animate-pulse" />
        )}
        <div className="space-y-2">
          <div className="h-8 w-48 bg-muted rounded-lg animate-pulse" />
          {showSubtitle && (
            <div className="h-5 w-64 bg-muted rounded animate-pulse" />
          )}
        </div>
      </div>
    </div>
  );
}
