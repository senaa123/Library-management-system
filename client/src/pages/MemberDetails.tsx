import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import MemberQrCard from "../components/MemberQrCard";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import api from "../services/axiosConfig";
import type { Loan, UserProfile } from "../types/library";

function MemberDetails() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [member, setMember] = useState<UserProfile | null>(null);
  const [activeLoans, setActiveLoans] = useState<Loan[]>([]);
  const [isLoading, setIsLoading] = useState(true);

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
        alert(error.response?.data?.message ?? "Failed to load member details.");
        navigate("/members");
      })
      .finally(() => setIsLoading(false));
  }, [id, navigate]);

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
    </div>
  );
}

export default MemberDetails;
