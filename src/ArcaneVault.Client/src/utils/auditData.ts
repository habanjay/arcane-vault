export type AuditSeverity = 'Success' | 'Review' | 'Critical';

export interface AuditEvent {
  id: number;
  event: string;
  detail: string;
  actor: string;
  timestamp: string;
  severity: AuditSeverity;
}

export const auditEvents: AuditEvent[] = [
  { id: 1, event: 'Password updated', detail: 'Netflix', actor: 'You', timestamp: 'Today, 09:42', severity: 'Success' },
  { id: 2, event: 'New sign-in', detail: 'Chrome on Windows', actor: 'You', timestamp: 'Yesterday, 18:20', severity: 'Review' },
  { id: 3, event: 'Vault shared', detail: 'Project Atlas', actor: 'You', timestamp: 'Sep 24, 14:08', severity: 'Success' },
  { id: 4, event: 'Failed sign-in', detail: 'Unknown device', actor: 'Unknown', timestamp: 'Sep 23, 03:16', severity: 'Critical' },
  { id: 5, event: 'Password viewed', detail: 'Amazon Prime', actor: 'You', timestamp: 'Sep 22, 11:35', severity: 'Success' },
  { id: 6, event: 'Recovery email changed', detail: 'Account security', actor: 'You', timestamp: 'Sep 18, 16:50', severity: 'Review' },
];