using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public PageResponse<AuditLogResponse> GetAuditLogs(Guid userId, string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            if (from is { } startDate && to is { } endDate && startDate > endDate)
            {
                throw BadRequest("INVALID_DATE_RANGE", "from must be earlier than or equal to to.");
            }
            IEnumerable<AuditRecord> query = _auditLogs.Where(entry => entry.UserId == userId);
            if (severity is not null)
            {
                if (severity is not ("Success" or "Review" or "Critical")) throw BadRequest("INVALID_SEVERITY", "severity must be Success, Review, or Critical.");
                query = query.Where(entry => entry.Severity == severity);
            }
            if (eventType is not null) query = query.Where(entry => entry.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
            if (from is { } start) query = query.Where(entry => entry.CreatedAt >= start);
            if (to is { } end) query = query.Where(entry => entry.CreatedAt <= end);
            return Paginate(query.OrderByDescending(entry => entry.CreatedAt).Select(entry =>
                new AuditLogResponse(entry.Id, entry.EventType, entry.Detail, entry.Severity, entry.CreatedAt)).ToArray(), page, pageSize);
        }
    }
    public AuditSummaryResponse GetAuditSummary(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            var month = DateTimeOffset.UtcNow;
            var events = _auditLogs.Where(entry => entry.UserId == userId && entry.CreatedAt.Year == month.Year && entry.CreatedAt.Month == month.Month).ToArray();
            return new AuditSummaryResponse(events.Length, events.Count(entry => entry.Severity == "Success"),
                events.Count(entry => entry.Severity == "Review"), events.Count(entry => entry.Severity == "Critical"));
        }
    }
}
