import { cn } from "@/lib/utils";
import { controlClasses } from "@/components/ui/input";

export function Textarea({
  className,
  ...props
}: React.TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={cn(controlClasses, "min-h-[92px] resize-y py-2.5", className)}
      {...props}
    />
  );
}
