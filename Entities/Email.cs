using FullPost.Contracts;
using FullPost.Models;

namespace FullPost.Entities;
public class Email : AuditableEntity
{
    public string ReceiverName { get; set; }
    public string ReceiverEmail { get; set; }
    public string Message { get; set; }
    public string Subject { get; set; }
    public string AttachmentUrl { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; }
}
