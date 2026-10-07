using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IDashboardService
{
    DashboardSummaryResponse GetDashboardSummary(Guid userId);
}
