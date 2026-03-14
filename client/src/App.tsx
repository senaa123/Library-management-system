import { Routes, Route, Link, useNavigate, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";

import BookList from "./pages/BookList";
import AddBook from "./pages/AddBook";
import EditBook from "./pages/EditBook";
import Login from "./pages/Login";
import Register from "./pages/Register";

function App() {
  // Tracks login state and username
  const [loggedIn, setLoggedIn] = useState(false);
  const [username, setUsername] = useState("");

  const navigate = useNavigate();
  const location = useLocation(); // Used to detect the current route

  // Check login status when the app mounts
  useEffect(() => {
    const token = localStorage.getItem("token");
    const user = localStorage.getItem("username");

    if (token && user) {
      setLoggedIn(true);
      setUsername(user);
    }
  }, []);

  // Logout user and redirect to login page
  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("username");
    setLoggedIn(false);
    setUsername("");
    navigate("/login");
  };

  return (
    <>
      {/* NAVBAR SECTION*/}
      <nav className="bg-gradient-to-r from-slate-900 to-indigo-900 shadow-2xl px-8 py-5 relative z-50">
        <div className="max-w-7xl mx-auto flex justify-between items-center">

          {/* ---- Logo + Title ---- */}
          <Link to="/" className="flex items-center gap-3 group">
            <div>
              <h1 className="text-2xl font-bold bg-gradient-to-r from-white to-blue-200 bg-clip-text text-transparent">
                Book Ledger
              </h1>
              <p className="text-xs text-blue-300 font-medium">
                 Library Management System
              </p>
            </div>
          </Link>

          {/* ---- Navigation Links ---- */}
          <div className="flex items-center gap-8">

            {/* Book Catalog Link */}
            <Link
              to="/"
              className="text-slate-200 hover:text-white font-medium transition-colors duration-300 relative group"
            >
              <span className="flex items-center gap-2">Book Catalog</span>
              <span className="absolute -bottom-1 left-0 w-0 h-0.5 bg-gradient-to-r from-blue-400 to-purple-400 group-hover:w-full transition-all duration-300"></span>
            </Link>

            {/* Add Book (Visible only if logged in) */}
            {loggedIn && (
              <Link
                to="/add"
                className="bg-gradient-to-r from-blue-600 to-indigo-600 text-white px-5 py-2.5 rounded-xl font-medium hover:from-blue-700 hover:to-indigo-700 transition-all duration-300 shadow-lg hover:shadow-xl hover:-translate-y-0.5"
              >
                Add  New Book
              </Link>
            )}

            {/* User Profile Badge (Visible only if logged in) */}
            {loggedIn && (
              <div className="flex items-center gap-3 bg-white/10 backdrop-blur-sm px-4 py-2.5 rounded-xl border border-white/20">
                <div className="w-10 h-10 bg-gradient-to-br from-cyan-500 to-blue-600 rounded-full flex items-center justify-center text-white font-bold shadow-lg">
                  {username.charAt(0).toUpperCase()}
                </div>
                <p className="text-white font-semibold">Welcome back, {username}</p>
              </div>
            )}

            {/* Sign In & Register Buttons (Hidden if logged in) */}
            {!loggedIn && (
              <div className="flex items-center gap-4">
                {/* Sign In Button is disabled when already on the login page */}
                <Link
                  to="/login"
                  className={`text-slate-200 font-medium px-4 py-2.5 rounded-xl border border-white/20 
                  transition-all duration-300 
                  ${location.pathname === "/login"
                    ? "opacity-50 pointer-events-none"
                    : "hover:bg-white/10 hover:text-white"}`}
                >
                  Sign In
                </Link>

                {/* Get Started (Register) */}
                <Link
                  to="/register"
                  className={`font-medium px-5 py-2.5 rounded-xl 
                  transition-all duration-300 shadow-lg
                  ${location.pathname === "/register"
                    ? "bg-slate-200 text-slate-600 opacity-50 pointer-events-none"
                    : "bg-white text-slate-900 hover:bg-slate-100 hover:shadow-xl hover:-translate-y-0.5"}`}
                >
                  Get Started
                </Link>
              </div>
            )}

            {/* Logout Button (Visible only if logged in) */}
            {loggedIn && (
              <button
                onClick={handleLogout}
                className="text-slate-300 hover:text-white font-medium px-4 py-2.5 rounded-xl border border-red-500/30 hover:bg-red-500/20 transition-all duration-300"
              >
                Logout
              </button>
            )}
          </div>
        </div>
      </nav>

      {/* ===========================
           ROUTING + MAIN CONTENT
         =========================== */}
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50 p-6 md:p-8">
        <div className="max-w-7xl mx-auto">
          <Routes>
            <Route path="/" element={<BookList />} />
            <Route path="/add" element={<AddBook />} />
            <Route path="/edit/:id" element={<EditBook />} />
            <Route path="/login" element={<Login onLogin={(u) => { setLoggedIn(true); setUsername(u); }} />} />
            <Route path="/register" element={<Register />} />
          </Routes>
        </div>
      </div>

      {/* ===========================
                FOOTER
         =========================== */}
      <footer className="bg-gradient-to-r from-slate-900 to-indigo-900 text-white py-8 px-8">
        <div className="max-w-7xl mx-auto text-center">
          <h2 className="text-2xl font-bold">Book Ledger</h2>
          <p className="text-slate-400 mt-2">Professional Library Management System</p>

          <div className="mt-8 pt-8 border-t border-slate-800 text-center text-slate-500">
            <p>© {new Date().getFullYear()} Created by Binada. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </>
  );
}

export default App;
