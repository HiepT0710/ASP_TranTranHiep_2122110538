import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { getMe, login, logout } from "../services/apiService";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  const refreshMe = useCallback(async () => {
    try {
      const me = await getMe();
      setUser(me);
      return me;
    } catch {
      setUser(null);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshMe();
  }, [refreshMe]);

  const value = useMemo(
    () => ({
      user,
      loading,
      isAuthenticated: !!user,
      login: async (payload) => {
        const result = await login(payload);
        if (result?.user) setUser(result.user);
        await refreshMe();
      },
      logout: async () => {
        await logout();
        setUser(null);
      },
      refreshMe,
    }),
    [user, loading, refreshMe]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
