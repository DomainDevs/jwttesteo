import { createRouter, createWebHistory } from 'vue-router'
import { jwtDecode } from 'jwt-decode'
import axios from 'axios'
import HomeView from '../views/HomeView.vue'
import Login from '../views/Login.vue'
import Dashboard from '../components/Dashboard.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/login',
      name: 'login',
      component: Login,
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: Dashboard,
      meta: { requiresAuth: true }
    },
  ],
})

// 👉 Función para verificar access token
const isAccessTokenValid = (token) => {
  if (!token) return false;
  try {
    const decoded = jwtDecode(token);
    const currentTime = Date.now() / 1000;
    return decoded.exp > currentTime;
  } catch (e) {
    return false;
  }
};

router.beforeEach(async (to, from, next) => {
  const accessToken = localStorage.getItem('accessToken');
  const refreshToken = localStorage.getItem('refreshToken');
  const refreshMaxExpiry = localStorage.getItem('refreshMaxExpiry');

  // 1️⃣ Validar que la sesión no haya alcanzado su límite máximo
  if (refreshMaxExpiry && Date.now() > new Date(refreshMaxExpiry).getTime()) {
    console.warn("⛔ La sesión alcanzó su tiempo máximo. Cerrando...");
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('refreshExpiry');
    localStorage.removeItem('refreshMaxExpiry');
    if (to.meta.requiresAuth) {
      return next({ name: 'login' });
    }
  }

  let finalAccessToken = accessToken;

  // 2️⃣ Si el access token está vencido pero existe refresh token → pedir uno nuevo
  if (!isAccessTokenValid(accessToken) && refreshToken) {
    try {
      const response = await axios.post('https://localhost:7047/api/auth/refresh', {
        refreshToken
      });

      // Guardar nuevos tokens
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      localStorage.setItem('refreshExpiry', response.data.refreshExpiry);
      localStorage.setItem('refreshMaxExpiry', response.data.refreshMaxExpiry);

      finalAccessToken = response.data.accessToken;
    } catch (err) {
      console.error("Error al refrescar token:", err);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('refreshExpiry');
      localStorage.removeItem('refreshMaxExpiry');
    }
  }

  // 3️⃣ Verificar autenticación
  const isAuthenticated = isAccessTokenValid(finalAccessToken) && !!refreshToken;

  if (to.name === 'login' && isAuthenticated) {
    next({ name: 'dashboard' });
  } else if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: 'login' });
  } else {
    next();
  }
});

export default router;
