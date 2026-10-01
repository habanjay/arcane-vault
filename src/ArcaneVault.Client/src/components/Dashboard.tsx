import { useState } from 'react';
import {
  recentPasswords,
  toolNavigation,
  vaultCategories,
  vaultStats,
  workspaceNavigation,
  type NavigationItem,
  type RecentPassword,
} from '../utils/dashboardData';
import Footer from './layout/Footer';

function Brand() {
  return (
    <a className="flex items-center gap-[11px] text-white no-underline" href="#dashboard" aria-label="Arcane Vault home">
      <span className="grid size-[38px] place-items-center rounded-xl bg-linear-to-br from-[#ff4a87] to-[#cb185b] text-xl shadow-[0_8px_18px_rgba(246,37,112,0.22)]" aria-hidden="true">✦</span>
      <span>
        <strong className="block font-display text-base tracking-[-0.4px]">Arcane Vault</strong>
        <small className="mt-0.5 block text-[10px] text-[#a9a2b6]">Secure your world</small>
      </span>
    </a>
  );
}

function Navigation({ items, activeItem, onSelect }: { items: NavigationItem[]; activeItem: string; onSelect: (label: string) => void }) {
  return (
    <nav className="grid gap-[7px]" aria-label="Workspace navigation">
      {items.map((item) => (
          <a
          className={`flex w-full items-center gap-[13px] rounded-xl px-[13px] py-3 text-left text-sm no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] ${activeItem === item.label ? 'bg-white/12 text-white shadow-[inset_3px_0_#f62570]' : 'text-[#aaa4b8] hover:bg-white/8 hover:text-white'}`}
          href={item.href}
          key={item.label}
          onClick={() => onSelect(item.label)}
          aria-current={activeItem === item.label ? 'page' : undefined}
        >
          <span className="w-[19px] text-center text-[17px]" aria-hidden="true">{item.icon}</span>
          <span>{item.label}</span>
        </a>
      ))}
    </nav>
  );
}

function Sidebar({ activeItem, onSelect }: { activeItem: string; onSelect: (label: string) => void }) {
  return (
    <aside className="flex basis-[244px] flex-col bg-[#281e3e] px-5 py-[29px] text-white" aria-label="Main navigation">
      <div className="mb-[58px] px-3"><Brand /></div>
      <p className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Workspace</p>
      <Navigation items={workspaceNavigation} activeItem={activeItem} onSelect={onSelect} />
      <p className="mb-3 mt-[34px] px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Tools</p>
      <Navigation items={toolNavigation} activeItem={activeItem} onSelect={onSelect} />
    </aside>
  );
}

function Header({ onNotification }: { onNotification: () => void }) {
  return (
    <header className="mb-6 flex items-center justify-between gap-5 min-[841px]:mb-[33px]">
      <div>
        <p className="mb-[7px] text-xs text-[#727891]">Saturday, September 26, 2026</p>
        <h1 className="font-display text-[clamp(24px,3vw,34px)] tracking-[-1.2px]">Hello, Design Monks <span aria-hidden="true">👋</span></h1>
      </div>
      <div className="flex items-center gap-[14px]">
        <button className="grid size-[42px] place-items-center rounded-full border border-[#e5e9f0] bg-white text-lg text-[#172047] hover:border-[#f62570] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] max-[480px]:hidden" type="button" onClick={onNotification} aria-label="Notifications">♧</button>
        <button className="grid size-[42px] place-items-center rounded-full border-0 bg-linear-to-br from-[#e94079] to-[#8d356d] text-xs font-bold text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" aria-label="Open profile">DM</button>
      </div>
    </header>
  );
}

