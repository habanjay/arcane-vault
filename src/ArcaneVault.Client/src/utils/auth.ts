export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface AuthSession {
  user: AuthUser;
}

export interface AuthProvider {
  signIn(credentials: LoginCredentials): Promise<AuthSession>;
}

interface MockUser extends AuthUser {
  password: string;
}

async function loadMockUser(): Promise<MockUser> {
  const response = await fetch('/mockuser.json');

  if (!response.ok) {
    throw new Error('Unable to load the mock account.');
  }

  return response.json() as Promise<MockUser>;
}

export const mockAuthProvider: AuthProvider = {
  async signIn(credentials) {
    const user = await loadMockUser();
    const emailMatches = user.email.toLowerCase() === credentials.email.trim().toLowerCase();

    if (!emailMatches || user.password !== credentials.password) {
      throw new Error('The email or master password is incorrect.');
    }

    const { password: _password, ...authenticatedUser } = user;
    return { user: authenticatedUser };
  },
};

export const authProvider: AuthProvider = mockAuthProvider;