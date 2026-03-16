import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../services/axiosConfig";
import { getStoredRole, isLoggedIn, isStaffRole } from "../lib/session";

function EditBook() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("");
  const [quantity, setQuantity] = useState(1);

  useEffect(() => {
    if (!isLoggedIn()) {
      navigate("/login");
      return;
    }

    if (!isStaffRole(getStoredRole())) {
      navigate("/");
      return;
    }

    api.get(`/Books/${id}`)
      .then((response) => {
        setTitle(response.data.title);
        setAuthor(response.data.author);
        setDescription(response.data.description);
        setCategory(response.data.category);
        setQuantity(response.data.totalCopies);
      })
      .catch((error) => {
        alert(error.response?.data?.message ?? "Failed to load book details.");
        navigate("/");
      });
  }, [id, navigate]);

  const handleUpdate = (event: React.FormEvent) => {
    event.preventDefault();

    const updatedBook = {
      id,
      title,
      author,
      description,
      category,
      totalCopies: quantity,
    };

    api.put(`/Books/${id}`, updatedBook)
      .then(() => {
        alert("Book updated successfully!");
        navigate("/");
      })
      .catch((error) => alert(error.response?.data?.message ?? "Failed to update book!"));
  };

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-2xl overflow-hidden rounded-3xl border border-white/50 bg-white/85 shadow-2xl backdrop-blur-xl">
        <div className="bg-gradient-to-r from-slate-900 to-indigo-900 p-6">
          <p className="text-sm uppercase tracking-[0.3em] text-blue-200">Admin Section</p>
          <h2 className="mt-2 text-3xl font-bold text-white">Edit Book</h2>
        </div>

        <div className="p-8">
          <form onSubmit={handleUpdate} className="space-y-5">
            <input
              type="text"
              placeholder="Book Title"
              required
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />

            <input
              type="text"
              placeholder="Author"
              required
              value={author}
              onChange={(event) => setAuthor(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />

            <textarea
              placeholder="Description"
              required
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              className="h-32 w-full rounded-2xl border border-slate-200 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />

            <select
              required
              value={category}
              onChange={(event) => setCategory(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value="">Select Category</option>
              <option value="Fiction">Fiction</option>
              <option value="Non-Fiction">Non-Fiction</option>
              <option value="Science">Science</option>
              <option value="Biography">Biography</option>
              <option value="History">History</option>
              <option value="Other">Other</option>
            </select>

            <div>
              <label className="mb-2 block text-sm font-semibold text-slate-700">
                Quantity (total copies for this title)
              </label>
              <input
                type="number"
                min={1}
                value={quantity}
                onChange={(event) => setQuantity(Number(event.target.value))}
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>

            <button
              type="submit"
              className="w-full rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 py-3 font-semibold text-white shadow-lg transition hover:shadow-xl"
            >
              Update Book
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

export default EditBook;
