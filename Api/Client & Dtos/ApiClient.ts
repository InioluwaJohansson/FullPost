const BASE_URL = "https://localhost:7275";

import axios, { AxiosRequestConfig } from "axios";

const apiClient = axios.create({
  baseURL: BASE_URL,
});

apiClient.interceptors.request.use((config: AxiosRequestConfig) => {
  const token = localStorage.getItem("jwtToken");
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
export { BASE_URL };
