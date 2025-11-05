using FullPost.Contracts;
using FullPost.Entities.Identity;
using FullPost.Models.Enums;

namespace FullPost.Entities;

public class Staff: AuditableEntity
{
    public UserDetails UserDetails { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
    public string StaffNo { get; set; }
}
