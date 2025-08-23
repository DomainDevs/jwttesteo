// Dashboard.vue
<template>
  <div class="dashboard-container">
    <h2>¡Bienvenido a tu Dashboard!</h2>
    <p>Has iniciado sesión con éxito.</p>
    <p v-if="data">Datos del API: {{ data }}</p>
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
    // Haz una llamada a un endpoint protegido
    const response = await api.get('/auth/protected');
    data.value = response.data;
  } catch (err) {
    console.error("Error al obtener datos protegidos:", err);
  }
});

const logout = async () => {
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      await api.post('/auth/logout', { refreshToken });
    }
    
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    
    router.push('/login');
  } catch (err) {
    console.error('Error al cerrar sesión:', err);
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    router.push('/login');
  }
};
</script>