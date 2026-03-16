import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../services/axiosConfig";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import type { UserProfile } from "../types/library";

function Members() {
  const navigate = useNavigate();
  const [members, setMembers] = useState<UserProfile[]>([]);
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

    // This screen is the staff directory for active members only.
    api.get("/Users", { params: { role: "Member", isActive: true } })
      .then((response) => setMembers(response.data))
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to load current members.");
      })
      .finally(() => setIsLoading(false));
  }, [navigate]);

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Admin Section</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Current Members</h1>
        <p className="mt-2 text-slate-600">
          Select a member to view their details and current borrowed books.
        </p>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading members...</p>
        ) : members.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No active members were found.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">Username</th>
                  <th className="px-4 py-3">Email</th>
                  <th className="px-4 py-3">Phone</th>
                  <th className="px-4 py-3 text-center">Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {members.map((member) => (
                  <tr key={member.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4 font-semibold text-slate-900">{member.fullName || member.username}</td>
                    <td className="px-4 py-4">@{member.username}</td>
                    <td className="px-4 py-4">{member.email || "Not provided"}</td>
                    <td className="px-4 py-4">{member.phoneNumber || "Not provided"}</td>
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
