import { useEffect, useState, type ReactNode } from 'react';
import Dashboard from './Dashboard';
import CreateAccount from './CreateAccount';
import AddPassword from './AddPassword';
import Login from './Login';
import MyVault from './MyVault';
import PasswordGenerator from './PasswordGenerator';
import Categories from './Categories';
import CreateCategory from './CreateCategory';
import Settings from './Settings';
import Footer from './layout/Footer';

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

function RoutedPage({ route, children }: { route: string; children: ReactNode }) {
  if (route === '#login' || route === '#sign-in' || route === '#dashboard' || route === '#vaults' || route === '#generator' || route === '#categories' || route === '#settings') return <>{children}</>;
  return <><div>{children}</div><div className="bg-[#f1f5f8] px-[18px] pb-6 min-[841px]:ml-[244px] min-[841px]:px-[42px]"><div className="mx-auto max-w-[1500px]"><Footer /></div></div></>;
}

function App() {
  const route = useHashRoute();

  useEffect(() => {
    document.title = route === '#login' || route === '#sign-in' ? 'Arcane Vault | Sign In' : route === '#create-account' ? 'Arcane Vault | Create Account' : route === '#add' ? 'Arcane Vault | Add Password' : route === '#create-category' ? 'Arcane Vault | Create Category' : route === '#settings' ? 'Arcane Vault | Settings' : 'Arcane Vault | My Vaults';
  }, [route]);

  let page: ReactNode;
  if (route === '#login' || route === '#sign-in') page = <Login />;
  else if (route === '#create-account') page = <CreateAccount />;
  else if (route === '#dashboard') page = <Dashboard />;
  else if (route === '#vaults') page = <MyVault />;
  else if (route === '#add') page = <AddPassword />;
  else if (route === '#categories') page = <Categories />;
  else if (route === '#create-category') page = <CreateCategory />;
  else if (route === '#shared-vaults') page = <RoutePlaceholder title="Shared vaults" description="Secure collaboration for the people and projects you trust." />;
  else if (route === '#generator') page = <PasswordGenerator />;
  else if (route === '#settings') page = <Settings />;
  else if (route === '#premium') page = <RoutePlaceholder title="Premium" description="Advanced security reports and unlimited vaults are coming soon." />;
  else page = <RoutePlaceholder title="Page not found" description="That Arcane Vault destination is not available." />;

  return <RoutedPage route={route}>{page}</RoutedPage>;
}

export default App;
