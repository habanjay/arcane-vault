import type { NavigationItem } from '../../utils/dashboardData';

interface NavigationProps {
  items: NavigationItem[];
  activeItem: string;
  onSelect?: (label: string) => void;
}

export default function Navigation({ items, activeItem, onSelect }: NavigationProps) {
  return (
    <nav className="grid gap-[7px]" aria-label="Workspace navigation">
      {items.map((item) => (
        <a
          className={`flex w-full items-center gap-[13px] rounded-xl px-[13px] py-3 text-left text-sm no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570] ${activeItem === item.label ? 'bg-white/12 text-white shadow-[inset_3px_0_#f62570]' : 'text-[#aaa4b8] hover:bg-white/8 hover:text-white'}`}
          href={item.href}
          key={item.label}
          onClick={() => onSelect?.(item.label)}
          aria-current={activeItem === item.label ? 'page' : undefined}
        >
          <span className="w-[19px] text-center text-[17px]" aria-hidden="true">{item.icon}</span>
          <span>{item.label}</span>
        </a>
      ))}
    </nav>
  );
}