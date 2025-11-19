using FullPost.Models.DTOs;

namespace FullPost.Interfaces.Services;

public interface IUserService
{
    Task<LoginResponse> Login(string email, string password);
    Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
    Task<BaseResponse> ForgotPassword(string email);
    Task<BaseResponse> ResetPassword(ResetPasswordRequest request);
    Task<bool> CheckUserName(string userName, int userId);
}