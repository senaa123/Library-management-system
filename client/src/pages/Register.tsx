import { useState } from "react";
import { Link } from "react-router-dom";
import MemberQrCard from "../components/MemberQrCard";
import { extractApiMessage, notifyError } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { RegisterResponse } from "../types/library";

function Register() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [nicNumber, setNicNumber] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<{ password?: string; phoneNumber?: string; nicNumber?: string }>({});
  const [registeredMember, setRegisteredMember] = useState<RegisterResponse | null>(null);

  const validateForm = () => {
    const nextErrors: { password?: string; phoneNumber?: string; nicNumber?: string } = {};

    if (password.length < 6) {
      nextErrors.password = "Password must be at least 6 characters";
    }

    if (password !== confirmPassword) {
      nextErrors.password = "Passwords do not match";
    }

    if (phoneNumber.trim().length < 7) {
      nextErrors.phoneNumber = "Phone number should be at least 7 characters";
    }

    if (nicNumber.trim().length < 5) {
      nextErrors.nicNumber = "NIC number is required";
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleRegister = (event: React.FormEvent) => {
    event.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    api.post<RegisterResponse>("/Auth/register", {
      fullName,
      email,
      phoneNumber,
      nicNumber,
      username,
      password,
    })
      .then((response) => {
        // The generated QR is the member identity token for all counter-based issues.
        setRegisteredMember(response.data);
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Registration failed. Try again."));
      })
      .finally(() => setIsLoading(false));
  };

  const passwordStrength = (value: string) => {
    if (value.length === 0) {
      return { width: "0%", color: "bg-gray-200" };
    }

    if (value.length < 6) {
      return { width: "33%", color: "bg-red-500" };
    }

    if (value.length < 8) {
      return { width: "66%", color: "bg-yellow-500" };
    }

    const checks = [
      /[A-Z]/.test(value),
      /[a-z]/.test(value),
      /[0-9]/.test(value),
      /[^A-Za-z0-9]/.test(value),
    ].filter(Boolean).length;

    if (checks === 1) {
      return { width: "33%", color: "bg-red-500" };
    }

    if (checks === 2) {
      return { width: "66%", color: "bg-yellow-500" };
    }

    if (checks === 3) {
      return { width: "85%", color: "bg-green-500" };
    }

    return { width: "100%", color: "bg-green-600" };
  };

  const strength = passwordStrength(password);

  if (registeredMember) {
    return (
      <div className="mx-auto grid max-w-5xl gap-6 py-8 lg:grid-cols-[1.2fr_0.8fr]">
        <div className="rounded-3xl border border-white/60 bg-white/85 p-8 shadow-2xl backdrop-blur-xl">
          <p className="text-sm uppercase tracking-[0.3em] text-emerald-500">Registration Complete</p>
          <h1 className="mt-2 text-3xl font-bold text-slate-900">Your member account is ready</h1>
          <p className="mt-3 max-w-2xl text-slate-600">
            Present this QR code at the library desk whenever the librarian issues a reserved or walk-in book to you.
          </p>

          <div className="mt-8 grid gap-4 md:grid-cols-2">
            <div className="rounded-2xl bg-slate-900 p-5 text-white">
              <p className="text-xs uppercase tracking-[0.2em] text-emerald-200">Member ID</p>
              <p className="mt-2 text-lg font-semibold">#{registeredMember.userId}</p>
            </div>
            <div className="rounded-2xl bg-white p-5 shadow-md ring-1 ring-slate-200">
              <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Username</p>
              <p className="mt-2 text-lg font-semibold text-slate-900">@{registeredMember.username}</p>
            </div>
          </div>

          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              to="/login"
              className="inline-flex rounded-xl bg-indigo-600 px-5 py-3 font-semibold text-white shadow-lg transition hover:bg-indigo-700"
            >
              Continue to sign in
            </Link>
            <Link
              to="/"
              className="inline-flex rounded-xl border border-slate-300 px-5 py-3 font-semibold text-slate-700 transition hover:bg-slate-50"
            >
              Back to catalog
            </Link>
          </div>
        </div>

        <MemberQrCard
          fullName={registeredMember.fullName}
          username={registeredMember.username}
          qrCodeValue={registeredMember.qrCodeValue}
          subtitle="This code is scanned by the librarian when a reservation is collected or a book is issued at the desk."
        />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-blue-50 p-4">
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-blue-300 opacity-20 blur-3xl animate-blob" />
        <div className="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-emerald-300 opacity-20 blur-3xl animate-blob animation-delay-2000" />
        <div className="absolute left-1/2 top-1/2 h-96 w-96 -translate-x-1/2 -translate-y-1/2 rounded-full bg-cyan-300 opacity-10 blur-3xl animate-blob animation-delay-4000" />
      </div>

      <div className="relative w-full max-w-2xl">
        <div className="absolute -left-8 -top-8 h-32 w-32 rounded-3xl bg-gradient-to-br from-blue-500 to-indigo-600 opacity-10 rotate-12" />
        <div className="absolute -bottom-8 -right-8 h-32 w-32 rounded-3xl bg-gradient-to-br from-cyan-400 to-emerald-500 opacity-10 -rotate-12" />

        <div className="overflow-hidden rounded-3xl border border-white/50 bg-white/90 shadow-2xl backdrop-blur-xl">
          <div className="bg-gradient-to-r from-slate-900 to-indigo-900 p-8">
            <h2 className="mb-2 text-center text-3xl font-bold text-white">Create Account</h2>
            <p className="text-center text-blue-200">Register to reserve books and receive your member QR</p>
          </div>

          <div className="p-8">
            <form onSubmit={handleRegister} className="space-y-6">
              <div className="grid gap-5 md:grid-cols-2">
                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-semibold text-slate-700">Full Name</label>
                  <input
                    type="text"
                    placeholder="Enter your full name"
                    required
                    value={fullName}
                    onChange={(event) => setFullName(event.target.value)}
                    disabled={isLoading}
                    className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-semibold text-slate-700">Email</label>
                  <input
                    type="email"
                    placeholder="name@example.com"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    disabled={isLoading}
                    className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-semibold text-slate-700">Phone Number</label>
                  <input
                    type="tel"
                    placeholder="Enter your phone number"
                    required
                    value={phoneNumber}
                    onChange={(event) => setPhoneNumber(event.target.value)}
                    disabled={isLoading}
                    className={`w-full rounded-xl border ${
                      errors.phoneNumber ? "border-red-300" : "border-slate-200"
                    } bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500`}
                  />
                  {errors.phoneNumber && (
                    <p className="mt-2 text-sm text-red-600">{errors.phoneNumber}</p>
                  )}
                </div>

                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-semibold text-slate-700">NIC Number</label>
                  <input
                    type="text"
                    placeholder="Enter your NIC number"
                    required
                    value={nicNumber}
                    onChange={(event) => setNicNumber(event.target.value)}
                    disabled={isLoading}
                    className={`w-full rounded-xl border ${
                      errors.nicNumber ? "border-red-300" : "border-slate-200"
                    } bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500`}
                  />
                  {errors.nicNumber && (
                    <p className="mt-2 text-sm text-red-600">{errors.nicNumber}</p>
                  )}
                </div>
              </div>

              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">Username</label>
                <input
                  type="text"
                  placeholder="Choose a username"
                  required
                  value={username}
                  onChange={(event) => setUsername(event.target.value)}
                  disabled={isLoading}
                  className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">Password</label>
                <input
                  type="password"
                  placeholder="Create a password"
                  required
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  disabled={isLoading}
                  className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />

                <div className="mt-2">
                  <div className="mb-1 flex justify-between text-xs text-slate-600">
                    <span>Password strength</span>
                    <span className={password.length >= 8 ? "font-semibold text-green-600" : ""}>
                      {password.length >= 8 ? "Strong" : password.length >= 6 ? "Medium" : "Weak"}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-gray-200">
                    <div className={`h-full ${strength.color} transition-all duration-500`} style={{ width: strength.width }} />
                  </div>
                  <ul className="mt-2 grid grid-cols-2 gap-1 text-xs text-slate-500">
                    <li className={password.length >= 6 ? "text-green-600" : ""}>[x] At least 6 characters</li>
                    <li className={/[A-Z]/.test(password) ? "text-green-600" : ""}>[x] Uppercase letter</li>
                    <li className={/[a-z]/.test(password) ? "text-green-600" : ""}>[x] Lowercase letter</li>
                    <li className={/[0-9]/.test(password) ? "text-green-600" : ""}>[x] Number</li>
                  </ul>
                </div>
              </div>

              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">Confirm Password</label>
                <input
                  type="password"
                  placeholder="Re-enter your password"
                  required
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                  disabled={isLoading}
                  className={`w-full rounded-xl border ${
                    errors.password ? "border-red-300" : "border-slate-200"
                  } bg-slate-50 px-4 py-3.5 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500`}
                />
                {errors.password && (
                  <p className="mt-2 text-sm text-red-600">{errors.password}</p>
                )}
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full rounded-xl bg-gradient-to-r from-emerald-600 to-green-600 py-3.5 font-semibold text-white shadow-lg transition hover:-translate-y-0.5 hover:from-emerald-700 hover:to-green-700 hover:shadow-xl disabled:opacity-50"
              >
                {isLoading ? "Creating Account..." : "Sign Up"}
              </button>

              <div className="mt-6 text-center text-sm">
                <span className="text-slate-600">Already have an account? </span>
                <Link to="/login" className="font-medium text-blue-600 hover:text-blue-800">
                  Sign In
                </Link>
              </div>
            </form>
          </div>
        </div>
      </div>

      <style>{`
        @keyframes blob {
          0% { transform: translate(0px, 0px) scale(1); }
          33% { transform: translate(30px, -50px) scale(1.1); }
          66% { transform: translate(-20px, 20px) scale(0.9); }
          100% { transform: translate(0px, 0px) scale(1); }
        }
        .animate-blob {
          animation: blob 7s infinite;
        }
        .animation-delay-2000 {
          animation-delay: 2s;
        }
        .animation-delay-4000 {
          animation-delay: 4s;
        }
      `}</style>
    </div>
  );
}

export default Register;
