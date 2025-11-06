using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Entities;
namespace FullPost.Interfaces.Services;

public interface ICustomerService
{
    Task<BaseResponse> CreateCustomer(CreateCustomerDto createCustomerDto);
    Task<BaseResponse> UpdateCustomer(UpdateCustomerDto updateCustomerDto);
    Task<CustomerResponse> GetCustomerById(int userId);
    Task<BaseResponse> DeleteAccount(int userId);
}