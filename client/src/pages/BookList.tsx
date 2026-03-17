import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getStoredRole, isLoggedIn, isMemberRole, isStaffRole } from "../lib/session";
import { extractApiMessage, notifyError, notifySuccess } from "../lib/notifications";
import api from "../services/axiosConfig";
import type { Book, Reservation } from "../types/library";

type RemoveMode = "all" | "copies";

function BookList() {
  const [books, setBooks] = useState<Book[]>([]);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [bookToRemove, setBookToRemove] = useState<Book | null>(null);
  const [removeMode, setRemoveMode] = useState<RemoveMode>("copies");
  const [removeQuantity, setRemoveQuantity] = useState(1);
  const [bookToReserve, setBookToReserve] = useState<Book | null>(null);
  const username = localStorage.getItem("username");
  const role = getStoredRole();

  const isStaff = isStaffRole(role);
  const isMember = isMemberRole(role);

  const loadBooks = () => {
    api.get<Book[]>("/Books")
      .then((response) => setBooks(response.data))
      .catch((error) => notifyError(extractApiMessage(error, "Error fetching books.")));
  };

  useEffect(() => {
    loadBooks();
  }, []);

  const filteredBooks = books.filter((book) =>
    (book.title.toLowerCase().includes(search.toLowerCase()) ||
      book.author.toLowerCase().includes(search.toLowerCase()) ||
      book.isbn.toLowerCase().includes(search.toLowerCase())) &&
    (categoryFilter === "" || book.category === categoryFilter),
  );

  const submitRemove = () => {
    if (!bookToRemove) {
      return;
    }

    const payload = removeMode === "all"
      ? { removeAllCopies: true }
      : { removeAllCopies: false, quantityToRemove: removeQuantity };

    api.post(`/Books/${bookToRemove.id}/remove`, payload)
      .then(() => {
        notifySuccess("Book stock updated successfully.");
        setBookToRemove(null);
        setRemoveMode("copies");
        setRemoveQuantity(1);
        loadBooks();
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to remove book copies."));
      });
  };

  const submitReservation = () => {
    if (!bookToReserve) {
      return;
    }

    api.post<Reservation>("/Reservations", { bookId: bookToReserve.id })
      .then((response) => {
        const message = response.data.status === "Available"
          ? "Reservation placed. A copy is now held for pickup for 5 days."
          : "Reservation placed. You have been added to the waiting queue.";

        notifySuccess(message);
        setBookToReserve(null);
        loadBooks();
      })
      .catch((error) => {
        notifyError(extractApiMessage(error, "Failed to reserve this book."));
      });
  };

  return (
    <>
      <div className="rounded-3xl border border-white/60 bg-white/75 p-8 shadow-2xl backdrop-blur-xl">
        <div className="flex flex-wrap items-start justify-between gap-6">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Library Catalog</p>
            <h1 className="mt-2 text-3xl font-bold text-slate-900">Browse Books</h1>
            <p className="mt-3 max-w-2xl text-slate-600">
              Search the collection, check live library status, and place reservations online. Loans start only when a librarian issues the book at the desk.
            </p>
          </div>

          {isLoggedIn() && (
            <div className="rounded-2xl bg-slate-900 px-5 py-4 text-white shadow-lg">
              <p className="text-xs uppercase tracking-[0.2em] text-blue-200">Signed in as</p>
              <p className="mt-1 text-xl font-bold">{username}</p>
              <p className="mt-1 text-xs uppercase tracking-[0.2em] text-slate-300">{role || "Member"}</p>
            </div>
          )}
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

      <div className="mt-6 overflow-x-auto rounded-3xl border border-white/60 bg-white/80 p-4 shadow-2xl backdrop-blur-xl">
        <table className="w-full min-w-[1100px] border-collapse text-slate-800">
          <thead className="bg-gradient-to-r from-slate-900 to-indigo-900 text-sm uppercase text-white">
            <tr>
              <th className="px-5 py-4 text-left">Title</th>
              <th className="px-5 py-4 text-left">Author</th>
              <th className="px-5 py-4 text-left">ISBN</th>
              <th className="px-5 py-4 text-left">Category</th>
              <th className="px-5 py-4 text-left">Quantity</th>
              <th className="px-5 py-4 text-left">Status</th>
              <th className="px-5 py-4 text-left">Description</th>
              {(isStaff || isMember) && <th className="px-5 py-4 text-center">Action</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {filteredBooks.map((book) => (
              <tr key={book.id} className="transition hover:bg-indigo-50/70">
                <td className="px-5 py-4">
                  <p className="font-semibold text-slate-900">{book.title}</p>
                </td>
                <td className="px-5 py-4">{book.author}</td>
                <td className="px-5 py-4">{book.isbn || "N/A"}</td>
                <td className="px-5 py-4">{book.category}</td>
                <td className="px-5 py-4">{book.totalCopies}</td>
                <td className="px-5 py-4">
                  <div className="space-y-1">
                    <span className={`inline-flex rounded-full px-3 py-1 text-sm font-semibold ${
                      book.availableCopies > 0 ? "bg-emerald-100 text-emerald-700" : "bg-amber-100 text-amber-700"
                    }`}>
                      {book.availabilityStatus}
                    </span>
                    <p className="text-xs text-slate-500">
                      {book.availableCopies} available out of {book.totalCopies}
                    </p>
                  </div>
                </td>
                <td className="px-5 py-4 text-sm leading-6 text-slate-600">{book.description}</td>

                {(isStaff || isMember) && (
                  <td className="px-5 py-4 text-center">
                    <div className="flex justify-center gap-3">
                      {isStaff && (
                        <>
                          <Link
                            to={`/edit/${book.id}`}
                            className="rounded-xl bg-blue-600 px-4 py-2 font-semibold text-white transition hover:bg-blue-700"
                          >
                            Edit
                          </Link>
                          <button
                            onClick={() => {
                              setBookToRemove(book);
                              setRemoveMode(book.availableCopies > 0 ? "copies" : "all");
                              setRemoveQuantity(1);
                            }}
                            className="rounded-xl bg-rose-600 px-4 py-2 font-semibold text-white transition hover:bg-rose-700"
                          >
                            Remove
                          </button>
                        </>
                      )}

                      {isMember && (
                        <button
                          onClick={() => setBookToReserve(book)}
                          className="rounded-xl bg-indigo-600 px-4 py-2 font-semibold text-white transition hover:bg-indigo-700"
                        >
                          Reserve
                        </button>
                      )}
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {bookToRemove && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-rose-500">Remove Copies</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{bookToRemove.title}</h2>
            <p className="mt-2 text-slate-600">
              Choose whether you want to remove the whole title from the library or only some of the currently available copies.
            </p>

            <div className="mt-6 space-y-4">
              <label className="flex items-start gap-3 rounded-2xl border border-slate-200 p-4">
                <input
                  type="radio"
                  name="removeMode"
                  checked={removeMode === "copies"}
                  onChange={() => setRemoveMode("copies")}
                  className="mt-1"
                />
                <div>
                  <p className="font-semibold text-slate-900">Remove specific number of copies</p>
                  <p className="text-sm text-slate-500">
                    Only copies currently in the library can be removed this way.
                  </p>
                </div>
              </label>

              <label className="flex items-start gap-3 rounded-2xl border border-slate-200 p-4">
                <input
                  type="radio"
                  name="removeMode"
                  checked={removeMode === "all"}
                  onChange={() => setRemoveMode("all")}
                  className="mt-1"
                />
                <div>
                  <p className="font-semibold text-slate-900">Remove all copies under this title</p>
                  <p className="text-sm text-slate-500">
                    This works only when no copy is currently borrowed or held for reservation.
                  </p>
                </div>
              </label>
            </div>

            {removeMode === "copies" && (
              <div className="mt-6">
                <label className="mb-2 block text-sm font-semibold text-slate-700">Copies to remove</label>
                <input
                  type="number"
                  min={1}
                  max={Math.max(bookToRemove.availableCopies, 1)}
                  value={removeQuantity}
                  onChange={(event) => setRemoveQuantity(Number(event.target.value))}
                  className="w-full rounded-2xl border border-slate-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-rose-500"
                />
                <p className="mt-2 text-sm text-slate-500">
                  Available now: {bookToRemove.availableCopies} copies
                </p>
              </div>
            )}

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={() => setBookToRemove(null)}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={submitRemove}
                className="rounded-xl bg-rose-600 px-5 py-2 font-semibold text-white transition hover:bg-rose-700"
              >
                Confirm remove
              </button>
            </div>
          </div>
        </div>
      )}

      {bookToReserve && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
          <div className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
            <p className="text-sm uppercase tracking-[0.3em] text-indigo-500">Reserve Book</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">{bookToReserve.title}</h2>
            <p className="mt-2 text-slate-600">
              Reserve this title online. If a copy is available right now, the library will hold it for pickup for up to 5 days. If not, you will join the waiting queue.
            </p>

            <div className="mt-6 rounded-2xl bg-indigo-50 p-4 text-sm text-indigo-900 ring-1 ring-indigo-100">
              The due timer does not start until a librarian issues the book to you in person. If the pickup window expires without collection, the reservation is cancelled and a $0.25 no-show fine is added.
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                onClick={() => setBookToReserve(null)}
                className="rounded-xl border border-slate-300 px-4 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={submitReservation}
                className="rounded-xl bg-indigo-600 px-5 py-2 font-semibold text-white transition hover:bg-indigo-700"
              >
                Confirm reservation
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

export default BookList;
