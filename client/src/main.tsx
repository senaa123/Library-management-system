import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import "./index.css";
import { BrowserRouter } from "react-router-dom";

// Entry point of the React application.
// It mounts the App component into the "root" div in index.html.
ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    
    {/* Enables navigation between pages without reloading the browser */}
    <BrowserRouter>
      <App />
    </BrowserRouter>

  </React.StrictMode>
);
