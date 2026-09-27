export type IconTone = 'dark' | 'blue' | 'green' | 'light';

export interface NavigationItem {
  label: string;
  icon: string;
  href: string;
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

export interface VaultPassword {
  service: string;
  initial: string;
  account: string;
  favorite: boolean;
  tone: IconTone;
  category: 'Personal' | 'Work' | 'Finance';
}

export type CategoryTone = 'pink' | 'blue' | 'green' | 'orange';

export interface PasswordCategory {
  slug: string;
  name: string;
  icon: string;
  tone: CategoryTone;
  passwordCount: number;
}

export const workspaceNavigation: NavigationItem[] = [
  { label: 'Dashboard', icon: '⌂', href: '#dashboard' },
  { label: 'All passwords', icon: '▣', href: '#vaults' },
  { label: 'Categories', icon: '◈', href: '#categories' },
];

export const toolNavigation: NavigationItem[] = [
  { label: 'Password generator', icon: '⌁', href: '#generator' },
  { label: 'Settings', icon: '⚙', href: '#settings' },
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

export const vaultPasswords: VaultPassword[] = [
  { service: 'Amazon Prime', initial: 'a', account: 'hello@designmonk.com', favorite: true, tone: 'dark', category: 'Personal' },
  { service: 'Gmail', initial: 'M', account: 'hello@designmonk.com', favorite: false, tone: 'light', category: 'Work' },
  { service: 'Messenger', initial: 'M', account: 'hello@designmonk.com', favorite: true, tone: 'blue', category: 'Personal' },
  { service: 'Udemy', initial: 'u', account: 'hello@designmonk.com', favorite: true, tone: 'light', category: 'Work' },
  { service: 'Netflix', initial: 'N', account: 'hello@designmonk.com', favorite: false, tone: 'light', category: 'Personal' },
  { service: 'Coursera', initial: 'C', account: 'hello@designmonk.com', favorite: true, tone: 'light', category: 'Work' },
];

export const passwordCategories: PasswordCategory[] = [
  { slug: 'personal', name: 'Personal', icon: '♡', tone: 'pink', passwordCount: 12 },
  { slug: 'work', name: 'Work', icon: '▣', tone: 'blue', passwordCount: 9 },
  { slug: 'finance', name: 'Finance', icon: '◈', tone: 'green', passwordCount: 8 },
  { slug: 'social', name: 'Social media', icon: '✣', tone: 'orange', passwordCount: 7 },
  { slug: 'shopping', name: 'Shopping', icon: '◇', tone: 'pink', passwordCount: 6 },
  { slug: 'travel', name: 'Travel', icon: '⌁', tone: 'blue', passwordCount: 6 },
];