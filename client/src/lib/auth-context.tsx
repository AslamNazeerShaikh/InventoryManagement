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
import type { UserDto } from "@/lib/types";

interface AuthContextValue {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isAdmin: boolean;
  isProvider: boolean;
  /** Admin or Provider — may manage inventory & assignments. */
  canManage: boolean;
  /** Admin only — may manage users. */
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

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      isAdmin: Boolean(user?.isAdmin),
      isProvider: Boolean(user?.isProvider),
      canManage: Boolean(user?.isAdmin || user?.isProvider),
      canManageUsers: Boolean(user?.isAdmin),
      login,
      logout,
      applyUser,
    }),
    [user, isLoading, login, logout, applyUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
