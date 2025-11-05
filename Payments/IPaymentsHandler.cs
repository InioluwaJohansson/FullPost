using System.Net;
using FullPost.Entities;
using FullPost.Models.DTOs;

namespace FullPost.Payments;
public interface IPaymentsHandler
{
    Task<HttpStatusCode> PaystackPayment(PayStackPackage package);
    Task<HttpStatusCode> VisaPayment(Card card, decimal amount);
}
