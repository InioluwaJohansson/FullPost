using FullPost.Models.DTOs;
namespace FullPost.Authentication;
public interface IJWTAuthentication
{
    string GenerateToken(LoginResponse loginResponse);
}
