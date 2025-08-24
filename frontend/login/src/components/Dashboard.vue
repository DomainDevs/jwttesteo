<template>
  <div class="dashboard-container">
    <h2>¡Bienvenido a tu Dashboard!</h2>
    <p>Has iniciado sesión con éxito.</p>

    <!-- Mostrar el dato del API -->
    <p v-if="data">Datos del API: {{ data.message }}</p>
    <p v-else>Cargando datos...</p>

    <!-- Botón de logout -->
    <button @click="logout" class="logout-button">Cerrar Sesión</button>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import api from '@/api.js';
import { jwtDecode } from 'jwt-decode';
import axios from 'axios';

const router = useRouter();
const data = ref(null);
let checkInterval = null;

// 👉 Verificar si un accessToken sigue válido
const isAccessTokenValid = (token) => {
  if (!token) return false;
  try {
    const decoded = jwtDecode(token);
    const currentTime = Date.now() / 1000;
    return decoded.exp > currentTime;
  } catch {
    return false;
  }
};

// 👉 Intentar refrescar el token
const tryRefreshToken = async () => {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) return false;

  try {
    const response = await axios.post('https://localhost:7047/api/auth/refresh', { refreshToken });

    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    localStorage.setItem('refreshExpiry', response.data.refreshExpiry);
    localStorage.setItem('refreshMaxExpiry', response.data.refreshMaxExpiry);

    console.log("🔄 Access token renovado con éxito");
    return true;
  } catch (err) {
    console.info("ℹ️ Token de refresh vencido:", err);
    return false;
  }
};

// 👉 Función para cargar datos protegidos
const loadProtectedData = async () => {
  try {
    const response = await api.get('/auth/protected');
    data.value = response.data;
  } catch (err) {
    console.error("Error al obtener datos protegidos:", err);
    router.push('/login');
  }
};

// 👉 Cerrar sesión (manual o automático)
const logout = async () => {
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      await api.post('/auth/logout', { refreshToken });
    }
  } catch (err) {
    console.error('Error al cerrar sesión en el servidor:', err);
  } finally {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('refreshExpiry');
    localStorage.removeItem('refreshMaxExpiry');

    router.push('/login');
  }
};

onMounted(async () => {
  let token = localStorage.getItem('accessToken');
  const maxExpiry = localStorage.getItem('refreshMaxExpiry');

  // Si no hay token o está vencido → intentar refrescar
  if (!token || !isAccessTokenValid(token)) {
    const refreshed = await tryRefreshToken();
    if (!refreshed) {
      router.push('/login');
      return;
    }
    token = localStorage.getItem('accessToken');
  }

  // Cargar datos protegidos
  await loadProtectedData();

  // Revisar cada 10 segundos si el token sigue válido
  checkInterval = setInterval(async () => {
    const currentToken = localStorage.getItem('accessToken');
    const refreshMaxExpiry = new Date(localStorage.getItem('refreshMaxExpiry'));
    console.log(`Fecha de expiración del token: ${refreshMaxExpiry}`); // Muestra la fecha y hora
    //console.log(`Fecha de expiración del token: ${expiryDate.toLocaleString()}`); // Muestra la fecha y hora

    // ⏳ Verificar expiración máxima
    if (Date.now() > refreshMaxExpiry.getTime()) {
      console.warn("⛔ La sesión alcanzó su tiempo máximo. Cerrando...");
      logout();
      return;
    }

    // ⚠️ Verificar expiración normal
    if (!isAccessTokenValid(currentToken)) {
      console.warn("⚠️ Token expirado, intentando refrescar...");
      const refreshed = await tryRefreshToken();
      if (!refreshed) {
        console.warn("❌ Refresh inválido, cerrando sesión...");
        logout();
      }
    }
  }, 10000);
});

onUnmounted(() => {
  if (checkInterval) clearInterval(checkInterval);
});
</script>

<style scoped>
.dashboard-container {
  max-width: 600px;
  margin: 2rem auto;
  padding: 1.5rem;
  background: #9B2828FF;
  border-radius: 8px;
  text-align: center;
  box-shadow: 0 2px 6px rgba(0,0,0,0.1);
}
.logout-button {
  margin-top: 1rem;
  padding: 0.6rem 1.2rem;
  background: #e63946;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}
.logout-button:hover {
  background: #d62828;
}
</style>
