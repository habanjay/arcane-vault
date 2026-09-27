import { useState } from 'react';
import {
  recentPasswords,
  toolNavigation,
  vaultCategories,
  vaultStats,
  workspaceNavigation,
  type NavigationItem,
  type RecentPassword,
} from './dashboardData';

const navIcons: Record<string, string> = {
  Dashboard: '⌂',
  Vaults: '▣',
  Add: '+',
  Settings: '⚙',
};

function Brand() {
  return (
    <a className="flex items-center gap-3 text-white no-underline" href="#dashboard" aria-label="Arcane Vault dashboard">
      <span className="grid size-10 place-items-center rounded-xl bg-linear-to-br from-[#ff4a87] to-[#cb185b] text-xl shadow-[0_8px_18px_rgba(246,37,112,0.22)]">✦</span>
      <span>
        <strong className="block font-display text-base tracking-[-0.4px]">Arcane Vault</strong>
        <small className="mt-0.5 block text-[10px] text-[#a9a2b6]">Secure your world</small>
      </span>
    </a>
  );
}

function NavigationGroup({ items, activeItem, onSelect }: { items: NavigationItem[]; activeItem: string; onSelect: (label: string) => void }) {
  return (
    <nav className="grid gap-1.5" aria-label="Workspace navigation">
      {items.map((item) => (
        <button
          key={item.label}
          className={`flex w-full items-center gap-3 rounded-xl border-0 px-3 py-3 text-left text-sm transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-pink-400 ${activeItem === item.label ? 'bg-white/12 text-white shadow-[inset_3px_0_#f62570]' : 'bg-transparent text-[#aaa4b8] hover:bg-white/8 hover:text-white'}`}
          onClick={() => onSelect(item.label)}
          type="button"
        >
          <span className="w-5 text-center text-[17px]" aria-hidden="true">{item.icon}</span>
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
  );
}

function Sidebar({ activeItem, onSelect }: { activeItem: string; onSelect: (label: string) => void }) {
  return (
    <aside className="hidden w-61 shrink-0 flex-col bg-[#281e3e] px-5 py-7 text-white lg:flex" aria-label="Main navigation">
      <div className="mb-14 px-3"><Brand /></div>
      <p className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Workspace</p>
      <NavigationGroup items={workspaceNavigation} activeItem={activeItem} onSelect={onSelect} />
      <p className="mb-3 mt-8 px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Tools</p>
      <NavigationGroup items={toolNavigation} activeItem={activeItem} onSelect={onSelect} />
      <div className="mt-auto border-t border-white/10 px-3 pb-1 pt-5">
        <div className="rounded-[15px] border border-white/10 bg-white/6 p-4">
          <p className="mb-3 text-[11px] leading-[1.5] text-[#d5d0db]">Get unlimited vaults and advanced security reports.</p>
          <a className="block rounded-lg bg-[#f62570] px-2 py-2 text-center text-[11px] font-bold text-white no-underline hover:bg-[#d91d60]" href="#premium">Explore premium</a>
        </div>
      </div>
    </aside>
  );
}

function ScoreCard() {
  return (
    <section className="relative min-h-[330px] overflow-hidden rounded-[25px] bg-linear-to-br from-[#573049] to-[#281e3e] p-7 text-white shadow-[0_16px_35px_rgba(24,29,65,0.08)] sm:min-h-[385px]" aria-labelledby="health-title">
      <div className="absolute -bottom-30 -right-23 size-75 rounded-full bg-[#f62570]/8" aria-hidden="true" />
      <p className="relative z-1 m-0 text-[11px] font-bold uppercase tracking-[1px] text-[#c4b9c7]">Security overview</p>
      <h2 className="relative z-1 mt-2 font-display text-xl">Your health score</h2>
      <div className="relative z-1 mx-auto my-7 grid size-51 rotate-[-34deg] place-items-center rounded-full p-3 [background:conic-gradient(#f62570_0deg_270deg,#65546f_270deg_360deg)] sm:size-60">
        <div className="grid size-full place-items-center rounded-full bg-linear-to-br from-[#f51e71] to-[#ff7c7e] p-4">
          <div className="grid size-full place-content-center rounded-full bg-[#3f2b4a] text-center outline-10 outline-[#281e3e]">
            <small className="mb-0.5 text-[11px] font-bold" style={{ transform: 'rotate(34deg)' }}>Health score</small>
            <strong className="font-display text-5xl leading-none tracking-[-3px]" style={{ transform: 'rotate(34deg)' }}>75%</strong>
          </div>
        </div>
      </div>
      <p className="relative z-1 m-0 text-center text-xs text-[#c9c0cf]"><b className="text-white">Good progress!</b> You have 3 passwords to improve.</p>
    </section>
  );
}

function Panel({ title, action, children, className = '' }: { title: string; action: string; children: React.ReactNode; className?: string }) {
  return (
    <section className={`rounded-[20px] border border-[#e5e9f0] bg-white p-5 shadow-[0_16px_35px_rgba(24,29,65,0.08)] sm:p-6 ${className}`}>
      <div className="mb-5 flex items-center justify-between gap-3"><h2 className="font-display text-base tracking-[-0.4px]">{title}</h2><a className="text-[11px] font-bold text-[#f62570] no-underline hover:underline" href={`#${action.toLowerCase().replace(/ /g, '-')}`}>{action}</a></div>
      {children}
    </section>
  );
}

function VaultCategories() {
  return <Panel title="Your vaults" action="View all"><div className="grid grid-cols-3 gap-2 sm:gap-3">{vaultCategories.map((category) => <article className="rounded-[14px] border border-[#e5e9f0] bg-[#fbfcfd] p-3 text-center sm:p-3.5" key={category.name}><div className="mx-auto mb-2.5 grid size-10 place-items-center rounded-full bg-[#fff0f5] text-lg text-[#f62570]">{category.icon}</div><strong className="block text-xs">{category.name}</strong><small className="mt-1 block text-[10px] text-[#727891]">{String(category.passwordCount).padStart(2, '0')} passwords</small></article>)}</div></Panel>;
}

function ActivityStats() {
  return <Panel title="Vault activity" action="This month"><div className="grid grid-cols-3 gap-2 sm:gap-3">{vaultStats.map((stat) => <div className="rounded-[13px] bg-[#f8f9fb] px-2.5 py-3 sm:px-4" key={stat.label}><strong className="block font-display text-xl">{stat.value}</strong><span className="text-[10px] text-[#727891]">{stat.label}</span></div>)}</div></Panel>;
}

function PasswordRow({ password, onFavorite }: { password: RecentPassword; onFavorite: (service: string) => void }) {
  return <article className="flex items-center gap-3 rounded-[14px] border border-transparent bg-[#f8f9fb] px-3.5 py-3 transition-colors hover:border-[#f5c5d8] hover:bg-white"><div className={`grid size-10 shrink-0 place-items-center rounded-xl font-display text-lg font-extrabold text-white ${password.tone === 'blue' ? 'bg-linear-to-br from-[#3d5bf4] to-[#55b5ff]' : password.tone === 'green' ? 'bg-[#28ae70]' : 'bg-[#171717]'}`}>{password.initial}</div><div className="min-w-0 flex-1"><strong className="block text-[13px]">{password.service}</strong><span className="mt-1 block overflow-hidden text-ellipsis whitespace-nowrap text-[10px] text-[#727891]">{password.account}</span></div><span className="hidden text-right text-[10px] text-[#727891] sm:block">{password.updated}</span><button className="text-lg text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-pink-400" onClick={() => onFavorite(password.service)} aria-label={`${password.favorite ? 'Remove' : 'Add'} ${password.service} favorite`} type="button">{password.favorite ? '♥' : '♡'}</button></article>;
}

function RecentPasswords({ passwords, onFavorite }: { passwords: RecentPassword[]; onFavorite: (service: string) => void }) {
  return <Panel title="Recently used" action="See more" className="lg:col-span-2"><div className="grid gap-2">{passwords.map((password) => <PasswordRow key={password.service} password={password} onFavorite={onFavorite} />)}</div></Panel>;
}

function App() {
  const [activeItem, setActiveItem] = useState('Dashboard');
  const [passwords, setPasswords] = useState(recentPasswords);
  const [notificationsOpen, setNotificationsOpen] = useState(false);

  const toggleFavorite = (service: string) => {
    setPasswords((current) => current.map((password) => password.service === service ? { ...password, favorite: !password.favorite } : password));
  };

  return (
    <div className="min-h-screen bg-[#dfe5eb] font-sans text-[#172047]">
      <div className="mx-auto flex min-h-screen max-w-[1500px] bg-[#f1f5f8]">
        <Sidebar activeItem={activeItem} onSelect={setActiveItem} />
        <main className="min-w-0 flex-1 px-[18px] pb-24 pt-6 sm:px-10 sm:py-9">
          <header className="mb-6 flex items-center justify-between gap-5 sm:mb-8">
            <div><p className="mb-1.5 text-xs text-[#727891]">Saturday, September 26, 2026</p><h1 className="font-display text-[26px] tracking-[-1.2px] sm:text-[34px]">Hello, Design Monks <span aria-hidden="true">👋</span></h1></div>
            <div className="flex items-center gap-3.5"><div className="relative"><button className="grid size-10 place-items-center rounded-full border border-[#e5e9f0] bg-white text-lg hover:border-[#f62570] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-pink-400" onClick={() => setNotificationsOpen(!notificationsOpen)} aria-label="Notifications" aria-expanded={notificationsOpen} type="button">♧</button>{notificationsOpen && <div className="absolute right-0 top-12 z-10 w-52 rounded-xl border border-[#e5e9f0] bg-white p-3 text-xs shadow-xl">No new notifications</div>}</div><button className="grid size-10 place-items-center rounded-full bg-linear-to-br from-[#e94079] to-[#8d356d] text-xs font-bold text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-pink-400" aria-label="Open profile" type="button">DM</button></div>
          </header>
          <div className="grid gap-5 lg:grid-cols-[minmax(310px,0.92fr)_minmax(420px,1.5fr)]">
            <ScoreCard />
            <div className="grid gap-5"><VaultCategories /><ActivityStats /></div>
            <RecentPasswords passwords={passwords} onFavorite={toggleFavorite} />
          </div>
        </main>
        <nav className="fixed bottom-3 left-3 right-3 z-5 flex justify-around rounded-[18px] border border-white/80 bg-[#281e3e]/96 px-2 py-2 shadow-[0_12px_30px_rgba(24,29,65,0.2)] lg:hidden" aria-label="Mobile navigation">
          {['Dashboard', 'Vaults', 'Add', 'Settings'].map((label) => <button className={`grid min-w-14 gap-0.5 border-0 bg-transparent p-1 text-center text-lg ${activeItem === label || (label === 'Dashboard' && activeItem === 'Dashboard') ? 'text-white' : 'text-[#a9a2b6]'}`} key={label} onClick={() => setActiveItem(label)} type="button"><span aria-hidden="true">{navIcons[label]}</span><span className="text-[9px]">{label === 'Dashboard' ? 'Home' : label}</span></button>)}
        </nav>
      </div>
    </div>
  );
}

export default App;
