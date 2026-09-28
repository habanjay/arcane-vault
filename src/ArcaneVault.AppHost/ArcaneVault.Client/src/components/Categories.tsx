import { useMemo, useState } from 'react';
import { passwordCategories, type PasswordCategory } from '../utils/dashboardData';
import Footer from './layout/Footer';
import Header from './layout/Header';
import Sidebar from './layout/Sidebar';

function CategoryCard({ category }: { category: PasswordCategory }) {
  return (
    <article className="group relative min-h-[164px] rounded-2xl border border-[#e5e9f0] bg-[#fbfcfd] p-[19px] transition hover:-translate-y-0.5 hover:border-[#f5c5d8] hover:bg-white hover:shadow-[0_10px_22px_rgba(24,29,65,0.07)]">
      <button className="absolute right-3 top-3 grid size-7 place-items-center rounded-full border-0 bg-transparent text-base text-[#727891] hover:bg-[#fff0f5] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" aria-label={`More options for ${category.name}`}>
        ...
      </button>
      <div className={`mb-[17px] grid size-[45px] place-items-center rounded-[13px] text-xl ${category.tone === 'blue' ? 'bg-[#eaf5ff] text-[#2c82d8]' : category.tone === 'green' ? 'bg-[#e8f8f1] text-[#249b70]' : category.tone === 'orange' ? 'bg-[#fff3e4] text-[#d88435]' : 'bg-[#fff0f5] text-[#f62570]'}`} aria-hidden="true">{category.icon}</div>
      <strong className="block font-display text-[13px]">{category.name}</strong>
      <p className="mt-[5px] text-[11px] text-[#727891]">{String(category.passwordCount).padStart(2, '0')} passwords</p>
      <span className="absolute bottom-5 right-[18px] text-[17px] text-[#aeb4c3] transition group-hover:text-[#f62570]" aria-hidden="true">→</span>
    </article>
  );
}

function CreateCategoryCard({ onCreate }: { onCreate: () => void }) {
  return (
    <article className="grid min-h-[164px] place-items-center rounded-2xl border border-dashed border-[#f5c5d8] bg-[#fffafd] p-4 text-center">
      <div className="text-[11px] text-[#727891]"><strong className="mb-[5px] block font-display text-xs text-[#172047]">Need another group?</strong><span>Create a category for anything you want to keep close.</span><br /><button className="mt-[9px] rounded-lg border border-[#f5c5d8] bg-white px-[11px] py-[7px] text-[10px] font-bold text-[#f62570] hover:bg-[#fff0f5] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={onCreate}>＋ New category</button></div>
    </article>
  );
}

function MobileCategoryNavigation() {
  const items = [{ label: 'Home', icon: '⌂', href: '#dashboard' }, { label: 'Vaults', icon: '▣', href: '#vaults' }, { label: 'Categories', icon: '◈', href: '#categories' }, { label: 'Settings', icon: '⚙', href: '#settings' }];
  return <nav className="fixed inset-x-3 bottom-3 z-5 flex justify-around rounded-[18px] border border-white/80 bg-[#281e3e]/96 px-2 py-2.5 shadow-[0_12px_30px_rgba(24,29,65,0.2)] min-[841px]:hidden" aria-label="Mobile navigation">{items.map((item) => <a className={`grid min-w-[55px] gap-[3px] p-1 text-center text-[17px] no-underline ${item.label === 'Categories' ? 'text-white' : 'text-[#a9a2b6]'}`} href={item.href} key={item.label} aria-current={item.label === 'Categories' ? 'page' : undefined}>{item.icon}<span className="text-[9px]">{item.label}</span></a>)}</nav>;
}

export default function Categories() {
  const [query, setQuery] = useState('');
  const isCreating = false;
  const searchRef = { current: null };
  const visibleCategories = useMemo(() => passwordCategories.filter((category) => category.name.toLowerCase().includes(query.trim().toLowerCase())), [query]);
  const startCreating = () => { window.location.hash = '#create-category'; };

  return <div className="flex min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] max-[840px]:bg-[#f1f5f8]"><Sidebar activeItem="Categories" /><main className="min-w-0 flex-1 bg-[#f1f5f8] px-[18px] pb-[92px] pt-8 min-[481px]:pt-6 min-[841px]:px-[42px] min-[841px]:pb-[42px] min-[841px]:pt-[35px]"><Header title="Categories" onNotify={() => undefined} /><div className="mb-[21px] flex items-end justify-between gap-6 max-[560px]:items-start"><div><h2 className="mb-[6px] font-display text-lg tracking-[-0.4px] max-[560px]:text-base">Organize your vault</h2><p className="text-xs text-[#727891] max-[560px]:max-w-[220px] max-[560px]:leading-[1.45]">Group your passwords into categories that make sense for you.</p></div><button className="rounded-[10px] border-0 bg-linear-to-r from-[#f62570] to-[#ff7d82] px-[17px] py-[11px] text-xs font-bold text-white shadow-[0_8px_18px_rgba(246,37,112,0.2)] hover:bg-[#281e3e] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] max-[560px]:grid max-[560px]:size-10 max-[560px]:place-items-center max-[560px]:p-0 max-[560px]:text-[0px] max-[560px]:after:text-[22px] max-[560px]:after:font-normal max-[560px]:after:content-['+']" type="button" onClick={startCreating}>＋ Create category</button></div><section className="max-w-[1030px] rounded-[20px] border border-[#e5e9f0] bg-white p-[25px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] max-[840px]:p-[18px] max-[560px]:border-0 max-[560px]:bg-transparent max-[560px]:p-0 max-[560px]:shadow-none" aria-labelledby="category-list-title"><div className="mb-[22px] flex items-center justify-between gap-[18px] max-[560px]:items-stretch max-[560px]:flex-col max-[560px]:gap-[13px] max-[560px]:mb-4"><div><h3 className="font-display text-base tracking-[-0.4px]" id="category-list-title">Your categories</h3><span className="text-[11px] text-[#727891]">6 categories · 48 passwords</span></div><label className="relative block w-[min(255px,100%)]"><span className="absolute left-[14px] top-1/2 -translate-y-1/2 text-lg text-[#727891]" aria-hidden="true">⌕</span><input ref={searchRef} className="w-full rounded-[10px] border border-[#e5e9f0] bg-[#fbfcfd] px-[13px] py-[11px] pl-[39px] text-[#172047] outline-0 focus:border-[#f62570] focus:shadow-[0_0_0_3px_rgba(246,37,112,0.1)]" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={isCreating ? 'Name your new category' : 'Search categories'} aria-label="Search categories" /></label></div><div className="grid grid-cols-3 gap-[14px] max-[960px]:grid-cols-2 max-[560px]:grid-cols-2 max-[560px]:gap-[9px]">{visibleCategories.map((category) => <CategoryCard category={category} key={category.slug} />)}<CreateCategoryCard onCreate={startCreating} /></div>{visibleCategories.length === 0 && <p className="pt-[25px] text-center text-xs text-[#727891]" role="status">No categories match your search.</p>}</section><Footer /></main><MobileCategoryNavigation /></div>;
}