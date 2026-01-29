/**
 * Skip to main content link for keyboard users
 *
 * Add this at the top of your layout:
 * <SkipLink href="#main-content" />
 *
 * And add id="main-content" to your <main> element.
 */
export function SkipLink({ href = '#main-content' }: { href?: string }) {
  return (
    <a
      href={href}
      className="
        sr-only focus:not-sr-only
        focus:absolute focus:top-4 focus:left-4 focus:z-50
        focus:bg-background focus:px-4 focus:py-2
        focus:rounded-md focus:ring-2 focus:ring-primary
        focus:outline-none
      "
    >
      Skip to main content
    </a>
  );
}
