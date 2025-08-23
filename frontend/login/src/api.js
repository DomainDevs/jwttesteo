// src/api.js

import axios from 'axios';
import router from './router'; // Asegúrate de que el router esté importado


const api = axios.create({
    baseURL: 'https://localhost:7047/api', // Reemplaza con tu URL de API
    headers: {
        'Content-Type': 'application/json'
    }
});

// Interceptor de peticiones para añadir el Access Token
api.interceptors.request.use(
    (config) => {
        const accessToken = localStorage.getItem('accessToken');
        if (accessToken) {
            config.headers['Authorization'] = `Bearer ${accessToken}`;
        }
        return config;
    },
    (error) => {
        console.error("Interceptor de REQUEST: Error en la petición.", error);
        return Promise.reject(error);
    }
);

// Interceptor de respuestas para manejar el refresco del token
api.interceptors.response.use(
    (response) => {
        return response;
    },
    async (error) => {
        const originalRequest = error.config;
        console.log("Interceptor de RESPONSE: Error de respuesta recibido.", error.response); // Debug 1
        console.log("Interceptor de RESPONSE: Estado del error:", error.response ? error.response.status : "No hay estado de respuesta."); // Debug 2

        // Asegúrate de que error.response exista antes de acceder a sus propiedades
        if (error.response && error.response.status === 401 && !originalRequest._retry) {
            console.log("Interceptor de RESPONSE: Error 401 detectado, intentando refrescar token."); // Debug 3
            originalRequest._retry = true; // Marca la petición original como reintentada
            
            try {
                const refreshToken = localStorage.getItem('refreshToken');
                console.log("Interceptor de RESPONSE: Refresh Token del localStorage:", refreshToken); // Debug 4

                if (!refreshToken) {
                    console.log("Interceptor de RESPONSE: No hay refresh token, redirigiendo a login."); // Debug 5
                    localStorage.removeItem('accessToken');
                    localStorage.removeItem('refreshToken');
                    router.push({ name: 'login' });
                    return Promise.reject(new Error("No hay refresh token para renovar."));
                }

                console.log("Interceptor de RESPONSE: Enviando petición de refresco a /auth/refresh."); // Debug 6
                const res = await api.post('/auth/refresh', { refreshToken });
                console.log("Interceptor de RESPONSE: Respuesta del refresco recibida:", res.data); // Debug 7

                const { accessToken, refreshToken: newRefreshToken } = res.data; // Asegúrate de que el backend envía 'refreshToken' y no 'newRefreshToken'
                localStorage.setItem('accessToken', accessToken);
                localStorage.setItem('refreshToken', newRefreshToken);
                console.log("Interceptor de RESPONSE: Tokens actualizados, reintentando petición original."); // Debug 8

                originalRequest.headers['Authorization'] = `Bearer ${accessToken}`;
                return api(originalRequest); // Reintenta la petición original con el nuevo token

            } catch (refreshError) {
                console.error("Interceptor de RESPONSE: ERROR CRÍTICO al intentar refrescar el token:", refreshError.response ? refreshError.response.data : refreshError.message, refreshError); // Debug 9
                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                router.push({ name: 'login' });
                return Promise.reject(refreshError);
            }
        }
        console.log("Interceptor de RESPONSE: Error no es 401 o ya se reintentó, propagando error original."); // Debug 10
        return Promise.reject(error);
    }
);

export default api;