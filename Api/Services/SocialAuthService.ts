import apiClient from "../Client & Dtos/ApiClient";

const getAuthHeader = () => {
  const token = localStorage.getItem("jwtToken");
  return token ? { Authorization: `Bearer ${token}` } : {};
};
export const connectTwitter = async (): Promise<void> => {
  try {
    const response = await apiClient.get(`/twitter/connect`, {
      headers: getAuthHeader(),
    });
    window.location.href = response.request.responseURL;
  } catch (error) {
    console.error("Twitter connect failed:", error);
  }
};

export const handleTwitterCallback = async (oauthVerifier: string, userId: number) => {
  try {
    const response = await apiClient.get(
      `/twitter/callback?oauth_verifier=${oauthVerifier}&userId=${userId}`,
      { headers: getAuthHeader() }
    );
    return response.data;
  } catch (error) {
    console.error("Twitter callback failed:", error);
    throw error;
  }
};

export const connectFacebook = async (): Promise<void> => {
  try {
    const response = await apiClient.get(`/facebook/connect`, {
      headers: getAuthHeader(),
    });
    window.location.href = response.request.responseURL;
  } catch (error) {
    console.error("Facebook connect failed:", error);
  }
};

export const handleFacebookCallback = async (code: string, userId: number) => {
  try {
    const response = await apiClient.get(
      `/facebook/callback?code=${code}&userId=${userId}`,
      { headers: getAuthHeader() }
    );
    return response.data;
  } catch (error) {
    console.error("Facebook callback failed:", error);
    throw error;
  }
};

export const connectInstagram = async (): Promise<void> => {
  try {
    const response = await apiClient.get(`/instagram/connect`, {
      headers: getAuthHeader(),
    });
    window.location.href = response.request.responseURL;
  } catch (error) {
    console.error("Instagram connect failed:", error);
  }
};

export const handleInstagramCallback = async (code: string, userId: number) => {
  try {
    const response = await apiClient.get(
      `/instagram/callback?code=${code}&userId=${userId}`,
      { headers: getAuthHeader() }
    );
    return response.data;
  } catch (error) {
    console.error("Instagram callback failed:", error);
    throw error;
  }
};

export const connectYouTube = async (): Promise<void> => {
  try {
    const response = await apiClient.get(`/youtube/connect`, {
      headers: getAuthHeader(),
    });
    window.location.href = response.request.responseURL;
  } catch (error) {
    console.error("YouTube connect failed:", error);
  }
};

export const handleYouTubeCallback = async (code: string, userId: number) => {
  try {
    const response = await apiClient.get(
      `/youtube/callback?code=${code}&userId=${userId}`,
      { headers: getAuthHeader() }
    );
    return response.data;
  } catch (error) {
    console.error("YouTube callback failed:", error);
    throw error;
  }
};

export const connectTikTok = async (): Promise<void> => {
  try {
    const response = await apiClient.get(`/connect/tiktok`, {
      headers: getAuthHeader(),
    });
    window.location.href = response.request.responseURL;
  } catch (error) {
    console.error("TikTok connect failed:", error);
  }
};

export const handleTikTokCallback = async (code: string, userId: number) => {
  try {
    const response = await apiClient.get(
      `/tiktok/callback?code=${code}&userId=${userId}`,
      { headers: getAuthHeader() }
    );
    return response.data;
  } catch (error) {
    console.error("TikTok callback failed:", error);
    throw error;
  }
};