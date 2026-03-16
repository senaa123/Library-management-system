import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../services/axiosConfig";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import type { Loan } from "../types/library";

function AdminLoans() {
  const navigate = useNavigate();
  const [loans, setLoans] = useState<Loan[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadLoans = (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    // Staff always see the live list of books that are still out with members.
    api.get("/Loans", { params: { activeOnly: true } })
      .then((response) => setLoans(response.data))
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to load issued books.");
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
        const message = typeof error === "object" && error && "response" in error
          ? (error as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
        alert(message ?? "Failed to load issued books.");
      } finally {
        setIsLoading(false);
      }
    };

    void fetchInitialLoans();
  }, [navigate]);

  const handleReceived = (loanId: number) => {
    // Returning through the API updates the loan status and adds the copy back into library stock.
    api.post(`/Loans/${loanId}/return`)
      .then(() => {
        alert("Book marked as received.");
        loadLoans();
      })
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to mark this book as received.");
      });
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
                        onClick={() => handleReceived(loan.id)}
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
    </div>
  );
}

export default AdminLoans;
