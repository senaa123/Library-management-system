import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import MemberQrCard from "../components/MemberQrCard";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import { extractApiMessage, notifyError, notifySuccess } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { Loan, UserProfile } from "../types/library";

function MemberDetails() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [member, setMember] = useState<UserProfile | null>(null);
  const [activeLoans, setActiveLoans] = useState<Loan[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [restrictionDays, setRestrictionDays] = useState(7);
  const [restrictionReason, setRestrictionReason] = useState("");
  const [restrictionModalOpen, setRestrictionModalOpen] = useState(false);
  const [isSavingRestriction, setIsSavingRestriction] = useState(false);

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isStaffRole(getStoredRole())) {
      navigate("/");
      return;
    }

    if (!id) {
      navigate("/members");
      return;
    }

    // We load the profile and current loans together so the detail page opens with one staff action.
    Promise.all([
      api.get<UserProfile>(`/Users/${id}`),
      api.get<Loan[]>("/Loans", { params: { memberId: id, activeOnly: true } }),
    ])
      .then(([memberResponse, loanResponse]) => {
        setMember(memberResponse.data);
        setActiveLoans(loanResponse.data);
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to load member details."));
        navigate("/members");
      })
      .finally(() => setIsLoading(false));
  }, [id, navigate]);

  const refreshMember = async () => {
    if (!id) {
      return;
    }

    const [memberResponse, loanResponse] = await Promise.all([
      api.get<UserProfile>(`/Users/${id}`),
      api.get<Loan[]>("/Loans", { params: { memberId: id, activeOnly: true } }),
    ]);

    setMember(memberResponse.data);
    setActiveLoans(loanResponse.data);
  };

  const handleRestrictionSubmit = async () => {
    if (!id || !restrictionReason.trim()) {
      return;
    }

    setIsSavingRestriction(true);

    try {
      await api.put(`/Users/${id}/restriction`, {
        days: restrictionDays,
        reason: restrictionReason,
      });

      notifySuccess("Member restriction saved.");
      setRestrictionModalOpen(false);
      setRestrictionReason("");
      setRestrictionDays(7);
      await refreshMember();
    } catch (error) {
      notifyError(extractApiMessage(error, "Failed to save the member restriction."));
    } finally {
      setIsSavingRestriction(false);
    }
  };

  if (isLoading) {
    return (
      <div className="rounded-3xl border border-white/60 bg-white/80 p-10 text-center shadow-2xl backdrop-blur-xl">
        Loading member details...
      </div>
    );
  }

  if (!member) {
    return null;
  }

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Member Profile</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">{member.fullName || member.username}</h1>
            <p className="mt-2 text-slate-600">Username: @{member.username}</p>
          </div>
          <Link
            to="/members"
            className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            Back to members
          </Link>
        </div>

        <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-2xl bg-slate-900 p-5 text-white">
            <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Phone</p>
            <p className="mt-2 text-lg font-semibold">{member.phoneNumber || "Not provided"}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Email</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">{member.email || "Not provided"}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Role</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">{member.role}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Active borrowed books</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">{activeLoans.length}</p>
          </div>
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">NIC</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">{member.nicNumber || "Not provided"}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Outstanding fine</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">${member.totalOutstandingFine.toFixed(2)}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Current limit</p>
            <p className="mt-2 text-lg font-semibold text-slate-900">{member.maxCirculationItems} books</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Actions</p>
            <button
              onClick={() => setRestrictionModalOpen(true)}
              className="mt-2 rounded-xl bg-rose-600 px-4 py-2 font-semibold text-white transition hover:bg-rose-700"
            >
              Add restriction
            </button>
          </div>
        </div>

        {member.restrictionWarning && (
          <div className={`mt-6 rounded-2xl px-5 py-4 text-sm font-medium ${
            member.isCirculationBlocked
              ? "bg-rose-50 text-rose-800 ring-1 ring-rose-200"
              : "bg-amber-50 text-amber-800 ring-1 ring-amber-200"
          }`}>
            {member.restrictionWarning}
          </div>
        )}
      </div>

      <MemberQrCard
        fullName={member.fullName}
        username={member.username}
        qrCodeValue={member.qrCodeValue}
        subtitle="Scan this member QR code when issuing a walk-in loan or completing a reservation pickup."
      />

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        <h2 className="text-2xl font-bold text-slate-900">Current Borrowed Books</h2>
        {activeLoans.length === 0 ? (
          <p className="mt-4 text-slate-500">This member does not currently have any books out.</p>
        ) : (
          <div className="mt-6 overflow-x-auto">
            <table className="w-full min-w-[780px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Borrowed</th>
                  <th className="px-4 py-3">Due</th>
                  <th className="px-4 py-3">Time Left</th>
                  <th className="px-4 py-3">Fine</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {activeLoans.map((loan) => (
                  <tr key={loan.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4 font-semibold text-slate-900">{loan.bookTitle}</td>
                    <td className="px-4 py-4">{new Date(loan.issuedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-4">{new Date(loan.dueDate).toLocaleDateString()}</td>
                    <td className="px-4 py-4">{loan.timeLeftLabel}</td>
                    <td className="px-4 py-4">${loan.outstandingFine.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {restrictionModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-rose-500">Temporary Restriction</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{member.fullName || member.username}</h2>
            <p className="mt-2 text-slate-600">
              Enter the reason and the number of days this member should be restricted from borrowing or reserving books.
            </p>

            <div className="mt-6 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">Reason</label>
                <textarea
                  value={restrictionReason}
                  onChange={(event) => setRestrictionReason(event.target.value)}
                  rows={3}
                  className="w-full rounded-2xl border border-slate-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-rose-500"
                  placeholder="Explain why this member is being restricted"
                />
              </div>

              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">Days</label>
                <select
                  value={restrictionDays}
                  onChange={(event) => setRestrictionDays(Number(event.target.value))}
                  className="w-full rounded-2xl border border-slate-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-rose-500"
                >
                  {[1, 3, 5, 7, 14, 30].map((days) => (
                    <option key={days} value={days}>
                      {days} {days === 1 ? "day" : "days"}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={() => setRestrictionModalOpen(false)}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={handleRestrictionSubmit}
                disabled={isSavingRestriction || !restrictionReason.trim()}
                className="rounded-xl bg-rose-600 px-5 py-2 font-semibold text-white transition hover:bg-rose-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSavingRestriction ? "Saving..." : "Save restriction"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default MemberDetails;
