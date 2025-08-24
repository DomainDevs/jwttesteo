// api.js
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://localhost:7047/api',
});

// Interceptor de request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor de respuesta
api.interceptors.response.use(
  response => response,
  async error => {
    const originalRequest = error.config;

    // ⚠️ Si recibimos un 401 y no se intentó ya refrescar
    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const refreshToken = localStorage.getItem('refreshToken');
      const refreshMaxExpiry = localStorage.getItem('refreshMaxExpiry');

      // 🔴 Validar si el refresh ya alcanzó su vida máxima
      if (refreshMaxExpiry && Date.now() > new Date(refreshMaxExpiry).getTime()) {
        console.warn("⛔ La sesión alcanzó su tiempo máximo. Cerrando...");
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('refreshExpiry');
        localStorage.removeItem('refreshMaxExpiry');
        window.location.href = '/login';
        return Promise.reject(error);
      }

      if (refreshToken) {
        try {
          const res = await axios.post('https://localhost:7047/api/auth/refresh', { refreshToken });

          // Guardar nuevos tokens
          localStorage.setItem('accessToken', res.data.accessToken);
          localStorage.setItem('refreshToken', res.data.refreshToken);
          localStorage.setItem('refreshExpiry', res.data.refreshExpiry);
          localStorage.setItem('refreshMaxExpiry', res.data.refreshMaxExpiry);

          // Reintentar la request original con el nuevo token
          originalRequest.headers['Authorization'] = `Bearer ${res.data.accessToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          console.error("❌ Refresh inválido, cerrando sesión...", refreshError);
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('refreshExpiry');
          localStorage.removeItem('refreshMaxExpiry');
          window.location.href = '/login';
        }
      }
    }

    return Promise.reject(error);
  }
);

export default api;
