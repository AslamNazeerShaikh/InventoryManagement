import { cn } from "@/lib/utils";

export function Table({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="overflow-x-auto">
      <table className={cn("w-full text-left text-sm", className)}>
        {children}
      </table>
    </div>
  );
}

export function THead({ children }: { children: React.ReactNode }) {
  return (
    <thead className="border-b border-white/[0.06] text-xs uppercase tracking-wide text-slate-500">
      {children}
    </thead>
  );
}

export function TH({
  className,
  children,
}: {
  className?: string;
  children?: React.ReactNode;
}) {
  return (
    <th className={cn("whitespace-nowrap px-4 py-3 font-medium", className)}>
      {children}
    </th>
  );
}

export function TBody({ children }: { children: React.ReactNode }) {
  return <tbody className="divide-y divide-white/[0.05]">{children}</tbody>;
}

export function TR({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <tr className={cn("transition hover:bg-white/[0.02]", className)}>
      {children}
    </tr>
  );
}

export function TD({
  className,
  children,
  colSpan,
}: {
  className?: string;
  children?: React.ReactNode;
  colSpan?: number;
}) {
  return (
    <td colSpan={colSpan} className={cn("px-4 py-3 align-middle", className)}>
      {children}
    </td>
  );
}