function SecurityScore() {
  return (
    <section className="relative min-h-[330px] overflow-hidden rounded-[22px] bg-linear-to-br from-[#573049] to-[#281e3e] p-[22px] text-white shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[841px]:min-h-[385px] min-[841px]:rounded-[25px] min-[841px]:p-[27px]" aria-labelledby="health-title">
      <div className="absolute -bottom-[120px] -right-[90px] size-[300px] rounded-full bg-[#f62570]/8" aria-hidden="true" />
      <p className="relative z-1 text-[11px] font-bold uppercase tracking-[1px] text-[#c4b9c7]">Security overview</p>
      <h2 className="relative z-1 mt-2 font-display text-xl" id="health-title">Your health score</h2>
      <div className="relative z-1 mx-auto my-5 grid size-[205px] rotate-[-34deg] place-items-center rounded-full border-[10px] border-[#65546f] border-r-[#f62570] border-t-[#f62570] p-3 min-[841px]:my-7 min-[841px]:size-[240px]">
        <div className="absolute inset-[14px] rounded-full bg-linear-to-br from-[#f51e71] to-[#ff7c7e]" />
        <div className="relative grid rotate-[34deg] place-content-center text-center">
          <small className="mb-[3px] text-[11px] font-bold">Health score</small>
          <strong className="font-display text-5xl leading-none tracking-[-3px]">75%</strong>
        </div>
      </div>
      <p className="relative z-1 text-center text-xs text-[#c9c0cf]"><b className="text-white">Good progress!</b> You have 3 passwords to improve.</p>
    </section>
  );
}

function PanelHeader({ title, titleId, linkLabel, href = '#' }: { title: string; titleId: string; linkLabel: string; href?: string }) {
  return <div className="mb-5 flex items-center justify-between"><h2 className="font-display text-base tracking-[-0.4px]" id={titleId}>{title}</h2><a className="text-[11px] font-bold text-[#f62570] no-underline hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href={href}>{linkLabel}</a></div>;
}

function VaultsPanel() {
  return <section className="rounded-[18px] border border-[#e5e9f0] bg-white p-[18px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[481px]:rounded-[20px] min-[481px]:p-[25px]" aria-labelledby="categories-title">
    <PanelHeader title="Your vaults" titleId="categories-title" linkLabel="View all" href="#vaults" />
    <div className="grid grid-cols-3 gap-[7px] min-[481px]:gap-3">
      {vaultCategories.map((category) => <article className="rounded-[14px] border border-[#e5e9f0] bg-[#fbfcfd] px-[5px] py-[11px] text-center min-[481px]:px-[10px] min-[481px]:py-[14px]" key={category.name}>
        <div className="mx-auto mb-2.5 grid size-10 place-items-center rounded-full bg-[#fff0f5] text-lg text-[#f62570]" aria-hidden="true">{category.icon}</div>
        <strong className="block text-xs">{category.name}</strong>
        <small className="mt-1 block text-[10px] text-[#727891]">{String(category.passwordCount).padStart(2, '0')} passwords</small>
      </article>)}
    </div>
  </section>;
}

function ActivityPanel() {
  return <section className="rounded-[18px] border border-[#e5e9f0] bg-white p-[18px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[481px]:rounded-[20px] min-[481px]:p-[25px]" aria-labelledby="stats-title">
    <PanelHeader title="Vault activity" titleId="stats-title" linkLabel="This month" href="#audit" />
    <div className="grid grid-cols-3 gap-[7px] min-[481px]:gap-3">{vaultStats.map((stat) => <div className="rounded-[13px] bg-[#f8f9fb] px-[9px] py-[11px] min-[481px]:px-[15px] min-[481px]:py-[13px]" key={stat.label}><strong className="block font-display text-xl">{stat.value}</strong><span className="text-[10px] text-[#727891]">{stat.label}</span></div>)}</div>
  </section>;
}

function PasswordRow({ password, onToggleFavorite }: { password: RecentPassword; onToggleFavorite: (service: string) => void }) {
  return <article className="flex items-center gap-[13px] rounded-[14px] border border-transparent bg-[#f8f9fb] px-[14px] py-3 hover:border-[#f5c5d8] hover:bg-white">
    <div className={`grid size-[42px] shrink-0 place-items-center rounded-xl font-display text-lg font-extrabold text-white ${password.tone === 'blue' ? 'bg-linear-to-br from-[#3d5bf4] to-[#55b5ff]' : password.tone === 'green' ? 'bg-[#28ae70]' : 'bg-[#171717]'}`} aria-hidden="true">{password.initial}</div>
    <div className="min-w-0 flex-1"><strong className="block text-[13px]">{password.service}</strong><span className="mt-1 block overflow-hidden text-ellipsis whitespace-nowrap text-[10px] text-[#727891]">{password.account}</span></div>
    <span className="text-right text-[10px] text-[#727891] max-[480px]:hidden">{password.updated}</span>
    <button className="border-0 bg-transparent p-1 text-lg text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={() => onToggleFavorite(password.service)} aria-label={`${password.favorite ? 'Remove' : 'Add'} ${password.service} favorite`} aria-pressed={password.favorite}>{password.favorite ? '♥' : '♡'}</button>
  </article>;
}

