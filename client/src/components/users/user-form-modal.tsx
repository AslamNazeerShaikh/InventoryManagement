"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type {
  CreateUserDto,
  RoleDto,
  UpdateUserDto,
  UserDto,
} from "@/lib/types";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";

interface Draft {
  name: string;
  email: string;
  password: string;
  roleIds: number[];
  isActive: boolean;
}

function emptyDraft(): Draft {
  return { name: "", email: "", password: "", roleIds: [], isActive: true };
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
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    let active = true;
    api.roles
      .list()
      .then((list) => {
        if (!active) return;
        setRoles(list);
        setDraft(
          initial
            ? {
                name: initial.name,
                email: initial.email,
                password: "",
                roleIds: list
                  .filter((r) => initial.roles.includes(r.name))
                  .map((r) => r.id),
                isActive: initial.isActive,
              }
            : emptyDraft(),
        );
      })
      .catch(() => {
        if (active) setRoles([]);
      });
    return () => {
      active = false;
    };
  }, [open, initial]);

  function toggleRole(id: number) {
    setDraft((d) => ({
      ...d,
      roleIds: d.roleIds.includes(id)
        ? d.roleIds.filter((r) => r !== id)
        : [...d.roleIds, id],
    }));
  }

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
          roleIds: draft.roleIds,
          isActive: draft.isActive,
        };
        await api.users.update(initial.id, payload);
        toast.success("User updated", { description: draft.name });
      } else {
        const payload: CreateUserDto = {
          name: draft.name.trim(),
          email: draft.email.trim(),
          password: draft.password,
          roleIds: draft.roleIds,
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
          <Field label="Roles" hint="Grant one or more roles" className="sm:col-span-2">
            <div className="grid gap-2 sm:grid-cols-2">
              {roles.map((r) => (
                <label
                  key={r.id}
                  className="flex items-center gap-2 rounded-lg border border-white/[0.06] px-3 py-2 text-sm"
                >
                  <input
                    type="checkbox"
                    checked={draft.roleIds.includes(r.id)}
                    onChange={() => toggleRole(r.id)}
                    className="size-4 accent-brand-500"
                  />
                  <span className="text-slate-200">{r.name}</span>
                  {r.isSystem && (
                    <span className="ml-auto text-xs text-slate-500">system</span>
                  )}
                </label>
              ))}
              {roles.length === 0 && (
                <p className="text-sm text-slate-500">No roles available.</p>
              )}
            </div>
          </Field>
        </div>

        {isEdit && (
          <Switch
            label="Active account"
            description="Can sign in and use the system"
            checked={draft.isActive}
            onChange={(v) => setDraft((d) => ({ ...d, isActive: v }))}
          />
        )}
      </form>
    </Modal>
  );
}
