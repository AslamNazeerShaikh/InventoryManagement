"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import {
  UserRole,
  type CreateUserDto,
  type UpdateUserDto,
  type UserDto,
} from "@/lib/types";
import { roleLabels } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";

interface Draft {
  name: string;
  email: string;
  password: string;
  role: UserRole;
  isAdmin: boolean;
  isProvider: boolean;
  isActive: boolean;
}

function emptyDraft(): Draft {
  return {
    name: "",
    email: "",
    password: "",
    role: UserRole.Staff,
    isAdmin: false,
    isProvider: false,
    isActive: true,
  };
}

export function UserFormModal({
  open,
  onClose,
  initial,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: UserDto | null;
  onSaved: () => void;
}) {
  const isEdit = Boolean(initial);
  const [draft, setDraft] = useState<Draft>(emptyDraft());
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setDraft(
        initial
          ? {
              name: initial.name,
              email: initial.email,
              password: "",
              role: initial.role,
              isAdmin: initial.isAdmin,
              isProvider: initial.isProvider,
              isActive: initial.isActive,
            }
          : emptyDraft(),
      );
    }
  }, [open, initial]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!draft.name.trim() || !draft.email.trim()) {
      toast.error("Name and email are required");
      return;
    }
    if (!isEdit && draft.password.length < 8) {
      toast.error("Password must be at least 8 characters");
      return;
    }

    setSaving(true);
    try {
      if (isEdit && initial) {
        const payload: UpdateUserDto = {
          name: draft.name.trim(),
          email: draft.email.trim(),
          role: draft.role,
          isAdmin: draft.isAdmin,
          isProvider: draft.isProvider,
          isActive: draft.isActive,
        };
        await api.users.update(initial.id, payload);
        toast.success("User updated", { description: draft.name });
      } else {
        const payload: CreateUserDto = {
          name: draft.name.trim(),
          email: draft.email.trim(),
          password: draft.password,
          role: draft.role,
          isAdmin: draft.isAdmin,
          isProvider: draft.isProvider,
        };
        await api.users.create(payload);
        toast.success("User created", { description: draft.email });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save user.";
      toast.error("Save failed", { description: msg });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title={isEdit ? "Edit user" : "Invite user"}
      description={
        isEdit
          ? "Update this member's profile and access."
          : "Create a new account with role-based access."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="user-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Create user"}
          </Button>
        </>
      }
    >
      <form id="user-form" onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Full name" required>
            <Input
              value={draft.name}
              onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
              placeholder="Dr. Sarah Johnson"
              required
            />
          </Field>
          <Field label="Email" required>
            <Input
              type="email"
              value={draft.email}
              onChange={(e) => setDraft((d) => ({ ...d, email: e.target.value }))}
              placeholder="user@hospital.com"
              required
            />
          </Field>
          {!isEdit && (
            <Field label="Password" required hint="Min. 8 characters" className="sm:col-span-2">
              <Input
                type="password"
                value={draft.password}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, password: e.target.value }))
                }
                placeholder="••••••••"
                required
              />
            </Field>
          )}
          <Field label="Role" className="sm:col-span-2">
            <Select
              value={draft.role}
              onChange={(e) =>
                setDraft((d) => ({ ...d, role: Number(e.target.value) }))
              }
            >
              {Object.values(UserRole)
                .filter((v): v is number => typeof v === "number")
                .map((v) => (
                  <option key={v} value={v}>
                    {roleLabels[v as UserRole]}
                  </option>
                ))}
            </Select>
          </Field>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <Switch
            label="Administrator"
            description="Full system access"
            checked={draft.isAdmin}
            onChange={(v) => setDraft((d) => ({ ...d, isAdmin: v }))}
          />
          <Switch
            label="Clinical provider"
            description="Manage inventory & assignments"
            checked={draft.isProvider}
            onChange={(v) => setDraft((d) => ({ ...d, isProvider: v }))}
          />
          {isEdit && (
            <Switch
              label="Active account"
              description="Can sign in and use the system"
              checked={draft.isActive}
              onChange={(v) => setDraft((d) => ({ ...d, isActive: v }))}
            />
          )}
        </div>
      </form>
    </Modal>
  );
}
