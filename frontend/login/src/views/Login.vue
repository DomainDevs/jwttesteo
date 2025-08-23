<template>
  <div class="login-container">
    <form @submit.prevent="login" class="login-form">
      <h2>Iniciar Sesión</h2>
      
      <div class="form-group">
        <label for="username">Usuario</label>
        <input 
          id="username" 
          type="text" 
          v-model="username" 
          required 
          autocomplete="username"
        />
      </div>

      <div class="form-group">
        <label for="password">Contraseña</label>
        <input 
          id="password" 
          type="password" 
          v-model="password" 
          required 
          autocomplete="current-password"
        />
      </div>
      
      <p v-if="error" class="error-message">{{ error }}</p>

      <button type="submit" class="submit-button">Acceder</button>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import api from '@/api.js'; // Importa la instancia de axios que creamos antes

const username = ref('');
const password = ref('');
const error = ref(null);

const router = useRouter();

const login = async () => {
  try {
    error.value = null;
    const response = await api.post('/auth/login', {
      username: username.value,
      password: password.value
    });

    const { accessToken, refreshToken } = response.data;

    // Almacena los tokens de forma segura
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);

    // Redirige al usuario a la página de inicio (dashboard)
    router.push('/dashboard');
    
  } catch (err) {
    if (err.response && err.response.status === 401) {
      error.value = 'Credenciales inválidas. Por favor, inténtalo de nuevo.';
    } else {
      error.value = 'Ha ocurrido un error. Por favor, inténtalo más tarde.';
    }
    console.error('Error de inicio de sesión:', err);
  }
};
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #f4f7f9;
}

.login-form {
  background: #fff;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
}

h2 {
  text-align: center;
  margin-bottom: 1.5rem;
  color: #333;
}

.form-group {
  margin-bottom: 1rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: bold;
  color: #555;
}

input[type="text"],
input[type="password"] {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-sizing: border-box;
}

.error-message {
  color: #e74c3c;
  margin-top: 1rem;
  text-align: center;
}

.submit-button {
  width: 100%;
  padding: 0.75rem;
  border: none;
  border-radius: 4px;
  background-color: #007bff;
  color: white;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.submit-button:hover {
  background-color: #0056b3;
}
</style>