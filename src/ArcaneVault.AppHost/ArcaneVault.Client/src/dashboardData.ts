export type IconTone = 'dark' | 'blue' | 'green';

export interface NavigationItem {
  label: string;
  icon: string;
}

export interface VaultCategory {
  name: string;
  icon: string;
  passwordCount: number;
}

export interface VaultStat {
  label: string;
  value: string;
}

export interface RecentPassword {
  service: string;
  initial: string;
  account: string;
  updated: string;
  favorite: boolean;
  tone: IconTone;
}

export const workspaceNavigation: NavigationItem[] = [
  { label: 'Dashboard', icon: '⌂' },
  { label: 'All passwords', icon: '▣' },
  { label: 'Categories', icon: '◈' },
  { label: 'Shared vaults', icon: '♢' },
];

export const toolNavigation: NavigationItem[] = [
  { label: 'Password generator', icon: '⌁' },
  { label: 'Settings', icon: '⚙' },
];

export const vaultCategories: VaultCategory[] = [
  { name: 'Browser', icon: '◎', passwordCount: 18 },
  { name: 'Mobile app', icon: '✣', passwordCount: 12 },
  { name: 'Payment', icon: '▣', passwordCount: 6 },
];

export const vaultStats: VaultStat[] = [
  { value: '36', label: 'Total passwords' },
  { value: '12', label: 'Strong passwords' },
  { value: '03', label: 'Need attention' },
];

export const recentPasswords: RecentPassword[] = [
  { service: 'Netflix', initial: 'N', account: 'hello@designmonk.com', updated: 'Updated today', favorite: true, tone: 'dark' },
  { service: 'Messenger', initial: 'M', account: 'hello@designmonk.com', updated: 'Updated yesterday', favorite: true, tone: 'blue' },
  { service: 'Spotify', initial: 'S', account: 'hello@designmonk.com', updated: 'Updated Sep 22', favorite: false, tone: 'green' },
];