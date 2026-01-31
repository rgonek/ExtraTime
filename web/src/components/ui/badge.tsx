import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center justify-center rounded-[var(--radius-xl)] border border-transparent px-2.5 py-0.5 text-xs font-medium w-fit whitespace-nowrap shrink-0 [&>svg]:size-3 gap-1 [&>svg]:pointer-events-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive transition-all duration-200 overflow-hidden",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground [a&]:hover:bg-primary/90",
        secondary:
          "bg-secondary text-secondary-foreground [a&]:hover:bg-secondary/90",
        accent:
          "bg-accent text-accent-foreground [a&]:hover:bg-accent/90",
        destructive:
          "bg-destructive text-white [a&]:hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:focus-visible:ring-destructive/40 dark:bg-destructive/80",
        outline:
          "border-border text-foreground [a&]:hover:bg-muted",
        ghost: "bg-muted/50 text-muted-foreground [a&]:hover:bg-muted",
        link: "text-primary underline-offset-4 [a&]:hover:underline",
        // Semantic variants
        success:
          "bg-success/15 text-success border-success/20 dark:bg-success/20",
        warning:
          "bg-warning/15 text-warning border-warning/20 dark:bg-warning/20 dark:text-warning",
        info:
          "bg-info/15 text-info border-info/20 dark:bg-info/20",
        // Sports/Gamification variants
        live:
          "bg-destructive text-white animate-pulse shadow-sm shadow-destructive/50 dark:bg-destructive dark:shadow-destructive/60",
        points:
          "bg-primary/15 text-primary font-bold border-primary/20 dark:bg-primary/20",
        streak:
          "bg-accent/15 text-accent font-bold border-accent/20 dark:bg-accent/20",
        rank:
          "bg-secondary/15 text-secondary font-semibold border-secondary/20 dark:bg-secondary/20",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot : "span"

  return (
    <Comp
      data-slot="badge"
      data-variant={variant}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }
