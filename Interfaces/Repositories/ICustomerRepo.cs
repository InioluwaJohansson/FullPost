using FullPost.Entities;
namespace FullPost.Interfaces.Respositories;

public interface ICustomerRepo : IRepo<Customer>
{
    Task<Customer?> GetById(int userId);
}