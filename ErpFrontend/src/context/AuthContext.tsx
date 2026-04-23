import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

interface AuthContextType {
  token: string | null;
  role: string | null;
  branchId: number | null;
  branchName: string | null;
  login: (token: string, role: string, branchId: number, branchName: string) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [role, setRole] = useState<string | null>(localStorage.getItem('role'));
  const [branchId, setBranchId] = useState<number | null>(Number(localStorage.getItem('branchId')) || null);
  const [branchName, setBranchName] = useState<string | null>(localStorage.getItem('branchName'));

  const login = (newToken: string, newRole: string, newBranchId: number, newBranchName: string) => {
    localStorage.setItem('token', newToken);
    localStorage.setItem('role', newRole);
    localStorage.setItem('branchId', newBranchId.toString());
    localStorage.setItem('branchName', newBranchName);
    setToken(newToken);
    setRole(newRole);
    setBranchId(newBranchId);
    setBranchName(newBranchName);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('branchId');
    localStorage.removeItem('branchName');
    setToken(null);
    setRole(null);
    setBranchId(null);
    setBranchName(null);
  };

  // Sync state if localStorage changes from another tab
  useEffect(() => {
    const handleStorageChange = () => {
      setToken(localStorage.getItem('token'));
      setRole(localStorage.getItem('role'));
      setBranchId(Number(localStorage.getItem('branchId')) || null);
      setBranchName(localStorage.getItem('branchName'));
    };
    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  return (
    <AuthContext.Provider value={{ token, role, branchId, branchName, login, logout, isAuthenticated: !!token }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
