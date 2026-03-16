import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import QrCameraScanner from "../components/QrCameraScanner";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";
import api from "../services/axiosConfig";
import type { Book } from "../types/library";

function StaffBookSection() {
  const navigate = useNavigate();
  const [books, setBooks] = useState<Book[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [bookToIssue, setBookToIssue] = useState<Book | null>(null);
  const [borrowDays, setBorrowDays] = useState(14);
  const [scannerVisible, setScannerVisible] = useState(false);
  const [scannerSession, setScannerSession] = useState(0);
  const [scanError, setScanError] = useState("");
  const [isIssuing, setIsIssuing] = useState(false);

  const loadBooks = (showLoader = true) => {
    if (showLoader) {
      setIsLoading(true);
    }

    // This catalog intentionally excludes copies already held by reservations.
    api.get<Book[]>("/Books", { params: { availableOnly: true } })
      .then((response) => setBooks(response.data))
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to load available books.");
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

    const fetchInitialBooks = async () => {
      try {
        const response = await api.get<Book[]>("/Books", { params: { availableOnly: true } });
        setBooks(response.data);
      } catch (error: unknown) {
        const message = typeof error === "object" && error && "response" in error
          ? (error as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
        alert(message ?? "Failed to load available books.");
      } finally {
        setIsLoading(false);
      }
    };

    void fetchInitialBooks();
  }, [navigate]);

  const filteredBooks = books.filter((book) =>
    (book.title.toLowerCase().includes(search.toLowerCase()) ||
      book.author.toLowerCase().includes(search.toLowerCase()) ||
      book.isbn.toLowerCase().includes(search.toLowerCase())) &&
    (categoryFilter === "" || book.category === categoryFilter),
  );

  const resetIssueDialog = () => {
    setBookToIssue(null);
    setBorrowDays(14);
    setScannerVisible(false);
    setScannerSession(0);
    setScanError("");
    setIsIssuing(false);
  };

  const startScanner = () => {
    setScanError("");
    setScannerVisible(true);
    setScannerSession((current) => current + 1);
  };

  const handleDetected = (memberQrCodeValue: string) => {
    if (!bookToIssue || isIssuing) {
      return;
    }

    setIsIssuing(true);
    setScanError("");

    // QR-based issuing links the selected book to the exact member standing at the desk.
    api.post("/Loans/issue-by-qr", {
      bookId: bookToIssue.id,
      memberQrCodeValue,
      borrowDays,
    })
      .then(() => {
        alert("Book issued successfully.");
        resetIssueDialog();
        loadBooks();
      })
      .catch((error) => {
        setScanError(error.response?.data?.message ?? "Failed to issue this book.");
        setIsIssuing(false);
        setScannerSession((current) => current + 1);
      });
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Admin Section</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">Book Section</h1>
            <p className="mt-2 max-w-3xl text-slate-600">
              Search the live library catalog, choose a borrow period, and then scan the member QR code with the browser camera to issue a book at the counter.
            </p>
          </div>
          <div className="rounded-2xl bg-slate-900 px-5 py-4 text-white shadow-lg">
            <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Available titles</p>
            <p className="mt-1 text-3xl font-bold">{books.length}</p>
          </div>
        </div>

        <div className="mt-8 flex flex-wrap gap-4">
          <input
            type="text"
            placeholder="Search by title, author, or ISBN..."
            className="min-w-[260px] flex-1 rounded-2xl border border-slate-200 bg-white/90 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <select
            className="min-w-[220px] rounded-2xl border border-slate-200 bg-white/90 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            value={categoryFilter}
            onChange={(event) => setCategoryFilter(event.target.value)}
          >
            <option value="">All Categories</option>
            <option value="Fiction">Fiction</option>
            <option value="Non-Fiction">Non-Fiction</option>
            <option value="Science">Science</option>
            <option value="Biography">Biography</option>
            <option value="History">History</option>
            <option value="Other">Other</option>
          </select>
        </div>
      </div>

      <div className="rounded-3xl border border-white/60 bg-white/80 p-6 shadow-2xl backdrop-blur-xl">
        {isLoading ? (
          <p className="py-10 text-center text-slate-500">Loading available books...</p>
        ) : filteredBooks.length === 0 ? (
          <p className="py-10 text-center text-slate-500">No available books matched the current search.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[980px] text-left text-slate-800">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.2em] text-slate-500">
                <tr>
                  <th className="px-4 py-3">Book</th>
                  <th className="px-4 py-3">Author</th>
                  <th className="px-4 py-3">ISBN</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Available Copies</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {filteredBooks.map((book) => (
                  <tr key={book.id} className="transition hover:bg-slate-50/80">
                    <td className="px-4 py-4">
                      <p className="font-semibold text-slate-900">{book.title}</p>
                      <p className="text-sm text-slate-500">{book.description}</p>
                    </td>
                    <td className="px-4 py-4">{book.author}</td>
                    <td className="px-4 py-4">{book.isbn || "N/A"}</td>
                    <td className="px-4 py-4">{book.category}</td>
                    <td className="px-4 py-4">{book.availableCopies}</td>
                    <td className="px-4 py-4">
                      <span className="rounded-full bg-emerald-100 px-3 py-1 text-sm font-semibold text-emerald-700">
                        {book.availabilityStatus}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-center">
                      <button
                        onClick={() => {
                          setBookToIssue(book);
                          setBorrowDays(14);
                          setScannerVisible(false);
                          setScanError("");
                        }}
                        className="rounded-xl bg-indigo-600 px-4 py-2 font-semibold text-white transition hover:bg-indigo-700"
                      >
                        Issue
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {bookToIssue && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-2xl rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Issue Book</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{bookToIssue.title}</h2>
            <p className="mt-2 text-slate-600">
              First choose the borrow period. After that, scan the member QR code with the browser camera to complete the issue.
            </p>

            <div className="mt-6 grid gap-4 lg:grid-cols-[0.9fr_1.1fr]">
              <div className="rounded-3xl bg-slate-50 p-5 ring-1 ring-slate-200">
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

                <button
                  onClick={startScanner}
                  className="mt-4 w-full rounded-xl bg-indigo-600 px-4 py-3 font-semibold text-white transition hover:bg-indigo-700"
                >
                  {scannerVisible ? "Restart camera scan" : "Start camera scan"}
                </button>

                <p className="mt-4 text-sm text-slate-500">
                  The system will use the scanned QR to identify the member before creating the loan.
                </p>
              </div>

              <div className="rounded-3xl border border-slate-200 p-5">
                {scannerVisible ? (
                  <>
                    <QrCameraScanner key={scannerSession} onDetected={handleDetected} />
                    {isIssuing && (
                      <p className="mt-4 rounded-2xl bg-indigo-50 px-4 py-3 text-sm text-indigo-700 ring-1 ring-indigo-100">
                        QR detected. Creating the loan now...
                      </p>
                    )}
                    {scanError && (
                      <p className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700 ring-1 ring-rose-200">
                        {scanError}
                      </p>
                    )}
                  </>
                ) : (
                  <div className="flex min-h-[320px] items-center justify-center rounded-3xl bg-slate-50 p-6 text-center text-slate-500 ring-1 ring-slate-200">
                    Start the scanner when you are ready to scan the member QR code.
                  </div>
                )}
              </div>
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={resetIssueDialog}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default StaffBookSection;
