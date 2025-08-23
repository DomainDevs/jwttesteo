import { createRouter, createWebHistory } from 'vue-router'
import { jwtDecode } from 'jwt-decode' // Solo se usará para el Access Token
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

router.beforeEach((to, from, next) => {
  const accessToken = localStorage.getItem('accessToken');
  const refreshToken = localStorage.getItem('refreshToken');

  // Función para verificar si un token JWT (access token) es válido
  const isAccessTokenValid = (token) => {
    if (!token) {
      return false;
    }
    try {
      const decoded = jwtDecode(token);
      const currentTime = Date.now() / 1000;
      return decoded.exp > currentTime;
    } catch (e) {
      // El error de decodificación o expiración hace que el token sea inválido
      return false;
    }
  };

  const isAuthenticated = isAccessTokenValid(accessToken) && !!refreshToken;

  if (to.name === 'login' && isAuthenticated) {
    next({ name: 'dashboard' });
  } else if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: 'login' });
  } else {
    next();
  }
});

export default router;