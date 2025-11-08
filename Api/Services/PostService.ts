import apiClient from "../Client & Dtos/ApiClient";
import type {
  CreatePostDto,
  EditPostDto,
  PostsResponseModel,
} from "../Client & Dtos/Dto";

/**
 * Service for interacting with the backend PostController
 * (.NET 8 API: /FullPost/Post)
 */
export const postService = {
  async createPost(data: CreatePostDto): Promise<PostsResponseModel> {
    const formData = new FormData();
    formData.append("UserId", data.userId.toString());
    formData.append("Caption", data.caption);

    data.mediaFiles?.forEach((file) => formData.append("MediaFiles", file));

    data.platforms?.forEach((platform) =>
      formData.append("Platforms", platform)
    );

    const response = await apiClient.post<PostsResponseModel>(
      "/FullPost/Post/create",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return response.data;
  },

  async editPost(data: EditPostDto): Promise<PostsResponseModel> {
    const formData = new FormData();
    formData.append("PostId", data.postId);
    formData.append("UserId", data.userId.toString());
    formData.append("NewCaption", data.newCaption);

    data.newMediaFiles?.forEach((file) =>
      formData.append("NewMediaFiles", file)
    );

    const response = await apiClient.put<PostsResponseModel>(
      `/FullPost/Post/edit/${data.postId}`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return response.data;
  },

  async deletePost(postId: string, userId: number): Promise<PostsResponseModel> {
    const response = await apiClient.delete<PostsResponseModel>(
      `/FullPost/Post/delete/${postId}`,
      { params: { userId } }
    );
    return response.data;
  },

  async getAllPosts(userId: number): Promise<PostsResponseModel> {
    const response = await apiClient.get<PostsResponseModel>(
      `/FullPost/Post/allposts/${userId}`
    );
    return response.data;
  },
};
