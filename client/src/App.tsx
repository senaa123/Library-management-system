import { Routes, Route, Link, useNavigate, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import BookList from "./pages/BookList";
import AddBook from "./pages/AddBook";
import EditBook from "./pages/EditBook";
import Login from "./pages/Login";
import Register from "./pages/Register";
import MyBorrowedBooks from "./pages/MyBorrowedBooks";
import AdminLoans from "./pages/AdminLoans";
import Members from "./pages/Members";
import MemberDetails from "./pages/MemberDetails";
import { getStoredRole, isLoggedIn, isStaffRole } from "./lib/session";

function App() {
  const [loggedIn, setLoggedIn] = useState(false);
  const [username, setUsername] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [role, setRole] = useState("");

  const navigate = useNavigate();
  const location = useLocation();
  const isStaff = isStaffRole(role);

  useEffect(() => {
    if (isLoggedIn()) {
      const user = localStorage.getItem("username") ?? "";
      const storedRole = getStoredRole();
      const storedFullName = localStorage.getItem("fullName") ?? user;

      setLoggedIn(true);
      setUsername(user);
      setRole(storedRole);
      setDisplayName(storedFullName);
      return;
    }

    setLoggedIn(false);
    setUsername("");
    setRole("");
    setDisplayName("");
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("username");
    localStorage.removeItem("role");
    localStorage.removeItem("userId");
    localStorage.removeItem("fullName");

    setLoggedIn(false);
    setUsername("");
    setDisplayName("");
    setRole("");
    navigate("/login");
  };

  return (
    <>
      <nav className="sticky top-0 z-50 border-b border-white/10 bg-gradient-to-r from-slate-950 via-slate-900 to-indigo-950 px-6 py-5 shadow-2xl">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-6">
          <Link to="/" className="flex items-center gap-3">
            <div>
              <h1 className="text-2xl font-bold text-white">Book Ledger</h1>
              <p className="text-xs uppercase tracking-[0.25em] text-blue-200">Library Management System</p>
            </div>
          </Link>

          <div className="flex flex-wrap items-center gap-3">
            <Link
              to="/"
              className={`rounded-xl px-4 py-2 font-medium transition ${
                location.pathname === "/" ? "bg-white text-slate-900" : "text-slate-200 hover:bg-white/10 hover:text-white"
              }`}
            >
              Catalog
            </Link>

            {loggedIn && (
              <Link
                to="/borrowed"
                className={`rounded-xl px-4 py-2 font-medium transition ${
                  location.pathname === "/borrowed" ? "bg-white text-slate-900" : "text-slate-200 hover:bg-white/10 hover:text-white"
                }`}
              >
                My Borrowed
              </Link>
            )}

            {loggedIn && isStaff && (
              <>
                <Link
                  to="/staff/loans"
                  className={`rounded-xl px-4 py-2 font-medium transition ${
                    location.pathname.startsWith("/staff/loans") ? "bg-white text-slate-900" : "text-slate-200 hover:bg-white/10 hover:text-white"
                  }`}
                >
                  Current Loans
                </Link>
                <Link
                  to="/members"
                  className={`rounded-xl px-4 py-2 font-medium transition ${
                    location.pathname.startsWith("/members") ? "bg-white text-slate-900" : "text-slate-200 hover:bg-white/10 hover:text-white"
                  }`}
                >
                  Members
                </Link>
                <Link
                  to="/add"
                  className={`rounded-xl px-4 py-2 font-medium transition ${
                    location.pathname === "/add"
                      ? "bg-emerald-400 text-slate-950"
                      : "bg-emerald-500 text-slate-950 hover:bg-emerald-400"
                  }`}
                >
                  Add Book
                </Link>
              </>
            )}

            {loggedIn ? (
              <>
                <div className="rounded-2xl border border-white/20 bg-white/10 px-4 py-2 text-white">
                  <p className="text-sm font-semibold">{displayName || username}</p>
                  <p className="text-[11px] uppercase tracking-[0.25em] text-blue-200">{role || "Member"}</p>
                </div>
                <button
                  onClick={handleLogout}
                  className="rounded-xl border border-rose-400/30 px-4 py-2 font-medium text-slate-200 transition hover:bg-rose-500/20 hover:text-white"
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className={`rounded-xl px-4 py-2 font-medium transition ${
                    location.pathname === "/login" ? "bg-white/10 text-white" : "text-slate-200 hover:bg-white/10 hover:text-white"
                  }`}
                >
                  Sign In
                </Link>
                <Link
                  to="/register"
                  className={`rounded-xl px-4 py-2 font-medium transition ${
                    location.pathname === "/register" ? "bg-white text-slate-900" : "bg-white text-slate-900 hover:bg-slate-100"
                  }`}
                >
                  Register
                </Link>
              </>
            )}
          </div>
        </div>
      </nav>

      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(59,130,246,0.18),_transparent_30%),radial-gradient(circle_at_bottom_right,_rgba(15,118,110,0.16),_transparent_28%),linear-gradient(180deg,_#f8fafc_0%,_#eff6ff_48%,_#f8fafc_100%)] px-6 py-8">
        <div className="mx-auto max-w-7xl">
          <Routes>
            <Route path="/" element={<BookList />} />
            <Route path="/add" element={<AddBook />} />
            <Route path="/edit/:id" element={<EditBook />} />
            <Route path="/borrowed" element={<MyBorrowedBooks />} />
            <Route path="/staff/loans" element={<AdminLoans />} />
            <Route path="/members" element={<Members />} />
            <Route path="/members/:id" element={<MemberDetails />} />
            <Route
              path="/login"
              element={
                <Login
                  onLogin={(user, nextRole, fullName) => {
                    setLoggedIn(true);
                    setUsername(user);
                    setRole(nextRole);
                    setDisplayName(fullName || user);
                  }}
                />
              }
            />
            <Route path="/register" element={<Register />} />
          </Routes>
        </div>
      </div>

      <footer className="bg-slate-950 px-8 py-8 text-white">
        <div className="mx-auto max-w-7xl text-center">
          <h2 className="text-2xl font-bold">Book Ledger</h2>
          <p className="mt-2 text-slate-400">Professional Library Management System</p>
          <div className="mt-8 border-t border-slate-800 pt-8 text-slate-500">
            <p>(c) {new Date().getFullYear()} Created by Binada. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </>
  );
}

export default App;
