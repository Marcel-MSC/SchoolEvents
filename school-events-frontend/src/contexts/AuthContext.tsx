import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { User, LoginData } from '@/types';
import { authService } from '@/services/auth';

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (loginData: LoginData) => Promise<void>;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        checkAuth();
    }, []);

    const checkAuth = async () => {
        const token = authService.getToken();
        if (token) {
            try {
                const response = await authService.validateToken();
                if (response.isValid) {
                    setUser(response.user);
                } else {
                    authService.removeToken();
                }
            } catch (error) {
                console.error('Erro ao validar token:', error);
                authService.removeToken();
            }
        }
        setIsLoading(false);
    };

    const login = async (loginData: LoginData) => {
        try {
            const response = await authService.login(loginData);
            authService.saveToken(response.token);
            setUser(response.user);
        } catch (error) {
            throw error;
        }
    };

    const logout = () => {
        authService.removeToken();
        setUser(null);
    };

    const value: AuthContextType = {
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth deve ser usado dentro de um AuthProvider');
    }
    return context;
};