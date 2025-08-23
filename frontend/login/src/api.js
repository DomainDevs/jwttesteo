// api.js
import axios from "axios";

const api = axios.create({ baseURL: "/api" });

let isRefreshing = false;
let refreshPromise = null;
let subscribers = [];

function subscribeTokenRefresh(cb) {
  subscribers.push(cb);
}

function onRrefreshed(newAccessToken) {
  subscribers.forEach(cb => cb(newAccessToken));
  subscribers = [];
}

api.interceptors.request.use((config) => {
  const access = sessionStorage.getItem("accessToken"); // o en memoria (mejor)
  if (access) config.headers.Authorization = `Bearer ${access}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const { config, response } = error;

    // Si no es 401 o ya se reintentó, salimos
    if (!response || response.status !== 401 || config.__retry) {
      return Promise.reject(error);
    }

    // Marcamos para no reintentar infinito
    config.__retry = true;

    if (!isRefreshing) {
      isRefreshing = true;
      const refreshToken = localStorage.getItem("refreshToken"); // ideal: cookie HttpOnly en el backend

      refreshPromise = fetch("/api/auth/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken })
      })
        .then(async (r) => {
          if (!r.ok) throw new Error("Refresh failed");
          const data = await r.json();
          sessionStorage.setItem("accessToken", data.accessToken);
          localStorage.setItem("refreshToken", data.refreshToken); // o dejarlo en cookie HttpOnly
          onRrefreshed(data.accessToken);
          return data.accessToken;
        })
        .catch((e) => {
          // Falló el refresh → logout
          sessionStorage.removeItem("accessToken");
          localStorage.removeItem("refreshToken");
          // Redirige a /login o estado no autenticado
          throw e;
        })
        .finally(() => {
          isRefreshing = false;
          refreshPromise = null;
        });
    }

    // Esperar a que el refresh termine y reintentar esta request con el nuevo access
    return new Promise((resolve, reject) => {
      subscribeTokenRefresh((newAccess) => {
        config.headers.Authorization = `Bearer ${newAccess}`;
        resolve(api(config));
      });
      if (refreshPromise) {
        refreshPromise.catch(reject);
      }
    });
  }
);

export default api;
