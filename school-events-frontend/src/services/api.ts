import axios from 'axios';
import { authService } from './auth';

const api = axios.create({
  baseURL: 'http://localhost:5101/api',
  timeout: 10000,
});

api.interceptors.request.use(
  (config) => {
    const token = authService.getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    console.log(`🔄 Fazendo requisição para: ${config.url}`);
    return config;
  },
  (error) => {
    console.error('❌ Erro na requisição:', error);
    return Promise.reject(error);
  }
);

// Interceptor para tratar erros de autenticação
api.interceptors.response.use(
  (response) => {
    console.log('✅ Resposta recebida:', response.status);
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Token expirado ou inválido
      authService.removeToken();
      window.location.href = '/login';
    }
    console.error('❌ Erro na resposta:', error.response?.status, error.message);
    return Promise.reject(error);
  }
);

export default api;