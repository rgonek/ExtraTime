'use client';

import { motion, type Variants, type Transition } from 'framer-motion';

/**
 * Animation duration tokens (in seconds) matching globals.css
 * --duration-fast: 150ms = 0.15s
 * --duration-normal: 200ms = 0.2s
 * --duration-slow: 300ms = 0.3s
 */
const DURATION = {
  fast: 0.15,
  normal: 0.2,
  slow: 0.3,
} as const;

/**
 * Easing curves matching globals.css
 * --ease-default: cubic-bezier(0.4, 0, 0.2, 1)
 * --ease-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55)
 */
const EASE = {
  default: [0.4, 0, 0.2, 1] as const,
  bounce: [0.68, -0.55, 0.265, 1.55] as const,
  spring: { type: 'spring', stiffness: 300, damping: 20 } as const,
} as const;

interface AnimatedContainerProps {
  children: React.ReactNode;
  className?: string;
}

interface AnimatedContainerWithDelayProps extends AnimatedContainerProps {
  delay?: number;
}

/**
 * Fade-in container for page transitions
 */
export function FadeIn({
  children,
  className,
  delay = 0,
}: AnimatedContainerWithDelayProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: DURATION.normal, ease: EASE.default, delay }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Slide-up container for cards and list items
 */
export function SlideUp({
  children,
  className,
  delay = 0,
}: AnimatedContainerWithDelayProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -16 }}
      transition={{ duration: DURATION.slow, ease: EASE.default, delay }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Slide-down container for dropdown menus and popovers
 */
export function SlideDown({
  children,
  className,
  delay = 0,
}: AnimatedContainerWithDelayProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: -16 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -16 }}
      transition={{ duration: DURATION.slow, ease: EASE.default, delay }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Scale-in container with bounce effect
 */
export function ScaleIn({
  children,
  className,
  delay = 0,
}: AnimatedContainerWithDelayProps) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.9 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.9 }}
      transition={{ duration: DURATION.slow, ease: EASE.bounce, delay }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Pop-in animation for points, badges, and notifications
 */
export function PopIn({
  children,
  className,
  delay = 0,
}: AnimatedContainerWithDelayProps) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.5 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.5 }}
      transition={{
        type: 'spring',
        stiffness: 400,
        damping: 15,
        delay,
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Staggered list container
 */
const staggerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.05,
      delayChildren: 0,
    },
  },
};

export function StaggeredList({
  children,
  className,
  staggerDelay = 0.05,
}: {
  children: React.ReactNode;
  className?: string;
  staggerDelay?: number;
}) {
  return (
    <motion.div
      initial="hidden"
      animate="visible"
      variants={{
        hidden: { opacity: 0 },
        visible: {
          opacity: 1,
          transition: {
            staggerChildren: staggerDelay,
            delayChildren: 0,
          },
        },
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Staggered list item
 */
const itemVariants: Variants = {
  hidden: { opacity: 0, y: 16 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: DURATION.slow,
      ease: EASE.default,
    },
  },
};

export function StaggeredItem({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div variants={itemVariants} className={className}>
      {children}
    </motion.div>
  );
}

/**
 * Scale on hover with customizable scale factor
 */
export function HoverScale({
  children,
  className,
  scale = 1.02,
  tapScale = 0.98,
}: AnimatedContainerProps & { scale?: number; tapScale?: number }) {
  return (
    <motion.div
      whileHover={{ scale }}
      whileTap={{ scale: tapScale }}
      transition={{ duration: DURATION.fast, ease: EASE.default }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Hover lift effect (translate Y with shadow change)
 */
export function HoverLift({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      whileHover={{ y: -2 }}
      transition={{ duration: DURATION.normal, ease: EASE.default }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Pulse animation for live indicators and notifications
 */
export function Pulse({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      animate={{
        scale: [1, 1.05, 1],
        opacity: [1, 0.8, 1],
      }}
      transition={{
        duration: 2,
        repeat: Infinity,
        ease: 'easeInOut',
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Bounce animation for emphasis
 */
export function Bounce({
  children,
  className,
  animate = true,
}: AnimatedContainerProps & { animate?: boolean }) {
  return (
    <motion.div
      animate={
        animate
          ? {
              y: [0, -8, 0],
            }
          : {}
      }
      transition={{
        duration: 0.6,
        ease: EASE.bounce,
        repeat: animate ? Infinity : 0,
        repeatDelay: 1,
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Shake animation for error states
 */
export function Shake({
  children,
  className,
  shake = false,
}: AnimatedContainerProps & { shake?: boolean }) {
  return (
    <motion.div
      animate={shake ? { x: [-4, 4, -4, 4, 0] } : { x: 0 }}
      transition={{ duration: 0.4, ease: EASE.default }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Layout animation wrapper for smooth layout changes
 */
export function LayoutGroup({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div layout className={className}>
      {children}
    </motion.div>
  );
}

/**
 * Presence wrapper for enter/exit animations
 */
export { AnimatePresence } from 'framer-motion';

/**
 * Export animation constants for custom animations
 */
export const animationConfig = {
  duration: DURATION,
  ease: EASE,
} as const;
