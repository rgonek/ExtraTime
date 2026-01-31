/**
 * ExtraTime Design System Tokens
 *
 * This file documents all design tokens used throughout the application.
 * Tokens are defined as CSS variables in globals.css and mapped here for reference.
 *
 * Design Direction: Vibrant Sports with Light/Dark themes
 * Font: Space Grotesk
 */

// ============ COLOR TOKENS ============

/**
 * Core semantic colors - use these for consistent theming
 */
export const colors = {
  // Base colors
  background: "var(--background)",
  foreground: "var(--foreground)",

  // Card surfaces
  card: "var(--card)",
  cardForeground: "var(--card-foreground)",

  // Popover surfaces
  popover: "var(--popover)",
  popoverForeground: "var(--popover-foreground)",

  // Primary - Emerald Green (main brand color)
  primary: "var(--primary)",
  primaryForeground: "var(--primary-foreground)",
  primaryHover: "var(--primary-hover)",
  primaryLight: "var(--primary-light)",

  // Secondary - Blue (complementary brand color)
  secondary: "var(--secondary)",
  secondaryForeground: "var(--secondary-foreground)",
  secondaryHover: "var(--secondary-hover)",
  secondaryLight: "var(--secondary-light)",

  // Accent - Amber (highlights, gamification)
  accent: "var(--accent)",
  accentForeground: "var(--accent-foreground)",
  accentHover: "var(--accent-hover)",
  accentLight: "var(--accent-light)",

  // Muted surfaces
  muted: "var(--muted)",
  mutedForeground: "var(--muted-foreground)",

  // Destructive/Error
  destructive: "var(--destructive)",
  destructiveForeground: "var(--destructive-foreground)",

  // Semantic status colors
  success: "var(--success)",
  successForeground: "var(--success-foreground)",
  warning: "var(--warning)",
  warningForeground: "var(--warning-foreground)",
  info: "var(--info)",
  infoForeground: "var(--info-foreground)",

  // Border & Input
  border: "var(--border)",
  input: "var(--input)",
  ring: "var(--ring)",

  // Chart colors (for data visualization)
  chart1: "var(--chart-1)",
  chart2: "var(--chart-2)",
  chart3: "var(--chart-3)",
  chart4: "var(--chart-4)",
  chart5: "var(--chart-5)",
} as const;

/**
 * Light mode hex values (for reference)
 */
export const lightModeColors = {
  background: "#ffffff",
  foreground: "#0f172a",
  card: "#f8fafc",
  primary: "#10b981", // Emerald-500
  secondary: "#3b82f6", // Blue-500
  accent: "#f59e0b", // Amber-500
  muted: "#f8fafc",
  mutedForeground: "#64748b",
  destructive: "#ef4444",
  success: "#22c55e",
  warning: "#eab308",
  info: "#0ea5e9",
  border: "#e2e8f0",
} as const;

/**
 * Dark mode hex values (for reference)
 */
export const darkModeColors = {
  background: "#0f172a",
  foreground: "#f1f5f9",
  card: "#1e293b",
  primary: "#34d399", // Emerald-400 (brighter)
  secondary: "#60a5fa", // Blue-400 (brighter)
  accent: "#fbbf24", // Amber-400 (brighter)
  muted: "#1e293b",
  mutedForeground: "#94a3b8",
  destructive: "#f87171",
  success: "#4ade80",
  warning: "#fde047",
  info: "#38bdf8",
  border: "#475569",
} as const;

// ============ TYPOGRAPHY TOKENS ============

/**
 * Font families
 */
export const fonts = {
  sans: "var(--font-space-grotesk), system-ui, sans-serif",
  mono: "var(--font-geist-mono), ui-monospace, monospace",
} as const;

/**
 * Font weights used in the design system
 */
export const fontWeights = {
  normal: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
} as const;

/**
 * Font sizes (Tailwind defaults)
 */
export const fontSizes = {
  xs: "0.75rem", // 12px
  sm: "0.875rem", // 14px
  base: "1rem", // 16px
  lg: "1.125rem", // 18px
  xl: "1.25rem", // 20px
  "2xl": "1.5rem", // 24px
  "3xl": "1.875rem", // 30px
  "4xl": "2.25rem", // 36px
} as const;

