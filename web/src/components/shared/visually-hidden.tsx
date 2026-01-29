/**
 * Visually hidden text for screen readers
 *
 * Use this to provide context that is visually apparent
 * but needs to be explicitly stated for screen readers.
 *
 * Example:
 * <button>
 *   <TrashIcon />
 *   <VisuallyHidden>Delete item</VisuallyHidden>
 * </button>
 */
export function VisuallyHidden({ children }: { children: React.ReactNode }) {
  return (
    <span className="absolute w-px h-px p-0 -m-px overflow-hidden whitespace-nowrap border-0 [clip:rect(0,0,0,0)]">
      {children}
    </span>
  );
}
