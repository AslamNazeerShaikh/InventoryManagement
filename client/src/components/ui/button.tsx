"use client";

import { motion, type HTMLMotionProps } from "motion/react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

type Variant =
  | "primary"
  | "secondary"
  | "outline"
  | "ghost"
  | "danger"
  | "subtle";
type Size = "sm" | "md" | "lg" | "icon";

const variants: Record<Variant, string> = {
  primary:
    "text-ink-950 bg-gradient-to-br from-brand-300 to-brand-500 hover:brightness-110 shadow-[0_10px_30px_-10px_rgba(18,184,139,0.65)]",
  secondary:
    "text-white bg-white/[0.06] border border-white/10 hover:bg-white/[0.1]",
  outline:
    "text-white border border-white/15 hover:bg-white/[0.06] hover:border-white/25",
  ghost: "text-slate-300 hover:bg-white/[0.06] hover:text-white",
  danger:
    "text-white bg-gradient-to-br from-rose-500 to-red-600 hover:brightness-110 shadow-[0_10px_30px_-10px_rgba(244,63,94,0.6)]",
  subtle:
    "text-brand-200 bg-brand-500/10 border border-brand-400/20 hover:bg-brand-500/20",
};

const sizes: Record<Size, string> = {
  sm: "h-9 px-3 text-sm gap-1.5",
  md: "h-10 px-4 text-sm gap-2",
  lg: "h-12 px-6 text-base gap-2",
  icon: "h-10 w-10 justify-center",
};

export interface ButtonProps
  extends Omit<HTMLMotionProps<"button">, "ref" | "children"> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  children?: React.ReactNode;
}

export function Button({
  className,
  variant = "primary",
  size = "md",
  loading = false,
  leftIcon,
  rightIcon,
  children,
  disabled,
  ...props
}: ButtonProps) {
  return (
    <motion.button
      whileTap={{ scale: 0.97 }}
      disabled={disabled || loading}
      className={cn(
        "relative inline-flex select-none items-center justify-center rounded-xl font-medium outline-none transition focus-visible:ring-2 focus-visible:ring-brand-400/50 disabled:pointer-events-none disabled:opacity-50",
        variants[variant],
        sizes[size],
        className,
      )}
      {...props}
    >
      {loading ? (
        <Loader2 className="size-4 animate-spin" />
      ) : (
        leftIcon && <span className="shrink-0">{leftIcon}</span>
      )}
      {children}
      {!loading && rightIcon && <span className="shrink-0">{rightIcon}</span>}
    </motion.button>
  );
}
