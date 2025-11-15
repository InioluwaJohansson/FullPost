import apiClient, { BASE_URL } from "../Client & Dtos/ApiClient";
import { BaseResponse, CreateCustomerDto, CustomerResponse, UpdateCustomerDto } from "../Client & Dtos/Dto";

export const createCustomer = async (data: CreateCustomerDto) => {
  const formData = new FormData();
  Object.entries(data).forEach(([key, value]) => {
    if (value !== undefined && value !== null)
      formData.append(key, value as any);
  });

  const response = await apiClient.post<BaseResponse>(
    `/CreateCustomer`,
    formData
  );
  return response.data;
};

export const redirectToGoogleSignup = () => {
  window.location.href = `${BASE_URL}/google/signup`;
};

export const handleGoogleSignupCallback = async (code: string) => {
  const response = await apiClient.get<any>(
    `/google/signup/callback?code=${code}`
  );
  return response.data;
};

export const updateCustomer = async (data: UpdateCustomerDto) => {
  const formData = new FormData();
  Object.entries(data).forEach(([key, value]) => {
    if (value !== undefined && value !== null)
      formData.append(key, value as any);
  });

  const response = await apiClient.put<BaseResponse>(
    `/UpdateCustomer`,
    formData,
  );
  return response.data;
};

export const getCustomerById = async (userId: number) => {
  const response = await apiClient.get<CustomerResponse>(
    `/GetCustomerById?userId=${userId}`,
  );
  return response.data;
};

export const deleteAccount = async (userId: number) => {
  const response = await apiClient.put<BaseResponse>(
    `/DeleteAccount?userId=${userId}`,
    {}
  );
  return response.data;
};