import { type FormEvent, useState } from 'react';
import Header from './layout/Header';
import Sidebar from './layout/Sidebar';

interface CategoryOption {
  value: string;
  label: string;
}

interface ColorOption extends CategoryOption {
  color: string;
  softColor: string;
}

const iconOptions: CategoryOption[] = [
  { value: '♡', label: 'Heart' },
  { value: '▣', label: 'Square' },
  { value: '◈', label: 'Diamond' },
  { value: '✣', label: 'Flower' },
  { value: '◇', label: 'Outline diamond' },
  { value: '⌁', label: 'Wave' },
];

const colorOptions: ColorOption[] = [
  { value: 'pink', label: 'Pink', color: '#f62570', softColor: '#fff0f5' },
  { value: 'blue', label: 'Blue', color: '#2c82d8', softColor: '#eaf5ff' },
  { value: 'green', label: 'Green', color: '#249b70', softColor: '#e8f8f1' },
  { value: 'orange', label: 'Orange', color: '#d88435', softColor: '#fff3e4' },
  { value: 'purple', label: 'Purple', color: '#7358d1', softColor: '#f0edff' },
];

function MobileCategoryNavigation() {
  const items = [
    { label: 'Home', icon: '⌂', href: '#dashboard' },
    { label: 'Vaults', icon: '▣', href: '#vaults' },
    { label: 'Categories', icon: '◈', href: '#categories' },
    { label: 'Settings', icon: '⚙', href: '#settings' },
  ];

  return <nav className="fixed inset-x-3 bottom-3 z-5 flex justify-around rounded-[18px] border border-white/80 bg-[#281e3e]/96 px-2 py-2.5 shadow-[0_12px_30px_rgba(24,29,65,0.2)] min-[841px]:hidden" aria-label="Mobile navigation">{items.map((item) => <a className={`grid min-w-[55px] gap-[3px] p-1 text-center text-[17px] no-underline ${item.label === 'Categories' ? 'text-white' : 'text-[#a9a2b6]'}`} href={item.href} key={item.label} aria-current={item.label === 'Categories' ? 'page' : undefined}>{item.icon}<span className="text-[9px]">{item.label}</span></a>)}</nav>;
}

