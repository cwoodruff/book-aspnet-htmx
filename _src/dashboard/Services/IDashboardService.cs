using ChinookDashboard.Models;

namespace ChinookDashboard.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
    Task<IEnumerable<GenreDistribution>> GetGenreDistributionAsync();
}
