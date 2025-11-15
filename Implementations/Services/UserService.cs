using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;

namespace FullPost.Implementations.Services;

public class UserService : IUserService
{
    private readonly ICustomerRepo _customerRepo;
    private readonly IUserRepo _userRepo;
    private IEmailService _emailService;
    UserService(ICustomerRepo customerRepo, IUserRepo userRepo, IEmailService emailService)
    {
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _emailService = emailService;
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
    public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        var user = await _userRepo.Get(x => x.Email.Equals(request.Email));
        if (user != null)
        {
            var customer = await _customerRepo.Get(x => x.UserId == user.Id);
            customer.GoogleAccessToken = request.GoogleAccessToken;
            customer.GoogleTokenExpiry = request.GoogleTokenExpiry;
            await _customerRepo.Update(customer);
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
    public async Task<BaseResponse> ForgotPassword(string email)
    {
        var user = await _userRepo.Get(x => x.Email.Equals(email));
        if (user != null)
        {
            var resetLink = $"email={email}token={((await _customerRepo.Get(x => x.UserId == user.Id)).CustomerId)}time={DateTime.UtcNow.AddMinutes(10)}subToken={Guid.NewGuid().ToString().Substring(0,18)}";
            await _emailService.SendEmailAsync(email, "Password Reset", $"Click the link to reset your password. {resetLink}");
            return new BaseResponse()
            {
                Status = true,
                Message = $"Reset password mail sent to {email}."
            };
        }
        return new BaseResponse()
        {
            Status = false,
            Message = $"{email} does not exist. Signup to continue."
        };
    }
    public async Task<BaseResponse> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userRepo.Get(x => x.Email.Equals(request.Email));
        if (user != null && ((await _customerRepo.Get(x => x.UserId == user.Id))).CustomerId == request.Token)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _emailService.SendEmailAsync(request.Email, "Password Changed", $"Your password has been changed successfully.");
            return new BaseResponse()
            {
                Status = true,
                Message = $"Password changed!"
            };
        }
        return new BaseResponse()
        {
            Status = false,
            Message = $"Failed to reset password!"
        };
    }
}