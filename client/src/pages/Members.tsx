import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import { extractApiMessage, notifyError } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { UserProfile } from "../types/library";

function Members() {
  const navigate = useNavigate();
  const [members, setMembers] = useState<UserProfile[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isStaffRole(getStoredRole())) {
      navigate("/");
      return;
    }

    // This screen is the staff directory for active members only.
    api.get<UserProfile[]>("/Users", { params: { role: "Member", isActive: true } })
      .then((response) => setMembers(response.data))
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to load current members."));
      })
      .finally(() => setIsLoading(false));
  }, [navigate]);

  const filteredMembers = members.filter((member) => {
    const query = search.toLowerCase();
    return (
      member.fullName.toLowerCase().includes(query) ||
      member.username.toLowerCase().includes(query) ||
      member.email.toLowerCase().includes(query) ||
      member.phoneNumber.toLowerCase().includes(query) ||
      member.nicNumber.toLowerCase().includes(query)
    );
  });

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Admin Section</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Current Members</h1>
        <p className="mt-2 text-slate-600">
          Search the active member directory, see each member's current fine balance, and open the detail view to manage restrictions.
        </p>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        <div className="mb-5 flex flex-wrap gap-4">
          <input
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search by name, username, NIC, phone, or email..."
            className="min-w-[260px] flex-1 rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading members...</p>
        ) : filteredMembers.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No active members were found.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1040px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">Username</th>
                  <th className="px-4 py-3">NIC</th>
                  <th className="px-4 py-3">Email</th>
                  <th className="px-4 py-3">Phone</th>
                  <th className="px-4 py-3">Fine</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-center">Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {filteredMembers.map((member) => (
                  <tr key={member.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4 font-semibold text-slate-900">{member.fullName || member.username}</td>
                    <td className="px-4 py-4">@{member.username}</td>
                    <td className="px-4 py-4">{member.nicNumber || "Not provided"}</td>
                    <td className="px-4 py-4">{member.email || "Not provided"}</td>
                    <td className="px-4 py-4">{member.phoneNumber || "Not provided"}</td>
                    <td className="px-4 py-4 font-semibold text-slate-900">${member.totalOutstandingFine.toFixed(2)}</td>
                    <td className="px-4 py-4">
                      {member.restrictionWarning ? (
                        <span className={`rounded-full px-3 py-1 text-xs font-semibold ${
                          member.isCirculationBlocked ? "bg-rose-100 text-rose-700" : "bg-amber-100 text-amber-700"
                        }`}>
                          {member.isCirculationBlocked ? "Restricted" : "Limited"}
                        </span>
                      ) : (
                        <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">
                          Clear
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-4 text-center">
                      <Link
                        to={`/members/${member.id}`}
                        className="inline-flex rounded-xl bg-indigo-600 px-4 py-2 font-semibold text-white transition hover:bg-indigo-700"
                      >
                        View details
                      </Link>
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

export default Members;
