'use client';

import { motion } from 'framer-motion';

interface AnimatedContainerProps {
  children: React.ReactNode;
  className?: string;
}

/**
 * Fade-in container for page transitions
 */
export function FadeIn({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Slide-up container for cards and list items
 */
export function SlideUp({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Staggered list container
 */
export function StaggeredList({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <motion.div
      initial="hidden"
      animate="visible"
      variants={{
        visible: {
          transition: { staggerChildren: 0.05 },
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
export function StaggeredItem({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 20 },
        visible: { opacity: 1, y: 0 },
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Scale on hover
 */
export function HoverScale({
  children,
  className,
  scale = 1.02,
}: AnimatedContainerProps & { scale?: number }) {
  return (
    <motion.div whileHover={{ scale }} whileTap={{ scale: 0.98 }} className={className}>
      {children}
    </motion.div>
  );
}