// ============ SPACING TOKENS ============

/**
 * Spacing scale (Tailwind defaults - for reference)
 */
export const spacing = {
  0: "0",
  0.5: "0.125rem", // 2px
  1: "0.25rem", // 4px
  1.5: "0.375rem", // 6px
  2: "0.5rem", // 8px
  2.5: "0.625rem", // 10px
  3: "0.75rem", // 12px
  3.5: "0.875rem", // 14px
  4: "1rem", // 16px
  5: "1.25rem", // 20px
  6: "1.5rem", // 24px
  7: "1.75rem", // 28px
  8: "2rem", // 32px
  9: "2.25rem", // 36px
  10: "2.5rem", // 40px
  12: "3rem", // 48px
  14: "3.5rem", // 56px
  16: "4rem", // 64px
  20: "5rem", // 80px
} as const;

// ============ BORDER RADIUS TOKENS ============

/**
 * Border radius values
 */
export const radius = {
  sm: "var(--radius-sm)", // 6px
  md: "var(--radius-md)", // 10px - buttons
  lg: "var(--radius-lg)", // 16px - cards
  xl: "var(--radius-xl)", // 20px - badges
  "2xl": "var(--radius-2xl)", // 24px
  "3xl": "var(--radius-3xl)", // 32px
  "4xl": "var(--radius-4xl)", // 40px
  full: "var(--radius-full)", // 9999px - circles
} as const;

/**
 * Border radius pixel values (for reference)
 */
export const radiusPx = {
  sm: "6px",
  md: "10px",
  lg: "16px",
  xl: "20px",
  "2xl": "24px",
  "3xl": "32px",
  "4xl": "40px",
  full: "9999px",
} as const;

/**
 * Recommended radius usage
 */
export const radiusUsage = {
  buttons: "radius-md (10px)",
  cards: "radius-lg (16px)",
  badges: "radius-xl (20px)",
  avatars: "radius-full",
  inputs: "radius-md (10px)",
  modals: "radius-lg (16px)",
} as const;

// ============ SHADOW TOKENS ============

/**
 * Box shadow values
 */
export const shadows = {
  sm: "var(--shadow-sm)", // 0 1px 3px rgba(0,0,0,0.08)
  default: "var(--shadow)", // 0 4px 12px rgba(0,0,0,0.08)
  md: "var(--shadow-md)", // 0 4px 12px rgba(0,0,0,0.1)
  lg: "var(--shadow-lg)", // 0 8px 24px rgba(0,0,0,0.12)
  xl: "var(--shadow-xl)", // 0 12px 32px rgba(0,0,0,0.15)
} as const;

/**
 * Glow shadow effects (for gamification elements)
 */
export const glowShadows = {
  primary: "var(--shadow-glow-primary)", // 0 0 20px var(--primary)
  secondary: "var(--shadow-glow-secondary)", // 0 0 20px var(--secondary)
  accent: "var(--shadow-glow-accent)", // 0 0 20px var(--accent)
} as const;

// ============ ANIMATION TOKENS ============

/**
 * Animation durations
 */
export const durations = {
  fast: "var(--duration-fast)", // 150ms
  normal: "var(--duration-normal)", // 200ms
  slow: "var(--duration-slow)", // 300ms
} as const;

/**
 * Easing functions
 */
export const easings = {
  default: "var(--ease-default)", // cubic-bezier(0.4, 0, 0.2, 1)
  bounce: "var(--ease-bounce)", // cubic-bezier(0.68, -0.55, 0.265, 1.55)
} as const;

/**
 * Available keyframe animations (CSS classes)
 */
export const animations = {
  // Entry animations
  fadeIn: "animate-fade-in", // fade with subtle translateY
  slideUp: "animate-slide-up", // vertical slide from below
  slideDown: "animate-slide-down", // vertical slide from above

  // Continuous animations
  bounce: "animate-bounce-light", // light bounce effect
  pulseGlow: "animate-pulse-glow", // pulsing box-shadow
  fire: "animate-fire", // fire icon flickering
  shimmer: "animate-shimmer", // loading shimmer effect

  // One-shot animations
  pointsPop: "animate-points-pop", // points badge pop-in
  floatUp: "animate-float-up", // floating disappear effect
} as const;

