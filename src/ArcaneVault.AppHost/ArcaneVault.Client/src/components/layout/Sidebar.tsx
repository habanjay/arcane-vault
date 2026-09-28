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
    </aside>
  );
}