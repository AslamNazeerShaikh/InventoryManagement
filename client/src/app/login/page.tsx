"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { motion } from "motion/react";
import {
  Activity,
  ArrowRight,
  CalendarClock,
  Eye,
  EyeOff,
  Lock,
  Mail,
  PackageCheck,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api";
import { APP_NAME } from "@/lib/config";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";

const DEMO = {
  email: "admin@inventorymanagement.com",
  password: "ChangeMe_LocalDev!2026",
};

const features = [
  {
    icon: PackageCheck,
    title: "Real-time inventory",
    text: "Track medical equipment, stock levels and availability at a glance.",
  },
  {
    icon: CalendarClock,
    title: "Expiry & assignment workflows",
    text: "Automated alerts, allocations and transactional returns.",
  },
  {
    icon: ShieldCheck,
    title: "Role-based access",
    text: "JWT security with Admin, Provider and Staff policies.",
  },
];

export default function LoginPage() {
  const router = useRouter();
  const { login, isAuthenticated, isLoading } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPw, setShowPw] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isLoading && isAuthenticated) router.replace("/dashboard");
  }, [isLoading, isAuthenticated, router]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await login(email.trim(), password);
      toast.success("Welcome back", { description: "Signed in successfully." });
      router.replace("/dashboard");
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? err.message
          : "Unable to sign in. Please try again.";
      setError(msg);
      toast.error("Sign in failed", { description: msg });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      {/* Brand / marketing panel */}
      <div className="relative hidden overflow-hidden border-r border-white/[0.06] lg:flex lg:flex-col lg:justify-between lg:p-12">
        <div className="pointer-events-none absolute -left-24 top-10 size-96 rounded-full bg-brand-500/20 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-20 right-0 size-96 rounded-full bg-violet-500/20 blur-3xl" />

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="relative flex items-center gap-3"
        >
          <div className="flex size-11 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-300 to-brand-600 shadow-glow">
            <Activity className="size-6 text-ink-950" strokeWidth={2.5} />
          </div>
          <span className="text-lg font-semibold gradient-text">{APP_NAME}</span>
        </motion.div>

        <div className="relative max-w-md">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.1 }}
            className="mb-8"
          >
            <span className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.03] px-3 py-1 text-xs font-medium text-brand-200">
              <Sparkles className="size-3.5" /> Inventory intelligence for healthcare
            </span>
            <h1 className="mt-5 text-4xl font-semibold leading-tight tracking-tight text-white">
              Manage medical equipment with{" "}
              <span className="gradient-text">clarity and control.</span>
            </h1>
          </motion.div>

          <div className="space-y-4">
            {features.map((f, i) => (
              <motion.div
                key={f.title}
                initial={{ opacity: 0, x: -16 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.5, delay: 0.25 + i * 0.1 }}
                className="flex items-start gap-3.5"
              >
                <div className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-xl bg-white/[0.04] ring-1 ring-white/10">
                  <f.icon className="size-4.5 text-brand-300" />
                </div>
                <div>
                  <p className="text-sm font-medium text-white">{f.title}</p>
                  <p className="text-sm text-slate-400">{f.text}</p>
                </div>
              </motion.div>
            ))}
          </div>
        </div>

        <p className="relative text-xs text-slate-500">
          Secured with JWT authentication · Optimistic concurrency · Idempotent operations
        </p>
      </div>

      {/* Form panel */}
      <div className="flex items-center justify-center p-6 sm:p-10">
        <motion.div
          initial={{ opacity: 0, y: 18 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="w-full max-w-sm"
        >
          <div className="mb-8 flex items-center gap-3 lg:hidden">
            <div className="flex size-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-300 to-brand-600 shadow-glow">
              <Activity className="size-5 text-ink-950" strokeWidth={2.5} />
            </div>
            <span className="text-lg font-semibold gradient-text">{APP_NAME}</span>
          </div>

          <h2 className="text-2xl font-semibold tracking-tight text-white">
            Sign in
          </h2>
          <p className="mt-1.5 text-sm text-slate-400">
            Welcome back. Enter your credentials to continue.
          </p>

          <form onSubmit={onSubmit} className="mt-8 space-y-5">
            <Field label="Email address" htmlFor="email">
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
                <Input
                  id="email"
                  type="email"
                  autoComplete="username"
                  required
                  placeholder="you@hospital.com"
                  className="pl-10"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
            </Field>

            <Field label="Password" htmlFor="password">
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
                <Input
                  id="password"
                  type={showPw ? "text" : "password"}
                  autoComplete="current-password"
                  required
                  placeholder="••••••••••"
                  className="px-10"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <button
                  type="button"
                  onClick={() => setShowPw((s) => !s)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 transition hover:text-slate-300"
                  aria-label={showPw ? "Hide password" : "Show password"}
                >
                  {showPw ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </div>
            </Field>

            {error && (
              <motion.p
                initial={{ opacity: 0, y: -4 }}
                animate={{ opacity: 1, y: 0 }}
                className="rounded-lg border border-rose-500/20 bg-rose-500/10 px-3 py-2 text-sm text-rose-300"
              >
                {error}
              </motion.p>
            )}

            <Button
              type="submit"
              size="lg"
              loading={submitting}
              className="w-full"
              rightIcon={!submitting && <ArrowRight className="size-4" />}
            >
              {submitting ? "Signing in" : "Sign in"}
            </Button>
          </form>

          <div className="mt-6 rounded-xl border border-white/[0.06] bg-white/[0.02] p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs font-medium text-slate-300">
                  Local demo account
                </p>
                <p className="mt-0.5 text-xs text-slate-500">
                  Prefill the seeded administrator credentials.
                </p>
              </div>
              <Button
                type="button"
                variant="subtle"
                size="sm"
                onClick={() => {
                  setEmail(DEMO.email);
                  setPassword(DEMO.password);
                }}
              >
                Use demo admin
              </Button>
            </div>
          </div>
        </motion.div>
      </div>
    </div>
  );
}
