using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;

namespace FullPost.Implementations.Services;

public class UserService : IUserService
{
    ICustomerRepo _customerRepo;
    IUserRepo _userRepo;
    UserService(ICustomerRepo customerRepo, IUserRepo userRepo)
    {
        _customerRepo = customerRepo;
        _userRepo = userRepo;
    }
    public async Task<LoginResponse> Login(string email, string password)
    {
        var user = await _userRepo.Get(x => x.Email.Equals(email));
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            return new LoginResponse()
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Status = true
            };
        }
        return new LoginResponse()
        {
            Status = false
        };
    }
}