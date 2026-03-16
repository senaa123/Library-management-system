import ReactDOM from "react-dom/client";
import App from "./App";
import "./index.css";
import { BrowserRouter } from "react-router-dom";

// Entry point of the React application.
// It mounts the App component into the "root" div in index.html.
ReactDOM.createRoot(document.getElementById("root")!).render(
  // The QR scanner talks to a real device camera, so we avoid StrictMode's
  // development-only double mount/unmount cycle here.
  <BrowserRouter>
    <App />
  </BrowserRouter>
);
