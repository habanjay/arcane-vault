interface HeaderProps {
  title: string;
  onNotify: () => void;
}

export default function Header({ title, onNotify }: HeaderProps) {
  return (
    <header className="mb-[25px] flex items-center justify-center gap-5 min-[481px]:mb-[30px] min-[481px]:justify-between min-[841px]:mb-[33px]">
      <div className="text-center min-[481px]:text-left"><p className="mb-[7px] text-xs text-[#727891] max-[480px]:hidden">Saturday, September 26, 2026</p><h1 className="font-display text-lg tracking-[-0.5px] min-[481px]:text-[clamp(24px,3vw,34px)] min-[481px]:tracking-[-1.2px]">{title}</h1></div>
      <div className="hidden items-center gap-[14px] min-[481px]:flex"><button className="grid size-[42px] place-items-center rounded-full border border-[#e5e9f0] bg-white text-lg text-[#172047] hover:border-[#f62570] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={onNotify} aria-label="Notifications">♧</button><a className="grid size-[42px] place-items-center rounded-full bg-linear-to-br from-[#e94079] to-[#8d356d] text-xs font-bold text-white no-underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#profile" aria-label="Open profile">DM</a></div>
    </header>
  );
}