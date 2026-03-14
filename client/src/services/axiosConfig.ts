import axios from "axios";

// Create an axios instance
const api = axios.create({
  baseURL: "http://localhost:5156/api",
});

// Automatically attach token if available
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default api;
