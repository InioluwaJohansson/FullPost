using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface IAnalyticsService
{
    Task GetSubscribersAnalyticsData();
    Task<AnalyticsResponseModel> GetUserAnalytics(int userId);
}