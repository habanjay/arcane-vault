import { useState, type FormEvent } from 'react';

function BrandPanel() {
  return (
    <section className="relative flex min-h-[260px] flex-col justify-between overflow-hidden bg-linear-to-br from-[#573049] to-[#281e3e] px-[26px] py-7 text-white sm:min-h-[275px] sm:px-[38px] sm:py-9 lg:min-h-0" aria-labelledby="welcome-title">
      <div className="absolute -right-30 -top-32 size-[310px] rounded-full border-[48px] border-[#ff7d82]/13" aria-hidden="true" />
      <div className="absolute -bottom-31 -left-23 size-[260px] rounded-full bg-[#f62570]/10" aria-hidden="true" />
      <a className="relative z-1 flex items-center gap-[11px] text-white no-underline" href="#login" aria-label="Arcane Vault home">
        <span className="grid size-10 place-items-center rounded-xl bg-linear-to-br from-[#ff4a87] to-[#cb185b] text-[21px] shadow-[0_8px_18px_rgba(246,37,112,0.22)]" aria-hidden="true">✦</span>
        <span>
          <strong className="block font-display text-base tracking-[-0.4px]">Arcane Vault</strong>
          <small className="mt-0.5 block text-[10px] text-[#bdb4c6]">Secure your world</small>
        </span>
      </a>
      <div className="relative z-1 mt-[45px] max-w-[360px] lg:mt-auto lg:max-w-[270px]">
        <p className="mb-3 text-[10px] font-bold uppercase tracking-[1.2px] text-[#d7c4d2]">Welcome back</p>
        <h1 id="welcome-title" className="mb-[15px] font-display text-[28px] leading-[1.08] tracking-[-1.5px] sm:text-[38px]">Your secrets belong somewhere safe.</h1>
        <p className="text-xs leading-[1.65] text-[#c9c0cf]">Keep every password protected, organized, and ready whenever you need it.</p>
      </div>
      <div className="relative z-1 mt-[30px] flex items-center gap-2.5 text-[11px] text-[#ded5e1] lg:mt-[42px]">
        <span className="grid size-[29px] place-items-center rounded-lg bg-[#f62570]/30 text-white" aria-hidden="true">⌁</span>
        <span>Private by design. Secure by default.</span>
      </div>
    </section>
  );
}

function InputControl({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return <span className={`flex min-h-11 items-center rounded-[10px] border border-[#e5e9f0] bg-[#fbfcfd] transition-shadow focus-within:border-[#f62570] focus-within:shadow-[0_0_0_3px_rgba(246,37,112,0.1)] ${className}`}>{children}</span>;
}

function LoginForm() {
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [status, setStatus] = useState('');

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;

    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    setStatus('Vault unlocked for this mockup.');
  };

  return (
    <section className="flex flex-col justify-center px-5 py-[30px] sm:px-[26px] sm:py-[38px] lg:px-[clamp(30px,6vw,72px)] lg:py-12" aria-labelledby="signin-title">
      <div className="mb-[27px]">
        <h2 id="signin-title" className="mb-1.5 font-display text-[25px] tracking-[-0.8px]">Sign in to your vault</h2>
        <p className="m-0 text-xs text-[#727891]">Enter your details to continue.</p>
      </div>
      <form onSubmit={handleSubmit}>
        <label className="mb-[17px] block" htmlFor="email">
          <span className="mb-1.5 block text-[11px] font-bold text-[#4d5670]">Email address</span>
          <InputControl><input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-3 text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="email" name="email" type="email" placeholder="you@example.com" autoComplete="email" required /></InputControl>
        </label>
        <label className="mb-[17px] block" htmlFor="password">
          <span className="mb-1.5 block text-[11px] font-bold text-[#4d5670]">Master password</span>
          <InputControl className="relative">
            <input className="w-full min-w-0 border-0 bg-transparent px-[13px] py-3 pr-[58px] text-[#172047] outline-0 placeholder:text-[#a1a6b5]" id="password" name="password" type={passwordVisible ? 'text' : 'password'} placeholder="Enter your master password" autoComplete="current-password" required />
            <button className="absolute right-2 rounded-[7px] border-0 bg-[#fff0f5] px-2.5 py-1.5 text-[10px] font-bold text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={() => setPasswordVisible((visible) => !visible)} aria-label={passwordVisible ? 'Hide master password' : 'Show master password'}>{passwordVisible ? 'Hide' : 'Show'}</button>
          </InputControl>
        </label>
        <div className="mb-6 mt-0.5 flex items-center justify-between gap-3 text-[11px]">
          <label className="flex items-center gap-1.5 text-[#727891]" htmlFor="remember"><input className="size-[15px] accent-[#f62570]" id="remember" name="remember" type="checkbox" /> Remember me</label>
          <a className="font-bold text-[#f62570] no-underline hover:underline" href="#forgot-password">Forgot password?</a>
        </div>
        <button className="min-h-[46px] w-full rounded-[10px] border-0 bg-linear-to-r from-[#f62570] to-[#ff7d82] text-xs font-bold text-white shadow-[0_9px_18px_rgba(246,37,112,0.18)] transition-[filter] hover:brightness-[0.97] focus-visible:outline-2 focus-visible:outline-offset-3 focus-visible:outline-[#f62570]" type="submit">Unlock my vault <span aria-hidden="true">→</span></button>
        <p className="m-[10px_0_0] min-h-[18px] text-center text-[11px] text-[#f62570]" role="status" aria-live="polite">{status}</p>
      </form>
      <div className="my-5 flex items-center gap-3 text-[10px] text-[#a1a6b5] before:h-px before:flex-1 before:bg-[#e5e9f0] after:h-px after:flex-1 after:bg-[#e5e9f0]"><span>or</span></div>
      <p className="m-0 text-center text-[11px] text-[#727891]">New to Arcane Vault? <a className="font-bold text-[#f62570] no-underline hover:underline" href="#create-account">Create an account</a></p>
    </section>
  );
}

export default function Login() {
  return (
    <main className="grid min-h-screen min-w-[320px] place-items-center bg-[#dfe5eb] px-5 py-0 font-sans text-[#172047] max-[700px]:bg-[#f1f5f8] max-[700px]:py-5 max-[400px]:px-0 max-[400px]:py-0">
      <div className="grid w-full max-w-[940px] overflow-hidden rounded-[28px] bg-white shadow-[0_20px_45px_rgba(24,29,65,0.12)] max-[700px]:max-w-[480px] max-[700px]:rounded-[22px] max-[400px]:rounded-none lg:min-h-[620px] lg:grid-cols-[minmax(280px,0.86fr)_minmax(360px,1.14fr)]">
        <BrandPanel />
        <LoginForm />
      </div>
    </main>
  );
}