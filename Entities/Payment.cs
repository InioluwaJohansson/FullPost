using FullPost.Contracts;
using FullPost.Entities;
using FullPost.Models.DTOs;

namespace FullPost.Entities;

public class Payment : AuditableEntity
{
    public string ReferenceNumber { get; set; }
    public int CustomerId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal StoreTaxes { get; set; }
    public DateTime DateOfPayment { get; set; }
}
