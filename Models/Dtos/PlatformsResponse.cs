namespace FullPost.Models.DTOs;
public class TwitterTweetResponse
{
    public string Id { get; set; }
    public string Text { get; set; }
    public TwitterMedia Media { get; set; }
    public TwitterUser User { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class TwitterMedia
{
    public List<string> Photos { get; set; }
    public List<string> Videos { get; set; }
}
public class TwitterUser
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string ProfileImageUrl { get; set; }
}
public class FacebookPostResponse
{
    public string Id { get; set; }
    public string Message { get; set; }
    public List<FacebookMediaItem> Media { get; set; } = new();
    public DateTime CreatedTime { get; set; }
}
public class FacebookMediaItem
{
    public string MediaType { get; set; } // photo, video, share, album
    public string MediaUrl { get; set; }
    public string ThumbnailUrl { get; set; }
}
public class TikTokVideoResponse
{
    public string VideoId { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
    public string CoverImageUrl { get; set; }
    public string PlayUrl { get; set; }
    public TikTokAuthor Author { get; set; }
}
public class TikTokAuthor
{
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string AvatarUrl { get; set; }
}
public class YouTubeVideoResponse
{
    public string VideoId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public YouTubeThumbnails Thumbnails { get; set; }
    public DateTime PublishedAt { get; set; }
}
public class YouTubeThumbnails
{
    public string Default { get; set; }
    public string Medium { get; set; }
    public string High { get; set; }
}
public class LinkedInPostResponse
{
    public string Urn { get; set; }
    public string Text { get; set; }
    public LinkedInMedia Media { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class LinkedInMedia
{
    public string MediaType { get; set; } // IMAGE, VIDEO, ARTICLE
    public string Url { get; set; }
}
public class InstagramPostResponse
{
    public string Id { get; set; }
    public string Caption { get; set; }
    public DateTime Timestamp { get; set; }
    public List<InstagramMediaItem> Media { get; set; } = new();
}
public class InstagramMediaItem
{
    public string Id { get; set; }
    public string MediaType { get; set; } // IMAGE, VIDEO
    public string MediaUrl { get; set; }
    public string ThumbnailUrl { get; set; }
}
public class TwitterResponseModel : BaseResponse
{
    public ICollection<TwitterTweetResponse> Data { get; set; } = new HashSet<TwitterTweetResponse>();
}
public class FacebookResponseModel : BaseResponse
{
    public ICollection<FacebookPostResponse> Data { get; set; } = new HashSet<FacebookPostResponse>();
}
public class InstagramResponseModel : BaseResponse
{
    public ICollection<InstagramPostResponse> Data { get; set; } = new HashSet<InstagramPostResponse>();
}
public class TikTokResponseModel : BaseResponse
{
    public ICollection<TikTokVideoResponse> Data { get; set; } = new HashSet<TikTokVideoResponse>();
}
public class YouTubeResponseModel : BaseResponse
{
    public ICollection<YouTubeVideoResponse> Data { get; set; } = new HashSet<YouTubeVideoResponse>();
}
public class LinkedInResponseModel : BaseResponse
{
    public ICollection<LinkedInPostResponse> Data { get; set; } = new HashSet<LinkedInPostResponse>();
}