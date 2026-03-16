import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import MemberQrCard from "../components/MemberQrCard";
import { getStoredRole, isLoggedIn, isMemberRole } from "../lib/session";
import api from "../services/axiosConfig";
import type { Reservation, UserProfile } from "../types/library";

function reservationStatusClasses(status: string) {
  if (status === "Available") {
    return "bg-emerald-100 text-emerald-700";
  }

  if (status === "Active") {
    return "bg-amber-100 text-amber-700";
  }

  if (status === "Fulfilled") {
    return "bg-blue-100 text-blue-700";
  }

  return "bg-slate-100 text-slate-600";
}

function MyReservations() {
  const navigate = useNavigate();
  const [member, setMember] = useState<UserProfile | null>(null);
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [activeReservationId, setActiveReservationId] = useState<number | null>(null);

  const loadData = (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    Promise.all([
      api.get<UserProfile>("/Users/me"),
      api.get<Reservation[]>("/Reservations"),
    ])
      .then(([memberResponse, reservationsResponse]) => {
        setMember(memberResponse.data);
        setReservations(reservationsResponse.data);
      })
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to load your reservations.");
      })
      .finally(() => setIsLoading(false));
  };

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isMemberRole(getStoredRole())) {
      navigate("/");
      return;
    }

    const fetchInitialData = async () => {
      try {
        const [memberResponse, reservationsResponse] = await Promise.all([
          api.get<UserProfile>("/Users/me"),
          api.get<Reservation[]>("/Reservations"),
        ]);

        setMember(memberResponse.data);
        setReservations(reservationsResponse.data);
      } catch (error: unknown) {
        const message = typeof error === "object" && error && "response" in error
          ? (error as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
        alert(message ?? "Failed to load your reservations.");
      } finally {
        setIsLoading(false);
      }
    };

    void fetchInitialData();
  }, [navigate]);

  const handleCancel = (reservationId: number) => {
    setActiveReservationId(reservationId);

    api.post(`/Reservations/${reservationId}/cancel`)
      .then(() => {
        alert("Reservation cancelled.");
        loadData();
      })
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to cancel reservation.");
      })
      .finally(() => setActiveReservationId(null));
  };

  const readyReservations = reservations.filter((reservation) => reservation.status === "Available").length;
  const queuedReservations = reservations.filter((reservation) => reservation.status === "Active").length;

  return (
    <div className="space-y-6">
      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
          <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Member Section</p>
          <h1 className="mt-2 text-3xl font-bold text-slate-900">My Reservations</h1>
          <p className="mt-3 max-w-2xl text-slate-600">
            Reserve books online, monitor the 5-day collection window, and cancel reservations you no longer need.
          </p>

          <div className="mt-8 grid gap-4 md:grid-cols-2">
            <div className="rounded-2xl bg-slate-900 p-5 text-white">
              <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Ready for pickup</p>
              <p className="mt-2 text-3xl font-bold">{readyReservations}</p>
            </div>
            <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
              <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Waiting in queue</p>
              <p className="mt-2 text-3xl font-bold text-slate-900">{queuedReservations}</p>
            </div>
          </div>
        </div>

        {member && (
          <MemberQrCard
            fullName={member.fullName}
            username={member.username}
            qrCodeValue={member.qrCodeValue}
            subtitle="Show this QR to the librarian when collecting a reservation or borrowing a book at the counter."
          />
        )}
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading your reservations...</p>
        ) : reservations.length === 0 ? (
          <div className="py-10 text-center">
            <p className="text-lg font-semibold text-slate-700">You do not have any reservations yet.</p>
            <p className="mt-2 text-slate-500">Reserve a title from the catalog and it will appear here.</p>
            <Link
              to="/"
              className="mt-6 inline-flex rounded-xl bg-indigo-600 px-5 py-3 font-semibold text-white shadow-lg transition hover:bg-indigo-700"
            >
              Browse catalog
            </Link>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Reserved On</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Pickup Window</th>
                  <th className="px-4 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {reservations.map((reservation) => {
                  const canCancel = reservation.status === "Active" || reservation.status === "Available";

                  return (
                    <tr key={reservation.id} className="transition hover:bg-slate-50/80">
                      <td className="px-4 py-4">
                        <p className="font-semibold text-slate-900">{reservation.bookTitle}</p>
                        <p className="text-sm text-slate-500">Reservation #{reservation.id}</p>
                      </td>
                      <td className="px-4 py-4">{new Date(reservation.reservedAt).toLocaleDateString()}</td>
                      <td className="px-4 py-4">
                        <span className={`rounded-full px-3 py-1 text-sm font-semibold ${reservationStatusClasses(reservation.status)}`}>
                          {reservation.status === "Available" ? "Ready for pickup" : reservation.status}
                        </span>
                      </td>
                      <td className="px-4 py-4">
                        <p className="font-medium text-slate-800">{reservation.timeLeftLabel}</p>
                        <p className="text-sm text-slate-500">
                          {reservation.pickupDeadline
                            ? `Deadline: ${new Date(reservation.pickupDeadline).toLocaleDateString()}`
                            : "The library will notify this reservation when a copy is held for you."}
                        </p>
                      </td>
                      <td className="px-4 py-4 text-center">
                        {canCancel ? (
                          <button
                            onClick={() => handleCancel(reservation.id)}
                            disabled={activeReservationId === reservation.id}
                            className="rounded-xl border border-rose-200 px-4 py-2 font-semibold text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60"
                          >
                            {activeReservationId === reservation.id ? "Cancelling..." : "Cancel"}
                          </button>
                        ) : (
                          <span className="text-sm text-slate-400">No action</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

export default MyReservations;
