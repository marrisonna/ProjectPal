import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { apiClient, setAuthToken, type WhoAmI } from "../api/client";

const TOKEN_STORAGE_KEY = "projectpal.token";

interface AuthContextValue {
  person: WhoAmI | null;
  isLoading: boolean;
  login: (externalLogin: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [person, setPerson] = useState<WhoAmI | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const loadWhoAmI = useCallback(async () => {
    const { data, error } = await apiClient.GET("/auth/whoami");
    if (error || !data) {
      setAuthToken(null);
      sessionStorage.removeItem(TOKEN_STORAGE_KEY);
      setPerson(null);
      return;
    }
    setPerson(data as WhoAmI);
  }, []);

  useEffect(() => {
    const storedToken = sessionStorage.getItem(TOKEN_STORAGE_KEY);
    if (!storedToken) {
      setIsLoading(false);
      return;
    }
    setAuthToken(storedToken);
    loadWhoAmI().finally(() => setIsLoading(false));
  }, [loadWhoAmI]);

  const login = useCallback(
    async (externalLogin: string, password: string) => {
      const { data, error } = await apiClient.POST("/auth/login", {
        body: { external_login: externalLogin, password },
      });
      if (error || !data) {
        throw new Error("Login failed — check the login name and password.");
      }
      setAuthToken(data.token);
      sessionStorage.setItem(TOKEN_STORAGE_KEY, data.token);
      await loadWhoAmI();
    },
    [loadWhoAmI],
  );

  const logout = useCallback(() => {
    setAuthToken(null);
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
    setPerson(null);
  }, []);

  return (
    <AuthContext.Provider value={{ person, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
