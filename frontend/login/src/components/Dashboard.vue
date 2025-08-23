<!-- Dashboard.vue -->
<template>
  <div class="dashboard-container">
    <h2>¡Bienvenido a tu Dashboard!</h2>
    <p>Has iniciado sesión con éxito.</p>

    <!-- Mostrar el dato del API -->
    <p v-if="data">Datos del API: {{ data }}</p>
    <p v-else>Cargando datos...</p>

    <!-- Botón de logout -->
    <button @click="logout" class="logout-button">Cerrar Sesión</button>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import api from '@/api.js';

const router = useRouter();
const data = ref(null);

onMounted(async () => {
  try {
    // Llamada al endpoint protegido
    const response = await api.get('/auth/protected');
    data.value = response.data;
  } catch (err) {
    console.error("Error al obtener datos protegidos:", err);
    // Si hay error 401 no autorizado, Axios interceptor ya habrá intentado refrescar
    // pero si falla el refresh, redirigimos al login
    router.push('/login');
  }
});

const logout = async () => {
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      await api.post('/auth/logout', { refreshToken });
    }
  } catch (err) {
    console.error('Error al cerrar sesión en el servidor:', err);
  } finally {
    // Limpiar tokens siempre, aunque falle
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    router.push('/login');
  }
};
</script>

<style scoped>
.dashboard-container {
  max-width: 600px;
  margin: 2rem auto;
  padding: 1.5rem;
  background: #856868FF;
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
