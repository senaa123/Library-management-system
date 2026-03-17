import { useEffect, useState } from "react";
import { subscribeToNotices, type Notice } from "../lib/notifications";

function toneClasses(tone: Notice["tone"]) {
  if (tone === "success") {
    return "border-emerald-200 bg-emerald-50 text-emerald-800";
  }

  if (tone === "error") {
    return "border-rose-200 bg-rose-50 text-rose-800";
  }

  return "border-slate-200 bg-white text-slate-800";
}

function ToastViewport() {
  const [notices, setNotices] = useState<Notice[]>([]);

  useEffect(() => {
    return subscribeToNotices((notice) => {
      setNotices((current) => [...current, notice]);

      window.setTimeout(() => {
        setNotices((current) => current.filter((entry) => entry.id !== notice.id));
      }, 4500);
    });
  }, []);

  if (notices.length === 0) {
    return null;
  }

  return (
    <div className="pointer-events-none fixed bottom-5 right-5 z-[70] flex w-full max-w-sm flex-col gap-3">
      {notices.map((notice) => (
        <div
          key={notice.id}
          className={`pointer-events-auto rounded-2xl border px-4 py-3 text-sm font-medium shadow-xl backdrop-blur ${toneClasses(notice.tone)}`}
        >
          {notice.message}
        </div>
      ))}
    </div>
  );
}

export default ToastViewport;
