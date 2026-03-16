import { QRCodeSVG } from "qrcode.react";

type MemberQrCardProps = {
  fullName: string;
  username: string;
  qrCodeValue: string;
  subtitle: string;
};

function MemberQrCard({ fullName, username, qrCodeValue, subtitle }: MemberQrCardProps) {
  return (
    <div className="rounded-3xl border border-white/60 bg-white/85 p-6 shadow-2xl backdrop-blur-xl">
      <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Member QR</p>
      <h2 className="mt-2 text-2xl font-bold text-slate-900">{fullName || username}</h2>
      <p className="mt-2 text-slate-600">{subtitle}</p>

      <div className="mt-6 flex flex-col items-center gap-4 rounded-3xl bg-slate-50 p-6 ring-1 ring-slate-200">
        <QRCodeSVG value={qrCodeValue} size={180} includeMargin />
        <div className="text-center">
          <p className="text-xs uppercase tracking-[0.25em] text-slate-500">Scan Value</p>
          <p className="mt-2 break-all font-mono text-sm text-slate-700">{qrCodeValue}</p>
        </div>
      </div>
    </div>
  );
}

export default MemberQrCard;