export default function CreateCategory() {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [icon, setIcon] = useState(iconOptions[0].value);
  const [color, setColor] = useState(colorOptions[0]);
  const [status, setStatus] = useState('');

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!name.trim()) return;
    setStatus(`${name.trim()} is ready to use.`);
  };

  return <div className="flex min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] max-[840px]:bg-[#f1f5f8]">
    <Sidebar activeItem="Categories" />
    <main className="min-w-0 flex-1 bg-[#f1f5f8] px-[18px] pb-[92px] pt-8 min-[481px]:pt-6 min-[841px]:px-[42px] min-[841px]:pb-[42px] min-[841px]:pt-[35px]">
      <Header title="Categories" onNotify={() => undefined} />
      <div className="grid max-w-[1000px] items-start gap-6 min-[961px]:grid-cols-[minmax(0,680px)_minmax(220px,280px)]">
        <section className="rounded-[20px] border border-[#e5e9f0] bg-white p-[25px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] max-[560px]:rounded-[18px] max-[560px]:p-[18px]" aria-labelledby="create-title">
          <div className="mb-6 flex items-start gap-[15px] max-[560px]:mb-[21px]"><a className="grid size-9 shrink-0 place-items-center rounded-full bg-[#f8f9fb] text-[25px] leading-none text-[#172047] no-underline hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#categories" aria-label="Back to categories">←</a><div><h2 className="mt-px mb-[5px] font-display text-[17px] tracking-[-0.4px]" id="create-title">Create a new category</h2><p className="text-[11px] leading-[1.5] text-[#727891]">Give your passwords a place that feels easy to find.</p></div></div>
          <form onSubmit={handleSubmit}>
            <div className="mb-[18px] grid gap-[7px]"><label className="text-[11px] font-bold text-[#4d5670]" htmlFor="category-name">Category name</label><input className="min-h-11 w-full rounded-[10px] border border-[#e5e9f0] bg-[#fbfcfd] p-3 text-[#172047] outline-0 focus:border-[#f62570] focus:shadow-[0_0_0_3px_rgba(246,37,112,0.1)]" id="category-name" name="category-name" type="text" placeholder="e.g. Subscriptions" maxLength={32} value={name} onChange={(event) => { setName(event.target.value); setStatus(''); }} required /><small className="text-[10px] text-[#727891]">Choose a short name you will recognize at a glance.</small></div>
            <div className="mb-0 grid gap-[7px]"><label className="text-[11px] font-bold text-[#4d5670]" htmlFor="category-description">Description <span aria-hidden="true">(optional)</span></label><textarea className="min-h-[88px] w-full resize-y rounded-[10px] border border-[#e5e9f0] bg-[#fbfcfd] p-3 text-[#172047] outline-0 focus:border-[#f62570] focus:shadow-[0_0_0_3px_rgba(246,37,112,0.1)]" id="category-description" name="category-description" placeholder="What belongs in this category?" maxLength={90} value={description} onChange={(event) => setDescription(event.target.value)} /></div>
            <fieldset className="mt-[23px] border-0 border-t border-[#e5e9f0] p-0 pt-[21px]"><legend className="mb-[11px] text-[11px] font-bold text-[#4d5670]">Choose an icon</legend><div className="flex flex-wrap gap-[9px]" role="radiogroup" aria-label="Category icon">{iconOptions.map((option) => <button className={`grid size-[42px] place-items-center rounded-[11px] border text-[19px] ${icon === option.value ? 'border-[#f5c5d8] bg-[#fff0f5] text-[#f62570]' : 'border-[#e5e9f0] bg-[#fbfcfd] text-[#727891]'} focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]`} key={option.value} type="button" role="radio" aria-label={option.label} aria-checked={icon === option.value} onClick={() => setIcon(option.value)}>{option.value}</button>)}</div></fieldset>
            <fieldset className="mt-[23px] border-0 border-t border-[#e5e9f0] p-0 pt-[21px]"><legend className="mb-[11px] text-[11px] font-bold text-[#4d5670]">Choose a color</legend><div className="flex flex-wrap gap-[9px]" role="radiogroup" aria-label="Category color">{colorOptions.map((option) => <button className={`grid size-[29px] place-items-center rounded-full border-[3px] border-transparent focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] ${color.value === option.value ? 'border-[#172047] shadow-[0_0_0_2px_white,0_0_0_3px_#172047]' : ''}`} style={{ backgroundColor: option.color }} key={option.value} type="button" role="radio" aria-label={option.label} aria-checked={color.value === option.value} onClick={() => setColor(option)}>{color.value === option.value && <span className="size-[7px] rounded-full bg-white" />}</button>)}</div></fieldset>
            <div className="mt-[26px] flex items-center justify-end gap-[13px] border-t border-[#e5e9f0] pt-5 max-[560px]:flex-wrap max-[560px]:justify-between"><p className="m-0 w-full text-[10px] text-[#f62570] max-[560px]:order-3" role="status" aria-live="polite">{status}</p><a className="px-[15px] py-2.5 text-[11px] font-bold text-[#727891] no-underline hover:text-[#172047]" href="#categories">Cancel</a><button className="rounded-[9px] bg-linear-to-r from-[#f62570] to-[#ff7d82] px-[18px] py-[11px] text-[11px] font-bold text-white shadow-[0_8px_18px_rgba(246,37,112,0.18)] hover:bg-[#281e3e] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] max-[560px]:flex-1" type="submit">Create category</button></div>
          </form>
        </section>
        <aside className="rounded-[20px] border border-[#e5e9f0] bg-white p-[25px] text-center shadow-[0_16px_35px_rgba(24,29,65,0.08)] max-[960px]:hidden" aria-label="Category preview"><p className="mb-[21px] text-[10px] font-bold uppercase tracking-[1px] text-[#727891]">Live preview</p><div className="mx-auto mb-4 grid size-[66px] place-items-center rounded-[19px] text-[30px]" style={{ color: color.color, backgroundColor: color.softColor }} aria-hidden="true">{icon}</div><strong className="block overflow-hidden text-ellipsis whitespace-nowrap font-display text-[15px]">{name.trim() || 'Your category'}</strong><p className="my-[7px] mb-[21px] min-h-[34px] text-[11px] leading-[1.5] text-[#727891]">{description.trim() || 'A place for your saved passwords.'}</p><div className="border-t border-[#e5e9f0] pt-4 text-[10px] text-[#727891]"><strong className="mb-[3px] block font-display text-lg text-[#172047]">0</strong>passwords yet</div></aside>
      </div>
    </main>
    <MobileCategoryNavigation />
  </div>;
}
