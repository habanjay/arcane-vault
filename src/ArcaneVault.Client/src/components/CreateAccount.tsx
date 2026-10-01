import { useState, type FormEvent, type ReactNode } from 'react';

type StrengthScore = 0 | 1 | 2 | 3 | 4;

interface FormFieldProps {
  id: string;
  label: string;
  children: ReactNode;
}

function BrandPanel() {
  return (
    <section className="relative flex min-h-[285px] flex-col justify-between overflow-hidden bg-linear-to-br from-[#573049] to-[#281e3e] px-[26px] py-7 text-white sm:px-[38px] sm:py-9 lg:min-h-0" aria-labelledby="welcome-title">
      <div className="absolute -right-30 -top-32 size-[310px] rounded-full border-[48px] border-[#ff7d82]/13" aria-hidden="true" />
      <div className="absolute -bottom-31 -left-23 size-[260px] rounded-full bg-[#f62570]/10" aria-hidden="true" />
      <a className="relative z-1 flex items-center gap-[11px] text-white no-underline" href="#create-account" aria-label="Arcane Vault home">
        <span className="grid size-10 place-items-center rounded-xl bg-linear-to-br from-[#ff4a87] to-[#cb185b] text-[21px] shadow-[0_8px_18px_rgba(246,37,112,0.22)]" aria-hidden="true">✦</span>
        <span>
          <strong className="block font-display text-base tracking-[-0.4px]">Arcane Vault</strong>
          <small className="mt-0.5 block text-[10px] text-[#bdb4c6]">Secure your world</small>
        </span>
      </a>
      <div className="relative z-1 mt-[45px] max-w-[360px] lg:mt-auto lg:max-w-[280px]">
        <p className="mb-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#d7c4d2]">Start fresh</p>
        <h1 id="welcome-title" className="mb-[15px] font-display text-[28px] leading-[1.08] tracking-[-1.5px] sm:text-[38px]">A safer place for every secret.</h1>
        <p className="text-xs leading-[1.65] text-[#c9c0cf]">Create one secure home for the passwords and accounts that keep your world moving.</p>
      </div>
      <div className="relative z-1 mt-[30px] grid gap-3 lg:mt-[42px]" aria-label="Security benefits">
        <SecurityItem icon="⌁">Private by design</SecurityItem>
        <SecurityItem icon="✓">Your vault, under your control</SecurityItem>
      </div>
    </section>
  );
}

function SecurityItem({ icon, children }: { icon: string; children: ReactNode }) {
  return (
    <div className="flex items-center gap-2.5 text-[11px] text-[#ded5e1]">
      <span className="grid size-[29px] place-items-center rounded-lg bg-[#f62570]/30 text-white" aria-hidden="true">{icon}</span>
      <span>{children}</span>
    </div>
  );
}

function FormField({ id, label, children }: FormFieldProps) {
  return (
    <label className="mb-3.5 block" htmlFor={id}>
      <span className="mb-1.5 block text-[11px] font-bold text-[#4d5670]">{label}</span>
      {children}
    </label>
  );
}

