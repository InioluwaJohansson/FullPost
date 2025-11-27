namespace FullPost.Models.DTOs;

public class PlatformStats
{
    public int Followers { get; set; }
    public int Views { get; set; }
    public int Likes { get; set; }
}
public class YouTubePlatformStats
{
    public ulong Followers { get; set; }
    public ulong Views { get; set; }
    public ulong Likes { get; set; }
}
