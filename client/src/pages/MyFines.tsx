import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { isLoggedIn, isMemberRole } from "../lib/session";
import api from "../services/axiosConfig";
import type { FineCheckoutSession, FinePaymentRecord, FineSummary } from "../types/library";
import { extractApiMessage, notifyError, notifySuccess } from "../lib/notifications";

function statusClasses(summary: FineSummary | null) {
  if (!summary) {
    return "bg-slate-100 text-slate-700";
  }

  if (summary.status.isCirculationBlocked) {
    return "bg-rose-100 text-rose-700";
  }

  if (summary.status.isFineLimited) {
    return "bg-amber-100 text-amber-700";
  }

  return "bg-emerald-100 text-emerald-700";
}

function MyFines() {
  const navigate = useNavigate();
  const location = useLocation();
  const [summary, setSummary] = useState<FineSummary | null>(null);
  const [payments, setPayments] = useState<FinePaymentRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isPaying, setIsPaying] = useState(false);
  const processedSessionRef = useRef<string | null>(null);

  const searchParams = useMemo(() => new URLSearchParams(location.search), [location.search]);

  const loadFineData = async (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    try {
      const [summaryResponse, paymentsResponse] = await Promise.all([
        api.get<FineSummary>("/Fines/summary"),
        api.get<FinePaymentRecord[]>("/Fines/payments"),
      ]);

      setSummary(summaryResponse.data);
      setPayments(paymentsResponse.data);
    } catch (error) {
      notifyError(extractApiMessage(error, "Failed to load fine details."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isMemberRole(localStorage.getItem("role"))) {
      navigate("/");
      return;
    }

    void loadFineData();
  }, [navigate]);

  useEffect(() => {
    const sessionId = searchParams.get("session_id");
    const wasCancelled = searchParams.get("payment") === "cancelled";

    if (wasCancelled) {
      notifyError("Stripe checkout was cancelled before the payment was completed.");
      window.history.replaceState({}, document.title, location.pathname);
      return;
    }

    if (!sessionId || processedSessionRef.current === sessionId) {
      return;
    }

    processedSessionRef.current = sessionId;

    void api.post<FinePaymentRecord>("/Fines/checkout/complete", { sessionId })
      .then(() => {
        notifySuccess("Fine payment completed successfully.");
        window.history.replaceState({}, document.title, location.pathname);
        return loadFineData(false);
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to confirm the Stripe payment."));
      });
  }, [location.pathname, searchParams]);

  const handlePayFine = async () => {
    setIsPaying(true);

    try {
      const response = await api.post<FineCheckoutSession>("/Fines/checkout/session");
      window.location.assign(response.data.checkoutUrl);
    } catch (error) {
      notifyError(extractApiMessage(error, "Unable to start Stripe checkout."));
      setIsPaying(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Member Section</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">My Fines</h1>
            <p className="mt-2 max-w-3xl text-slate-600">
              Review overdue and condition-based fines, see the current restriction status, and pay the outstanding amount through Stripe.
            </p>
          </div>
          <div className={`rounded-2xl px-5 py-4 shadow-lg ${statusClasses(summary)}`}>
            <p className="text-xs uppercase tracking-[0.2em]">Outstanding</p>
            <p className="mt-1 text-3xl font-bold">${summary?.totalOutstanding.toFixed(2) ?? "0.00"}</p>
          </div>
        </div>

        {summary?.status.warningMessage && (
          <div className={`mt-6 rounded-2xl px-5 py-4 text-sm font-medium ${
            summary.status.isCirculationBlocked
              ? "bg-rose-50 text-rose-800 ring-1 ring-rose-200"
              : "bg-amber-50 text-amber-800 ring-1 ring-amber-200"
          }`}>
            {summary.status.warningMessage}
          </div>
        )}

        <div className="mt-6 grid gap-4 md:grid-cols-3">
          <div className="rounded-2xl bg-slate-900 p-5 text-white">
            <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Accrued</p>
            <p className="mt-2 text-2xl font-bold">${summary?.totalAccrued.toFixed(2) ?? "0.00"}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Paid so far</p>
            <p className="mt-2 text-2xl font-bold text-slate-900">${summary?.totalPaid.toFixed(2) ?? "0.00"}</p>
          </div>
          <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Current limit</p>
            <p className="mt-2 text-2xl font-bold text-slate-900">{summary?.status.maxCirculationItems ?? 0} books</p>
          </div>
        </div>

        <div className="mt-6 flex flex-wrap gap-3">
          <button
            onClick={handlePayFine}
            disabled={isPaying || !summary || summary.totalOutstanding <= 0}
            className="rounded-xl bg-emerald-600 px-5 py-3 font-semibold text-white shadow-lg transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isPaying ? "Opening Stripe..." : "Pay Fine"}
          </button>
          <Link
            to="/borrowed"
            className="rounded-xl border border-slate-300 px-5 py-3 font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            Back to borrowed books
          </Link>
        </div>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        <h2 className="text-2xl font-bold text-slate-900">Fine Breakdown</h2>
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading fine items...</p>
        ) : !summary || summary.items.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No fines are on this account right now.</p>
        ) : (
          <div className="mt-6 overflow-x-auto">
            <table className="w-full min-w-[920px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Details</th>
                  <th className="px-4 py-3">Assessed</th>
                  <th className="px-4 py-3">Accrued</th>
                  <th className="px-4 py-3">Paid</th>
                  <th className="px-4 py-3">Outstanding</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {summary.items.map((item, index) => (
                  <tr key={`${item.fineType}-${item.loanId ?? item.reservationId ?? index}`} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4 font-semibold text-slate-900">{item.fineType}</td>
                    <td className="px-4 py-4">{item.bookTitle || "Member account"}</td>
                    <td className="px-4 py-4 text-sm text-slate-600">{item.description}</td>
                    <td className="px-4 py-4">{new Date(item.assessedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-4">${item.accruedAmount.toFixed(2)}</td>
                    <td className="px-4 py-4 text-emerald-700">${item.paidAmount.toFixed(2)}</td>
                    <td className="px-4 py-4 font-semibold text-rose-700">${item.outstandingAmount.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        <h2 className="text-2xl font-bold text-slate-900">Payment History</h2>
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading payment history...</p>
        ) : payments.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No fine payments have been recorded yet.</p>
        ) : (
          <div className="mt-6 overflow-x-auto">
            <table className="w-full min-w-[780px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Paid On</th>
                  <th className="px-4 py-3">Method</th>
                  <th className="px-4 py-3">Amount</th>
                  <th className="px-4 py-3">Processed By</th>
                  <th className="px-4 py-3">Notes</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {payments.map((payment) => (
                  <tr key={payment.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4">{new Date(payment.paidAt).toLocaleString()}</td>
                    <td className="px-4 py-4">{payment.paymentMethod}</td>
                    <td className="px-4 py-4 font-semibold text-slate-900">${payment.amount.toFixed(2)}</td>
                    <td className="px-4 py-4">{payment.receivedByName || "Online payment"}</td>
                    <td className="px-4 py-4 text-sm text-slate-600">{payment.notes || "No notes"}</td>
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

export default MyFines;
