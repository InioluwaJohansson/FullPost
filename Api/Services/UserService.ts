import apiClient from "../Client & Dtos/ApiClient";
import type { CreateCustomerDto, LoginResponse } from "../Client & Dtos/Dto";
export const authService = {
  signup: async (data: CreateCustomerDto): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>("/auth/signup", data);
    if (response.data.token) {
      localStorage.setItem("jwtToken", response.data.token);
    }
    return response.data;
  },

  login: async (email: string, password: string): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>("/auth/login", {
      email,
      password,
    });
    if (response.data.token) {
      localStorage.setItem("jwtToken", response.data.token);
    }
    return response.data;
  },

  googleSignup: async (googleAccessToken: string): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>(
      "/auth/google/signup",
      { googleAccessToken }
    );
    if (response.data.token) {
      localStorage.setItem("jwtToken", response.data.token);
    }
    return response.data;
  },

  googleLogin: async (googleAccessToken: string): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>(
      "/auth/google/login",
      { googleAccessToken }
    );
    if (response.data.token) {
      localStorage.setItem("jwtToken", response.data.token);
      localStorage.setItem("UserId", response.data.userId.toString());
      localStorage.setItem("loginData", JSON.stringify(response.data));
    }
    return response.data;
  },

  logout: () => {
    localStorage.removeItem("jwtToken");
  },
};
