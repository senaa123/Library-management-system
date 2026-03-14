import { useState, useEffect } from "react";
import api from "../services/axiosConfig";
import { useNavigate } from "react-router-dom";

function AddBook() {
  const navigate = useNavigate();

  // Redirect user to login if they are not authenticated
  useEffect(() => {
    if (!localStorage.getItem("token")) {
      alert("Please login to add books!");
      navigate("/login");
    }
  }, [navigate]);

  // Form input state variables
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("");

  // Handle form submission to add a new book
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newBook = { title, author, description, category };

    api.post("/Books", newBook)
      .then(() => {
        alert("Book added successfully!");
        navigate("/");
      })
      .catch(() => alert("Failed to add book. (Check login status)"));
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50 flex items-center justify-center p-4">
      
      {/* Card Container */}
      <div className="backdrop-blur-xl bg-white/80 border border-white/50 shadow-2xl rounded-3xl overflow-hidden w-full max-w-lg">
        
        {/* Header */}
        <div className="bg-gradient-to-r from-slate-900 to-indigo-900 p-6 flex items-center gap-3">
          <h2 className="text-2xl font-bold text-white tracking-wide">Add New Book</h2>
        </div>

        {/* Form */}
        <div className="p-8">
          <form onSubmit={handleSubmit} className="space-y-5">

            <input
              type="text"
              placeholder="Book Title"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-4 py-3 bg-white/90 border border-slate-200 rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder:text-slate-400"
            />

            <input
              type="text"
              placeholder="Author"
              required
              value={author}
              onChange={(e) => setAuthor(e.target.value)}
              className="w-full px-4 py-3 bg-white/90 border border-slate-200 rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder:text-slate-400"
            />

            <textarea
              placeholder="Description"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-4 py-3 bg-white/90 border border-slate-200 rounded-xl shadow-sm h-32 focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder:text-slate-400"
            />

            <select
              required
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className="w-full px-4 py-3 bg-white/90 border border-slate-200 rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-700"
            >
              <option value="">Select Category</option>
              <option value="Fiction">Fiction</option>
              <option value="Non-Fiction">Non-Fiction</option>
              <option value="Science">Science</option>
              <option value="Biography">Biography</option>
              <option value="History">History</option>
              <option value="Other">Other</option>
            </select>

            <button
              type="submit"
              className="w-full bg-gradient-to-r from-blue-600 to-indigo-600 text-white py-3 rounded-xl font-semibold shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all"
            >
              Add Book
            </button>

          </form>
        </div>

      </div>
    </div>
  );
}

export default AddBook;
