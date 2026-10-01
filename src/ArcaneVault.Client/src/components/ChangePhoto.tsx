import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import Header from './layout/Header';
import MobileNavigation from './layout/MobileNavigation';
import Sidebar from './layout/Sidebar';

const profile = {
  firstName: 'Design',
  lastName: 'Monks',
  role: 'Account owner',
};
const maxPhotoSize = 5 * 1024 * 1024;

export default function ChangePhoto() {
  const inputRef = useRef<HTMLInputElement>(null);
  const [photoUrl, setPhotoUrl] = useState('');
  const [status, setStatus] = useState('');

  const initials = `${profile.firstName[0]}${profile.lastName[0]}`;

  useEffect(() => () => {
    if (photoUrl) URL.revokeObjectURL(photoUrl);
  }, [photoUrl]);

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setStatus('Choose an image file to continue.');
      return;
    }

    if (file.size > maxPhotoSize) {
      setStatus('Choose an image smaller than 5 MB.');
      return;
    }

    setPhotoUrl(URL.createObjectURL(file));
    setStatus('');
  };

  const removePhoto = () => {
    setPhotoUrl((currentUrl) => {
      if (currentUrl) URL.revokeObjectURL(currentUrl);
      return '';
    });
    if (inputRef.current) inputRef.current.value = '';
    setStatus('Photo removed. Save your changes to keep this update.');
  };

  const savePhoto = () => setStatus(photoUrl ? 'Your profile photo has been updated.' : 'Your initials will be used as your profile photo.');

  return <div className="flex min-h-screen min-w-[320px] bg-[#dfe5eb] font-sans text-[#172047] max-[840px]:bg-[#f1f5f8]"><Sidebar activeItem="Settings" /><main className="min-w-0 flex-1 bg-[#f1f5f8] px-[18px] pb-[92px] pt-8 min-[481px]:pt-6 min-[841px]:px-[42px] min-[841px]:pb-[42px] min-[841px]:pt-[35px]"><Header title="Change photo" onNotify={() => setStatus('You are all caught up.')} /><div className="max-w-[760px]"><a className="mb-5 inline-flex items-center gap-2 text-[11px] font-bold text-[#727891] no-underline hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#settings" aria-label="Back to settings">← <span>Back to settings</span></a><section className="rounded-[20px] border border-[#e5e9f0] bg-white p-[25px] shadow-[0_16px_35px_rgba(24,29,65,0.08)] max-[480px]:rounded-[18px] max-[480px]:p-[18px]" aria-labelledby="change-photo-title"><div className="mb-7"><h2 className="font-display text-base tracking-[-0.4px]" id="change-photo-title">Your profile photo</h2><p className="mt-[5px] text-[11px] leading-[1.5] text-[#727891]">Choose a clear image so your team can recognize you in shared vaults.</p></div><div className="grid gap-7 min-[600px]:grid-cols-[190px_1fr] min-[600px]:items-center"><div className="grid justify-items-center gap-3"><div className="grid size-[150px] place-items-center overflow-hidden rounded-[32px] bg-linear-to-br from-[#f35488] to-[#8e356f] font-display text-4xl font-bold text-white shadow-[0_16px_30px_rgba(142,53,111,0.2)]">{photoUrl ? <img className="size-full object-cover" src={photoUrl} alt="" /> : initials}</div><span className="text-[10px] text-[#727891]">{profile.firstName} {profile.lastName}</span></div><div><div className="rounded-[14px] border border-dashed border-[#f0b7cc] bg-[#fff8fa] p-5 text-center"><span className="mb-2 block text-2xl text-[#f62570]" aria-hidden="true">↑</span><strong className="block text-xs">Upload a new photo</strong><p className="mt-1 text-[10px] leading-[1.5] text-[#727891]">JPG, PNG, or GIF up to 5 MB.</p><input ref={inputRef} className="sr-only" id="profile-photo" type="file" accept="image/png,image/jpeg,image/gif" onChange={handleFileChange} /><label className="mt-4 inline-block cursor-pointer rounded-[9px] bg-[#f62570] px-[18px] py-[11px] text-[11px] font-bold text-white shadow-[0_8px_18px_rgba(246,37,112,0.18)] hover:bg-[#d9185e] focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-[#f62570]" htmlFor="profile-photo">Choose photo</label></div><button className="mt-3 border-0 bg-transparent px-1 py-2 text-[10px] font-bold text-[#727891] hover:text-[#f62570] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={removePhoto}>Remove current photo</button></div></div><div className="mt-7 flex items-center justify-end gap-[13px] border-t border-[#e5e9f0] pt-[22px] max-[480px]:justify-between"><p className="mr-auto text-[10px] text-[#f62570]" role="status" aria-live="polite">{status}</p><a className="px-[15px] py-2.5 text-[11px] font-bold text-[#727891] no-underline hover:text-[#172047] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" href="#settings">Cancel</a><button className="rounded-[9px] bg-linear-to-r from-[#f62570] to-[#ff7d82] px-[18px] py-[11px] text-[11px] font-bold text-white shadow-[0_8px_18px_rgba(246,37,112,0.18)] hover:brightness-[0.97] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#f62570]" type="button" onClick={savePhoto}>Save photo</button></div></section></div></main><MobileNavigation activeItem="Settings" /></div>;
}
