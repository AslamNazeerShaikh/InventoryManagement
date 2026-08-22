"use client";

import { useState } from "react";
import { Calendar, Clock, KeyRound, ShieldCheck, UserCog } from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { formatDate, formatDateTime } from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { RoleBadge } from "@/components/domain/status-badges";

export default function ProfilePage() {
  const { user, applyUser } = useAuth();

  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [savingProfile, setSavingProfile] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [savingPassword, setSavingPassword] = useState(false);

  if (!user) return null;

  async function saveProfile(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim() || !email.trim()) {
      toast.error("Name and email are required");
      return;
    }
    setSavingProfile(true);
    try {
      const updated = await api.users.update(user!.id, {
        name: name.trim(),
        email: email.trim(),
        roleIds: null,
        isActive: user!.isActive,
      });
      // The generic UserDto carries roles but not permission codes (those are issued
      // from JWT claims at login/refresh). A self-service name/email edit can't change
      // them, so keep the session's permissions to preserve client-side access gating.
      applyUser({ ...updated, permissions: user!.permissions });
      toast.success("Profile updated");
    } catch (err) {
      toast.error("Update failed", {
        description: err instanceof ApiError ? err.message : undefined,
      });
    } finally {
      setSavingProfile(false);
    }
  }

  async function changePassword(e: React.FormEvent) {
    e.preventDefault();
    if (newPassword.length < 8) {
      toast.error("New password must be at least 8 characters");
      return;
    }
    if (newPassword !== confirmPassword) {
      toast.error("New password and confirmation do not match");
      return;
    }
    setSavingPassword(true);
    try {
      await api.auth.changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
      });
      toast.success("Password changed", {
        description: "Other sessions have been signed out.",
      });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      toast.error("Change failed", {
        description: err instanceof ApiError ? err.message : undefined,
      });
    } finally {
      setSavingPassword(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={UserCog}
        title="Profile & security"
        description="Manage your personal details and password."
      />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          {/* Profile details */}
          <Card>
            <form onSubmit={saveProfile}>
              <CardHeader>
                <CardTitle>Personal details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label="Full name" required>
                    <Input
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      required
                    />
                  </Field>
                  <Field label="Email" required>
                    <Input
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                    />
                  </Field>
                </div>
              </CardContent>
              <CardFooter>
                <Button type="submit" loading={savingProfile}>
                  Save changes
                </Button>
              </CardFooter>
            </form>
          </Card>

          {/* Security */}
          <Card>
            <form onSubmit={changePassword}>
              <CardHeader>
                <CardTitle>
                  <span className="flex items-center gap-2">
                    <KeyRound className="size-4 text-brand-300" /> Change password
                  </span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <Field label="Current password" required>
                  <Input
                    type="password"
                    autoComplete="current-password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    required
                  />
                </Field>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label="New password" required hint="Min. 8 characters">
                    <Input
                      type="password"
                      autoComplete="new-password"
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      required
                    />
                  </Field>
                  <Field label="Confirm new password" required>
                    <Input
                      type="password"
                      autoComplete="new-password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      required
                    />
                  </Field>
                </div>
              </CardContent>
              <CardFooter>
                <Button type="submit" loading={savingPassword}>
                  Update password
                </Button>
              </CardFooter>
            </form>
          </Card>
        </div>

        {/* Account summary */}
        <Card className="h-fit">
          <CardContent className="flex flex-col items-center gap-3 py-8 text-center">
            <Avatar name={user.name} size="lg" className="size-16 text-lg" />
            <div>
              <p className="text-lg font-semibold text-white">{user.name}</p>
              <p className="text-sm text-slate-400">{user.email}</p>
            </div>
            <div className="flex flex-wrap items-center justify-center gap-2">
              {user.roles.map((r) => (
                <RoleBadge key={r} role={r} />
              ))}
              <Badge tone={user.isActive ? "success" : "neutral"} dot>
                {user.isActive ? "Active" : "Inactive"}
              </Badge>
            </div>
          </CardContent>
          <div className="space-y-3 border-t border-white/[0.06] px-5 py-4 text-sm">
            <div className="flex items-center gap-2.5 text-slate-400">
              <ShieldCheck className="size-4 text-slate-500" />
              <span>Account ID · {user.id}</span>
            </div>
            <div className="flex items-center gap-2.5 text-slate-400">
              <Calendar className="size-4 text-slate-500" />
              <span>Member since {formatDate(user.createdAt)}</span>
            </div>
            <div className="flex items-center gap-2.5 text-slate-400">
              <Clock className="size-4 text-slate-500" />
              <span>
                Last login{" "}
                {user.lastLoginAt ? formatDateTime(user.lastLoginAt) : "—"}
              </span>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
