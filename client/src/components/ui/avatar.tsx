import { cn, getInitials } from "@/lib/utils";

const sizes = {
  sm: "size-8 text-[11px]",
  md: "size-10 text-sm",
  lg: "size-12 text-base",
};

export function Avatar({
  name,
  size = "md",
  className,
}: {
  name: string | null | undefined;
  size?: keyof typeof sizes;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "inline-flex shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-brand-400/30 to-violet-500/30 font-semibold text-white ring-1 ring-white/10",
        sizes[size],
        className,
      )}
      aria-hidden
    >
      {getInitials(name)}
    </div>
  );
}
