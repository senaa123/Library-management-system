import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import { extractApiMessage, notifyError, notifySuccess } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { Reservation } from "../types/library";

function reservationStatusClasses(status: string) {
  return status === "Available" ? "bg-emerald-100 text-emerald-700" : "bg-amber-100 text-amber-700";
}

function AdminReservations() {
  const navigate = useNavigate();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [reservationToIssue, setReservationToIssue] = useState<Reservation | null>(null);
  const [borrowDays, setBorrowDays] = useState(14);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadReservations = (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    api.get<Reservation[]>("/Reservations")
      .then((response) => {
        setReservations(
          response.data.filter((reservation) => reservation.status === "Active" || reservation.status === "Available"),
        );
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to load reservations."));
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

    const fetchInitialReservations = async () => {
      try {
        const response = await api.get<Reservation[]>("/Reservations");
        setReservations(
          response.data.filter((reservation) => reservation.status === "Active" || reservation.status === "Available"),
        );
      } catch (error: unknown) {
        notifyError(extractApiMessage(error, "Failed to load reservations."));
      } finally {
        setIsLoading(false);
      }
    };

    void fetchInitialReservations();
  }, [navigate]);

  const handleIssueReservation = () => {
    if (!reservationToIssue) {
      return;
    }

    setIsSubmitting(true);

    // Staff choose the real loan period only when the member actually collects the reservation.
    api.post(`/Reservations/${reservationToIssue.id}/issue`, { borrowDays })
      .then(() => {
        notifySuccess("Reservation issued successfully.");
        setReservationToIssue(null);
        setBorrowDays(14);
        loadReservations();
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to issue reservation."));
      })
      .finally(() => setIsSubmitting(false));
  };

  const readyReservations = reservations.filter((reservation) => reservation.status === "Available").length;

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Admin Section</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">Reserved Books</h1>
            <p className="mt-2 max-w-3xl text-slate-600">
              Track the reservation queue, see which copies are ready for pickup, and issue them only when the member arrives at the desk.
            </p>
          </div>
          <div className="rounded-2xl bg-slate-900 px-5 py-4 text-white shadow-lg">
            <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Ready right now</p>
            <p className="mt-1 text-3xl font-bold">{readyReservations}</p>
          </div>
        </div>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading reservations...</p>
        ) : reservations.length === 0 ? (
          <p className="py-10 text-center text-slate-500">There are no active or ready reservations right now.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1000px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Member</th>
                  <th className="px-4 py-3">Phone</th>
                  <th className="px-4 py-3">Reserved On</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Pickup Window</th>
                  <th className="px-4 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {reservations.map((reservation) => (
                  <tr key={reservation.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4">
                      <p className="font-semibold text-slate-900">{reservation.bookTitle}</p>
                      <p className="text-sm text-slate-500">Reservation #{reservation.id}</p>
                    </td>
                    <td className="px-4 py-4">
                      <p className="font-medium text-slate-900">{reservation.memberName || reservation.memberUsername}</p>
                      <p className="text-sm text-slate-500">@{reservation.memberUsername}</p>
                    </td>
                    <td className="px-4 py-4">{reservation.memberPhoneNumber || "Not provided"}</td>
                    <td className="px-4 py-4">{new Date(reservation.reservedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-4">
                      <span className={`rounded-full px-3 py-1 text-sm font-semibold ${reservationStatusClasses(reservation.status)}`}>
                        {reservation.status === "Available" ? "Ready for pickup" : "Waiting"}
                      </span>
                    </td>
                    <td className="px-4 py-4">
                      <p className="font-medium text-slate-800">{reservation.timeLeftLabel}</p>
                      <p className="text-sm text-slate-500">
                        {reservation.pickupDeadline
                          ? `Deadline: ${new Date(reservation.pickupDeadline).toLocaleDateString()}`
                          : "Will become ready when a copy is free."}
                      </p>
                    </td>
                    <td className="px-4 py-4 text-center">
                      <button
                        onClick={() => {
                          setReservationToIssue(reservation);
                          setBorrowDays(14);
                        }}
                        disabled={!reservation.canIssue}
                        className="rounded-xl bg-indigo-600 px-4 py-2 font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300"
                      >
                        {reservation.canIssue ? "Issued" : "Waiting"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {reservationToIssue && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Issue Reserved Book</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{reservationToIssue.bookTitle}</h2>
            <p className="mt-2 text-slate-600">
              Choose the borrow period for {reservationToIssue.memberName || reservationToIssue.memberUsername}. The due timer starts immediately after issuance.
            </p>

            <div className="mt-6">
              <label className="mb-2 block text-sm font-semibold text-slate-700">Borrow period</label>
              <select
                value={borrowDays}
                onChange={(event) => setBorrowDays(Number(event.target.value))}
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                {Array.from({ length: 14 }, (_, index) => index + 1).map((days) => (
                  <option key={days} value={days}>
                    {days} {days === 1 ? "day" : "days"}
                  </option>
                ))}
              </select>
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={() => setReservationToIssue(null)}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={handleIssueReservation}
                disabled={isSubmitting}
                className="rounded-xl bg-indigo-600 px-5 py-2 font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-70"
              >
                {isSubmitting ? "Issuing..." : "Confirm issue"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminReservations;
