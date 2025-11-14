import api from './api';
import type { LoginData, AuthResponse } from '@/types';

export const authService = {
    async login(loginData: LoginData): Promise<AuthResponse> {
        const response = await api.post<AuthResponse>('/auth/login', loginData);
        return response.data;
    },

    async validateToken(): Promise<{ isValid: boolean; user: any }> {
        const response = await api.post('/auth/validate');
        return response.data;
    },

    saveToken(token: string): void {
        localStorage.setItem('token', token);
    },

    getToken(): string | null {
        return localStorage.getItem('token');
    },

    removeToken(): void {
        localStorage.removeItem('token');
    },

    isAuthenticated(): boolean {
        return !!this.getToken();
    }
};