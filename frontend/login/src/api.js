import axios from "axios";
import router from "./router";

// Ajusta a TU puerto correcto
const api = axios.create({
  baseURL: "https://localhost:7047/api",
});

// Función para verificar si el token sigue siendo válido
function isTokenExpired(token) {
  if (!token) return true;
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    const exp = payload.exp * 1000; // exp está en segundos
    return Date.now() >= exp;
  } catch {
    return true;
  }
}

// Interceptor de requests
api.interceptors.request.use((config) => {
  // 🚨 Ignorar validación en login y refresh
  if (config.url.includes("/auth/login") || config.url.includes("/auth/refresh")) {
    return config;
  }

  const token = localStorage.getItem("accessToken");

  if (isTokenExpired(token)) {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    router.push("/login");
    throw new Error("Token expirado");
  }

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

export default api;
