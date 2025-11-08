import apiClient from "../Client & Dtos/ApiClient";
import { BaseResponse, CreateSubscriptionDto, SubscriptionPlanResponseModel, UserSubscriptionResponseModel } from "../Client & Dtos/Dto";

export const createPlan = async (payload: CreateSubscriptionDto) => {
  const response = await apiClient.post<BaseResponse>(
    `/create`,
    payload,
  );
  return response.data;
};

/**
 * Fetch all available subscription plans
 */
export const getPlans = async () => {
  const response = await apiClient.get<SubscriptionPlanResponseModel>(
    `/plans`,
  );
  return response.data;
};

/**
 * Subscribe a user to a plan
 */
export const subscribeToPlan = async (userId: number, planId: number) => {
  const response = await apiClient.post<BaseResponse>(
    `/subscribe?userId=${userId}&planId=${planId}`,
    {},
  );
  return response.data;
};

/**
 * Cancel a subscription using its code
 */
export const cancelSubscription = async (subscriptionCode: string) => {
  const response = await apiClient.post<BaseResponse>(
    `/cancel/${subscriptionCode}`,
    {},
  );
  return response.data;
};

/**
 * Get all subscriptions for a user
 */
export const getUserSubscriptions = async (userId: number) => {
  const response = await apiClient.get<UserSubscriptionResponseModel>(
    `/user/${userId}`,
  );
  return response.data;
};