import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';

const categories = ['Browser', 'Mobile app', 'Payment', 'Social media'];
const vaultIcons = ['◎', '◈', '✦', '✣'];

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="block"><span className="mb-[5px] block text-xs font-medium text-[#727891]">{label}</span>{children}</label>;
}

function Control({ icon, children, invalid = false }: { icon?: string; children: ReactNode; invalid?: boolean }) {
  return <span className={`flex min-h-[43px] items-center rounded-lg border bg-white ${invalid ? 'border-[#ff5275]' : 'border-[#d4d7df]'} focus-within:border-[#f62570] focus-within:shadow-[0_0_0_3px_rgba(246,37,112,0.1)]`}>{icon && <span className="ml-[10px] w-7 shrink-0 text-center text-[19px] text-[#545768]" aria-hidden="true">{icon}</span>}{children}</span>;
}

export default function AddPassword() {
  const [iconIndex, setIconIndex] = useState(0);
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [tags, setTags] = useState(['Design', 'Browser', 'Login']);
  const [status, setStatus] = useState('');

  const addTag = () => {
    const tagName = window.prompt('Name this tag');
    if (!tagName?.trim()) return;
    setTags((current) => [...current, tagName.trim()]);
  };

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!event.currentTarget.checkValidity()) {
      event.currentTarget.reportValidity();
      return;
    }
    setStatus('Vault ready to be created.');
  };

  return <main className="min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] min-[760px]:py-7">
    <div className="mx-auto min-h-screen w-full bg-linear-to-b from-[#f0edf3] to-[#f1f5f8] px-5 pb-4 pt-[35px] min-[760px]:min-h-[712px] min-[760px]:w-[min(100%,760px)] min-[760px]:rounded-[24px] min-[760px]:shadow-[0_20px_45px_rgba(24,29,65,0.12)] max-[380px]:px-[18px]">
      <header className="mb-[18px] flex items-center gap-[37px]"><a className="grid size-9 shrink-0 place-items-center rounded-full bg-white text-[27px] leading-none text-[#172047] no-underline shadow-[0_8px_18px_rgba(24,29,65,0.06)] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#vaults" aria-label="Back to my vaults">←</a><h1 className="font-display text-[17px] tracking-[-0.5px]">Create New Vaults</h1></header>
      <section className="mb-4 text-center" aria-label="Vault icon"><button className="mx-auto mb-[9px] grid size-[51px] place-items-center rounded-full border-0 bg-[#f62570] text-[25px] text-white shadow-[0_8px_20px_rgba(246,37,112,0.16)] hover:bg-[#d9185e] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={() => setIconIndex((current) => (current + 1) % vaultIcons.length)} aria-label="Change vault icon">{vaultIcons[iconIndex]}</button><span className="block text-[11px]">Change icon</span></section>
      <form className="grid gap-4" onSubmit={submit}>
        <section className="grid gap-[11px] rounded-[15px] bg-white p-[15px]" aria-labelledby="credential-title"><h2 className="mb-1 text-sm font-bold text-[#112034]" id="credential-title">Credential</h2><Field label="Select Categories"><Control><select className="w-full appearance-none border-0 bg-transparent px-[10px] py-[11px] pr-[34px] text-xs font-semibold text-[#4a4d60] outline-0" defaultValue="Browser" aria-label="Select category">{categories.map((category) => <option key={category}>{category}</option>)}</select><span className="pointer-events-none -ml-7 mr-[13px] text-[17px] font-bold" aria-hidden="true">⌄</span></Control></Field><Field label="Site Address"><Control icon="⊕"><input className="min-w-0 flex-1 border-0 bg-transparent px-0 py-[11px] text-xs font-semibold text-[#4a4d60] outline-0" type="url" defaultValue="https://www.dribbble.com" aria-label="Site address" required /></Control></Field><Field label="User Name"><Control icon="♧" invalid><input className="min-w-0 flex-1 border-0 bg-transparent px-0 py-[11px] text-xs font-semibold text-[#4a4d60] outline-0" type="email" defaultValue="hello@designmonk.co" aria-label="User name" required /></Control></Field><Field label="Password"><Control icon="🔒"><input className="min-w-0 flex-1 border-0 bg-transparent px-0 py-[11px] text-xs font-semibold tracking-[2px] text-[#4a4d60] outline-0" type={passwordVisible ? 'text' : 'password'} defaultValue="vault-password" aria-label="Password" required /><button className="mr-[9px] rounded-lg border-0 bg-[#ffebeb] px-[10px] py-[5px] text-[10px] text-[#f0526d] hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={() => setPasswordVisible((current) => !current)}>{passwordVisible ? 'Hide' : 'View'}</button></Control></Field></section>
        <section className="rounded-[15px] bg-white p-[15px]" aria-labelledby="tags-title"><h2 className="mb-[13px] text-sm font-bold text-[#112034]" id="tags-title">Add Tag</h2><div className="flex flex-wrap gap-[7px]">{tags.map((tag) => <button className="rounded-[7px] border-0 bg-[#ffebeb] px-[10px] py-2 text-[11px] text-[#f62570] hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" key={tag}>{tag}</button>)}<button className="rounded-[7px] border-0 bg-[#f0f3f6] px-[10px] py-2 text-[11px] text-[#4a5062] hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" id="add-tag" type="button" onClick={addTag}>Add +</button></div></section>
        <button className="min-h-[46px] w-full rounded-[14px] border-0 bg-linear-to-r from-[#f62570] to-[#ff6e76] text-xs font-bold text-white shadow-[0_9px_18px_rgba(246,37,112,0.16)] hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="submit">Create the vault</button><p className="min-h-[18px] text-center text-[11px] text-[#f62570]" role="status" aria-live="polite">{status}</p>
      </form>
    </div>
  </main>;
}