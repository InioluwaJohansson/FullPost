using FullPost.Contracts;

namespace FullPost.Entities.Identity;

public class Role : AuditableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
}
