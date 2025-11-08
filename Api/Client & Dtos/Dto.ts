import { createCustomer } from "../../Api_Url";

export interface BaseResponse {
  success: boolean;
  message: string;
}

export interface CreateCustomerDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  pictureUrl?: string;
  googleId?: string;
  googleAccessToken?: string;
  googleRefreshToken?: string;
  googleTokenExpiry?: Date;
}

export interface UpdateCustomerDto {
  userId: number;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  pictureUrl?: File;
}

export interface GetCustomerDto {
  id: number;
  userId: number;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  pictureUrl?: string;
  googleConnected: boolean;
  twitterConnected: boolean;
  facebookConnected: boolean;
  instagramConnected: boolean;
  youTubeConnected: boolean;
  tikTokConnected: boolean;
  linkedInConnected: boolean;
}

export interface CreatePostDto {
  userId: number;
  caption: string;
  mediaFiles?: File[];
  platforms?: string[];
}

export interface EditPostDto {
  postId: string;
  userId: number;
  newCaption: string;
  newMediaFiles?: File[];
}

export interface GetPostDto {
  platform: string;
  id: string;
  text: string;
  mediaUrl?: string;
  createdAt: Date;
  permalink: string;
}

export interface PostsResponseModel extends BaseResponse {
  data: Record<string, any>; // Dictionary<string, object>
}

export interface CreateSubscriptionDto {
  name: string;
  amount: number;
  interval: string;
  description?: string;
}

export interface SubscriptionDto {
  id: number;
  name: string;
  price: number;
  interval: string;
  description?: string;
}

export interface UserSubscriptionDto {
  id: number;
  plan: SubscriptionDto;
  startDate: Date;
  endDate?: Date | null;
  isActive: boolean;
  paystackSubscriptionCode?: string;
  paystackCustomerCode?: string;
}

export interface UserSubscriptionResponseModel extends BaseResponse {
  data: UserSubscriptionDto[];
}

export interface SubscriptionPlanResponseModel extends BaseResponse {
  data: SubscriptionDto[];
}

export interface LoginResponse extends BaseResponse {
  userId: number;
  userName: string;
  email: string;
  token: string;
}

