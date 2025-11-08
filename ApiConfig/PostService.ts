import apiClient from "./Client & Dtos/ApiClient";
import type {
  CreatePostDto,
  EditPostDto,
  PostsResponseModel,
} from "../types/dto";

export const postService = {
  createPost: async (data: CreatePostDto): Promise<PostsResponseModel> => {
    const formData = new FormData();
    formData.append("UserId", data.userId.toString());
    formData.append("Caption", data.caption);

    data.mediaFiles?.forEach((file) => formData.append("MediaFiles", file));
    data.platforms?.forEach((platform) =>
      formData.append("Platforms", platform)
    );

    const response = await apiClient.post<PostsResponseModel>(
      "/posts/create",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return response.data;
  },

  editPost: async (data: EditPostDto): Promise<PostsResponseModel> => {
    const formData = new FormData();
    formData.append("PostId", data.postId);
    formData.append("UserId", data.userId.toString());
    formData.append("NewCaption", data.newCaption);

    data.newMediaFiles?.forEach((file) =>
      formData.append("NewMediaFiles", file)
    );

    const response = await apiClient.put<PostsResponseModel>(
      "/posts/edit",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return response.data;
  },

  getAllPosts: async (userId: number): Promise<PostsResponseModel> => {
    const response = await apiClient.get<PostsResponseModel>(
      `/posts/user/${userId}`
    );
    return response.data;
  },

  deletePost: async (postId: string): Promise<PostsResponseModel> => {
    const response = await apiClient.delete<PostsResponseModel>(
      `/posts/${postId}`
    );
    return response.data;
  },
};
