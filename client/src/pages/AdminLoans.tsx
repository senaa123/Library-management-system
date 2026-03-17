import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../services/axiosConfig";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import { extractApiMessage, notifyError, notifySuccess } from "../lib/notifications";
import type { Loan } from "../types/library";

type ReturnFineType = "DamagedBook" | "LostBook" | "MissingPages";

function AdminLoans() {
  const navigate = useNavigate();
  const [loans, setLoans] = useState<Loan[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loanToReceive, setLoanToReceive] = useState<Loan | null>(null);
  const [addFine, setAddFine] = useState(false);
  const [fineType, setFineType] = useState<ReturnFineType>("DamagedBook");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadLoans = (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    // Staff always see the live list of books that are still out with members.
    api.get("/Loans", { params: { activeOnly: true } })
      .then((response) => setLoans(response.data))
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to load issued books."));
      })
      .finally(() => setIsLoading(false));
  };

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isStaffRole(getStoredRole())) {
      navigate("/");
      return;
    }

    const fetchInitialLoans = async () => {
      try {
        const response = await api.get<Loan[]>("/Loans", { params: { activeOnly: true } });
        setLoans(response.data);
      } catch (error: unknown) {
        notifyError(extractApiMessage(error, "Failed to load issued books."));
      } finally {
        setIsLoading(false);
      }
    };

    void fetchInitialLoans();
  }, [navigate]);

  const handleReceived = async () => {
    if (!loanToReceive) {
      return;
    }

    setIsSubmitting(true);

    try {
      await api.post(`/Loans/${loanToReceive.id}/return`, {
        addFine,
        fineType: addFine ? fineType : null,
      });

      notifySuccess("Book marked as received.");
      setLoanToReceive(null);
      setAddFine(false);
      setFineType("DamagedBook");
      loadLoans();
    } catch (error) {
      notifyError(extractApiMessage(error, "Failed to mark this book as received."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Admin Section</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Books Given To Members</h1>
        <p className="mt-2 text-slate-600">
          See who has each book, how long is left, and mark books as received when they return.
        </p>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading active loans...</p>
        ) : loans.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No books are currently out with members.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[980px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Member</th>
                  <th className="px-4 py-3">Phone</th>
                  <th className="px-4 py-3">Borrowed</th>
                  <th className="px-4 py-3">Due</th>
                  <th className="px-4 py-3">Time Left</th>
                  <th className="px-4 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {loans.map((loan) => (
                  <tr key={loan.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4">
                      <p className="font-semibold text-slate-900">{loan.bookTitle}</p>
                      <p className="text-sm text-slate-500">ISBN: {loan.isbn || "N/A"}</p>
                    </td>
                    <td className="px-4 py-4">
                      <p className="font-medium">{loan.borrowerName || loan.borrowerUsername}</p>
                      <p className="text-sm text-slate-500">@{loan.borrowerUsername}</p>
                    </td>
                    <td className="px-4 py-4">{loan.borrowerPhoneNumber || "Not provided"}</td>
                    <td className="px-4 py-4">{new Date(loan.issuedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-4">{new Date(loan.dueDate).toLocaleDateString()}</td>
                    <td className="px-4 py-4">
                      <span className={`rounded-full px-3 py-1 text-sm font-semibold ${
                        loan.daysLeft < 0 ? "bg-rose-100 text-rose-700" : "bg-amber-100 text-amber-700"
                      }`}>
                        {loan.timeLeftLabel}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-center">
                      <button
                        onClick={() => {
                          setLoanToReceive(loan);
                          setAddFine(false);
                          setFineType("DamagedBook");
                        }}
                        className="rounded-xl bg-emerald-600 px-4 py-2 font-semibold text-white transition hover:bg-emerald-700"
                      >
                        Received
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {loanToReceive && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-emerald-500">Receive Book</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{loanToReceive.bookTitle}</h2>
            <p className="mt-2 text-slate-600">
              Confirm the return, and optionally add a condition fine to the member account.
            </p>

            <div className="mt-6 rounded-2xl bg-slate-50 p-4 ring-1 ring-slate-200">
              <label className="flex items-center gap-3 text-sm font-semibold text-slate-800">
                <input
                  type="checkbox"
                  checked={addFine}
                  onChange={(event) => setAddFine(event.target.checked)}
                />
                Add a fine
              </label>

              {addFine && (
                <div className="mt-4">
                  <label className="mb-2 block text-sm font-semibold text-slate-700">Fine type</label>
                  <select
                    value={fineType}
                    onChange={(event) => setFineType(event.target.value as ReturnFineType)}
                    className="w-full rounded-2xl border border-slate-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                  >
                    <option value="DamagedBook">Damaged Book: $0.50</option>
                    <option value="LostBook">Lost Book: $10.00</option>
                    <option value="MissingPages">Missing Pages: $1.00</option>
                  </select>
                </div>
              )}
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={() => setLoanToReceive(null)}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={handleReceived}
                disabled={isSubmitting}
                className="rounded-xl bg-emerald-600 px-5 py-2 font-semibold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting ? "Saving..." : "Confirm receive"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminLoans;
