import { useState } from 'react';
import Footer from './layout/Footer';
import Header from './layout/Header';
import MobileNavigation from './layout/MobileNavigation';
import Sidebar from './layout/Sidebar';

type CharacterType = 'lowercase' | 'uppercase' | 'numbers' | 'symbols';

const alphabets: Record<CharacterType, string> = {
  lowercase: 'abcdefghijkmnopqrstuvwxyz',
  uppercase: 'ABCDEFGHJKLMNPQRSTUVWXYZ',
  numbers: '23456789',
  symbols: '!@#$%^&*()-_=+[]{}:,.?',
};

const characterOptions: Array<{ id: CharacterType; label: string }> = [
  { id: 'lowercase', label: 'Lowercase letters' },
  { id: 'uppercase', label: 'Uppercase letters' },
  { id: 'numbers', label: 'Numbers' },
  { id: 'symbols', label: 'Symbols' },
];

function randomIndex(maximum: number): number {
  const values = new Uint32Array(1);
  const limit = Math.floor(0x100000000 / maximum) * maximum;
  do {
    crypto.getRandomValues(values);
  } while (values[0] >= limit);
  return values[0] % maximum;
}

function buildPassword(length: number, selectedTypes: CharacterType[], excludeAmbiguous: boolean): string {
  const available = selectedTypes.map((type) => {
    const alphabet = alphabets[type];
    return excludeAmbiguous ? alphabet.replace(/[Il1O0o]/g, '') : alphabet;
  });
  const pool = available.join('');
  const characters = available.map((alphabet) => alphabet[randomIndex(alphabet.length)]);

  while (characters.length < length) {
    characters.push(pool[randomIndex(pool.length)]);
  }

  for (let index = characters.length - 1; index > 0; index -= 1) {
    const swapIndex = randomIndex(index + 1);
    [characters[index], characters[swapIndex]] = [characters[swapIndex], characters[index]];
  }

  return characters.join('');
}

function strengthFor(poolSize: number, length: number): { score: number; label: string } {
  const entropy = Math.round(length * Math.log2(poolSize));
  const score = entropy >= 100 ? 4 : entropy >= 70 ? 3 : entropy >= 50 ? 2 : 1;
  return { score, label: score === 4 ? 'Excellent' : score === 3 ? 'Strong' : score === 2 ? 'Fair' : 'Weak' };
}

