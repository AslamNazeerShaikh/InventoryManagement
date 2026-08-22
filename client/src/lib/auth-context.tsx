"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  api,
  clearSession,
  getStoredUser,
  hasSession,
  registerUnauthorizedHandler,
  setSession,
} from "@/lib/api";
import { STORAGE_KEYS } from "@/lib/config";
import { PERMISSIONS } from "@/lib/permissions";
import type { UserDto } from "@/lib/types";

interface AuthContextValue {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  /** All permission codes granted to the signed-in user. */
  permissions: string[];
  /** Whether the user holds a specific permission code. */
  hasPermission: (code: string) => boolean;
  isAdmin: boolean;
  isProvider: boolean;
  /** Convenience: may manage inventory (elevated operator). */
  canManage: boolean;
  /** May manage users. */
  canManageUsers: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  applyUser: (user: UserDto) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<UserDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Hydrate the session from localStorage on first mount (client only).
  useEffect(() => {
    if (hasSession()) {
      setUser(getStoredUser());
    }
    setIsLoading(false);
  }, []);

  // When a token refresh ultimately fails, drop the user back to the login page.
  useEffect(() => {
    registerUnauthorizedHandler(() => {
      setUser(null);
      toast.error("Session expired", {
        description: "Please sign in again to continue.",
      });
      router.replace("/login");
    });
  }, [router]);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await api.auth.login({ email, password });
    setSession(auth);
    setUser(auth.user);
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.auth.logout();
    } catch {
      /* best-effort — clear locally regardless */
    }
    clearSession();
    setUser(null);
    router.replace("/login");
  }, [router]);

  const applyUser = useCallback((next: UserDto) => {
    setUser(next);
    if (typeof window !== "undefined") {
      localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(next));
    }
  }, []);

  const permissionSet = useMemo(() => new Set(user?.permissions ?? []), [user]);
  const hasPermission = useCallback(
    (code: string) => permissionSet.has(code),
    [permissionSet],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      permissions: user?.permissions ?? [],
      hasPermission,
      isAdmin: permissionSet.has(PERMISSIONS.users.manage),
      isProvider: permissionSet.has(PERMISSIONS.inventory.manage),
      canManage: permissionSet.has(PERMISSIONS.inventory.manage),
      canManageUsers: permissionSet.has(PERMISSIONS.users.manage),
      login,
      logout,
      applyUser,
    }),
    [user, isLoading, login, logout, applyUser, hasPermission, permissionSet],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
