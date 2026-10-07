using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IAuditService
{
    PageResponse<AuditLogResponse> GetAuditLogs(Guid userId, string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize);
    AuditSummaryResponse GetAuditSummary(Guid userId);
}
