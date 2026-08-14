import { cn } from "@/lib/utils";

export function Label({
  className,
  children,
  ...props
}: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={cn("text-sm font-medium text-slate-300", className)}
      {...props}
    >
      {children}
    </label>
  );
}

export function Field({
  label,
  htmlFor,
  hint,
  error,
  required,
  className,
  children,
}: {
  label?: string;
  htmlFor?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={cn("space-y-1.5", className)}>
      {label && (
        <div className="flex items-center justify-between">
          <Label htmlFor={htmlFor}>
            {label}
            {required && <span className="ml-0.5 text-brand-300">*</span>}
          </Label>
          {hint && <span className="text-xs text-slate-500">{hint}</span>}
        </div>
      )}
      {children}
      {error && <p className="text-xs text-rose-400">{error}</p>}
    </div>
  );
}