function RecentPasswords({ passwords, onToggleFavorite }: { passwords: RecentPassword[]; onToggleFavorite: (service: string) => void }) {
  return <section className="rounded-[18px] border border-[#e5e9f0] bg-white p-[18px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[481px]:rounded-[20px] min-[481px]:p-[25px] min-[841px]:col-span-2" aria-labelledby="recent-title">
    <PanelHeader title="Recently used" titleId="recent-title" linkLabel="See more" href="#audit" />
    <div className="grid gap-[9px]">{passwords.map((password) => <PasswordRow key={password.service} password={password} onToggleFavorite={onToggleFavorite} />)}</div>
  </section>;
}

function MobileNavigation({ activeItem, onSelect }: { activeItem: string; onSelect: (label: string) => void }) {
  const items = [{ label: 'Dashboard', icon: '⌂', href: '#dashboard' }, { label: 'Vaults', icon: '▣', href: '#vaults' }, { label: 'Add', icon: '＋', href: '#add' }, { label: 'Settings', icon: '⚙', href: '#settings' }];
  return <nav className="fixed inset-x-3 bottom-3 z-5 flex justify-around rounded-[18px] border border-white/80 bg-[#281e3e]/96 px-2 py-2.5 shadow-[0_12px_30px_rgba(24,29,65,0.2)] min-[841px]:hidden" aria-label="Mobile navigation">{items.map((item) => <a className={`grid min-w-[55px] gap-[3px] p-1 text-center text-[17px] no-underline ${activeItem === item.label ? 'text-white' : 'text-[#a9a2b6]'}`} href={item.href} key={item.label} onClick={() => onSelect(item.label)} aria-current={activeItem === item.label ? 'page' : undefined}>{item.icon}<span className="text-[9px]">{item.label === 'Dashboard' ? 'Home' : item.label}</span></a>)}</nav>;
}

export default function Dashboard() {
  const [activeItem, setActiveItem] = useState('Dashboard');
  const [passwords, setPasswords] = useState(recentPasswords);
  const [notification, setNotification] = useState('');

  const toggleFavorite = (service: string) => setPasswords((current) => current.map((password) => password.service === service ? { ...password, favorite: !password.favorite } : password));

  return <div className="flex min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] max-[840px]:bg-[#f1f5f8]" id="dashboard">
    <Sidebar activeItem={activeItem} onSelect={setActiveItem} />
    <main className="min-w-0 flex-1 bg-[#f1f5f8] px-[18px] pb-[92px] pt-8 min-[481px]:pt-6 min-[841px]:px-[42px] min-[841px]:pb-[42px] min-[841px]:pt-[35px]">
      <Header onNotification={() => setNotification((current) => current ? '' : 'You are all caught up.')} />
      {notification && <p className="mb-4 rounded-lg bg-[#fff0f5] px-3 py-2 text-xs text-[#f62570]" role="status">{notification}</p>}
      <div className="grid gap-[18px] min-[841px]:grid-cols-[minmax(310px,0.92fr)_minmax(420px,1.5fr)] min-[841px]:gap-6">
        <SecurityScore />
        <div className="grid gap-[18px] min-[841px]:gap-6"><VaultsPanel /><ActivityPanel /></div>
        <RecentPasswords passwords={passwords} onToggleFavorite={toggleFavorite} />
      </div>
      <Footer />
    </main>
    <MobileNavigation activeItem={activeItem} onSelect={setActiveItem} />
  </div>;
}