import axios from "axios";

// ✅ Base URL of your .NET backend
const BASE_URL = import.meta.env.VITE_API_URL || "https://localhost:5001/api";

// ✅ Create Axios instance
const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// ✅ Interceptor for adding JWT from localStorage
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("jwtToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
