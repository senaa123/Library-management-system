import { useState } from "react";
import axios from "axios";
import { Link, useNavigate } from "react-router-dom";

function Register() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<{ password?: string }>({});
  const navigate = useNavigate();

  // Validate user input before sending to backend
  const validateForm = () => {
    const newErrors: { password?: string } = {};

    if (password.length < 6) {
      newErrors.password = "Password must be at least 6 characters";
    }
    if (password !== confirmPassword) {
      newErrors.password = "Passwords do not match";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle registration request
  const handleRegister = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setIsLoading(true);

    axios
      .post("http://localhost:5156/api/Auth/register", { username, password })
      .then(() => {
        // Wait before navigating to show completion
        setTimeout(() => navigate("/login"), 1000);
      })
      .catch(err => {
        alert(
          err.response?.data?.message ??
          err.response?.data ??
          "Registration failed. Try again."
        );
      })
      .finally(() => setIsLoading(false));
  };

  // Password strength logic for UI
  const passwordStrength = (pass: string) => {
    if (pass.length === 0) return { width: "0%", color: "bg-gray-200" };
    if (pass.length < 6) return { width: "33%", color: "bg-red-500" };
    if (pass.length < 8) return { width: "66%", color: "bg-yellow-500" };

    const hasUpper = /[A-Z]/.test(pass);
    const hasLower = /[a-z]/.test(pass);
    const hasNumber = /[0-9]/.test(pass);
    const hasSpecial = /[^A-Za-z0-9]/.test(pass);

    const strength = [hasUpper, hasLower, hasNumber, hasSpecial].filter(Boolean).length;
    if (strength === 1) return { width: "33%", color: "bg-red-500" };
    if (strength === 2) return { width: "66%", color: "bg-yellow-500" };
    if (strength === 3) return { width: "85%", color: "bg-green-500" };
    return { width: "100%", color: "bg-green-600" };
  };

  const strength = passwordStrength(password);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50 flex items-center justify-center p-4">
      {/* Background animation */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-purple-300 rounded-full blur-3xl opacity-20 animate-blob"></div>
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-blue-300 rounded-full blur-3xl opacity-20 animate-blob animation-delay-2000"></div>
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-cyan-300 rounded-full blur-3xl opacity-10 animate-blob animation-delay-4000"></div>
      </div>

      <div className="relative w-full max-w-lg">
        {/* Decorative shapes */}
        <div className="absolute -top-8 -left-8 w-32 h-32 bg-gradient-to-br from-blue-500 to-purple-600 rounded-3xl rotate-12 opacity-10"></div>
        <div className="absolute -bottom-8 -right-8 w-32 h-32 bg-gradient-to-br from-cyan-400 to-blue-500 rounded-3xl -rotate-12 opacity-10"></div>

        {/* Registration Card */}
        <div className="bg-white/90 backdrop-blur-xl rounded-3xl shadow-2xl border border-white/50 overflow-hidden">
          <div className="bg-gradient-to-r from-slate-900 to-indigo-900 p-8">
            <h2 className="text-3xl font-bold text-center text-white mb-2">Create Account</h2>
            <p className="text-center text-blue-200">Register to continue</p>
          </div>

          {/* Form */}
          <div className="p-8">
            <form onSubmit={handleRegister} className="space-y-6">
              {/* Username */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">Username</label>
                <input
                  type="text"
                  placeholder="Choose a username"
                  required
                  value={username}
                  onChange={e => setUsername(e.target.value)}
                  className="w-full px-4 py-3.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-blue-500 transition-all placeholder:text-slate-400"
                  disabled={isLoading}
                />
              </div>

              {/* Password */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">Password</label>
                <input
                  type="password"
                  placeholder="Create a password"
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  className="w-full px-4 py-3.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-blue-500 transition-all placeholder:text-slate-400"
                  disabled={isLoading}
                />

                {/* Strength Bar */}
                <div className="mt-2">
                  <div className="flex justify-between text-xs text-slate-600 mb-1">
                    <span>Password strength</span>
                    <span className={password.length >= 8 ? "text-green-600 font-semibold" : ""}>
                      {password.length >= 8 ? "Strong" : password.length >= 6 ? "Medium" : "Weak"}
                    </span>
                  </div>
                  <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
                    <div className={`h-full ${strength.color} transition-all duration-500`} style={{ width: strength.width }}></div>
                  </div>

                  {/* Password Requirement Checklist */}
                  <ul className="mt-2 grid grid-cols-2 gap-1 text-xs text-slate-500">
                    <li className={`${password.length >= 6 ? "text-green-600" : ""}`}>✓ At least 6 characters</li>
                    <li className={`${/[A-Z]/.test(password) ? "text-green-600" : ""}`}>✓ Uppercase letter</li>
                    <li className={`${/[a-z]/.test(password) ? "text-green-600" : ""}`}>✓ Lowercase letter</li>
                    <li className={`${/[0-9]/.test(password) ? "text-green-600" : ""}`}>✓ Number</li>
                  </ul>
                </div>
              </div>

              {/* Confirm Password */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">Confirm Password</label>
                <input
                  type="password"
                  placeholder="Re-enter your password"
                  required
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  className={`w-full px-4 py-3.5 bg-slate-50 border ${
                    errors.password ? "border-red-300" : "border-slate-200"
                  } rounded-xl focus:ring-2 focus:ring-blue-500 transition-all placeholder:text-slate-400`}
                  disabled={isLoading}
                />
                {errors.password && (
                  <p className="mt-2 text-sm text-red-600">{errors.password}</p>
                )}
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                disabled={isLoading}
                className="w-full bg-gradient-to-r from-emerald-600 to-green-600 text-white py-3.5 rounded-xl font-semibold hover:from-emerald-700 hover:to-green-700 transition-all shadow-lg hover:shadow-xl hover:-translate-y-0.5 disabled:opacity-50"
              >
                {isLoading ? "Creating Account..." : "Sign Up"}
              </button>

              {/* Login Redirect */}
              <div className="text-center mt-6 text-sm">
                <span className="text-slate-600">Already have an account? </span>
                <Link to="/login" className="text-blue-600 hover:text-blue-800 font-medium">
                  Sign In
                </Link>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Animation Styles */}
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
