namespace ArcaneVault.Server.Dtos;

/// <summary>Represents an audit event.</summary>
public sealed record AuditLogResponse(Guid Id, string EventType, string Detail, string Severity, DateTimeOffset CreatedAt);

/// <summary>Contains counts displayed in the audit summary.</summary>
public sealed record AuditSummaryResponse(int EventsThisMonth, int SuccessfulEvents, int NeedsReview, int CriticalEvents);
