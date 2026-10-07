using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public DashboardSummaryResponse GetDashboardSummary(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            var items = _vaultItems.Where(item => item.UserId == userId).ToArray();
            var categories = _categories.Where(category => category.UserId == userId).ToArray();
            return new DashboardSummaryResponse(
                [
                    new DashboardStatResponse("Total passwords", items.Length),
                    new DashboardStatResponse("Strong passwords", items.Count(item => item.PasswordStrengthScore >= 3)),
                    new DashboardStatResponse("Need attention", items.Count(item => item.PasswordStrengthScore < 2))
                ],
                items.OrderByDescending(item => item.UpdatedAt).Take(5)
                    .Select(item => new RecentVaultItemResponse(item.Id, item.ServiceName, item.Username, item.IsFavorite, item.ColorTone, item.UpdatedAt)).ToArray(),
                categories.Select(category => new DashboardCategoryResponse(category.Id, category.Name, category.Icon,
                    items.Count(item => item.CategoryId == category.Id))).ToArray());
        }
    }
}
