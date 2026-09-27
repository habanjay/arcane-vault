import { useEffect, useState } from 'react';
import Dashboard from './Dashboard';
import CreateAccount from './CreateAccount';
import Login from './Login';
import MyVault from './MyVault';
import PasswordGenerator from './PasswordGenerator';

function useHashRoute() {
  const [route, setRoute] = useState(() => window.location.hash || '#login');

  useEffect(() => {
    const handleHashChange = () => setRoute(window.location.hash || '#login');
    window.addEventListener('hashchange', handleHashChange);
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  return route;
}

function RoutePlaceholder({ title, description }: { title: string; description: string }) {
  return <main className="grid min-h-screen place-items-center bg-[#f1f5f8] px-6 text-center text-[#172047]"><section className="max-w-md"><p className="mb-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#f62570]">Arcane Vault</p><h1 className="font-display text-3xl">{title}</h1><p className="mt-3 text-sm leading-6 text-[#727891]">{description}</p><a className="mt-6 inline-block rounded-[10px] bg-[#f62570] px-4 py-3 text-xs font-bold text-white no-underline hover:bg-[#d9185e]" href="#vaults">Back to my vaults</a></section></main>;
}

function App() {
  const route = useHashRoute();

  useEffect(() => {
    document.title = route === '#login' || route === '#sign-in' ? 'Arcane Vault | Sign In' : route === '#create-account' ? 'Arcane Vault | Create Account' : 'Arcane Vault | My Vaults';
  }, [route]);

  if (route === '#login' || route === '#sign-in') return <Login />;
  if (route === '#create-account') return <CreateAccount />;
  if (route === '#dashboard') return <Dashboard />;
  if (route === '#vaults') return <MyVault />;
  if (route === '#categories') return <RoutePlaceholder title="Categories" description="Organize your passwords into the groups that fit your world." />;
  if (route === '#shared-vaults') return <RoutePlaceholder title="Shared vaults" description="Secure collaboration for the people and projects you trust." />;
  if (route === '#generator') return <PasswordGenerator />;
  if (route === '#settings') return <RoutePlaceholder title="Settings" description="Manage your account, security, and vault preferences." />;
  if (route === '#premium') return <RoutePlaceholder title="Premium" description="Advanced security reports and unlimited vaults are coming soon." />;
  return <RoutePlaceholder title="Page not found" description="That Arcane Vault destination is not available." />;
}

export default App;
