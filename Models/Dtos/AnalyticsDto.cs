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
public class GetAnalyticsDto
{
    public int UserId { get; set; }
    public int NoOfPosts { get; set; }
    public int TotalReach { get; set; }
     public int TotalViews => 
        TwitterViews + TikTokViews + InstagramViews + FacebookViews + (int)YouTubeViews + LinkedInViews;
    public int TotalLikes =>
        TwitterLikes + TikTokLikes + InstagramLikes + FacebookReactions + (int)YouTubeLikes + LinkedInLikes; 
    public int TwitterFollowers { get; set; }
    public int TwitterViews { get; set; }
    public int TwitterLikes { get; set; }
    public int TikTokFollowers { get; set; }
    public int TikTokViews { get; set; }
    public int TikTokLikes { get; set; }
    public int InstagramFollowers { get; set; }
    public int InstagramViews { get; set; }
    public int InstagramLikes { get; set; }
    public int FacebookFollowers { get; set; }
    public int FacebookViews { get; set; }
    public int FacebookReactions { get; set; }
    public ulong YouTubeSubscribers { get; set; }
    public ulong YouTubeViews { get; set; }
    public ulong YouTubeLikes { get; set; }
    public int LinkedInConnections { get; set; }
    public int LinkedInViews { get; set; }
    public int LinkedInLikes { get; set; }
    public DateTime DateCreated { get; set; }
}
public class AnalyticsResponseModel : BaseResponse
{
    public int NoOfPosts { get; set; }
    public int TotalReach { get; set; }
     public int TotalViews { get; set; }
    public int TotalLikes { get; set; }
    public ICollection<GetAnalyticsDto> Data = new HashSet<GetAnalyticsDto>();
}