/**
 * Hover effect classes
 */
export const hoverEffects = {
  lift: "hover-lift", // translateY(-2px) with shadow
} as const;

// ============ GRADIENT TOKENS ============

/**
 * Gradient text utilities (CSS classes)
 */
export const textGradients = {
  primary: "text-gradient-primary", // primary to secondary
  accent: "text-gradient-accent", // accent to orange
} as const;

/**
 * Rank badge gradients (CSS classes)
 */
export const rankGradients = {
  gold: "bg-rank-gold", // #fbbf24 to #f59e0b
  silver: "bg-rank-silver", // #e2e8f0 to #94a3b8
  bronze: "bg-rank-bronze", // #f97316 to #c2410c
} as const;

/**
 * Glow effect utilities (CSS classes)
 */
export const glowEffects = {
  primary: "glow-primary",
  secondary: "glow-secondary",
  accent: "glow-accent",
  success: "glow-success",
} as const;

// ============ COMPONENT VARIANTS ============

/**
 * Button variants available
 */
export const buttonVariants = [
  "default", // primary green
  "secondary", // blue
  "accent", // amber
  "destructive", // red
  "outline", // bordered
  "ghost", // transparent
  "link", // text only
] as const;

/**
 * Badge variants available
 */
export const badgeVariants = [
  "default",
  "secondary",
  "accent",
  "destructive",
  "outline",
  "success",
  "warning",
  "info",
  "live", // pulsing red
  "points", // primary with bold
  "streak", // accent with bold
  "rank", // secondary with semibold
] as const;

/**
 * Card variants available
 */
export const cardVariants = [
  "default",
  "gradient", // subtle gradient background
  "elevated", // stronger shadow
  "ghost", // minimal styling
] as const;

/**
 * Avatar sizes available
 */
export const avatarSizes = ["xs", "sm", "default", "lg", "xl"] as const;

/**
 * Progress variants available
 */
export const progressVariants = [
  "default",
  "secondary",
  "accent",
  "success",
  "muted",
] as const;

// ============ TAILWIND CLASS HELPERS ============

/**
 * Common Tailwind class combinations for the design system
 */
export const tailwindPatterns = {
  // Card with hover
  interactiveCard:
    "rounded-lg bg-card shadow-md transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg",

  // Primary button shadow
  primaryButtonShadow:
    "shadow-md shadow-primary/25 hover:shadow-lg hover:shadow-primary/30",

  // Secondary button shadow
  secondaryButtonShadow:
    "shadow-md shadow-secondary/25 hover:shadow-lg hover:shadow-secondary/30",

  // Accent button shadow
  accentButtonShadow:
    "shadow-md shadow-accent/25 hover:shadow-lg hover:shadow-accent/30",

  // Badge with semantic color (example: success)
  successBadge: "bg-success/15 text-success border-success/20",

  // Icon container (for nav items, etc.)
  iconContainer:
    "flex h-8 w-8 items-center justify-center rounded-lg bg-muted",

  // Gradient icon background
  gradientIconBg:
    "bg-gradient-to-br from-primary to-secondary text-white rounded-lg",
} as const;

// ============ TYPE EXPORTS ============

export type ColorToken = keyof typeof colors;
export type RadiusToken = keyof typeof radius;
export type ShadowToken = keyof typeof shadows;
export type DurationToken = keyof typeof durations;
export type EasingToken = keyof typeof easings;
export type AnimationClass = (typeof animations)[keyof typeof animations];
export type ButtonVariant = (typeof buttonVariants)[number];
export type BadgeVariant = (typeof badgeVariants)[number];
export type CardVariant = (typeof cardVariants)[number];
export type AvatarSize = (typeof avatarSizes)[number];
export type ProgressVariant = (typeof progressVariants)[number];
