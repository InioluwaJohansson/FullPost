using FullPost.Contracts;
namespace FullPost.Entities;
public class Analytic : AuditableEntity
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
}