function InputControl({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <span className={`flex min-h-11 items-center rounded-[10px] border border-[#e5e9f0] bg-[#fbfcfd] transition-shadow focus-within:border-[#f62570] focus-within:shadow-[0_0_0_3px_rgba(246,37,112,0.1)] ${className}`}>{children}</span>;
}

function AccountForm() {
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [password, setPassword] = useState('');
  const [status, setStatus] = useState('');

  const strengthScore: StrengthScore = password.length >= 16 ? 4 : password.length >= 12 ? 3 : password.length >= 8 ? 2 : password.length > 0 ? 1 : 0;
  const strengthLabel = strengthScore === 4 ? 'Excellent' : strengthScore === 3 ? 'Strong' : strengthScore === 2 ? 'Good start' : 'Use 8+ characters';

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const confirmPassword = form.elements.namedItem('confirm-password');

    if (!(confirmPassword instanceof HTMLInputElement)) return;
    confirmPassword.setCustomValidity(confirmPassword.value === password ? '' : 'Passwords must match.');

    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    setStatus('Your vault is ready to be created.');
  };

  return (
    <section className="flex flex-col justify-center px-5 py-[30px] sm:px-[26px] sm:py-[38px] lg:px-[clamp(30px,6vw,72px)] lg:py-[42px]" aria-labelledby="signup-title">
      <div className="mb-[23px]">
        <h2 id="signup-title" className="mb-1.5 font-display text-[25px] tracking-[-0.8px]">Create your account</h2>
        <p className="m-0 text-xs text-[#727891]">It only takes a minute to get started.</p>
      </div>
      <form onSubmit={handleSubmit} noValidate={false}>
        <FormField id="name" label="Your name">
          <InputControl><input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-[11px] text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="name" name="name" type="text" placeholder="Alex Morgan" autoComplete="name" required /></InputControl>
        </FormField>
        <FormField id="email" label="Email address">
          <InputControl><input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-[11px] text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="email" name="email" type="email" placeholder="you@example.com" autoComplete="email" required /></InputControl>
        </FormField>
        <FormField id="password" label="Master password">
          <InputControl className="relative">
            <input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-[11px] pr-[58px] text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="password" name="password" type={passwordVisible ? 'text' : 'password'} placeholder="Create a strong password" autoComplete="new-password" minLength={8} value={password} onChange={(event) => { setPassword(event.target.value); setStatus(''); }} required />
            <button className="absolute right-2 rounded-[7px] border-0 bg-[#fff0f5] px-2.5 py-1.5 text-[10px] font-bold text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={() => setPasswordVisible((visible) => !visible)} aria-label={passwordVisible ? 'Hide master password' : 'Show master password'}>{passwordVisible ? 'Hide' : 'Show'}</button>
          </InputControl>
        </FormField>
        <div className="-mt-1 mb-3.5 flex items-center gap-2.25" aria-label={`Password strength: ${strengthLabel}`}>
          <div className="flex flex-1 gap-1" aria-hidden="true">{[0, 1, 2, 3].map((part) => <i className={`h-1 flex-1 rounded-full ${part < strengthScore ? 'bg-[#16b887]' : 'bg-[#e6e9ef]'}`} key={part} />)}</div>
          <span className="text-[10px] text-[#727891]">{strengthLabel}</span>
        </div>
        <FormField id="confirm-password" label="Confirm master password">
          <InputControl><input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-[11px] text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="confirm-password" name="confirm-password" type="password" placeholder="Repeat your password" autoComplete="new-password" minLength={8} onChange={(event) => event.currentTarget.setCustomValidity('')} required /></InputControl>
        </FormField>
        <label className="mb-[21px] mt-0.5 flex items-start gap-2 text-[10px] leading-[1.45] text-[#727891]">
          <input className="mt-px size-[15px] shrink-0 accent-[#f62570]" name="terms" type="checkbox" required />
          <span>I agree to the <a className="font-bold text-[#f62570] no-underline hover:underline" href="#terms">Terms of Service</a> and <a className="font-bold text-[#f62570] no-underline hover:underline" href="#privacy">Privacy Policy</a>.</span>
        </label>
        <button className="min-h-[46px] w-full rounded-[10px] border-0 bg-linear-to-r from-[#f62570] to-[#ff7d82] text-xs font-bold text-white shadow-[0_9px_18px_rgba(246,37,112,0.18)] transition-[filter] hover:brightness-[0.97] focus-visible:outline-2 focus-visible:outline-offset-3 focus-visible:outline-[#f62570]" type="submit">Create my vault <span aria-hidden="true">→</span></button>
        <p className="m-[10px_0_0] min-h-[18px] text-center text-[11px] text-[#f62570]" role="status" aria-live="polite">{status}</p>
      </form>
      <p className="mt-[23px] text-center text-[11px] text-[#727891]">Already have an account? <a className="font-bold text-[#f62570] no-underline hover:underline" href="#sign-in">Sign in</a></p>
    </section>
  );
}

export default function CreateAccount() {
  return (
    <div className="grid min-h-screen w-full place-items-center p-[14px] sm:p-5">
      <main className="grid w-[min(100%,940px)] overflow-hidden rounded-[22px] bg-white shadow-[0_20px_45px_rgba(24,29,65,0.12)] sm:min-h-[650px] sm:rounded-[28px] lg:grid-cols-[minmax(280px,0.86fr)_minmax(360px,1.14fr)]" id="create-account">
      <BrandPanel />
      <AccountForm />
      </main>
    </div>
  );
}
