using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FullPost.Entities;
using FullPost.Entities.Identity;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Tweetinvi.Models;

namespace FullPost.Implementations.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepo _customerRepo;
    private readonly IUserRepo _userRepo;
    private readonly Cloudinary _cloudinary;
    private readonly IEmailService _emailService;
    CustomerService(ICustomerRepo customerRepo, IUserRepo userRepo, IConfiguration config, IEmailService emailService)
    {
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _emailService = emailService;
        var cloudSettings = config.GetSection("CloudinarySettings").Get<CloudinarySettings>();

        var account = new Account(cloudSettings.CloudName, cloudSettings.ApiKey, cloudSettings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }
    public async Task<BaseResponse> CreateCustomer(CreateCustomerDto createCustomerDto)
    {
        var checkMail = await _userRepo.Get(x => x.Email.Equals(createCustomerDto.Email));
        if (createCustomerDto != null && checkMail == null)
        {
            var user = new User()
            {
                Email = createCustomerDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(createCustomerDto.Password)
            };
            await _userRepo.Create(user);
            var getUser = await _userRepo.Get(x => x.Email.Equals(createCustomerDto.Email));
            var customer = new Customer
            {
                UserId = getUser.Id,
                FirstName = createCustomerDto.FirstName ?? "",
                LastName = createCustomerDto.LastName ?? "",
                PictureUrl = "",
                GoogleId = createCustomerDto.GoogleId,
                GoogleAccessToken = createCustomerDto.GoogleAccessToken,
                GoogleRefreshToken = createCustomerDto.GoogleRefreshToken,
                GoogleTokenExpiry = createCustomerDto.GoogleTokenExpiry
            };
            await _customerRepo.Create(customer);
            await _emailService.SendEmailAsync(createCustomerDto.Email, "Account Created", "Your account has been successfully created!");
            return new BaseResponse
            {
                UserId = getUser.Id,
                Status = true,
                Message = "Account Created!"
            };
        }
        return new BaseResponse
        {
            Status = false,
            Message = "Account Already Exists!"
        };
    }
    public async Task<BaseResponse> CreateCustomerWithGoogle(CreateCustomerDto createCustomerDto)
    {
        var checkMail = await _userRepo.Get(x => x.Email.Equals(createCustomerDto.Email));
        if (createCustomerDto != null && checkMail == null)
        {
            var user = new User()
            {
                Email = createCustomerDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()) ?? ""
            };
            await _userRepo.Create(user);
            var getUser = await _userRepo.Get(x => x.Email.Equals(createCustomerDto.Email));
            var customer = new Customer
            {
                UserId = getUser.Id,
                FirstName = createCustomerDto.FirstName ?? "",
                LastName = createCustomerDto.LastName ?? "",
                PictureUrl = createCustomerDto.PictureUrl ?? "",
                GoogleId = createCustomerDto.GoogleId,
                GoogleAccessToken = createCustomerDto.GoogleAccessToken,
                GoogleRefreshToken = createCustomerDto.GoogleRefreshToken,
                GoogleTokenExpiry = createCustomerDto.GoogleTokenExpiry
            };
            await _customerRepo.Create(customer);
            await _emailService.SendEmailAsync(createCustomerDto.Email, "Account Created", "Your account has been successfully created!");
            return new BaseResponse
            {
                UserId = getUser.Id,
                Status = true,
                Message = "Account Created!"
            };
        }
        return new BaseResponse
        {
            Status = false,
            Message = "Account Already Exists!"
        };
    }
    public async Task<BaseResponse> DeleteAccount(int userId)
    {
        var customer = await _customerRepo.GetById(userId);
        if (customer == null)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Customer not found"
            };
        }
        await _customerRepo.Delete(customer);
        await _emailService.SendEmailAsync(customer.User!.Email, "Account Deleted", "Your account has been successfully deleted.");
        return new BaseResponse
        {
            Status = true,
            Message = "Account deleted successfully"
        };
    }

    public async Task<CustomerResponse> GetCustomerById(int userId)
    {
        var customer = await _customerRepo.GetById(userId);
        if (customer == null)
        {
            return new CustomerResponse
            {
                Status = false,
                Message = "Customer not found"
            };
        }

        var customerDto = new GetCustomerDto
        {
            Id = customer.Id,
            userId = customer.UserId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            UserName = customer.User.UserName,
            Email = customer.User.Email,
            PictureUrl = customer.PictureUrl,
            TwitterConnected = customer.TwitterAccessToken != null,
            FacebookConnected = customer.FacebookAccessToken != null,
            InstagramConnected = customer.InstagramAccessToken != null
        };

        return new CustomerResponse
        {
            Status = true,
            Data = customerDto
        };
    }

    public async Task<BaseResponse> UpdateCustomer(UpdateCustomerDto updateCustomerDto)
    {
        if (updateCustomerDto != null)
        {
            string imgUrl = null;
            if (updateCustomerDto.PictureUrl == null || updateCustomerDto.PictureUrl.Length == 0){}
            else {
                using var stream = updateCustomerDto.PictureUrl.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(updateCustomerDto.PictureUrl.FileName, stream),
                    Folder = "profile_pictures",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false,
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    imgUrl = uploadResult.SecureUrl.AbsoluteUri;
                }
                else
                {
                    throw new Exception("Cloudinary upload failed: " + uploadResult.Error?.Message);
                }
            }
            var customer = await _customerRepo.GetById(updateCustomerDto.userId);
            customer.User.Email = updateCustomerDto.Email ?? customer.User.Email;
            customer.FirstName = updateCustomerDto.FirstName ?? customer.FirstName;
            customer.LastName = updateCustomerDto.LastName ?? customer.LastName;
            customer.User.UserName = updateCustomerDto.UserName ?? customer.User.UserName;
            customer.PictureUrl = imgUrl ?? customer.PictureUrl;
            customer.TwitterUsername = updateCustomerDto.TwitterUsername ?? customer.TwitterUsername;
            customer.TwitterUserId = updateCustomerDto.TwitterUserId ?? customer.TwitterUserId;
            customer.TwitterAccessToken = updateCustomerDto.TwitterAccessToken ?? customer.TwitterAccessToken;
            customer.TwitterAccessSecret = updateCustomerDto.TwitterAccessSecret ?? customer.TwitterAccessSecret;
            customer.FacebookAccessToken = updateCustomerDto.FacebookAccessToken ?? customer.FacebookAccessToken;
            customer.FacebookPageId = updateCustomerDto.FacebookPageId ?? customer.FacebookPageId;
            customer.FacebookUserId = updateCustomerDto.FacebookUserId ?? customer.FacebookUserId;
            customer.FacebookPageName = updateCustomerDto.FacebookPageName ?? customer.FacebookPageName;
            customer.InstagramAccessToken = updateCustomerDto.InstagramAccessToken ?? customer.InstagramAccessToken;
            customer.InstagramUserId = updateCustomerDto.InstagramUserId ?? customer.InstagramUserId;
            customer.InstagramUsername = updateCustomerDto.InstagramUsername ?? customer.InstagramUsername;
            customer.TwitterTokenExpiry = updateCustomerDto.TwitterTokenExpiry ?? customer.TwitterTokenExpiry;
            customer.InstagramTokenExpiry = updateCustomerDto.InstagramTokenExpiry ?? customer.InstagramTokenExpiry;
            customer.FacebookTokenExpiry = updateCustomerDto.FacebookTokenExpiry ?? customer.FacebookTokenExpiry;
            await _customerRepo.Update(customer);
            return new BaseResponse
            {
                Status = true,
                Message = "Account Created!"
            };
        }
        return new BaseResponse
        {
            Status = false,
            Message = "Account Already Exists!"
        };
    }
}
