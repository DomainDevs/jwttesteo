<template>
  <div class="login-container">
    <h2>Login</h2>
    <form @submit.prevent="login">
      <input
        type="text"
        v-model="username"
        placeholder="Username"
        required
      />
      <input
        type="password"
        v-model="password"
        placeholder="Password"
        required
      />
      <button type="submit">Login</button>
    </form>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </div>
</template>

<script>
import api from "@/api.js"; // ✅ usamos el mismo cliente

export default {
  data() {
    return {
      username: "",
      password: "",
      errorMessage: "",
    };
  },
  methods: {
    async login() {
      try {
        const response = await api.post("/auth/login", {
          username: this.username,
          password: this.password,
        });

        // ✅ Guardar tokens con los nombres correctos
        localStorage.setItem("accessToken", response.data.accessToken);
        localStorage.setItem("refreshToken", response.data.refreshToken);

        // ✅ Redirigir al dashboard
        this.$router.push("/dashboard");
      } catch (error) {
        console.error("Error en login:", error);
        this.errorMessage = error.response?.data || "Login failed";
      }
    },
  },
};
</script>

<style scoped>
.login-container {
  max-width: 400px;
  margin: 50px auto;
  padding: 20px;
  border: 1px solid #ccc;
  border-radius: 8px;
}
input {
  display: block;
  width: 100%;
  margin: 10px 0;
  padding: 8px;
}
button {
  width: 100%;
  padding: 10px;
  background: #42b983;
  border: none;
  color: white;
  font-size: 16px;
  cursor: pointer;
}
.error {
  color: red;
  margin-top: 10px;
}
</style>
