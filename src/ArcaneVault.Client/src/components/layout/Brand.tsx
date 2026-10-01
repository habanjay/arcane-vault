export default function Brand() {
  return (
    <a className="flex items-center gap-[11px] text-white no-underline" href="#dashboard" aria-label="Arcane Vault home">
      <span className="grid size-[38px] place-items-center rounded-xl bg-linear-to-br from-[#ff4a87] to-[#cb185b] text-xl shadow-[0_8px_18px_rgba(246,37,112,0.22)]" aria-hidden="true">✦</span>
      <span><strong className="block font-display text-base tracking-[-0.4px]">Arcane Vault</strong><small className="mt-0.5 block text-[10px] text-[#a9a2b6]">Secure your world</small></span>
    </a>
  );
}