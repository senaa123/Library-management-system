import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getStoredRole, isLoggedIn, isMemberRole } from "../lib/session";
import { extractApiMessage, notifyError } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { Loan } from "../types/library";

function MyBorrowedBooks() {
  const navigate = useNavigate();
  const [loans, setLoans] = useState<Loan[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isMemberRole(getStoredRole())) {
      navigate("/");
      return;
    }

    // Members only get their own active loans from this endpoint.
    api.get<Loan[]>("/Loans", { params: { activeOnly: true } })
      .then((response) => setLoans(response.data))
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to load borrowed books."));
      })
      .finally(() => setIsLoading(false));
  }, [navigate]);

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Borrowed Section</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">My Borrowed Books</h1>
            <p className="mt-2 text-slate-600">
              Track the books that have already been issued to you by a librarian and see how long is left before each return.
            </p>
          </div>
          <div className="rounded-2xl bg-slate-900 px-5 py-4 text-white shadow-lg">
            <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Active books</p>
            <p className="mt-1 text-3xl font-bold">{loans.length}</p>
          </div>
        </div>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading your borrowed books...</p>
        ) : loans.length === 0 ? (
          <div className="py-10 text-center">
            <p className="text-lg font-semibold text-slate-700">No borrowed books right now.</p>
            <p className="mt-2 text-slate-500">Reserve a title online first, then collect it at the library desk for issuance.</p>
            <div className="mt-6 flex flex-wrap justify-center gap-3">
              <Link
                to="/reservations"
                className="inline-flex rounded-xl bg-indigo-600 px-5 py-3 font-semibold text-white shadow-lg transition hover:bg-indigo-700"
              >
                View my reservations
              </Link>
              <Link
                to="/fines"
                className="inline-flex rounded-xl border border-slate-300 px-5 py-3 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                View my fines
              </Link>
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Borrowed On</th>
                  <th className="px-4 py-3">Due Date</th>
                  <th className="px-4 py-3">Time Left</th>
                  <th className="px-4 py-3">Fine</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {loans.map((loan) => (
                  <tr key={loan.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4">
                      <p className="font-semibold text-slate-900">{loan.bookTitle}</p>
                      <p className="text-sm text-slate-500">ISBN: {loan.isbn || "N/A"}</p>
                    </td>
                    <td className="px-4 py-4">{new Date(loan.issuedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-4">{new Date(loan.dueDate).toLocaleDateString()}</td>
                    <td className="px-4 py-4">
                      <span className={`rounded-full px-3 py-1 text-sm font-semibold ${
                        loan.daysLeft < 0 ? "bg-rose-100 text-rose-700" : "bg-emerald-100 text-emerald-700"
                      }`}>
                        {loan.timeLeftLabel}
                      </span>
                    </td>
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

export default MyBorrowedBooks;
