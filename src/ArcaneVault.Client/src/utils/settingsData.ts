export interface SettingsProfile {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
}

export interface SecuritySetting {
  id: 'twoFactor' | 'autoLock';
  title: string;
  description: string;
  enabled: boolean;
  ariaLabel: string;
}

export interface SettingsPreferences {
  theme: 'Light' | 'System default' | 'Dark';
  securityReminders: boolean;
}

export interface SettingsData {
  profile: SettingsProfile;
  security: SecuritySetting[];
  preferences: SettingsPreferences;
}

export const settingsData: SettingsData = {
  profile: {
    firstName: 'Design',
    lastName: 'Monks',
    email: 'hello@designmonk.com',
    role: 'Account owner',
  },
  security: [
    {
      id: 'twoFactor',
      title: 'Two-factor authentication',
      description: 'Add an extra step when signing in to your account.',
      enabled: true,
      ariaLabel: 'Two-factor authentication',
    },
    {
      id: 'autoLock',
      title: 'Lock vault automatically',
      description: 'Lock your vault after 15 minutes of inactivity.',
      enabled: true,
      ariaLabel: 'Automatic vault lock',
    },
  ],
  preferences: {
    theme: 'Light',
    securityReminders: true,
  },
};