export default function PasswordGenerator() {
  const [length, setLength] = useState(20);
  const [selectedTypes, setSelectedTypes] = useState<CharacterType[]>(characterOptions.map(({ id }) => id));
  const [excludeAmbiguous, setExcludeAmbiguous] = useState(false);
  const [password, setPassword] = useState(() => buildPassword(20, characterOptions.map(({ id }) => id), false));
  const [copyStatus, setCopyStatus] = useState('');

  const poolSize = selectedTypes.reduce((total, type) => {
    const alphabet = excludeAmbiguous ? alphabets[type].replace(/[Il1O0o]/g, '') : alphabets[type];
    return total + alphabet.length;
  }, 0);
  const strength = strengthFor(poolSize, length);

  const generatePassword = () => {
    setPassword(buildPassword(length, selectedTypes, excludeAmbiguous));
    setCopyStatus('');
  };

  const toggleType = (type: CharacterType) => {
    const nextTypes = selectedTypes.includes(type)
      ? selectedTypes.length === 1 ? selectedTypes : selectedTypes.filter((item) => item !== type)
      : [...selectedTypes, type];
    setSelectedTypes(nextTypes);
    setPassword(buildPassword(length, nextTypes, excludeAmbiguous));
    setCopyStatus('');
  };

  const updateLength = (nextLength: number) => {
    setLength(nextLength);
    setPassword(buildPassword(nextLength, selectedTypes, excludeAmbiguous));
    setCopyStatus('');
  };

  const updateAmbiguousCharacters = (nextValue: boolean) => {
    setExcludeAmbiguous(nextValue);
    setPassword(buildPassword(length, selectedTypes, nextValue));
    setCopyStatus('');
  };

  const copyPassword = async () => {
    try {
      await navigator.clipboard.writeText(password);
      setCopyStatus('Password copied to clipboard');
    } catch {
      setCopyStatus('Copy unavailable. Select the password to copy it.');
    }
  };

  return <div className="flex min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] max-[840px]:bg-[#f1f5f8]">
    <Sidebar activeItem="Password generator" />
    <main className="min-w-0 flex-1 bg-[#f1f5f8] px-[14px] pb-[92px] pt-5 min-[481px]:px-[18px] min-[481px]:pt-6 min-[841px]:px-[42px] min-[841px]:pb-[42px] min-[841px]:pt-[35px]">
      <Header title="Password generator" onNotify={() => setCopyStatus('You are all caught up.')} />
      <div className="mx-auto grid max-w-[1100px] gap-[18px] min-[841px]:grid-cols-[minmax(310px,0.86fr)_minmax(420px,1.35fr)] min-[841px]:gap-6">
        <section className="relative min-h-[300px] overflow-hidden rounded-[18px] bg-linear-to-br from-[#573049] to-[#281e3e] p-5 text-white shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[481px]:rounded-[25px] min-[481px]:p-7 min-[841px]:min-h-[500px]" aria-labelledby="generator-intro-title">
          <p className="relative z-1 m-0 text-[11px] font-bold uppercase tracking-[1px] text-[#c4b9c7]">Security tool</p>
          <h2 className="relative z-1 mb-[13px] mt-[9px] max-w-[260px] font-display text-[23px] leading-[1.15] min-[481px]:text-[26px]" id="generator-intro-title">Create a password that stands alone.</h2>
          <p className="relative z-1 m-0 max-w-[275px] text-xs text-[#c9c0cf]">Generate unique credentials locally in your browser. Nothing is saved, synced, or sent away.</p>
          <div className="relative z-1 mt-7 grid gap-[13px] min-[841px]:absolute min-[841px]:inset-x-7 min-[841px]:bottom-[30px] min-[841px]:mt-0">
            {['Cryptographically random', 'Built for every account', 'Never stored by Arcane Vault'].map((principle, index) => <div className="flex items-center gap-[11px] text-[11px] text-[#e9e3ed]" key={principle}><span className="grid size-[27px] shrink-0 place-items-center rounded-lg bg-[#f62570]/30 text-white" aria-hidden="true">{['⌁', '◈', '✓'][index]}</span><b>{principle}</b></div>)}
          </div>
        </section>
        <section className="rounded-[18px] border border-[#e5e9f0] bg-white p-5 shadow-[0_16px_35px_rgba(24,29,65,0.08)] min-[481px]:rounded-[20px] min-[481px]:p-[27px]" aria-labelledby="recipe-title">
          <div className="mb-5"><h2 className="m-0 font-display text-[17px] tracking-[-0.4px]" id="recipe-title">Password recipe</h2><p className="m-0 mt-[5px] text-[11px] text-[#727891]">Use a different password for every service.</p></div>
          <div className="rounded-[15px] bg-[#f7f8fa] p-5"><label className="mb-[9px] block text-[10px] font-bold uppercase tracking-[0.8px] text-[#727891]" htmlFor="password-output">Generated password</label><div className="flex min-h-[55px] items-center gap-[10px] rounded-[11px] border border-[#e5e9f0] bg-white px-3"><output className="min-w-0 flex-1 overflow-auto whitespace-nowrap font-mono text-[13px] tracking-[0.3px] min-[481px]:text-base" id="password-output" aria-live="polite">{password}</output><button className="grid size-[38px] shrink-0 place-items-center rounded-[9px] border-0 bg-[#281e3e] text-[17px] text-white hover:bg-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#43b8ff]" type="button" onClick={copyPassword} aria-label="Copy password" title="Copy password">▣</button></div><p className="mb-[-16px] mt-2 min-h-4 text-[10px] text-[#16b887]" aria-live="polite">{copyStatus}</p></div>
          <div className="my-[22px] flex items-center gap-3"><div className="flex flex-1 gap-[5px]" aria-label={`Password strength: ${strength.label}`} role="img">{[0, 1, 2, 3].map((part) => <i className={`h-1.5 flex-1 rounded ${part < strength.score ? strength.score < 3 ? 'bg-[#ff7d82]' : 'bg-[#16b887]' : 'bg-[#e6e9ef]'}`} key={part} />)}</div><strong className={`min-w-[73px] text-right text-[11px] ${strength.score < 3 ? 'text-[#ff7d82]' : 'text-[#16b887]'}`}>{strength.label}</strong></div>
          <div className="border-t border-[#e5e9f0] py-[17px]"><div className="mb-[14px] flex items-center justify-between gap-3"><span className="text-xs font-bold">Password length</span><span className="text-xs font-bold text-[#f62570]">{length} characters</span></div><input className="w-full accent-[#f62570]" type="range" min="12" max="64" value={length} onChange={(event) => updateLength(Number(event.target.value))} aria-label="Password length" /><div className="mt-[5px] flex justify-between text-[10px] text-[#727891]"><span>12</span><span>64</span></div></div>
          <fieldset className="border-t border-[#e5e9f0] py-[17px]"><legend className="mb-[14px] text-xs font-bold">Character types</legend><div className="grid grid-cols-1 gap-[11px] min-[481px]:grid-cols-2 min-[481px]:gap-x-[15px]"><>{characterOptions.map(({ id, label }) => <label className="flex items-center gap-[9px] text-[11px] text-[#4d5670]" key={id}><input className="size-4 accent-[#f62570]" type="checkbox" checked={selectedTypes.includes(id)} onChange={() => toggleType(id)} />{label}</label>)}</></div></fieldset>
          <label className="flex items-center gap-[9px] border-t border-[#e5e9f0] py-[17px] text-[11px] text-[#4d5670]"><input className="size-4 accent-[#f62570]" type="checkbox" checked={excludeAmbiguous} onChange={(event) => updateAmbiguousCharacters(event.target.checked)} />Exclude ambiguous characters</label>
          <button className="mt-[5px] w-full rounded-[9px] border-0 bg-[#f62570] p-[13px] font-bold text-white shadow-[0_9px_18px_rgba(246,37,112,0.2)] hover:bg-[#d91b61] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#43b8ff]" type="button" onClick={generatePassword}>Generate new password <span aria-hidden="true">↗</span></button><p className="mb-0 mt-[15px] text-center text-[10px] text-[#727891]">Tip: passwords of 16+ characters are recommended for most accounts.</p>
        </section>
      </div>
      <Footer />
    </main>
    <MobileNavigation activeItem="Password generator" />
  </div>;
}