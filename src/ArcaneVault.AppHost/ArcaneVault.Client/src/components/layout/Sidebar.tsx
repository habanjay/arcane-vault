import { toolNavigation, workspaceNavigation } from '../../utils/dashboardData';
import Brand from './Brand';
import Navigation from './Navigation';

interface SidebarProps {
  activeItem: string;
}

export default function Sidebar({ activeItem }: SidebarProps) {
  return (
    <aside className="hidden basis-[244px] flex-col bg-[#281e3e] px-5 py-[29px] text-white min-[841px]:flex" aria-label="Main navigation">
      <div className="mb-[58px] px-3"><Brand /></div>
      <p className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Workspace</p>
      <Navigation items={workspaceNavigation} activeItem={activeItem} />
      <p className="mb-3 mt-[34px] px-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#91889f]">Tools</p>
      <Navigation items={toolNavigation} activeItem={activeItem} />
      <div className="mt-auto border-t border-white/10 px-3 pb-1 pt-[18px]">
        <div className="rounded-[15px] border border-white/10 bg-white/6 p-[15px]">
          <p className="mb-[11px] text-[11px] leading-[1.5] text-[#d5d0db]">Get unlimited vaults and advanced security reports.</p>
          <a className="block w-full rounded-lg bg-[#f62570] px-2 py-[9px] text-center text-[11px] font-bold text-white no-underline hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#premium">Explore premium</a>
        </div>
      </div>
    </aside>
  );
}